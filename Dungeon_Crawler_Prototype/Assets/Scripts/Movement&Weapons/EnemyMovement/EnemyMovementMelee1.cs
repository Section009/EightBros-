using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public string playerTag = "Player";
    public float detectRange = 12f;     // 发现/开始追击距离
    public float chargeRange = 4f;      // 进入冲刺的触发距离
    public float normalSpeed = 3.5f;    // 追击速度
    public float chargeSpeed = 10f;     // 冲刺速度
    public float chargeDuration = 0.6f; // 冲刺持续时长
    public float chargeCooldown = 1.5f; // 冷却

    private Transform player;
    private NavMeshAgent agent;
    private bool charging;
    private bool onCooldown;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0f;
        agent.updateRotation = true;
        agent.speed = normalSpeed;
    }

    void Start()
    {
        Assign_Player();
    }

    public void Assign_Player()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (charging) return; // 冲刺中由协程控制

        if (dist <= detectRange)
        {
            // 追击
            agent.isStopped = false;
            agent.speed = normalSpeed;
            agent.SetDestination(player.position);

            if (!onCooldown && dist <= chargeRange)
            {
                StartCoroutine(DoCharge());
            }
        }
        else
        {
            // 超出感知，停止
            agent.isStopped = true;
        }
    }

    IEnumerator DoCharge()
    {
        charging = true;
        onCooldown = true;

        // 记录冲刺起点朝向目标，锁定方向（防止冲刺中目标大幅绕圈）
        Vector3 chargeDir = (player.position - transform.position).normalized;
        float timer = 0f;

        // 临时提高速度/手动推进
        agent.isStopped = true; // 用刚体/位移推进，避免转弯
        while (timer < chargeDuration)
        {
            transform.position += chargeDir * chargeSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        charging = false;
        agent.isStopped = false;

        // 冷却
        yield return new WaitForSeconds(chargeCooldown);
        onCooldown = false;
    }
}
