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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.speed = normalSpeed;

        // ------------------   make agent naturally stop earlier ------------------
        agent.stoppingDistance = Mathf.Max(0f, approachStopDistance);
    }

    void Start()
    {
        Assign_Player();

        // damage-aggro baselines (no change to your health scripts)
        _hp   = GetComponent<Health>();
        _four = GetComponent<FourHitHealth>();
        if (_hp)   _lastHP  = _hp.currentHealth;
        if (_four) _lastSeg = _four.GetState().current;
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
        if (charging) return; // dash in progress

        bool shouldChase = (dist <= detectRange) || (_aggroTimer > 0f);

        if (shouldChase)
        {
            agent.isStopped = false;
            agent.speed = normalSpeed;

            // Let the agent walk toward the player but it will stop at 'stoppingDistance'
            agent.stoppingDistance = Mathf.Max(0f, approachStopDistance); // keep in sync if tweaked at runtime
            agent.SetDestination(player.position);

            // trigger dash if close enough (dash itself will also respect approachStopDistance)
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

        // temporarily step out of agent movement while dashing straight
        agent.isStopped = true;

        // ------------------   end dash early once close enough ------------------
        while (timer < chargeDuration)
        {
            // Move straight
            Vector3 next = transform.position + chargeDir * chargeSpeed * Time.deltaTime;

            // If next step would overshoot into the player's feet, clamp by stop distance:
            float nextDist = Vector3.Distance(next, player.position);
            if (nextDist <= approachStopDistance)
            {
                // snap just outside the stop distance (optional small epsilon)
                Vector3 fromPlayer = (next - player.position);
                fromPlayer.y = 0f;
                if (fromPlayer.sqrMagnitude > 0.0001f)
                {
                    next = player.position + fromPlayer.normalized * approachStopDistance;
                }
                transform.position = next;
                break; // end dash early
            }

            transform.position = next;
            timer += Time.deltaTime;
            yield return null;
        }

        charging = false;
        agent.isStopped = false;

        // Pause window if close enough now
        if (Vector3.Distance(transform.position, player.position) <= closeStopRange)
        {
            yield return StartCoroutine(DoPauseWindow());
        }

        // cooldown before next dash
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

            // keep facing the player while pausing (nice for attacks)
            if (player)
            {
                Vector3 to = (player.position - transform.position);
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
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
}
