using UnityEngine;

/// <summary>
/// Attach this to projectile prefabs. When the projectile hits a target,
/// it applies the configured status effects (Slow / Stun / DoT) to a
/// StatusReceiver found on the hit object or its parents.
/// This script does NOT deal direct damage; it only applies statuses.
/// </summary>
public class StatusOnHit : MonoBehaviour
{
    [Header("Target Filter")]
    [Tooltip("Only apply effects to objects with this tag. Leave empty to ignore tag filtering.")]
    public string targetTag = "Enemy";

    [Tooltip("Layers considered valid targets. Projectiles will only apply effects to these layers.")]
    public LayerMask targetLayers = ~0;

    [Header("Apply Slow")]
    [Tooltip("If true, apply a slow on hit.")]
    public bool applySlow = false;

    [Tooltip("Speed multiplier in 0..1 (smaller = slower). For example, 0.6 means -40% speed.")]
    public float slowMultiplier = 0.6f;

    [Tooltip("How long the slow lasts, in seconds.")]
    public float slowDuration = 2.0f;

    [Header("Apply Stun")]
    [Tooltip("If true, apply a stun on hit.")]
    public bool applyStun = false;

    [Tooltip("How long the stun lasts, in seconds.")]
    public float stunDuration = 1.0f;

    [Header("Apply Damage-over-Time (DoT)")]
    [Tooltip("If true, apply a DoT on hit.")]
    public bool applyDot = false;

    [Tooltip("Damage per second for the DoT.")]
    public float dotDps = 5f;

    [Tooltip("How long the DoT lasts, in seconds.")]
    public float dotDuration = 3.0f;

    [Tooltip("Seconds between DoT ticks. If <= 0, uses StatusReceiver's default.")]
    public float dotTickRate = -1f;

    [Header("Projectile Lifetime")]
    [Tooltip("If true, destroy this projectile after applying the effects once.")]
    public bool destroyAfterApply = true;

    // --- Hit detection hooks (supports both trigger and collision) ---

    void OnTriggerEnter(Collider other)
    {
        TryApply(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryApply(collision.collider.gameObject);
    }

    /// <summary>
    /// Tries to apply configured status effects to the given GameObject.
    /// </summary>
    void TryApply(GameObject hit)
    {
        if (!IsTarget(hit))
            return;

        // Find a StatusReceiver on the hit object or its parents.
        var receiver = hit.GetComponentInParent<StatusReceiver>();
        if (!receiver)
            return;

        if (applySlow)
            receiver.AddSlow(slowMultiplier, slowDuration);

        if (applyStun)
            receiver.AddStun(stunDuration);

        if (applyDot)
            receiver.AddDot(dotDps, dotDuration, dotTickRate);

        if (destroyAfterApply)
            Destroy(gameObject);
    }

    /// <summary>
    /// Returns true if the hit object passes the tag and layer filters.
    /// </summary>
    bool IsTarget(GameObject go)
    {
        // Layer check
        if (((1 << go.layer) & targetLayers.value) == 0)
            return false;

        // Tag check (if provided)
        if (!string.IsNullOrEmpty(targetTag) && !go.CompareTag(targetTag))
            return false;

        return true;
    }
}
