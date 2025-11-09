using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyLatchDashAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    [Header("Detect")]
    [Tooltip("进入追击/冲贴的感知半径（很小）")]
    public float detectRange = 5f;
    [Tooltip("认为“碰到玩家”的触发距离")]
    public float latchTriggerDistance = 1.0f;

    [Header("Speeds / Times")]
    public float approachSpeed = 12f;        // 极快靠近
    public float normalChaseSpeed = 4f;      // 备用常速
    public float latchDuration = 1.25f;      // 吸附在身后的时间
    public float leaveBurstSpeed = 14f;      // 快速脱离时速度
    public float leaveDistance = 6f;         // 脱离多远
    public float reengageCooldown = 1.0f;    // 脱离后冷却

    [Header("Latch (stick behind)")]
    public float behindOffset = 0.75f;       // 吸附时保持在玩家身后距离
    public float latchLerpSpeed = 12f;       // 吸附时位置插值速度
    public bool alignRotationWhileLatch = true;

    [Header("Grounding")]
    public bool alignToGroundOnLand = true;
    public LayerMask groundMask = ~0;
    public float groundRayLength = 3f;

    // --- internals
    NavMeshAgent agent;
    enum State { Idle, ApproachFast, Latch, Leave, Cooldown }
    State state;

    // 受击检测（仅两种：Health / FourHitHealth）
    Health hp;                 int lastHP = -1;           // Health.currentHealth
    FourHitHealth four;        int lastSeg4 = -1;         // FourHitHealth.GetState().current

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0f;
        agent.updateRotation = true;
        agent.speed = normalChaseSpeed;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        hp   = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();

        if (hp)   lastHP   = hp.currentHealth;
        if (four) lastSeg4 = four.GetState().current;

        state = State.Idle;
        StartCoroutine(FSM());
    }

    IEnumerator FSM()
    {
        while (true)
        {
            if (!player) { yield return null; continue; }

            bool gotHit = CheckDamagedThisFrame(); // 受击触发
            float dist = Vector3.Distance(transform.position, player.position);
            bool playerInRange = dist <= detectRange;

            switch (state)
            {
                case State.Idle:
                    agent.isStopped = true;
                    if (playerInRange || gotHit)
                    {
                        state = State.ApproachFast;
                        agent.isStopped = false;
                        agent.speed = approachSpeed;
                    }
                    break;

                case State.ApproachFast:
                    agent.isStopped = false;
                    agent.speed = approachSpeed;
                    agent.SetDestination(player.position);

                    if (dist <= latchTriggerDistance)
                    {
                        yield return StartCoroutine(DoLatch());
                        state = State.Leave;
                    }
                    break;

                case State.Leave:
                    yield return StartCoroutine(DoLeaveBurst());
                    state = State.Cooldown;
                    break;

                case State.Cooldown:
                    agent.isStopped = true;
                    yield return new WaitForSeconds(reengageCooldown);
                    state = State.Idle;
                    break;
            }

            yield return null;
        }
    }

    bool CheckDamagedThisFrame()
    {
        bool hit = false;

        if (hp)
        {
            if (lastHP < 0) lastHP = hp.currentHealth;          // uses Health.currentHealth
            if (hp.currentHealth < lastHP) hit = true;           // fell → 受击
            lastHP = hp.currentHealth;
        }

        if (four)
        {
            int cur = four.GetState().current;                   // uses FourHitHealth.GetState()
            if (lastSeg4 < 0) lastSeg4 = cur;
            if (cur < lastSeg4) hit = true;
            lastSeg4 = cur;
        }

        return hit;
    }

    IEnumerator DoLatch()
    {
        // 吸附：关闭 agent，用插值把自己放到玩家身后 behindOffset
        agent.isStopped = true; agent.ResetPath();
        bool prevUpdatePos = agent.updatePosition;
        bool prevUpdateRot = agent.updateRotation;
        agent.updatePosition = false; agent.updateRotation = false;

        float t = 0f;
        while (t < latchDuration && player)
        {
            t += Time.deltaTime;

            Vector3 target = player.position - player.forward * behindOffset;

            if (alignToGroundOnLand &&
                Physics.Raycast(target + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
                target.y = hit.point.y;
            else
                target.y = transform.position.y;

            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-latchLerpSpeed * Time.deltaTime));

            if (alignRotationWhileLatch)
            {
                Quaternion want = Quaternion.LookRotation(player.forward, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 720f * Time.deltaTime);
            }

            yield return null;
        }

        agent.updatePosition = prevUpdatePos;
        agent.updateRotation = prevUpdateRot;
        agent.Warp(transform.position); // 同步内部位置
    }

    IEnumerator DoLeaveBurst()
    {
        // 快速脱离：沿玩家背向冲出 leaveDistance
        agent.isStopped = true; agent.ResetPath();
        bool prevUpdatePos = agent.updatePosition;
        bool prevUpdateRot = agent.updateRotation;
        agent.updatePosition = false; agent.updateRotation = false;

        Vector3 start = transform.position;
        Vector3 dir = player ? (-player.forward) : transform.forward;
        Vector3 end = start + dir * leaveDistance;

        if (alignToGroundOnLand &&
            Physics.Raycast(end + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            end.y = hit.point.y;
        else
            end.y = start.y;

        float traveled = 0f;
        while (traveled < 1f)
        {
            float step = (leaveBurstSpeed * Time.deltaTime) / Mathf.Max(0.001f, leaveDistance);
            traveled += step;
            Vector3 pos = Vector3.Lerp(start, end, Mathf.Clamp01(traveled));
            transform.position = pos;

            Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 720f * Time.deltaTime);
            yield return null;
        }

        agent.updatePosition = prevUpdatePos;
        agent.updateRotation = prevUpdateRot;
        agent.Warp(transform.position);
        agent.isStopped = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, latchTriggerDistance);
    }
}
