using UnityEngine;

/// <summary>
/// Light attack: instant fan-shaped hit fired when the SHORT pause starts
/// (EnemyAI.onPauseAttackBegin AND useHeavyAttackPause == false).
/// On hit: deals damage and stuns the player by calling Player_Movement.Stun_Player(stunDuration).
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class FanConeAttack : MonoBehaviour
{
    [Header("Target Filter")]
    public LayerMask targetLayers = ~0;
    public string targetTag = "Player";

    [Header("Fan Shape")]
    public float radius = 2.5f;
    [Range(0f, 180f)] public float halfAngleDeg = 60f;
    public float verticalTolerance = 1.8f;

    [Header("Damage")]
    public int damage = 15;

    [Header("Stun")]
    [Tooltip("Apply stun to the player on hit (disables input via Player_Movement.Stun_Player).")]
    public bool applyStun = true;
    [Tooltip("Stun duration (seconds).")]
    public float stunDuration = 0.75f;

    [Header("Debug")]
    public bool debugGizmos = false;

    private EnemyAI ai;

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
        ai.onPauseAttackBegin.AddListener(OnPauseBegin);
        ai.onPauseAttackEnd.AddListener(OnPauseEnd);
    }

    void OnDestroy()
    {
        if (ai != null)
        {
            ai.onPauseAttackBegin.RemoveListener(OnPauseBegin);
            ai.onPauseAttackEnd.RemoveListener(OnPauseEnd);
        }
    }

    void OnPauseBegin()
    {
        // Only fire on SHORT pause (light attack mode)
        if (ai.useHeavyAttackPause) return;

        Vector3 fwd = transform.forward; 
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;

        Collider[] cols = Physics.OverlapSphere(transform.position, radius, targetLayers, QueryTriggerInteraction.Ignore);
        foreach (var col in cols)
        {
            if (!col) continue;
            if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag)) continue;

            Transform t = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;
            Vector3 to = (t.position - transform.position);
            float dy = Mathf.Abs(to.y);
            to.y = 0f;

            if (dy > verticalTolerance) continue;
            if (to.sqrMagnitude < 1e-4f) continue;

            float ang = Vector3.Angle(fwd, to);
            if (ang <= halfAngleDeg)
            {
                ApplyHit(t.gameObject);
            }
        }
    }

    void OnPauseEnd() { /* for VFX cleanup if needed */ }

    void ApplyHit(GameObject target)
    {
        // 1) Damage
        var hp = target.GetComponentInParent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(Mathf.Max(1, damage));
        }
        else
        {
            var four = target.GetComponentInParent<FourHitHealth>();
            if (four != null) four.RegisterHit();
        }

        // 2) Stun
        if (applyStun && stunDuration > 0f)
        {
            var pm = target.GetComponentInParent<Player_Movement>();
            if (pm != null)
            {
                pm.Stun_Player(stunDuration);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        Gizmos.color = Color.red;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 fwd = transform.forward;

        int rays = 16;
        for (int i = 0; i <= rays; i++)
        {
            float t = i / (float)rays;
            float ang = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
            Gizmos.DrawLine(origin, origin + dir.normalized * radius);
        }
    }
}
