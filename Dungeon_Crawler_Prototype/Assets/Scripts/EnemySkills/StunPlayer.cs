using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put this on the Player root. Call ApplyStun(seconds) to stun the player:
/// - Disables listed behaviours (e.g., Player_Movement, attack scripts)
/// - Invokes events for VFX/animation
/// - Supports re-stun: extends remaining time if a longer/new stun is applied
/// </summary>
public class StunReceiver : MonoBehaviour
{
    [Header("What to disable during stun")]
    [Tooltip("Scripts to disable while stunned (e.g., Player_Movement, PlayerAttack, etc.).")]
    public List<MonoBehaviour> behavioursToDisable = new List<MonoBehaviour>();

    [Header("Events")]
    public UnityEvent onStunStart;
    public UnityEvent onStunEnd;

    public bool IsStunned { get; private set; }
    float _stunRemain = 0f;
    Coroutine _co;

    /// <summary>Public entry to apply/refresh stun.</summary>
    public void ApplyStun(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        if (seconds <= 0f) return;

        // If already stunned, extend remaining time to the longer one
        if (IsStunned)
        {
            _stunRemain = Mathf.Max(_stunRemain, seconds);
            return;
        }

        _co = StartCoroutine(Co_Stun(seconds));
    }

    IEnumerator Co_Stun(float seconds)
    {
        IsStunned = true;
        _stunRemain = seconds;

        // Disable targeted behaviours
        SetBehavioursEnabled(false);
        onStunStart?.Invoke();

        // Hold
        while (_stunRemain > 0f)
        {
            _stunRemain -= Time.deltaTime;
            yield return null;
        }

        // Recover
        SetBehavioursEnabled(true);
        onStunEnd?.Invoke();
        IsStunned = false;
        _co = null;
    }

    void SetBehavioursEnabled(bool enabled)
    {
        foreach (var b in behavioursToDisable)
        {
            if (!b) continue;
            b.enabled = enabled;
        }
    }
}
