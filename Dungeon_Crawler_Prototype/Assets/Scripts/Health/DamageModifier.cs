using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic incoming-damage scaler.
/// - Attach to any entity that can receive damage (same GameObject as Health/FourHitHealth).
/// - Other scripts can set named multipliers (e.g., "HEAVY_DEFENSE" = 0.5f).
/// - Effective multiplier = min(all active named multipliers), clamped to [0,1].
/// </summary>
[DisallowMultipleComponent]
public class DamageModifier : MonoBehaviour
{
    private readonly Dictionary<string, float> _named = new Dictionary<string, float>();

    /// <summary>Set/replace a named multiplier. 1=normal, 0.5=half damage, 0=immune.</summary>
    public void SetExternalMultiplier(string key, float factor)
    {
        if (string.IsNullOrEmpty(key)) return;
        _named[key] = Mathf.Clamp01(factor);
    }

    /// <summary>Remove a named multiplier (stop affecting damage).</summary>
    public void ClearExternalMultiplier(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        _named.Remove(key);
    }

    /// <summary>Compute the effective incoming-damage multiplier.</summary>
    public float CurrentMultiplier()
    {
        float m = 1f;
        foreach (var kv in _named)
            m = Mathf.Min(m, kv.Value);
        return Mathf.Clamp01(m);
    }

    /// <summary>Apply to a raw integer damage value, ceil to int (so small hits still matter).</summary>
    public int Apply(int raw)
    {
        float mul = CurrentMultiplier();
        if (mul >= 0.999f) return raw;
        if (mul <= 0f) return 0;
        return Mathf.Max( (int)Mathf.Ceil(raw * mul), 0 );
    }
}
