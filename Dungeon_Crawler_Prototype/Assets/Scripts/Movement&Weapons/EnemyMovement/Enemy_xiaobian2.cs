using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Assassin-style pouncer with post-leave pause:
/// - Globally tracks the player (no vision needed).
/// - Approaches fast using NavMesh; stops at approachStopDistance (no overlapping).
/// - On close, chooses ONE pattern:
///    (A) Short pause at close range -> burst-leave (random dir) -> small pause -> re-approach
///    (B) Latch behind for a while   -> burst-leave (slightly slower) -> small pause -> re-approach
/// - While APPROACHING: taking damage causes knockback + stun, then resume.
/// - This script controls locomotion windows only; hook attacks via events.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAssassinPouncerAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    [Header("Approach")]
    [Tooltip("Enter the attack window when within this distance from the player.")]
    public float engageDistance = 2.0f;

    [Tooltip("Agent stops approaching once within this distance (keeps a buffer; should be <= engageDistance).")]
    public float approachStopDistance = 1.6f;

    [Tooltip("NavMeshAgent chase speed during approach.")]
    public float approachSpeed = 10f;

    [Tooltip("How often to refresh SetDestination while approaching.")]
    public float approachRepathInterval = 0.1f;

    [Header("Attack Move Patterns")]
    [Tooltip("Probability of choosing pattern B (Latch then leave). 0..1")]
    [Range(0f, 1f)] public float latchPatternProbability = 0.5f;

    // Pattern A: Short pause then leave-fast
    [Header("Pattern A: Short Pause then Leave Fast")]
    public Vector2 shortPauseRange = new Vector2(0.2f, 0.5f);
    public float leaveBurstSpeed = 16f;      // very fast leave speed
    public float leaveDistance = 7f;         // max burst travel if not early-stopped

    // Pattern B: Latch behind then leave (slightly slower but still fast)
    [Header("Pattern B: Latch Behind then Leave")]
    public float latchDuration = 1.0f;       // time to stick behind player
    public float behindOffset = 0.9f;        // distance behind player's back
    public float latchFollowLerp = 12f;      // how tightly we follow behind
    public float latchLeaveSpeed = 12f;      // leaving speed (still fast but < leaveBurstSpeed)
    public float latchLeaveDistance = 8f;    // max burst travel if not early-stopped

    [Header("Leave Stop & Post-Pause")]
    [Tooltip("End the leave-burst early once distance to player >= this value, then pause.")]
    public float reengageStartDistance = 6.0f;

    [Tooltip("Random small pause after leaving, before re-approach.")]
    public Vector2 postLeavePauseRange = new Vector2(0.25f, 0.5f);

    [Header("On-Damage Reaction (only during Approach)")]
    public bool enableKnockbackOnDamage = true;
    public float knockbackDistance = 3.0f;
    public float knockbackDuration = 0.15f;
    public float stunDuration = 0.6f;

    [Header("Rotation")]
    public float turnSpeed = 900f;

    [Header("Events (plug attacks here)")]
    [Tooltip("Raised when entering the close-range attack window (before pause/latch).")]
    public UnityEvent onAttackWindowBegin;
    [Tooltip("Raised when leaving the attack window (after pause/latch, right before burst-leave).")]
    public UnityEvent onAttackWindowEnd;

    [Header("Damage Aggro (global tracker)")]
    [Tooltip("Enable aggro extension when taking damage; not strictly necessary because we already globally chase.")]
    public bool enableDamageAggro = true;
    public float damageAggroTimeout = 4f;
    float aggroTimer;

    // Internals
    NavMeshAgent agent;
    enum State { Approach, ShortPause, LatchBehind, LeaveBurst, Knockback, Stunned }
    State state;

    // Damage polling
    Health hp;           int lastHP = -1;
    FourHitHealth four;  int lastSeg = -1;

    float approachRepathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.speed = approachSpeed;
        agent.acceleration = Mathf.Max(agent.acceleration, approachSpeed * 2f);
        agent.autoBraking = false;

        // ensure stop distance applies (we keep some buffer, do not overlap player)
        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        hp   = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();
        if (hp)   lastHP  = hp.currentHealth;
        if (four) lastSeg = four.GetState().current;

        state = State.Approach;
        if (player) agent.SetDestination(player.position);
        StartCoroutine(FSM());
    }

    IEnumerator FSM()
    {
        while (true)
        {
            if (!player) { yield return null; continue; }

            // Damage-aggro refresh (not required to start chase, but fine to keep)
            if (enableDamageAggro)
            {
                if (WasDamagedThisFrame()) aggroTimer = damageAggroTimeout;
                if (aggroTimer > 0f) aggroTimer -= Time.deltaTime;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            switch (state)
            {
                case State.Approach:
                {
                    // Periodically refresh navigation toward player
                    approachRepathTimer -= Time.deltaTime;
                    if (approachRepathTimer <= 0f)
                    {
                        agent.isStopped = false;
                        agent.speed = approachSpeed;
                        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);
                        agent.SetDestination(player.position);
                        approachRepathTimer = approachRepathInterval;
                    }

                    // Face in the moving direction
                    Vector3 vel = agent.desiredVelocity; vel.y = 0f;
                    if (vel.sqrMagnitude > 0.001f)
                    {
                        Quaternion want = Quaternion.LookRotation(vel.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                    }

                    // Close enough to start the attack window (but we already stop early at approachStopDistance)
                    if (dist <= engageDistance)
                    {
                        onAttackWindowBegin?.Invoke();

                        if (Random.value < latchPatternProbability)
                        {
                            state = State.LatchBehind;
                            StartCoroutine(DoLatchBehindThenLeave());
                        }
                        else
                        {
                            state = State.ShortPause;
                            StartCoroutine(DoShortPauseThenLeave());
                        }
                    }
                    else
                    {
                        // Only during APPROACH: if damaged, do knockback + stun
                        if (enableKnockbackOnDamage && WasDamagedThisFrame())
                        {
                            state = State.Knockback;
                            StartCoroutine(DoKnockbackThenStun());
                        }
                    }
                    break;
                }

                case State.ShortPause:
                case State.LatchBehind:
                case State.LeaveBurst:
                case State.Knockback:
                case State.Stunned:
                    // handled in coroutines
                    break;
            }

            yield return null;
        }
    }

    // ------------------------- Pattern A -------------------------
    IEnumerator DoShortPauseThenLeave()
    {
        // Halt on spot and face the player briefly
        agent.isStopped = true;
        agent.ResetPath();

        float pause = Random.Range(shortPauseRange.x, shortPauseRange.y);
        float t = 0f;
        while (t < pause)
        {
            t += Time.deltaTime;
            FacePlayer();
            yield return null;
        }

        onAttackWindowEnd?.Invoke();

        // Burst-leave (random direction) until far enough, then do a small post-leave pause
        state = State.LeaveBurst;
        yield return StartCoroutine(DoLeaveBurst(leaveBurstSpeed, leaveDistance));

        // Post-leave small pause
        yield return StartCoroutine(DoPostLeavePause());

        // Re-approach
        state = State.Approach;
        agent.isStopped = false;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }

    // ------------------------- Pattern B -------------------------
    IEnumerator DoLatchBehindThenLeave()
    {
        // Temporarily take over movement to stick behind the player
        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        float t = 0f;
        while (t < latchDuration && player)
        {
            t += Time.deltaTime;

            Vector3 target = player.position - player.forward * behindOffset;
            target.y = transform.position.y;

            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-latchFollowLerp * Time.deltaTime));

            Quaternion want = Quaternion.LookRotation(player.forward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);

            yield return null;
        }

        onAttackWindowEnd?.Invoke();

        // Restore agent control for the burst-leave
        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;

        state = State.LeaveBurst;
        yield return StartCoroutine(DoLeaveBurst(latchLeaveSpeed, latchLeaveDistance));

        // Post-leave small pause
        yield return StartCoroutine(DoPostLeavePause());

        // Resume approach
        state = State.Approach;
        agent.isStopped = false;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }

    // ------------------------- Leave (shared) -------------------------
    IEnumerator DoLeaveBurst(float speed, float maxDistance)
    {
        // Disable agent for a straight-line burst
        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Random horizontal direction
        Vector2 rnd = Random.insideUnitCircle.normalized;
        Vector3 dir = new Vector3(rnd.x, 0f, rnd.y);
        if (dir.sqrMagnitude < 0.001f) dir = transform.right;

        Vector3 start = transform.position;
        Vector3 endPlanar = start + dir * maxDistance;
        Vector3 end = SampleOnNavmesh(endPlanar, 0.8f);
        if (!IsFinite(end)) end = start + dir * (maxDistance * 0.6f);

        float traveled = 0f;
        while (traveled < 1f)
        {
            // Early stop once we are far enough from the player
            if (player)
            {
                float pd = Vector3.Distance(transform.position, player.position);
                if (pd >= reengageStartDistance)
                    break;
            }

            float step = (speed * Time.deltaTime) / Mathf.Max(0.001f, maxDistance);
            traveled += step;
            Vector3 pos = Vector3.Lerp(start, end, Mathf.Clamp01(traveled));
            transform.position = pos;

            Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);

            yield return null;
        }

        // Sync agent back
        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;

        yield return null;
    }

    IEnumerator DoPostLeavePause()
    {
        // Small pause after leaving before the next approach
        float t = 0f;
        float wait = Random.Range(postLeavePauseRange.x, postLeavePauseRange.y);

        // Stop and (optionally) face the player
        bool prevStopped = agent.isStopped;
        agent.isStopped = true;
        agent.ResetPath();

        while (t < wait)
        {
            t += Time.deltaTime;
            FacePlayer(); // looks nicer; remove if undesired
            yield return null;
        }

        agent.isStopped = prevStopped;
    }

    // ------------------------- On-Damage Reaction -------------------------
    IEnumerator DoKnockbackThenStun()
    {
        if (!player) { state = State.Approach; yield break; }

        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        Vector3 away = (transform.position - player.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f) away = -transform.forward;
        away.Normalize();

        Vector3 start = transform.position;
        Vector3 endPlanar = start + away * knockbackDistance;
        Vector3 end = SampleOnNavmesh(endPlanar, 0.8f);
        if (!IsFinite(end)) end = start + away * (knockbackDistance * 0.6f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, knockbackDuration);
            Vector3 pos = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            transform.position = pos;

            FacePlayer();
            yield return null;
        }

        // Sync & enter stunned
        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = true;

        state = State.Stunned;
        yield return new WaitForSeconds(stunDuration);

        // Recover
        agent.isStopped = prevStopped;
        state = State.Approach;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }


    void FacePlayer()
    {
        if (!player) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }
    }

    Vector3 SampleOnNavmesh(Vector3 pos, float maxDist)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
            return hit.position;
        return new Vector3(pos.x, float.PositiveInfinity, pos.z);
    }

    bool WasDamagedThisFrame()
    {
        bool hit = false;

        if (hp != null)
        {
            if (lastHP < 0) lastHP = hp.currentHealth;
            if (hp.currentHealth < lastHP) hit = true;
            lastHP = hp.currentHealth;
        }

        if (four != null)
        {
            int cur = four.GetState().current;
            if (lastSeg < 0) lastSeg = cur;
            if (cur < lastSeg) hit = true;
            lastSeg = cur;
        }

        return hit;
    }

    bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, approachStopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, reengageStartDistance);
    }
}
