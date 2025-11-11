using System.Collections;
using UnityEngine;

/// <summary>
/// 小探测范围；进入范围或被攻击后：
/// 1) 以极快速度冲向玩家；
/// 2) 贴在玩家身后一段时间（“吸附”）；
/// 3) 到时后快速脱离，拉开距离；
/// 4) 冷却后回到巡猎状态。
/// 不依赖 NavMesh，纯插值位移，原型期简单稳。
/// </summary>
[DisallowMultipleComponent]
public class EnemyLatchAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    [Header("Detection")]
    public float detectRange = 6f;          // 侦测半径（较小）
    public bool aggroOnHit = true;          // 允许被攻击时激活
    volatile bool externalAggro = false;    // 被外部脚本触发（比如子弹命中）

    [Header("Rush (朝玩家冲刺)")]
    public float rushSpeed = 18f;           // 冲刺速度（很快）
    public float rushStopDistance = 1.2f;   // 接近到这个距离就算“碰到了”

    [Header("Latch (贴背阶段)")]
    public float latchDuration = 1.6f;      // 贴背持续时间
    public float behindOffset = 0.8f;       // 距玩家身后多远（米）
    public float latchStickStrength = 16f;  // 贴背跟随插值强度（越大越“吸”）
    public float rotateFollowSpeed = 720f;  // 贴背时跟随玩家朝向的转速（度/秒）

    [Header("Disengage (脱离阶段)")]
    public float disengageSpeed = 20f;      // 脱离速度（快速离开）
    public float disengageDuration = 0.45f; // 脱离时长
    public float disengageSideJitter = 0.5f;// 脱离时左右随机偏移（避免每次同一路径）

    [Header("Cooldown")]
    public float cooldownAfterDisengage = 1.0f;

    [Header("Grounding (可选)")]
    public bool alignToGround = true;
    public LayerMask groundMask = ~0;
    public float groundRayLength = 3f;

    enum State { Idle, Hunting, Rush, Latch, Disengage, Cooldown }
    State state = State.Idle;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
        StartCoroutine(FSM());
    }

    public void NotifyAggro()   // 供外部（被打中）调用
    {
        if (aggroOnHit) externalAggro = true;
    }

    IEnumerator FSM()
    {
        state = State.Hunting;

        while (true)
        {
            if (!player) { yield return null; continue; }

            switch (state)
            {
                case State.Hunting:
                {
                    // 等待玩家进入探测圈，或外部激怒
                    float d = DistXZ(transform.position, player.position);
                    if (d <= detectRange || externalAggro)
                    {
                        externalAggro = false;
                        state = State.Rush;
                    }
                    yield return null;
                    break;
                }

                case State.Rush:
                {
                    // 极快冲向玩家，直到“接触”
                    while (true)
                    {
                        Vector3 dir = DirXZ(transform.position, player.position);
                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            // 位移
                            transform.position += dir * rushSpeed * Time.deltaTime;
                            // 面向玩家
                            transform.rotation = Quaternion.RotateTowards(
                                transform.rotation,
                                Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z), Vector3.up),
                                rotateFollowSpeed * Time.deltaTime
                            );
                        }

                        // 贴地（可选）
                        GroundSnap();

                        // 接触判定
                        float d = DistXZ(transform.position, player.position);
                        if (d <= rushStopDistance) break;

                        yield return null;
                        if (!player) break;
                    }

                    state = State.Latch;
                    break;
                }

                case State.Latch:
                {
                    // 吸附在玩家身后：位置 = 玩家位置 - 玩家forward * offset（平滑插值）
                    float t = 0f;
                    while (t < latchDuration)
                    {
                        t += Time.deltaTime;
                        if (!player) break;

                        Vector3 targetPos = player.position - player.forward * behindOffset;
                        // 仅在水平面跟随，保留当前高度（或直接使用 targetPos.y）
                        targetPos.y = transform.position.y;

                        transform.position = Vector3.Lerp(
                            transform.position, targetPos, 1f - Mathf.Exp(-latchStickStrength * Time.deltaTime));

                        // 面向与玩家相同方向
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(new Vector3(player.forward.x, 0, player.forward.z), Vector3.up),
                            rotateFollowSpeed * Time.deltaTime
                        );

                        GroundSnap();
                        yield return null;
                    }

                    state = State.Disengage;
                    break;
                }

                case State.Disengage:
                {
                    // 快速离开：沿玩家的反向 + 轻微左右随机
                    Vector3 baseDir = -player.forward;
                    Vector3 side = Random.value < 0.5f ? Vector3.left : Vector3.right;
                    Vector3 dir = (baseDir + side * disengageSideJitter).normalized;

                    float t = 0f;
                    while (t < disengageDuration)
                    {
                        t += Time.deltaTime;
                        transform.position += new Vector3(dir.x, 0, dir.z) * disengageSpeed * Time.deltaTime;

                        // 背向玩家（可选）
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z), Vector3.up),
                            rotateFollowSpeed * Time.deltaTime
                        );

                        GroundSnap();
                        yield return null;
                    }

                    state = State.Cooldown;
                    break;
                }

                case State.Cooldown:
                {
                    float t = 0f;
                    while (t < cooldownAfterDisengage) { t += Time.deltaTime; yield return null; }
                    state = State.Hunting;
                    break;
                }
            }
        }
    }

    // —— 工具 —— //
    float DistXZ(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    Vector3 DirXZ(Vector3 from, Vector3 to)
    {
        Vector3 v = (to - from);
        v.y = 0f;
        return v.sqrMagnitude < 0.0001f ? Vector3.zero : v.normalized;
    }

    void GroundSnap()
    {
        if (!alignToGround) return;
        Vector3 probe = transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(probe, Vector3.down, out var hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rushStopDistance);
    }
}
