using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyJumpAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    private Transform player;

    [Header("Ranges")]

    public float meleeRange = 2.0f;

    public float exitBufferTime = 0.6f;

    [Header("Hop Motion")]

    public float hopDistance = 3.5f;

    public float hopDuration = 0.45f;

    public float arcHeight = 1.2f;

    public float hopCooldown = 0.15f;

    public float turnSpeed = 720f;

    [Header("Grounding (optional)")]

    public bool alignToGroundOnLand = true;
    public LayerMask groundMask = ~0;
    public float groundRayLength = 3f;

    [Header("Events (optional)")]
    public ParticleSystem hopVfx; // 起跳特效
    public AudioSource hopSfx;    // 起跳音效

    private bool _inMeleeHold;
    private bool _isHopping;
    private float _exitTimer;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
        else Debug.LogWarning("[EnemyJumpAI] Can't find Player Tag");

        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        // 主循环：近战内等待、近战外跳跃
        while (true)
        {
            if (!player)
            {
                yield return null;
                continue;
            }

            float dist = Vector3.Distance(Flat(transform.position), Flat(player.position));

            if (dist <= meleeRange)
            {
                // 进入近战范围，停止跳跃，等待玩家离开一定时间
                _inMeleeHold = true;
                _exitTimer = 0f;

                while (_inMeleeHold)
                {
                    yield return null;
                    if (!player) break;

                    float d = Vector3.Distance(Flat(transform.position), Flat(player.position));
                    if (d > meleeRange)
                    {
                        _exitTimer += Time.deltaTime;
                        if (_exitTimer >= exitBufferTime)
                        {
                            _inMeleeHold = false; // 重新允许跳跃
                        }
                    }
                    else
                    {
                        _exitTimer = 0f; // 玩家又靠近了，重置计时
                    }
                }
            }
            else
            {
                // 近战外：执行一次跳跃
                if (!_isHopping)
                {
                    yield return StartCoroutine(DoOneHop());
                    yield return new WaitForSeconds(hopCooldown);
                }
                else yield return null;
            }
        }
    }

    IEnumerator DoOneHop()
    {
        if (!player) yield break;

        _isHopping = true;

        // 计算此次跳跃的目标点（水平面上）：
        Vector3 origin = transform.position;
        Vector3 toPlayer = Flat(player.position) - Flat(origin);
        float planarDist = toPlayer.magnitude;
        if (planarDist < 0.001f)
        {
            _isHopping = false;
            yield break;
        }
        Vector3 dir = toPlayer / planarDist;

        // 不要超过 hopDistance；若到玩家只剩更近，则跳到更近的点（避免穿过）
        float step = Mathf.Min(hopDistance, Mathf.Max(0f, planarDist - meleeRange));
        Vector3 landPos = new Vector3(origin.x, origin.y, origin.z) + dir * step;

        // 起跳朝向玩家
        RotateTowards(dir);

        // 触发起跳特效/音效
        if (hopVfx) hopVfx.Play();
        if (hopSfx) hopSfx.Play();

        // 抛物线插值：y = 起点y + parabola(0..1)*arcHeight
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, hopDuration);
            float clamped = Mathf.Clamp01(t);

            // 水平线性插值
            Vector3 pos = Vector3.Lerp(origin, landPos, clamped);

            // 垂直抛物线：4h * (t - t^2) 形成 0→峰→0 的弧度
            float height = 4f * arcHeight * clamped * (1f - clamped);
            pos.y = Mathf.Lerp(origin.y, landPos.y, clamped) + height;

            transform.position = pos;

            // 持续朝向玩家（可选：保持朝向一次即可）
            RotateTowards(dir);

            yield return null;
        }

        // 贴地（可选）
        if (alignToGroundOnLand)
        {
            if (Physics.Raycast(landPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            {
                landPos.y = hit.point.y;
            }
            transform.position = landPos;
        }

        _isHopping = false;
    }

    private void RotateTowards(Vector3 planarDir)
    {
        if (planarDir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(planarDir.x, 0f, planarDir.z), Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
