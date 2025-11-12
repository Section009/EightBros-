using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public string playerTag = "Player";
    public float detectRange = 12f;     // start chasing distance
    public float chargeRange = 4f;      // start dash distance
    public float normalSpeed = 3.5f;    // chase speed
    public float chargeSpeed = 10f;     // dash speed
    public float chargeDuration = 0.6f; // dash duration
    public float chargeCooldown = 1.5f; // cooldown between dashes

    // ------------------   stop earlier when close ------------------
    [Header("Approach Stop")]
    [Tooltip("Agent will stop chasing (and dash will also end) once within this distance from the player.")]
    public float approachStopDistance = 1.5f;

    // ------------------   NEW: stride cadence (step-pause rhythm) ------------------
    [Header("Stride Cadence (step-pause rhythm)")]
    [Tooltip("Enable step-pause movement rhythm while chasing (not during dash).")]
    public bool enableStrideCadence = true;

    [Tooltip("Move time range (seconds) per stride cycle. A value will be sampled each cycle.")]
    public Vector2 moveDurationRange = new Vector2(0.45f, 0.75f);

    [Tooltip("Idle time range (seconds) per stride cycle. A value will be sampled each cycle.")]
    public Vector2 idleDurationRange = new Vector2(0.20f, 0.40f);

    [Tooltip("While idling in cadence, keep facing the player (nice for animation).")]
    public bool facePlayerDuringIdle = true;

    // existing pause/attack-window settings
    [Header("Pause After Close Approach")]
    [Tooltip("If enemy is within this distance from the player after approach/dash, it will pause on the spot.")]
    public float closeStopRange = 2.0f;

    [Tooltip("Short pause (fast attack) [min,max] seconds.")]
    public Vector2 quickPauseRange = new Vector2(0.3f, 0.6f);

    [Tooltip("Heavy pause (slow/charge-up attack) [min,max] seconds.")]
    public Vector2 heavyPauseRange = new Vector2(3.0f, 4.0f);

    [Tooltip("Choose which pause profile to use next time.")]
    public bool useHeavyAttackPause = false;

    [Tooltip("Event before pause begins (hook attack windup/animation here).")]
    public UnityEvent onPauseAttackBegin;

    [Tooltip("Event after pause ends (hook attack release here).")]
    public UnityEvent onPauseAttackEnd;

    // damage-aggro
    [Header("Damage Aggro")]
    public bool enableDamageAggro = true;
    public float damageAggroTimeout = 6f;
    float _aggroTimer = 0f;
    Health _hp;  int _lastHP = -1;
    FourHitHealth _four; int _lastSeg = -1;

    private Transform player;
    private NavMeshAgent agent;
    private bool charging;
    private bool onCooldown;

    // --- cadence internals ---
    enum StrideState { Moving, Idling }
    StrideState _strideState = StrideState.Moving;
    float _strideTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.speed = normalSpeed;

        // stop a bit before overlapping
        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);
    }

    void Start()
    {
        Assign_Player();

        // damage-aggro baselines
        _hp   = GetComponent<Health>();
        _four = GetComponent<FourHitHealth>();
        if (_hp)   _lastHP  = _hp.currentHealth;
        if (_four) _lastSeg = _four.GetState().current;

        ResetStrideCycle(StrideState.Moving); // initialize cadence
    }

    public void Assign_Player()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        // damage-aggro polling
        if (enableDamageAggro)
        {
            if (WasDamagedThisFrame()) _aggroTimer = damageAggroTimeout;
            if (_aggroTimer > 0f) _aggroTimer -= Time.deltaTime;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (charging) return; // dash in progress - cadence is ignored

        bool shouldChase = (dist <= detectRange) || (_aggroTimer > 0f);

        if (shouldChase)
        {
            // keep stop distance up-to-date
            agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);

            if (enableStrideCadence && dist > approachStopDistance)
            {
                // --- cadence logic while chasing ---
                _strideTimer -= Time.deltaTime;

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

                    if (facePlayerDuringIdle && player)
                    {
                        Vector3 to = (player.position - transform.position);
                        to.y = 0f;
                        if (to.sqrMagnitude > 0.0001f)
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
                // no cadence (either disabled or already within stop distance)
                agent.isStopped = false;
                agent.speed = normalSpeed;
                agent.SetDestination(player.position);
            }

            // trigger dash if close enough (dash ignores cadence)
            if (!onCooldown && dist <= chargeRange)
            {
                StartCoroutine(DoCharge());
            }
        }
        else
        {
            agent.isStopped = true;
        }
    }

    IEnumerator DoCharge()
    {
        charging = true;
        onCooldown = true;

        // lock the dash direction at start
        Vector3 chargeDir = (player.position - transform.position);
        chargeDir.y = 0f;
        chargeDir = chargeDir.sqrMagnitude > 0.0001f ? chargeDir.normalized : transform.forward;

        float timer = 0f;

        // dash: step out of agent movement
        agent.isStopped = true;

        while (timer < chargeDuration)
        {
            // Move straight
            Vector3 next = transform.position + chargeDir * chargeSpeed * Time.deltaTime;

            // end early once we hit the stop distance
            float nextDist = Vector3.Distance(next, player.position);
            if (nextDist <= approachStopDistance)
            {
                Vector3 fromPlayer = (next - player.position);
                fromPlayer.y = 0f;
                if (fromPlayer.sqrMagnitude > 0.0001f)
                {
                    next = player.position + fromPlayer.normalized * approachStopDistance;
                }
                transform.position = next;
                break;
            }

            transform.position = next;
            timer += Time.deltaTime;
            yield return null;
        }

        charging = false;
        agent.isStopped = false;

        // optional: pause window if close enough now (unchanged)
        if (Vector3.Distance(transform.position, player.position) <= closeStopRange)
        {
            yield return StartCoroutine(DoPauseWindow());
        }

        // reset cadence after dash so rhythm feels intentional
        ResetStrideCycle(StrideState.Moving);

        // cooldown
        yield return new WaitForSeconds(chargeCooldown);
        onCooldown = false;
    }

    IEnumerator DoPauseWindow()
    {
        Vector2 range = useHeavyAttackPause ? heavyPauseRange : quickPauseRange;
        float pauseTime = Random.Range(range.x, range.y);

        onPauseAttackBegin?.Invoke();

        bool prevStopped = agent.isStopped;
        agent.isStopped = true;

        float t = 0f;
        while (t < pauseTime)
        {
            t += Time.deltaTime;

            // keep facing the player while pausing
            if (player)
            {
                Vector3 to = (player.position - transform.position);
                to.y = 0f;
                if (to.sqrMagnitude > 0f)
                {
                    Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 720f * Time.deltaTime);
                }
            }

            yield return null;
        }

        onPauseAttackEnd?.Invoke();
        agent.isStopped = prevStopped;
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

    // --- cadence helpers ---
    void ResetStrideCycle(StrideState next)
    {
        _strideState = next;
        if (_strideState == StrideState.Moving)
        {
            float t = Mathf.Clamp(Random.Range(moveDurationRange.x, moveDurationRange.y), 0.01f, 999f);
            _strideTimer = t;
        }
        else
        {
            float t = Mathf.Clamp(Random.Range(idleDurationRange.x, idleDurationRange.y), 0.01f, 999f);
            _strideTimer = t;
        }
    }
}
