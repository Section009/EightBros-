using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Xiao Bian (3rd enemy, no self-heal & no dodge version)
/// - Shoots a Mark projectile at range (applies StatusReceiver.Mark()).
/// - On close engage, randomly choose one pattern:
///     A) Short pause ("lick") then leave fast (optional small damage).
///     B) Latch behind player for a short duration, consume Mark
///        (StatusReceiver.ConsumeMarkAndLock), deal heavy damage, then leave fast.
/// - Player cannot be marked again for X seconds after consumption (handled by StatusReceiver).
/// - Only one Xiao Bian can be latched at any time (global token).
/// - Optional invulnerability during latch by using your existing InvulnerabilityToggle.
/// - Supports both Health (numeric) and FourHitHealth (segmented).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAssassinPouncerAI : MonoBehaviour
{
    // ---------------- Target ----------------
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    // ---------------- Range shooting (Mark) ----------------
    [Header("Mark Shooting (Licky Licky)")]
    [Tooltip("Start shooting if player within this range AND line-of-sight.")]
    public float markShootRange = 18f;
    [Tooltip("Seconds between two shots.")]
    public Vector2 shootCooldownRange = new Vector2(1.2f, 1.8f);
    [Tooltip("Projectile prefab; if null a builtin simple mark projectile is used.")]
    public GameObject markProjectilePrefab;
    [Tooltip("Projectile speed.")]
    public float projectileSpeed = 22f;
    [Tooltip("Projectile lifetime (sec).")]
    public float projectileLife = 5f;
    [Tooltip("Fire slight random yaw/pitch (deg).")]
    public float fireSpreadDeg = 2f;
    [Tooltip("Where to spawn the projectile; default = self transform.")]
    public Transform shootMuzzle;

    float _shootCooldown;

    // ---------------- Approach / engage ----------------
    [Header("Approach (agent-based)")]
    public float engageDistance = 2.0f;         // trigger attack pattern threshold (very close)
    public float approachStopDistance = 1.6f;   // nav stopping distance while chasing
    public float approachSpeed = 12f;
    public float approachRepathInterval = 0.1f;

    [Header("Vision/LOS used for shooting")]
    public bool requireLineOfSightForShoot = true;
    public LayerMask losBlockMask = ~0;

    // ---------------- Attack move patterns ----------------
    [Header("Attack Move Patterns (choose at engage)")]
    [Range(0f, 1f)] public float latchPatternProbability = 0.5f; // Pattern B probability

    [Header("Pattern A: Short Pause then Leave Fast")]
    public Vector2 shortPauseRange = new Vector2(0.2f, 0.5f);
    public int shortPauseDamage = 8;  // optional small tap damage
    public float leaveBurstSpeed = 18f;
    public float leaveDistance = 9f;

    [Header("Pattern B: Latch Behind then Leave")]
    public float latchDuration = 1.0f;
    public float behindOffset = 0.9f;
    public float latchFollowLerp = 12f;
    public float latchLeaveSpeed = 16f;
    public float latchLeaveDistance = 10f;

    [Header("Latch Damage / Mark rules")]
    [Tooltip("Heavy damage dealt when consuming player's mark during latch.")]
    public int latchHeavyDamage = 30;
    [Tooltip("Seconds after consuming mark during which player cannot be marked again.")]
    public float playerReMarkLockout = 3f;
    [Tooltip("If true, the enemy is invulnerable during latch (uses your existing InvulnerabilityToggle).")]
    public bool invulnerableWhileLatched = true;

    [Header("Leave Stop & Post-Pause")]
    public float reengageStartDistance = 6.0f;
    public Vector2 postLeavePauseRange = new Vector2(0.25f, 0.5f);

    [Header("Rotation")]
    public float turnSpeed = 900f;

    // ---------------- Damage Aggro ----------------
    [Header("Damage Aggro")]
    public bool enableDamageAggro = true;
    public float damageAggroTimeout = 4f;
    float aggroTimer;

    // ---------------- Events for VFX/SFX or extra logic ----------------
    [Header("Events (plug attacks/VFX here)")]
    public UnityEvent onAttackWindowBegin;
    public UnityEvent onAttackWindowEnd;
    public UnityEvent onShootMark;

    // ---------------- Internals / States ----------------
    NavMeshAgent agent;
    enum State { Approach, ShortPause, LatchBehind, LeaveBurst }
    State state;

    Health hp;           int lastHP = -1;
    FourHitHealth four;  int lastSeg = -1;
    float approachRepathTimer;

    // Use your existing InvulnerabilityToggle (from your Health folder)
    InvulnerabilityToggle invuln; // may be null

    // ---- Global latch token (only one xiao bian can latch at a time) ----
    static EnemyAssassinPouncerAI _globalLatchOwner;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.speed = approachSpeed;
        agent.acceleration = Mathf.Max(agent.acceleration, approachSpeed * 2f);
        agent.autoBraking = false;
        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);

        if (losBlockMask.value == 0)
        {
            losBlockMask = ~0;
            losBlockMask &= ~(1 << gameObject.layer);
        }

        invuln = GetComponent<InvulnerabilityToggle>();

        _shootCooldown = Random.Range(shootCooldownRange.x, shootCooldownRange.y);
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

    // =========================== FSM ===========================
    IEnumerator FSM()
    {
        while (true)
        {
            if (!player) { yield return null; continue; }

            // damage aggro memory
            if (enableDamageAggro)
            {
                if (WasDamagedThisFrame()) aggroTimer = damageAggroTimeout;
                if (aggroTimer > 0f) aggroTimer -= Time.deltaTime;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            // allow shooting outside of the close-quarters sub-states
            if (state == State.Approach || state == State.LeaveBurst)
                TryShootMark(dist);

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

                    // face desired velocity
                    Vector3 vel = agent.desiredVelocity; vel.y = 0f;
                    if (vel.sqrMagnitude > 0.001f)
                    {
                        Quaternion want = Quaternion.LookRotation(vel.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                    }

                    if (dist <= engageDistance)
                    {
                        onAttackWindowBegin?.Invoke();
                        bool canLatch = (_globalLatchOwner == null);
                        bool chooseLatch = (Random.value < latchPatternProbability) && canLatch;

                        if (chooseLatch)
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
                    break;
                }

                case State.ShortPause:
                case State.LatchBehind:
                case State.LeaveBurst:
                    // handled by coroutines
                    break;
            }

            yield return null;
        }
    }

    // ====================== Mark Shooting ======================
    void TryShootMark(float distanceToPlayer)
    {
        _shootCooldown -= Time.deltaTime;
        if (_shootCooldown > 0f) return;
        if (distanceToPlayer > markShootRange) return;

        if (requireLineOfSightForShoot && !HasLOSToPlayer())
        {
            _shootCooldown = 0.2f;
            return;
        }

        var sr = FindPlayerStatus();
        if (sr != null && sr.InLockout())
        {
            _shootCooldown = 0.2f;
            return;
        }

        FireMarkProjectile();
        onShootMark?.Invoke();
        _shootCooldown = Random.Range(shootCooldownRange.x, shootCooldownRange.y);
    }

    bool HasLOSToPlayer()
    {
        if (!player) return false;
        Vector3 origin = (shootMuzzle ? shootMuzzle.position : transform.position) + Vector3.up * 1.1f;
        Vector3 target = player.position + Vector3.up * 1.0f;

        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist <= 1e-4f) return true;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, losBlockMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag(playerTag) || hit.collider.transform.IsChildOf(player))
                return true;
            return false;
        }
        return true;
    }

    void FireMarkProjectile()
    {
        if (!player) return;

        Transform muzzle = shootMuzzle ? shootMuzzle : transform;
        Vector3 pos = muzzle.position;
        Vector3 dir = (player.position + Vector3.up * 1.0f) - pos;
        if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;
        dir.Normalize();

        float yaw = Random.Range(-fireSpreadDeg, fireSpreadDeg);
        float pitch = Random.Range(-fireSpreadDeg, fireSpreadDeg);
        dir = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right) * dir;

        GameObject go = markProjectilePrefab
            ? Instantiate(markProjectilePrefab, pos, Quaternion.LookRotation(dir, Vector3.up))
            : CreateBuiltinMarkProjectile(pos, dir);

        var proj = go.GetComponent<SimpleMarkProjectile>();
        if (!proj) proj = go.AddComponent<SimpleMarkProjectile>();
        proj.Init(playerTag, projectileSpeed, projectileLife);
    }

    GameObject CreateBuiltinMarkProjectile(Vector3 pos, Vector3 dir)
    {
        var go = new GameObject("(auto) MarkProjectile");
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir, Vector3.up));
        var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.2f;
        var rb  = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
        go.AddComponent<SimpleMarkProjectile>();
        return go;
    }

    StatusReceiver FindPlayerStatus()
    {
        if (!player) return null;
        return player.GetComponentInChildren<StatusReceiver>();
    }

    // ================= Pattern A: short pause -> small hit -> leave =================
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

        // optional tiny damage tap to represent "lick"
        DealSmallTapDamage();

        onAttackWindowEnd?.Invoke();

        state = State.LeaveBurst;
        yield return StartCoroutine(DoLeaveBurst(leaveBurstSpeed, leaveDistance));
        yield return StartCoroutine(DoPostLeavePause());

        state = State.Approach;
        agent.isStopped = false;
        agent.speed = approachSpeed;
        if (player) agent.SetDestination(player.position);
    }

    void DealSmallTapDamage()
    {
        if (!player) return;

        var h = player.GetComponentInParent<Health>();
        if (h) { if (shortPauseDamage > 0) h.TakeDamage(shortPauseDamage); }
        else
        {
            var fourP = player.GetComponentInParent<FourHitHealth>();
            if (fourP) fourP.RegisterHit();
        }
    }

    // ================= Pattern B: latch (consume mark) -> leave =================
    IEnumerator DoLatchBehindThenLeave()
    {
        // Acquire global latch token
        _globalLatchOwner = this;

        // Enable invulnerability during latch if component present
        if (invulnerableWhileLatched && invuln != null) invuln.EnableInvulnerability();

        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        float t = 0f;
        
        // Consume mark once at latch begin (if present)
        TryConsumePlayerMarkOnce();

        while (t < latchDuration && player)
        {
            t += Time.deltaTime;

            // stick behind player
            Vector3 target = player.position - player.forward * behindOffset;
            target.y = transform.position.y;

            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-latchFollowLerp * Time.deltaTime));

            Quaternion want = Quaternion.LookRotation(player.forward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);

            yield return null;
        }

        onAttackWindowEnd?.Invoke();

        // Disable invulnerability and release latch token
        if (invulnerableWhileLatched && invuln != null) invuln.DisableInvulnerability();
        if (_globalLatchOwner == this) _globalLatchOwner = null;

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

    /// <summary>
    /// Consume player's mark via StatusReceiver:
    /// - If marked: heavy damage to player, then lock mark for playerReMarkLockout sec.
    /// - If not marked: optional small tap damage (keeps pressure).
    /// </summary>
    void TryConsumePlayerMarkOnce()
    {
        var sr = FindPlayerStatus();
        if (sr != null && sr.IsMarked())
        {
            // heavy damage to player
            var h = player.GetComponentInParent<Health>();
            if (h) { if (latchHeavyDamage > 0) h.TakeDamage(latchHeavyDamage); }
            else
            {
                var fourP = player.GetComponentInParent<FourHitHealth>();
                if (fourP) fourP.RegisterHit();
            }

            // clear mark + lockout
            sr.ConsumeMarkAndLock(playerReMarkLockout);
        }
        else
        {
            // optional fallback damage when no mark available
            if (shortPauseDamage > 0)
            {
                var h2 = player.GetComponentInParent<Health>();
                if (h2) h2.TakeDamage(shortPauseDamage);
                else
                {
                    var fourP2 = player.GetComponentInParent<FourHitHealth>();
                    if (fourP2) fourP2.RegisterHit();
                }
            }
        }
    }

    // ================= Leave burst movement =================
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

    // ================= Helpers =================
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
        
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, markShootRange);
    }
}

#region --- Minimal helper kept in this file ---

/// <summary>
/// Minimal mark projectile:
/// - straight move
/// - on hit Player: StatusReceiver.Mark()
/// - destroy self
/// </summary>
public class SimpleMarkProjectile : MonoBehaviour
{
    string _targetTag;
    float _speed;
    float _life;
    Vector3 _dir;

    public void Init(string targetTag, float speed, float life)
    {
        _targetTag = targetTag;
        _speed = Mathf.Max(0f, speed);
        _life = Mathf.Max(0.01f, life);
        _dir = transform.forward;
        StartCoroutine(Co_Life());
    }

    IEnumerator Co_Life()
    {
        float t = 0f;
        while (t < _life)
        {
            transform.position += _dir * _speed * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag)) return;

        var sr = other.GetComponentInParent<StatusReceiver>();
        if (sr != null) sr.Mark();

        Destroy(gameObject);
    }
}
#endregion