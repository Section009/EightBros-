using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BellAI : MonoBehaviour
{
    public GameObject clapBox;
    public GameObject ringBox;
    public string playerTag = "Player";
    public float detectRange = 12f;     // 发现/开始追击距离
    public float chargeRange = 4f;      // 进入冲刺的触发距离
    public float normalSpeed = 3.5f;    // 追击速度
    public float chargeSpeed = 10f;     // 冲刺速度
    public float chargeDuration = 0.6f; // 冲刺持续时长
    public float chargeCooldown = 1.5f; // 冷却
    public float attackRange = 2f;
    public float attackCooldown = 5f;

    public bool ringing = false;

    private Transform player;
    private NavMeshAgent agent;
    private Dummy dm;
    private bool charging;
    private bool onCooldown;
    private bool attacking;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        dm = GetComponent<Dummy>();
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
        if (attacking) return;

        if (dist <= detectRange)
        {
            // 追击
            agent.isStopped = false;
            agent.speed = normalSpeed;
            agent.SetDestination(player.position);

            if (!onCooldown)
            {
                if (dist <= attackRange)
                {
                    StartCoroutine(DoAttack());
                }
                else if (dist <= chargeRange)
                {
                    StartCoroutine(DoCharge());
                }
            }
        }
        else
        {
            // 超出感知，停止
            agent.isStopped = true;
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Explosion"))
        {
            Standard_Explosion se = col.gameObject.GetComponent<Standard_Explosion>();

            if (ringing) dm.Health -= se.damage / 2;
            else dm.Health -= se.damage;

            dm.KnockBack(col.gameObject.transform, se.knockback_time, se.knockback_speed);
        }
    }

    IEnumerator DoAttack()
    {
        attacking = true;
        onCooldown = true;

        if (Random.value <= 0.5f)
        {
            Debug.Log("Clapped");
            Vector3 playerDir = (player.position - transform.position).normalized;
            Vector3 spawnPosition = transform.position + playerDir;

            GameObject spawnedHitbox = Instantiate(clapBox, spawnPosition, transform.rotation);

            yield return new WaitForSeconds(0.3f);

            attacking = false;
        }
        else
        {
            Debug.Log("Rung");
            Vector3 spawnPosition = transform.position;

            GameObject spawnedHitbox = Instantiate(ringBox, spawnPosition, transform.rotation, gameObject.transform);

            yield return new WaitForSeconds(0.3f);

            attacking = false;
            if (!ringing) RingCooldown();
        }


        yield return new WaitForSeconds(attackCooldown);
        onCooldown = false;
    }

    IEnumerator RingCooldown()
    {
        ringing = true;
        yield return new WaitForSeconds(10f);
        ringing = false;
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
