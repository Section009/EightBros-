using System.Collections;
using UnityEngine;

/// <summary>
/// Sonic aura that runs ONLY during the HEAVY pause window:
/// - On heavy pause BEGIN: enable invulnerability, start ticking AoE damage each second.
/// - If the enemy takes damage at any time during this heavy-pause window,
///   the aura damage becomes x2 for the REMAINDER of this window.
/// - On heavy pause END: stop aura and disable invulnerability.
/// 
/// Invulnerability options:
/// - If an InvulnerabilityToggle component is present, we use it (recommended).
/// - Otherwise (optional) you can switch the entire hierarchy to a specific layer
///   and ensure projectiles/melee ignore that layer in LayerMask.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class SonicAuraAttack : MonoBehaviour
{
    [Header("Target Filter")]
    [Tooltip("Layers to check for targets (e.g., Player).")]
    public LayerMask targetLayers = ~0;

    [Tooltip("Required tag on the target. Leave empty to ignore tag check.")]
    public string targetTag = "Player";

    [Header("Aura Damage")]
    [Tooltip("Aura radius (meters).")]
    public float radius = 3.0f;

    [Tooltip("Base damage per second (will be doubled if the enemy is hit during the heavy pause).")]
    public int baseDps = 5;

    [Tooltip("How often to apply damage ticks, in seconds.")]
    public float tickRate = 1.0f;

    [Header("Invulnerability")]
    [Tooltip("Try to use InvulnerabilityToggle if available; it's the safest option.")]
    public bool preferToggle = true;

    [Tooltip("If no toggle is found and this is enabled, switch to a layer during aura.")]
    public bool fallbackSwitchLayer = false;

    [Tooltip("Layer name used while invulnerable (for fallback). Ensure weapons ignore this layer.")]
    public string invulnerableLayerName = "Invulnerable";

    [Header("Debug")]
    [Tooltip("Draw gizmos for the aura radius when selected.")]
    public bool debugGizmos = false;

    EnemyAI ai;
    InvulnerabilityToggle toggle; // optional
    bool auraActive = false;
    bool tookDamageThisWindow = false;
    int originalLayer = -1;

    // Health snapshot to detect incoming damage (works with both Health and FourHitHealth)
    Health hp; int lastHP = -1;
    FourHitHealth four; int lastSeg = -1;

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
        toggle = GetComponent<InvulnerabilityToggle>();
        hp = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();

        ai.onPauseAttackBegin.AddListener(OnPauseBegin);
        ai.onPauseAttackEnd.AddListener(OnPauseEnd);
    }

    void OnDestroy()
    {
        ai.onPauseAttackBegin.RemoveListener(OnPauseBegin);
        ai.onPauseAttackEnd.RemoveListener(OnPauseEnd);
    }

    void OnPauseBegin()
    {
        // Only react on HEAVY pause (long pause)
        if (!ai.useHeavyAttackPause) return;

        tookDamageThisWindow = false;
        auraActive = true;

        // open invulnerability
        EnableInvulnerability();

        // prime snapshots for damage detection
        if (hp) lastHP = hp.currentHealth;
        if (four) lastSeg = four.GetState().current;

        StartCoroutine(AuraLoop());
    }

    void OnPauseEnd()
    {
        if (!auraActive) return;
        auraActive = false;

        // close invulnerability
        DisableInvulnerability();
    }

    IEnumerator AuraLoop()
    {
        float timer = 0f;

        while (auraActive)
        {
            // Damage detection: if enemy got hit at any time during the aura, set the double flag.
            if (hp && hp.currentHealth < lastHP) tookDamageThisWindow = true;
            if (four && four.GetState().current < lastSeg) tookDamageThisWindow = true;

            if (hp) lastHP = hp.currentHealth;
            if (four) lastSeg = four.GetState().current;

            timer += Time.deltaTime;
            if (timer >= tickRate)
            {
                timer = 0f;
                int dps = tookDamageThisWindow ? baseDps * 2 : baseDps;
                ApplyAuraTick(dps);
            }

            yield return null;
        }
    }

    void ApplyAuraTick(int dps)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            if (!col) continue;
            if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag)) continue;

            GameObject tgt = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;

            // Deal damage = DPS * tickRate (integer)
            int dmg = Mathf.Max(1, Mathf.RoundToInt(dps * Mathf.Max(0.01f, tickRate)));

            var targetHp = tgt.GetComponentInParent<Health>();
            if (targetHp != null)
            {
                targetHp.TakeDamage(dmg);
            }
            else
            {
                var targetFour = tgt.GetComponentInParent<FourHitHealth>();
                if (targetFour != null)
                {
                    // Treat each tick as one 'hit'
                    targetFour.RegisterHit();
                }
            }
        }
    }

    void EnableInvulnerability()
    {
        if (preferToggle && toggle != null)
        {
            toggle.EnableInvulnerability();
            return;
        }

        if (fallbackSwitchLayer)
        {
            int invulnLayer = LayerMask.NameToLayer(invulnerableLayerName);
            if (invulnLayer >= 0)
            {
                // Switch the entire hierarchy
                originalLayer = gameObject.layer;
                foreach (var t in GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = invulnLayer;
            }
            else
            {
                Debug.LogWarning($"[SonicAuraAttack] Layer '{invulnerableLayerName}' not found. " +
                                 $"Create it in Project Settings > Tags and Layers or add InvulnerabilityToggle.");
            }
        }
    }

    void DisableInvulnerability()
    {
        if (preferToggle && toggle != null)
        {
            toggle.DisableInvulnerability();
            return;
        }

        if (fallbackSwitchLayer && originalLayer >= 0)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = originalLayer;
            originalLayer = -1;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
