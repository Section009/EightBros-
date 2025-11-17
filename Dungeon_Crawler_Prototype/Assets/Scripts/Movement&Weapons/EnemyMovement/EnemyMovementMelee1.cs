using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    // ---------- Types (define ONCE) ----------
    enum StrideState { Moving, Idling }   // ← 只保留这一处

    [Header("Targeting")]
    public string playerTag = "Player";

    [Header("Ranges & Speeds")]
    [Tooltip("Begin chasing if player is within this distance, or when damage-aggro is active.")]
    public float detectRange = 12f;

    [Tooltip("If player is within this distance, start a dash (charge).")]
    public float chargeRange = 4f;

    [Tooltip("Regular chase speed (NavMeshAgent.speed).")]
    public float normalSpeed = 3.5f;

    [Tooltip("Dash (charge) speed while charging.")]
    public float chargeSpeed = 10f;

    [Tooltip("Dash duration in seconds.")]
    public float chargeDuration = 0.6f;

    [Tooltip("Dash cooldown in seconds.")]
    public float chargeCooldown = 1.5f;

    [Header("Approach Stop")]
    [Tooltip("Stop approaching once within this distance (also used as dash stop clamp).")]
    public float approachStopDistance = 1.5f;

    // ------------------ Stride cadence (step-pause) ------------------
    [Header("Stride Cadence (step-pause)")]
    [Tooltip("Enable 'move a bit → stop a bit' rhythm during chasing (not during dash).")]
    public bool enableStrideCadence = true;

    [Tooltip("Move duration range (seconds) when using random cadence.")]
    public Vector2 moveDurationRange = new Vector2(0.45f, 0.75f);

    [Tooltip("Idle duration range (seconds) when using random cadence.")]
    public Vector2 idleDurationRange = new Vector2(0.20f, 0.40f);

    [Tooltip("Keep facing the player while idling in cadence.")]
    public bool facePlayerDuringIdle = true;

    [Header("Cadence Controls")]
    [Tooltip("Use exact durations instead of random ranges.")]
    public bool useFixedCadence = false;

    [Tooltip("Exact move duration when useFixedCadence = true.")]
    public float fixedMoveDuration = 0.50f;

    [Tooltip("Exact idle duration when useFixedCadence = true.")]
    public float fixedIdleDuration = 0.30f;

    [Tooltip("Multiply both move & idle durations (1 = unchanged).")]
    public float cadenceTimeScale = 1.0f;

    // ------------------ Attack windows / pause settings ------------------
    [Header("Attack Loop Trigger")]
    [Tooltip("Enter attack loop when within this distance (also used after dash).")]
    public float closeStopRange = 2.0f;

    [Header("Attack Durations")]
    [Tooltip("Short pause (quick attack) [min,max] seconds.")]
    public Vector2 quickPauseRange = new Vector2(0.3f, 0.6f);

    [Tooltip("Heavy pause (sonic aura) [min,max] seconds.")]
    public Vector2 heavyPauseRange = new Vector2(3.0f, 4.0f);

    [Header("Attack Loop Policy")]
    [Tooltip("Chance to use HEAVY attack each cycle. 0 = always quick, 1 = always heavy.")]
    [Range(0f, 1f)] public float heavyAttackChance = 0.5f;

    [Tooltip("Gap after each attack before chaining or chasing [min,max] seconds.")]
    public Vector2 betweenAttacksDelayRange = new Vector2(0.25f, 0.75f);

    [Tooltip("If true, keep facing the player during the post-attack waiting gap.")]
    public bool facePlayerDuringBetweenDelay = true;

    [Tooltip("If true, chaining attacks also requires line-of-sight to player.")]
    public bool requireLOSInsideRange = false;

    [Tooltip("Layers used for LOS raycast if enabled.")]
    public LayerMask losBlockers = ~0;

    [Header("Pause Events (used by attack scripts)")]
    [Tooltip("DON'T set this manually at runtime; it is toggled when an attack type is chosen.\nfalse = quick (fan cone), true = heavy (sonic aura).")]
    public bool useHeavyAttackPause = false;

    [Tooltip("Invoked when an attack pause begins (FanCone/SonicAura listen here).")]
    public UnityEvent onPauseAttackBegin;

    [Tooltip("Invoked when an attack pause ends.")]
    public UnityEvent onPauseAttackEnd;

    // ------------------ Damage aggro ------------------
    [Header("Damage Aggro")]
    [Tooltip("Getting damaged forces aggro for a period, ignoring detectRange.")]
    public bool enableDamageAggro = true;

    [Tooltip("Seconds to keep aggro after being damaged.")]
    public float damageAggroTimeout = 6f;

    float _aggroTimer = 0f;
    Health _hp;  int _lastHP = -1;
    FourHitHealth _four; int _lastSeg = -1;

    // ------------------ Internals ------------------
    Transform player;
    NavMeshAgent agent;
    bool charging;
    bool onCooldown;

    StrideState _strideState = StrideState.Moving;
    float _strideTimer = 0f;

    bool _attackLoopRunning = false;

    // Debug helpers
    [Header("Debug")]
    public bool debugCadence = false;
    [SerializeField] string cadenceState = "";
    [SerializeField] float cadenceRemain = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.speed = normalSpeed;
        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);
        agent.autoBraking = false; // sharper stop-go
    }

    void Start()
    {
        Assign_Player();

        _hp   = GetComponent<Health>();
        _four = GetComponent<FourHitHealth>();
        if (_hp)   _lastHP  = _hp.currentHealth;
        if (_four) _lastSeg = _four.GetState().current;

        ResetStrideCycle(StrideState.Moving);
    }

    public void Assign_Player()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        // --- Damage aggro ---
        if (enableDamageAggro)
        {
            if (WasDamagedThisFrame()) _aggroTimer = damageAggroTimeout;
            if (_aggroTimer > 0f) _aggroTimer -= Time.deltaTime;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (charging) return; // dash owns motion

        bool shouldChase = (dist <= detectRange) || (_aggroTimer > 0f);

        if (shouldChase)
        {
            // If close enough and not already in attack loop, start it
            if (!_attackLoopRunning && dist <= closeStopRange)
            {
                StartCoroutine(AttackLoop());
                return; // AttackLoop will own stop/go this frame
            }

            // Normal chase (with cadence) if not attacking
            if (!_attackLoopRunning)
            {
                if (enableStrideCadence)
                {
                    _strideTimer -= Time.deltaTime;
                    cadenceRemain = _strideTimer;

                    if (_strideState == StrideState.Moving)
                    {
                        agent.isStopped = false;
                        agent.speed = normalSpeed;
                        agent.SetDestination(player.position);

                        if (_strideTimer <= 0f)
                            ResetStrideCycle(StrideState.Idling);
                    }
                    else // Idling
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                        agent.velocity = Vector3.zero;

                        if (facePlayerDuringIdle && player)
                        {
                            Vector3 to = (player.position - transform.position);
                            to.y = 0f;
                            if (to.sqrMagnitude > 1e-6f)
                            {
                                Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 720f * Time.deltaTime);
                            }
                        }

                        if (_strideTimer <= 0f)
                            ResetStrideCycle(StrideState.Moving);
                    }
                }
                else
                {
                    // no cadence
                    agent.isStopped = false;
                    agent.speed = normalSpeed;
                    agent.SetDestination(player.position);
                }

                // dash trigger (outside attack loop)
                if (!onCooldown && dist <= chargeRange)
                    StartCoroutine(DoCharge());
            }
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    IEnumerator DoCharge()
    {
        charging = true;
        onCooldown = true;

        Vector3 chargeDir = (player.position - transform.position);
        chargeDir.y = 0f;
        chargeDir = chargeDir.sqrMagnitude > 1e-6f ? chargeDir.normalized : transform.forward;

        float timer = 0f;

        agent.isStopped = true;
        agent.ResetPath();

        while (timer < chargeDuration)
        {
            Vector3 next = transform.position + chargeDir * chargeSpeed * Time.deltaTime;

            float nextDist = Vector3.Distance(next, player.position);
            if (nextDist <= approachStopDistance)
            {
                Vector3 fromPlayer = (next - player.position);
                fromPlayer.y = 0f;
                if (fromPlayer.sqrMagnitude > 1e-6f)
                    next = player.position + fromPlayer.normalized * approachStopDistance;

                transform.position = next;
                break;
            }

            transform.position = next;
            timer += Time.deltaTime;
            yield return null;
        }

        charging = false;
        agent.isStopped = false;

        // If close enough after dash, start the attack loop; else resume cadence
        if (Vector3.Distance(transform.position, player.position) <= closeStopRange)
        {
            if (!_attackLoopRunning)
                StartCoroutine(AttackLoop());
        }
        else
        {
            ResetStrideCycle(StrideState.Moving);
        }

        yield return new WaitForSeconds(chargeCooldown);
        onCooldown = false;
    }

    /// <summary>
    /// Randomly pick quick/heavy attack, perform its pause window, then wait a configurable gap.
    /// Continue chaining while the player stays within closeStopRange (and, optional, LOS).
    /// Leave loop and resume chase when out of range/LOS.
    /// </summary>
    IEnumerator AttackLoop()
    {
        if (_attackLoopRunning) yield break;
        _attackLoopRunning = true;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        while (true)
        {
            if (!player) break;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > closeStopRange || (requireLOSInsideRange && !HasLOS()))
                break;

            // --- Choose attack type for this cycle ---
            bool heavy = (Random.value < heavyAttackChance);
            useHeavyAttackPause = heavy; // drives FanCone/SonicAura via events

            // --- Pause/attack duration by type ---
            Vector2 range = heavy ? heavyPauseRange : quickPauseRange;
            float pauseTime = Random.Range(range.x, range.y);

            // --- Begin attack (notify listeners) ---
            onPauseAttackBegin?.Invoke();

            // Hold for attack/pause time
            float t = 0f;
            while (t < pauseTime)
            {
                t += Time.deltaTime;

                // Face the player while attacking
                if (player)
                {
                    Vector3 to = (player.position - transform.position);
                    to.y = 0f;
                    if (to.sqrMagnitude > 1e-6f)
                    {
                        Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 720f * Time.deltaTime);
                    }
                }

                yield return null;
            }

            // --- End attack ---
            onPauseAttackEnd?.Invoke();

            // --- Between-attacks idle gap ---
            float gap = Random.Range(betweenAttacksDelayRange.x, betweenAttacksDelayRange.y);
            float g = 0f;
            while (g < gap)
            {
                if (!player) { g = gap; break; }
                float nowDist = Vector3.Distance(transform.position, player.position);
                if (nowDist > closeStopRange || (requireLOSInsideRange && !HasLOS()))
                {
                    g = gap;
                    break;
                }

                if (facePlayerDuringBetweenDelay)
                {
                    Vector3 to = (player.position - transform.position);
                    to.y = 0f;
                    if (to.sqrMagnitude > 1e-6f)
                    {
                        Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 720f * Time.deltaTime);
                    }
                }

                g += Time.deltaTime;
                yield return null;
            }

            // Re-check exit after gap
            if (!player) break;
            float distAfterGap = Vector3.Distance(transform.position, player.position);
            if (distAfterGap > closeStopRange || (requireLOSInsideRange && !HasLOS()))
                break;

            // Otherwise continue next attack
            yield return null;
        }

        // Exit attack loop → resume chase
        _attackLoopRunning = false;
        agent.isStopped = false;
        ResetStrideCycle(StrideState.Moving);
    }

    bool HasLOS()
    {
        if (!player) return false;
        Vector3 origin = transform.position + Vector3.up * 1.1f;
        Vector3 target = player.position + Vector3.up * 1.1f;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist <= 1e-3f) return true;
        dir /= dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, losBlockers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag(playerTag))
                return false;
        }
        return true;
    }

    public void NotifyDamaged()
    {
        if (!enableDamageAggro) return;
        _aggroTimer = damageAggroTimeout;
    }

    bool WasDamagedThisFrame()
    {
        bool hit = false;

        if (_hp != null)
        {
            if (_lastHP < 0) _lastHP = _hp.currentHealth;
            if (_hp.currentHealth < _lastHP) hit = true;
            _lastHP = _hp.currentHealth;
        }

        if (_four != null)
        {
            int cur = _four.GetState().current;
            if (_lastSeg < 0) _lastSeg = cur;
            if (cur < _lastSeg) hit = true;
            _lastSeg = cur;
        }

        return hit;
    }

    // ------------------ Cadence helpers ------------------
    void ResetStrideCycle(StrideState next)
    {
        _strideState = next;

        float dur;
        if (_strideState == StrideState.Moving)
        {
            dur = useFixedCadence
                ? fixedMoveDuration
                : Random.Range(moveDurationRange.x, moveDurationRange.y);
        }
        else
        {
            dur = useFixedCadence
                ? fixedIdleDuration
                : Random.Range(idleDurationRange.x, idleDurationRange.y);
        }

        _strideTimer = Mathf.Max(0f, dur * Mathf.Max(0f, cadenceTimeScale));

        if (debugCadence)
        {
            cadenceState  = _strideState.ToString();
            cadenceRemain = _strideTimer;
            Debug.Log($"[Cadence] Enter {_strideState} for {_strideTimer:F2}s");
        }
    }
}
