using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// EnemyAssassinPouncerAI  (a.k.a. "Xiao Bian")
/// - Ranged: fires a "mark" projectile (applies StatusReceiver.Mark()) when player is in range & LOS.
/// - Melee engage: upon getting close enough, randomly chooses:
///     A) Short pause near the player, optionally deal small tap damage, then leave quickly.
///     B) Latch behind the player for a short duration; on latch start, if player is marked:
///        deal heavy damage and call StatusReceiver.ConsumeMarkAndLock(). Then leave quickly.
/// - Safe latch: each frame finds a safe behind-spot using wall rayback + NavMesh projection + side fallbacks.
///   If no safe spot is found for several frames, abort latch early to avoid getting stuck in walls.
/// - Leave burst: manual, collision-aware movement with CapsuleCast and NavMesh checks:
///     * moves away from the player with small randomness;
///     * if would hit a solid (non-player/non-enemy), stops immediately at the contact;
///     * if a step would leave the NavMesh, tries sliding along the wall; if still invalid, aborts.
/// - Global latch token: only one instance may be latched at a time.
/// - Optional invulnerability during latch if an InvulnerabilityToggle component exists on this enemy.
/// - Damage aggro: external scripts may call NotifyDamaged() to extend the chase timer.
/// - Supports both Health (numeric HP) and FourHitHealth (4-segment HP).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAssassinPouncerAI : MonoBehaviour
{
    // ---------------- Target ----------------
    [Header("Target")]
    public string playerTag = "Player";
    private Transform player;

    // ---------------- Ranged mark shooting ----------------
    [Header("Mark Shooting")]
    [Tooltip("Start shooting if player is within this range AND (optionally) in line of sight.")]
    public float markShootRange = 18f;

    [Tooltip("Seconds between two shots (random in range).")]
    public Vector2 shootCooldownRange = new Vector2(1.2f, 1.8f);

    [Tooltip("Projectile prefab; if null a minimal builtin mark projectile will be created.")]
    public GameObject markProjectilePrefab;

    [Tooltip("Projectile initial speed.")]
    public float projectileSpeed = 22f;

    [Tooltip("Projectile lifetime in seconds.")]
    public float projectileLife = 5f;

    [Tooltip("Random yaw/pitch spread in degrees when firing.")]
    public float fireSpreadDeg = 2f;

    [Tooltip("Optional muzzle transform for spawning the projectile; defaults to this.transform.")]
    public Transform shootMuzzle;

    private float _shootCooldown;

    // ---------------- Approach (NavMeshAgent) ----------------
    [Header("Approach (Agent)")]
    [Tooltip("Distance threshold for switching from chase to engage (attack patterns).")]
    public float engageDistance = 2.0f;

    [Tooltip("Agent stopping distance while approaching.")]
    public float approachStopDistance = 1.6f;

    [Tooltip("Agent move speed while approaching.")]
    public float approachSpeed = 12f;

    [Tooltip("How often we repath while approaching (sec).")]
    public float approachRepathInterval = 0.1f;

    [Header("Vision/LOS for Shooting")]
    [Tooltip("If true, requires a clear line of sight to fire.")]
    public bool requireLineOfSightForShoot = true;

    [Tooltip("Layers that block LOS tests. Typically include walls/geometry but exclude the enemy itself.")]
    public LayerMask losBlockMask = ~0;

    // ---------------- Patterns ----------------
    [Header("Attack Pattern Selection")]
    [Range(0f, 1f)]
    [Tooltip("Probability to choose the Latch pattern when engaging (Pattern B).")]
    public float latchPatternProbability = 0.5f;

    [Header("Pattern A: Short Pause then Leave Fast")]
    [Tooltip("Pause duration range near the player before leaving.")]
    public Vector2 shortPauseRange = new Vector2(0.2f, 0.5f);

    [Tooltip("Optional small tap damage dealt at the end of the short pause.")]
    public int shortPauseDamage = 8;

    [Tooltip("Manual leave speed after the short pause.")]
    public float leaveBurstSpeed = 18f;

    [Tooltip("Nominal leave distance for the burst (manual movement).")]
    public float leaveDistance = 9f;

    [Header("Pattern B: Latch Behind then Leave")]
    [Tooltip("Latch duration (seconds).")]
    public float latchDuration = 1.0f;

    [Tooltip("Desired behind offset from the player's position.")]
    public float behindOffset = 0.9f;

    [Tooltip("Follow lerp factor while latched (higher = snappier).")]
    public float latchFollowLerp = 12f;

    [Tooltip("Manual leave speed after latch.")]
    public float latchLeaveSpeed = 16f;

    [Tooltip("Nominal leave distance after latch.")]
    public float latchLeaveDistance = 10f;

    [Header("Latch Damage / Mark Rules")]
    [Tooltip("Heavy damage dealt when consuming player's mark at latch begin.")]
    public int latchHeavyDamage = 30;

    [Tooltip("Seconds for which the player cannot be marked again after consumption.")]
    public float playerReMarkLockout = 3f;

    [Tooltip("If true and an InvulnerabilityToggle exists, the enemy is invulnerable while latched.")]
    public bool invulnerableWhileLatched = true;

    [Header("Leave & Post Pause")]
    [Tooltip("If this far from the player during leave, stop leaving early and re-engage.")]
    public float reengageStartDistance = 6.0f;

    [Tooltip("Short pause after finishing a leave, before resuming approach.")]
    public Vector2 postLeavePauseRange = new Vector2(0.25f, 0.5f);

    [Header("Rotation")]
    [Tooltip("Turn speed in deg/sec when orienting during approach/latch/leave.")]
    public float turnSpeed = 900f;

    // ---------------- Damage Aggro ----------------
    [Header("Damage Aggro")]
    [Tooltip("If false, NotifyDamaged() has no effect.")]
    public bool enableDamageAggro = true;

    [Tooltip("Aggro memory duration (sec) after taking damage.")]
    public float damageAggroTimeout = 4f;

    private float aggroTimer;

    // ---------------- Events ----------------
    [Header("Events (VFX/SFX hooks)")]
    public UnityEvent onAttackWindowBegin;
    public UnityEvent onAttackWindowEnd;
    public UnityEvent onShootMark;

    // ---------------- Latch Safety (NEW) ----------------
    [Header("Latch Collision Safety")]
    [Tooltip("Physics layers treated as solid walls/level geometry for the backoff raycast.")]
    public LayerMask wallMask = ~0;

    [Tooltip("Radius used to check/snap when finding a safe latch spot.")]
    public float latchProbeRadius = 0.4f;

    [Tooltip("How far to search around a candidate position on NavMesh.")]
    public float latchNavProbe = 1.0f;

    [Tooltip("When ray hits a wall, step this much inside the playable space along the hit normal.")]
    public float latchSafetyBackoff = 0.15f;

    [Tooltip("If behind spot is blocked, try side fallback at this lateral offset.")]
    public float sideFallbackOffset = 0.8f;

    [Tooltip("Max consecutive frames failing to find a safe point before aborting latch.")]
    public int maxSafeFailFrames = 6;

    // ---------------- Manual Leave Burst (NEW) ----------------
    [Header("Leave Burst (manual, collision-aware)")]
    [Tooltip("Physics layers that are considered solid walls/props for leave burst.")]
    public LayerMask solidMask = ~0;  // include walls/props; exclude player/enemy layers

    [Tooltip("Allowed tag to pass through during leave (player).")]
    public string playerTagPass = "Player";

    [Tooltip("Allowed tag to pass through during leave (enemy).")]
    public string enemyTagPass = "Enemy";

    [Tooltip("Character capsule radius used for casts.")]
    public float bodyRadius = 0.35f;

    [Tooltip("Character capsule height used for casts.")]
    public float bodyHeight = 1.8f;

    [Tooltip("Nudge distance away from a hit surface to avoid clipping.")]
    public float surfaceInset = 0.02f;

    [Tooltip("Max time the manual leave can run before aborting (sec).")]
    public float leaveMaxTime = 1.5f;

    [Tooltip("If true, stop immediately when hitting a solid (non-player/non-enemy).")]
    public bool stopWhenHitSolid = true;

    [Tooltip("Probe distance when validating next step on NavMesh.")]
    public float navProbe = 0.8f;

    // ------------------ Animation_Components ------------------
    [Header("Animation Components")]
    public GameObject Model;
    private Animator animator;
    public string IdleName;
    public string MoveName;
    public string LatchName;
    public string RetreatName;
    public string SlashName;

    // ---------------- Internals ----------------
    private NavMeshAgent agent;

    private enum State { Approach, ShortPause, LatchBehind, LeaveBurst }
    private State state;

    private Health hp;           private int lastHP = -1;
    private FourHitHealth four;  private int lastSeg = -1;

    private float approachRepathTimer;

    // Optional component provided by your project (Health folder)
    private InvulnerabilityToggle invuln;

    // Only one latch owner at a time
    private static EnemyAssassinPouncerAI _globalLatchOwner;

    // ---------------- Unity ----------------
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
        animator = Model.GetComponent<Animator>();
        if (animator == null)
        {
            UnityEngine.Debug.LogError("Animator Failed");
        }
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

    // ---------------- FSM ----------------
    IEnumerator FSM()
    {
        while (true)
        {
            if (!player) { yield return null; continue; }

            // damage aggro countdown
            if (enableDamageAggro)
            {
                if (WasDamagedThisFrame()) aggroTimer = damageAggroTimeout;
                if (aggroTimer > 0f) aggroTimer -= Time.deltaTime;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            // try ranged mark shooting outside tight sub-states
            if (state == State.Approach || state == State.LeaveBurst)
                TryShootMark(dist);

            switch (state)
            {
                case State.Approach:
                {
                    animator.Play(MoveName);
                    // basic agent chasing
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

    // ---------------- Shooting ----------------
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

    // ---------------- Pattern A: short pause -> tap -> manual leave ----------------
    IEnumerator DoShortPauseThenLeave()
    {
        animator.Play(SlashName);
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

        // optional small tap damage
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

    // ---------------- Pattern B: latch behind (SAFE) -> manual leave ----------------
    IEnumerator DoLatchBehindThenLeave()
    {
        animator.Play(LatchName);
        _globalLatchOwner = this;

        if (invulnerableWhileLatched && invuln != null) invuln.EnableInvulnerability();

        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;

        agent.isStopped = true;
        agent.updatePosition = false; // manual transform while latched
        agent.updateRotation = false;

        float t = 0f;
        int failFrames = 0;

        // consume the player's mark once at latch begin (if present)
        TryConsumePlayerMarkOnce();

        while (t < latchDuration && player)
        {
            t += Time.deltaTime;

            // compute a safe latch point each frame
            Vector3 safePoint;
            if (FindSafeLatchPoint(out safePoint))
            {
                failFrames = 0;
                transform.position = Vector3.Lerp(transform.position, safePoint, 1f - Mathf.Exp(-latchFollowLerp * Time.deltaTime));

                // face the same direction as player
                Quaternion want = Quaternion.LookRotation(player.forward, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            }
            else
            {
                // failed to find safe position this frame
                failFrames++;
                if (failFrames >= maxSafeFailFrames)
                {
                    // abort latch early to avoid getting stuck
                    break;
                }
            }

            yield return null;
        }

        onAttackWindowEnd?.Invoke();

        // restore toggles / tokens
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
    /// Try to find a safe point near "player back" for latch:
    /// 1) Desired = player.pos - player.forward * behindOffset
    /// 2) Raycast from player towards -forward to see if wall blocks, then place slightly inside (hit.point + hit.normal * backoff)
    /// 3) Project to NavMesh
    /// 4) If fail, try left/right side fallback; else try a small position in front
    /// </summary>
    bool FindSafeLatchPoint(out Vector3 safe)
    {
        safe = transform.position;
        if (!player) return false;

        Vector3 playerPos = player.position;
        Vector3 backDir   = -player.forward;
        Vector3 desired   = playerPos + backDir * behindOffset;
        desired.y = transform.position.y;

        // 1) raycast from player toward behind to detect wall
        float rayDist = Mathf.Max(behindOffset + 0.2f, 0.2f);
        Vector3 origin = playerPos + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, backDir, out RaycastHit hit, rayDist, wallMask, QueryTriggerInteraction.Ignore))
        {
            // blocked: step slightly inside using the wall normal
            desired = hit.point + hit.normal * latchSafetyBackoff;
            desired.y = transform.position.y;
        }

        // 2) project to NavMesh
        if (NavMesh.SamplePosition(desired, out NavMeshHit navHit, latchNavProbe, NavMesh.AllAreas))
        {
            safe = navHit.position;
            return true;
        }

        // 3) side fallbacks
        Vector3 left  = playerPos - player.forward * 0.2f - player.right * sideFallbackOffset;
        Vector3 right = playerPos - player.forward * 0.2f + player.right * sideFallbackOffset;
        left.y = right.y = transform.position.y;

        if (NavMesh.SamplePosition(left, out navHit, latchNavProbe, NavMesh.AllAreas))
        { safe = navHit.position; return true; }

        if (NavMesh.SamplePosition(right, out navHit, latchNavProbe, NavMesh.AllAreas))
        { safe = navHit.position; return true; }

        // 4) small front fallback
        Vector3 front = playerPos + player.forward * 0.5f; front.y = transform.position.y;
        if (NavMesh.SamplePosition(front, out navHit, latchNavProbe, NavMesh.AllAreas))
        { safe = navHit.position; return true; }

        return false;
    }

    void TryConsumePlayerMarkOnce()
    {
        var sr = FindPlayerStatus();
        if (sr != null && sr.IsMarked())
        {
            var h = player.GetComponentInParent<Health>();
            if (h) { if (latchHeavyDamage > 0) h.TakeDamage(latchHeavyDamage); }
            else
            {
                var fourP = player.GetComponentInParent<FourHitHealth>();
                if (fourP) fourP.RegisterHit();
            }
            sr.ConsumeMarkAndLock(playerReMarkLockout);
        }
        else
        {
            // fallback tap damage if not marked
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

    // ---------------- Leave Burst: manual, collision-aware ----------------
    IEnumerator DoLeaveBurst(float speed, float maxDistance)
    {
        animator.Play(RetreatName);
        // Switch to manual transform movement
        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Away direction (from player) with small randomness
        Vector3 dir = transform.right;
        if (player)
        {
            Vector3 away = (transform.position - player.position);
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f) dir = away.normalized;
        }
        Vector2 rnd2 = Random.insideUnitCircle.normalized * 0.35f;
        dir = (dir + new Vector3(rnd2.x, 0f, rnd2.y)).normalized;

        // For capsule casts
        float half = Mathf.Max(0.0f, (bodyHeight * 0.5f) - bodyRadius);
        Vector3 capUp = Vector3.up * half;

        float traveled = 0f;
        float elapsed  = 0f;

        while (elapsed < leaveMaxTime)
        {
            elapsed += Time.deltaTime;

            // early out if already far enough
            if (player && Vector3.Distance(transform.position, player.position) >= reengageStartDistance)
                break;

            Vector3 stepVec = dir * speed * Time.deltaTime;
            if (stepVec.sqrMagnitude < 1e-6f) { yield return null; continue; }

            Vector3 pos = transform.position;
            Vector3 p1  = pos + capUp;
            Vector3 p2  = pos - capUp;

            // 1) Solid hit test (ignore player/enemy)
            if (Physics.CapsuleCast(p1, p2, bodyRadius, stepVec.normalized, out RaycastHit solidHit, stepVec.magnitude, solidMask, QueryTriggerInteraction.Ignore))
            {
                string tg = solidHit.collider.tag;
                bool pass = (!string.IsNullOrEmpty(playerTagPass) && tg == playerTagPass) ||
                            (!string.IsNullOrEmpty(enemyTagPass)  && tg == enemyTagPass);

                if (!pass)
                {
                    // Hit a solid prop/wall -> stop at contact (slightly inset)
                    Vector3 stopPos = solidHit.point - solidHit.normal * surfaceInset;
                    stopPos.y = pos.y;
                    transform.position = stopPos;

                    if (stopWhenHitSolid) break;

                    // Optional: if you prefer sliding along the surface instead of stopping,
                    // replace the 'break' with a tangent slide like below:
                    // Vector3 tangent = Vector3.ProjectOnPlane(stepVec, solidHit.normal);
                    // Vector3 slideTry = pos + tangent;
                    // if (NavMesh.SamplePosition(slideTry, out NavMeshHit navHitSlide, navProbe, NavMesh.AllAreas))
                    //     transform.position = navHitSlide.position;
                    // else
                    //     break;
                }
            }

            // 2) NavMesh validity for the next step
            Vector3 nextPos = pos + stepVec;
            if (!NavMesh.SamplePosition(nextPos, out NavMeshHit navHit, navProbe, NavMesh.AllAreas))
            {
                // Attempt to slide along the blocking surface normal if we have one
                if (Physics.CapsuleCast(p1, p2, bodyRadius, stepVec.normalized, out RaycastHit wHit, stepVec.magnitude * 1.2f, solidMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 tangent = Vector3.ProjectOnPlane(stepVec, wHit.normal);
                    Vector3 slideTry = pos + tangent;
                    if (NavMesh.SamplePosition(slideTry, out NavMeshHit navSlide, navProbe, NavMesh.AllAreas))
                    {
                        nextPos = navSlide.position;
                    }
                    else
                    {
                        break; // no valid slide spot -> abort leave
                    }
                }
                else
                {
                    break; // no wall info to slide along -> abort leave
                }
            }
            else
            {
                nextPos = navHit.position;
            }

            // apply movement & orientation
            transform.position = nextPos;
            traveled += stepVec.magnitude;

            Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);

            if (traveled >= maxDistance) break;

            yield return null;
        }

        // Restore agent driving
        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;
    }

    IEnumerator DoPostLeavePause()
    {
        float wait = Random.Range(postLeavePauseRange.x, postLeavePauseRange.y);
        float t = 0f;

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

    // ---------------- Small helpers ----------------
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

    // ---------------- External bridge: damage -> aggro ----------------
    /// <summary>
    /// External call to extend aggro memory (e.g., from AggroOnHealthLoss).
    /// </summary>
    public void NotifyDamaged(float duration = -1f)
    {
        if (!enableDamageAggro) return;
        if (duration <= 0f) duration = damageAggroTimeout;
        aggroTimer = Mathf.Max(aggroTimer, duration);
    }
}

#region --- Minimal helper kept in this file ---
/// <summary>
/// Minimal straight-moving mark projectile:
/// - Moves forward at constant speed;
/// - On trigger with Player: StatusReceiver.Mark(); then destroys itself.
/// </summary>
public class SimpleMarkProjectile : MonoBehaviour
{
    private string _targetTag; 
    private float  _speed; 
    private float  _life; 
    private Vector3 _dir;

    public void Init(string targetTag, float speed, float life)
    {
        _targetTag = targetTag;
        _speed = Mathf.Max(0f, speed);
        _life  = Mathf.Max(0.01f, life);
        _dir   = transform.forward;
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
