using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BellAI : MonoBehaviour
{
    public GameObject clapBox;
    public GameObject ringBox;
    public string playerTag = "Player";
    public float detectRange = 12f;     // 发现/开始追击距离
    public float chargeRange = 12f;      // 进入冲刺的触发距离
    public float normalSpeed = 3.5f;    // 追击速度
    public float chargeSpeed = 10f;     // 冲刺速度
    public float chargeDuration = 0.6f; // 冲刺持续时长
    public float chargeCooldown = 1.5f; // 冷却
    public float attackRange = 12f;
    public float clapRange = 1f;
    public float attackCooldown = 0f;

    public bool ringing = false;
    public bool clapping = false;

    private Transform player;
    private NavMeshAgent agent;
    private Health h;
    private StatusReceiver SR;
    private bool charging;
    private bool onCooldown;
    private bool attacking;
    private bool stunned;
    private bool moving;
    private bool OnChargeCooldown;
    private float ActSpeed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        h = GetComponent<Health>();
        SR = GetComponent<StatusReceiver>();
        agent.stoppingDistance = 1f;
        agent.updateRotation = true;
        agent.speed = normalSpeed;
        ActSpeed = normalSpeed;
    }

    void Start()
    {
        Assign_Player();
    }

    public void Assign_Player()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
        agent.isStopped = false;
        agent.speed = normalSpeed;
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);
        UnityEngine.Debug.Log(agent.speed);

        //if (charging) return; // 冲刺中由协程控制
        //if (stunned) return;

        if (dist <= detectRange)
        {
            // 追击

            agent.SetDestination(player.position);

            if (clapping && dist <= clapRange)
            {
                UnityEngine.Debug.Log("clapped");
                clapping = false;
                agent.isStopped = true;
                agent.speed = normalSpeed;
                Vector3 playerDir = (player.position - transform.position).normalized;
                Vector3 spawnPosition = transform.position + playerDir;

                GameObject spawnedHitbox = Instantiate(clapBox, spawnPosition, transform.rotation);
                attacking = false;

                StartCoroutine(WaitForOne());
            }

            if (!onCooldown)
            {
                if (dist <= attackRange)
                {
                    StartCoroutine(DoAttack());
                }
                else if (dist <= chargeRange && !OnChargeCooldown)
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
        if (col.gameObject.layer == LayerMask.NameToLayer("Player_Attacks"))
        {
            StatusOnHit stats = col.gameObject.GetComponent<StatusOnHit>();
            DamageOnHit dam = col.gameObject.GetComponent<DamageOnHit>();

            if (charging == true)
            {
                StopCoroutine(DoCharge());
                charging = false;
                SR.AddStun(stats.stunDuration);
            }

            /*
            if (stats.applySlow == true)
            {
                SR.AddSlow(stats.slowMultiplier, stats.slowDuration);
            }

            if (stats.applyStun == true && !stunned)
            {
                SR.AddStun(stats.stunDuration);
            }

            if (stats.applyDot == true)
            {
                SR.AddDot(stats.dotDps, stats.dotDuration);
            }
            */
        }
    }

    IEnumerator WaitForOne()
    {
        yield return new WaitForSeconds(1f);
        agent.isStopped = false;
    }

    IEnumerator DoAttack()
    {
        attacking = true;
        onCooldown = true;

        if (Random.value <= 1f)
        {
            UnityEngine.Debug.Log("Storm Clap");
            //Vector3 playerDir = (player.position - transform.position).normalized;
            //Vector3 spawnPosition = transform.position + playerDir;

            //GameObject spawnedHitbox = Instantiate(clapBox, spawnPosition, transform.rotation);

            agent.speed = chargeSpeed;
            clapping = true;

            yield return new WaitForSeconds(2f);
            if (clapping)
            {
                clapping = false;
                agent.speed = normalSpeed;
            }

            yield return new WaitForSeconds(0.3f);

            attacking = false;
        }
        else
        {
            UnityEngine.Debug.Log("Ringing");
            agent.isStopped = true;
            //Vector3 spawnPosition = transform.position;

            //GameObject spawnedHitbox = Instantiate(ringBox, spawnPosition, transform.rotation, gameObject.transform);
            yield return new WaitForSeconds(5f);

            agent.isStopped = false;
            yield return new WaitForSeconds(0.3f);

            attacking = false;
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
        /*
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
        */

        charging = true;
        OnChargeCooldown = true;
        agent.speed = chargeSpeed;
        yield return new WaitForSeconds(chargeDuration);
        agent.speed = normalSpeed;
        charging = false;
        yield return new WaitForSeconds(chargeCooldown);
        OnChargeCooldown = false;
    }
}
