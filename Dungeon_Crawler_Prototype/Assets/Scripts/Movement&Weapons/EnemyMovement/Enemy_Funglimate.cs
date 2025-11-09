using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyJumpChaseAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    private Transform player;

    [Header("Vision Cone")]
    public float halfFovDeg = 35f;
    public float visionDistance = 18f;
    public bool requireLineOfSight = true;
    [Tooltip("Layers that block LOS (do NOT include this enemy's layer).")]
    public LayerMask losBlockMask;

    [Header("Vision Transforms (optional)")]
    public Transform visionOrigin;   // if null, uses transform
    public Transform visionForward;  // if null, uses transform

    [Header("Engagement")]
    public bool ignoreVisionAfterFirstPause = true;
    public float disengageDistance = 0f;

    [Header("Vision Memory")]
    [Tooltip("Keep chasing for this many seconds after losing vision (prevents 'one hop then idle').")]
    public float visionMemoryTime = 1.0f;   // NEW
    private float visionMemoryTimer;        // NEW

    [Header("Hop Motion")]
    public float hopDistance = 4.0f;
    public float hopDuration = 0.45f;
    public float arcHeight = 1.2f;
    public float hopCooldown = 0.15f;
    public float stopDistance = 1.8f;
    public float turnSpeed = 720f;

    [Header("Pause (Attack Window)")]
    public Vector2 pauseTimeRange = new Vector2(0.6f, 1.0f);
    public UnityEvent onPauseBegin;
    public UnityEvent onPauseEnd;

    [Header("Damage Aggro")]
    public bool enableDamageAggro = true;
    public float damageAggroTimeout = 6f;

    // Internals
    private NavMeshAgent agent;
    private bool ignoreVisionPhase;
    private bool isHopping;
    private bool isPausing;
    private float aggroTimer;

    private Health hp;               int lastHP = -1;
    private FourHitHealth four;      int lastSeg = -1;

    private enum State { Idle, HopChase, Pause }
    private State state;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.stoppingDistance = Mathf.Max(0f, stopDistance);
        agent.speed = 5f;

        if (losBlockMask.value == 0)
        {
            int myLayer = gameObject.layer;
            losBlockMask = ~0;
            losBlockMask &= ~(1 << myLayer); // exclude self layer by default
        }
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        hp   = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();
        if (hp)   lastHP  = hp.currentHealth;
        if (four) lastSeg = four.GetState().current;

        state = State.Idle;
        StartCoroutine(FSM());
    }

    IEnumerator FSM()
    {
        while (true)
        {
            if (!player) { yield return null; continue; }

            // damage aggro
            if (enableDamageAggro)
            {
                if (WasDamagedThisFrame()) aggroTimer = damageAggroTimeout;
                if (aggroTimer > 0f) aggroTimer -= Time.deltaTime;
            }

            bool inVision = IsPlayerInVision();
            float dist = Vector3.Distance(transform.position, player.position);

            // ----   vision memory ----
            if (inVision) visionMemoryTimer = visionMemoryTime;
            else if (visionMemoryTimer > 0f) visionMemoryTimer -= Time.deltaTime;

            bool aggro = aggroTimer > 0f;

            // optional disengage
            if (disengageDistance > 0f && dist > disengageDistance && !aggro && visionMemoryTimer <= 0f && !ignoreVisionPhase)
            {
                ignoreVisionPhase = false;
                state = State.Idle;
            }

            switch (state)
            {
                case State.Idle:
                {
                    agent.isStopped = true;
                    isHopping = false; isPausing = false;

                    // Enter if player in cone OR damage-aggro OR memory still alive
                    if (inVision || aggro || visionMemoryTimer > 0f)
                    {
                        state = State.HopChase;
                        agent.isStopped = false;
                    }
                    break;
                }

                case State.HopChase:
                {
                    // ----   allow continue on memory ----
                    bool canContinue = ignoreVisionPhase ? true : (inVision || aggro || visionMemoryTimer > 0f);
                    if (!canContinue)
                    {
                        state = State.Idle;
                        agent.isStopped = true;
                        break;
                    }

                    if (dist <= stopDistance + 0.05f && !isHopping)
                    {
                        state = State.Pause;
                        break;
                    }

                    if (!isHopping)
                    {
                        yield return StartCoroutine(DoOneHop());
                        yield return new WaitForSeconds(hopCooldown);
                    }
                    break;
                }

                case State.Pause:
                {
                    if (!isPausing)
                    {
                        yield return StartCoroutine(DoPause());

                        if (ignoreVisionAfterFirstPause)
                            ignoreVisionPhase = true;

                        state = State.HopChase;
                    }
                    break;
                }
            }

            yield return null;
        }
    }

    bool IsPlayerInVision()
    {
        if (!player) return false;

        Transform o = visionOrigin ? visionOrigin : transform;
        Transform f = visionForward ? visionForward : transform;

        Vector3 to = player.position - o.position;
        float dist = to.magnitude;
        if (dist > visionDistance) return false;

        Vector3 fwd = f.forward;
        to.y = 0f; fwd.y = 0f;
        if (to.sqrMagnitude < 1e-6f || fwd.sqrMagnitude < 1e-6f) return false;

        float cos = Vector3.Dot(to.normalized, fwd.normalized);
        float cosLimit = Mathf.Cos(halfFovDeg * Mathf.Deg2Rad);
        if (cos < cosLimit) return false;

        if (!requireLineOfSight) return true;

        Vector3 origin = o.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 dir = (target - origin);
        float maxD = dir.magnitude;
        if (maxD <= 1e-4f) return true;
        dir /= maxD;

        // Ignore self & children; only blockers in mask matter
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxD, losBlockMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform))
                continue;

            if (h.collider.CompareTag(playerTag) || h.collider.transform.IsChildOf(player))
                return true;

            return false; // first valid hit is a blocker
        }

        return true; // nothing on blocker mask
    }

    IEnumerator DoOneHop()
    {
        isHopping = true;

        Vector3 origin = transform.position;
        Vector3 land = ChooseHopLanding(origin);

        Vector3 planarDir = land - origin; planarDir.y = 0f;
        if (planarDir.sqrMagnitude > 0.0001f)
        {
            Quaternion want = Quaternion.LookRotation(planarDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, hopDuration);
            float u = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(origin, land, u);
            float height = 4f * arcHeight * u * (1f - u);
            pos.y = Mathf.Lerp(origin.y, land.y, u) + height;

            transform.position = pos;

            if (planarDir.sqrMagnitude > 0.0001f)
            {
                Quaternion want = Quaternion.LookRotation(planarDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            }

            yield return null;
        }

        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;

        isHopping = false;
    }

    Vector3 ChooseHopLanding(Vector3 origin)
    {
        if (!player) return origin;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(player.position, path);

        Vector3 fallbackTarget = player.position;

        Vector3 target = fallbackTarget;
        if (path.status == NavMeshPathStatus.PathComplete && path.corners != null && path.corners.Length >= 2)
            target = FindFirstUsefulCorner(path.corners, origin, 0.1f, fallbackTarget);

        Vector3 to = target - origin; to.y = 0f;
        float planarDist = to.magnitude;

        float desired = Mathf.Min(hopDistance, Mathf.Max(0f, planarDist - stopDistance));

        Vector3 landPlanar = (planarDist < 0.0001f) ? origin : origin + to.normalized * desired;
        Vector3 land = ProjectToNavmesh(landPlanar, 1.0f);

        Vector3 toPlayer = player.position - land; toPlayer.y = 0f;
        float d = toPlayer.magnitude;
        if (d < stopDistance * 0.9f)
        {
            if (toPlayer.sqrMagnitude > 0.0001f)
                land = player.position - toPlayer.normalized * stopDistance;
        }

        land.y = float.IsInfinity(land.y) ? origin.y : land.y;
        return land;
    }

    Vector3 FindFirstUsefulCorner(Vector3[] corners, Vector3 origin, float minGap, Vector3 fallback)
    {
        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 c = corners[i];
            Vector3 oc = new Vector3(c.x, origin.y, c.z);
            if ((oc - origin).magnitude > minGap)
                return c;
        }
        return fallback;
    }

    Vector3 ProjectToNavmesh(Vector3 pos, float maxDist)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
            return hit.position;
        return new Vector3(pos.x, float.PositiveInfinity, pos.z);
    }

    IEnumerator DoPause()
    {
        isPausing = true;

        bool prevStopped = agent.isStopped;
        agent.isStopped = true;
        agent.ResetPath();

        float pause = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        onPauseBegin?.Invoke();

        float t = 0f;
        while (t < pause)
        {
            t += Time.deltaTime;

            if (player)
            {
                Vector3 to = player.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 1e-6f)
                {
                    Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                }
            }

            yield return null;
        }

        onPauseEnd?.Invoke();
        agent.isStopped = prevStopped;
        isPausing = false;
    }

    public void NotifyDamaged()
    {
        if (!enableDamageAggro) return;
        aggroTimer = damageAggroTimeout;
        if (state == State.Idle) state = State.HopChase;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionDistance);

        Transform f = visionForward ? visionForward : transform;
        Vector3 fwd = f.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude > 1e-6f)
        {
            float ang = halfFovDeg;
            Quaternion left = Quaternion.AngleAxis(-ang, Vector3.up);
            Quaternion right = Quaternion.AngleAxis(ang, Vector3.up);
            Vector3 l = left * fwd.normalized * visionDistance;
            Vector3 r = right * fwd.normalized * visionDistance;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
            Gizmos.DrawLine(transform.position, transform.position + l);
            Gizmos.DrawLine(transform.position, transform.position + r);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
