using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAssassinPouncerAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    [Header("Approach")]
    public float engageDistance = 2.0f;
    public float approachStopDistance = 1.6f;
    public float approachSpeed = 10f;
    public float approachRepathInterval = 0.1f;

    [Header("Attack Move Patterns")]
    [Range(0f, 1f)] public float latchPatternProbability = 0.5f;

    [Header("Pattern A: Short Pause then Leave Fast")]
    public Vector2 shortPauseRange = new Vector2(0.2f, 0.5f);
    public float leaveBurstSpeed = 16f;
    public float leaveDistance = 7f;

    [Header("Pattern B: Latch Behind then Leave")]
    public float latchDuration = 1.0f;
    public float behindOffset = 0.9f;
    public float latchFollowLerp = 12f;
    public float latchLeaveSpeed = 12f;
    public float latchLeaveDistance = 8f;

    [Header("Leave Stop & Post-Pause")]
    public float reengageStartDistance = 6.0f;
    public Vector2 postLeavePauseRange = new Vector2(0.25f, 0.5f);

    [Header("On-Damage Reaction (only during Approach)")]
    public bool enableKnockbackOnDamage = true;
    public float knockbackDistance = 3.0f;
    public float knockbackDuration = 0.15f;
    public float stunDuration = 0.6f;

    [Header("Rotation")]
    public float turnSpeed = 900f;

    [Header("Events (plug attacks here)")]
    public UnityEvent onAttackWindowBegin;
    public UnityEvent onAttackWindowEnd;

    [Header("Damage Aggro (global tracker)")]
    public bool enableDamageAggro = true;
    public float damageAggroTimeout = 4f;
    float aggroTimer;

    // Internals
    NavMeshAgent agent;
    enum State { Approach, ShortPause, LatchBehind, LeaveBurst, Knockback, Stunned }
    State state;

    Health hp;           int lastHP = -1;
    FourHitHealth four;  int lastSeg = -1;
    float approachRepathTimer;

    // NEW: invulnerability helper (optional)
    InvulnerabilityToggle invuln;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.speed = approachSpeed;
        agent.acceleration = Mathf.Max(agent.acceleration, approachSpeed * 2f);
        agent.autoBraking = false;
        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);

        invuln = GetComponent<InvulnerabilityToggle>(); // may be null if not added
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
                    approachRepathTimer -= Time.deltaTime;
                    if (approachRepathTimer <= 0f)
                    {
                        agent.isStopped = false;
                        agent.speed = approachSpeed;
                        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);
                        agent.SetDestination(player.position);
                        approachRepathTimer = approachRepathInterval;
                    }

                    Vector3 vel = agent.desiredVelocity; vel.y = 0f;
                    if (vel.sqrMagnitude > 0.001f)
                    {
                        Quaternion want = Quaternion.LookRotation(vel.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                    }

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
                    break;
            }

            yield return null;
        }
    }

    // -------- Pattern A: Short pause -> leave -> post-pause -> re-approach --------
    IEnumerator DoShortPauseThenLeave()
    {
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

        state = State.LeaveBurst;
        yield return StartCoroutine(DoLeaveBurst(leaveBurstSpeed, leaveDistance));

        yield return StartCoroutine(DoPostLeavePause());

        state = State.Approach;
        agent.isStopped = false;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }

    // -------- Pattern B: Latch (INVULNERABLE) -> leave -> post-pause -> re-approach --------
    IEnumerator DoLatchBehindThenLeave()
    {
        // OPEN invulnerability while latched
        invuln?.EnableInvulnerability();

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

        // CLOSE invulnerability before leaving
        invuln?.DisableInvulnerability();

        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;

        state = State.LeaveBurst;
        yield return StartCoroutine(DoLeaveBurst(latchLeaveSpeed, latchLeaveDistance));

        yield return StartCoroutine(DoPostLeavePause());

        state = State.Approach;
        agent.isStopped = false;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }

    // -------- Straight-line leave (random dir), with early stop by distance --------
    IEnumerator DoLeaveBurst(float speed, float maxDistance)
    {
        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

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
            if (player)
            {
                float pd = Vector3.Distance(transform.position, player.position);
                if (pd >= reengageStartDistance) break;
            }

            float step = (speed * Time.deltaTime) / Mathf.Max(0.001f, maxDistance);
            traveled += step;
            Vector3 pos = Vector3.Lerp(start, end, Mathf.Clamp01(traveled));
            transform.position = pos;

            Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);

            yield return null;
        }

        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;

        yield return null;
    }

    IEnumerator DoPostLeavePause()
    {
        float t = 0f;
        float wait = Random.Range(postLeavePauseRange.x, postLeavePauseRange.y);

        bool prevStopped = agent.isStopped;
        agent.isStopped = true;
        agent.ResetPath();

        while (t < wait)
        {
            t += Time.deltaTime;
            FacePlayer();
            yield return null;
        }

        agent.isStopped = prevStopped;
    }

    // -------- On-damage reaction while approaching: Knockback -> Stun --------
    IEnumerator DoKnockbackThenStun()
    {
        if (!player) { state = State.Approach; yield break; }

        // Safety: ensure invulnerability is OFF if interrupted during latch
        invuln?.DisableInvulnerability();

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

        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = true;

        state = State.Stunned;
        yield return new WaitForSeconds(stunDuration);

        agent.isStopped = prevStopped;
        state = State.Approach;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }

    // -------- Helpers --------
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
