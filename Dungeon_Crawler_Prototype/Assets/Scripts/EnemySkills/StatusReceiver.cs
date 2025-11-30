using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// StatusReceiver
/// Receives and updates status effects on an actor.
/// Existing features (kept as-is):
/// - Slow: keeps the STRONGEST (lowest multiplier); same-strength refreshes duration
/// - Stun: uses the maximum remaining time; pauses NavMeshAgent while stunned
/// - DoT : multiple instances can run in parallel; each uses its own DPS/duration/tick
/// - Mark system (for "Xiao Bian" design):
///   * Mark() : apply a mark if not currently in lockout
///   * IsMarked(), InLockout() : query states
///   * ConsumeMarkAndLock(sec) : clear mark, start lockout so player can't be marked again for 'sec'
///   * ClearMark(), ForceMarkLock(sec) helpers
///   * Exposes UnityEvents for VFX/SFX hooks
/// </summary>
public class StatusReceiver : MonoBehaviour
{
    // ========================== Slow ==========================
    [Header("Slow")]
    [Tooltip("Lower bound of speed multiplier to avoid zero-speed (e.g., 0.1 = at most 90% slow).")]
    public float minSpeedMultiplier = 0.1f;

    // =========================== DoT ==========================
    [Header("DOT")]
    [Tooltip("Global default tick rate (seconds) if a DoT instance does not specify its own.")]
    public float defaultDotTickRate = 0.5f;

    [Tooltip("If true, DoT ticks will ignore any external invulnerability gates (not used here).")]
    public bool dotIgnoresInvulnerability = false; // reserved for future; unused here

    [Tooltip("If true, this actor is completely immune to DoT (ignores new DoTs and clears existing ones).")]
    public bool immuneToDot = false;

    // ========================= Mark =========================
    [Header("Mark (for Xiao Bian)")]
    [Tooltip("If the actor is currently marked.")]
    [SerializeField] private bool marked = false;

    [Tooltip("Remaining lockout seconds during which Mark() calls are ignored.")]
    [SerializeField] private float markLockoutRemain = 0f;

    [Tooltip("Optional default lockout if you call ConsumeMarkAndLock with <= 0.")]
    public float defaultMarkLockout = 3f;

    [Header("Mark Events")]
    public UnityEvent onMarkApplied;
    public UnityEvent onMarkConsumed;
    public UnityEvent onMarkCleared;
    public UnityEvent onMarkLockoutStart;
    public UnityEvent onMarkLockoutEnd;

    // ---------------- Internals / references ----------------
    NavMeshAgent agent;
    float baseSpeed;
    bool hasAgent;

    Health health;
    FourHitHealth fourHit;

    // Slow pool: track all, apply the most severe (smallest multiplier)
    class SlowEntry { public float multiplier; public float remain; }
    readonly List<SlowEntry> slows = new List<SlowEntry>();

    // Stun: single timer stores the maximum remaining time among concurrent sources
    float stunRemain = 0f;

    // DoT: parallel list; each DoT instance has its own timers and values
    class DotEntry { public float dps; public float remain; public float tickRate; public float tickTimer; }
    readonly List<DotEntry> dots = new List<DotEntry>();

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hasAgent = agent != null;
        if (hasAgent) baseSpeed = agent.speed;

        health  = GetComponent<Health>();
        fourHit = GetComponent<FourHitHealth>();
    }

    void OnEnable()
    {
        if (hasAgent) baseSpeed = agent.speed;
        StartCoroutine(StatusLoop());
    }

    IEnumerator StatusLoop()
    {
        var wait = new WaitForEndOfFrame();
        while (true)
        {
            float dt = Time.deltaTime;

            // -------- Stun update --------
            if (stunRemain > 0f)
            {
                stunRemain -= dt;
                if (stunRemain <= 0f)
                {
                    stunRemain = 0f;
                    SetStun(false);
                }
            }

            // -------- Slow update --------
            for (int i = slows.Count - 1; i >= 0; --i)
            {
                slows[i].remain -= dt;
                if (slows[i].remain <= 0f) slows.RemoveAt(i);
            }
            ApplyBestSlow();

            // -------- DoT update --------
            if (immuneToDot)
            {
                if (dots.Count > 0) dots.Clear(); // allow runtime toggle
            }
            else
            {
                for (int i = dots.Count - 1; i >= 0; --i)
                {
                    var e = dots[i];
                    e.remain -= dt;
                    e.tickTimer -= dt;

                    if (e.remain <= 0f)
                    {
                        dots.RemoveAt(i);
                        continue;
                    }

                    float tr = (e.tickRate > 0f ? e.tickRate : defaultDotTickRate);
                    while (e.tickTimer <= 0f && e.remain > 0f)
                    {
                        e.tickTimer += tr;
                        DoDotTick(e);
                    }
                }
            }

            // -------- Mark update (lockout countdown) --------
            if (markLockoutRemain > 0f)
            {
                markLockoutRemain -= dt;
                if (markLockoutRemain <= 0f)
                {
                    markLockoutRemain = 0f;
                    onMarkLockoutEnd?.Invoke();
                }
            }

            yield return wait;
        }
    }

    void DoDotTick(DotEntry e)
    {
        if (immuneToDot) return;

        // damage per tick = dps * tickRate
        float tickSecs = (e.tickRate > 0f ? e.tickRate : defaultDotTickRate);
        float dmgFloat = e.dps * tickSecs;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(dmgFloat));

        if (health != null)
        {
            health.TakeDamage(dmg);
        }
        else if (fourHit != null)
        {
            fourHit.RegisterHit();
        }
    }

    void SetStun(bool on)
    {
        if (!hasAgent) return;
        agent.isStopped = on;
    }

    void ApplyBestSlow()
    {
        if (!hasAgent) return;

        // While stunned, keep agent stopped and don't touch speed.
        if (stunRemain > 0f)
        {
            agent.isStopped = true;
            return;
        }

        float bestMul = 1f;
        for (int i = 0; i < slows.Count; i++)
            bestMul = Mathf.Min(bestMul, slows[i].multiplier);

        bestMul = Mathf.Clamp(bestMul, minSpeedMultiplier, 1f);
        agent.speed = baseSpeed * bestMul;
        agent.isStopped = false;
    }

    // ===================== Public API: Slow / Stun / DoT =====================

    /// <summary>Apply a slow (0..1, smaller = slower) for 'duration' seconds.</summary>
    public void AddSlow(float multiplier, float duration)
    {
        multiplier = Mathf.Clamp(multiplier, 0f, 1f);
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f) return;

        for (int i = 0; i < slows.Count; i++)
        {
            if (Mathf.Approximately(slows[i].multiplier, multiplier))
            {
                slows[i].remain = Mathf.Max(slows[i].remain, duration);
                ApplyBestSlow();
                return;
            }
        }
        slows.Add(new SlowEntry { multiplier = multiplier, remain = duration });
        ApplyBestSlow();
    }

    /// <summary>Apply stun for 'duration' seconds; overlapping stuns use the maximum remaining time.</summary>
    public void AddStun(float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f) return;

        if (stunRemain <= 0f)
        {
            stunRemain = duration;
            SetStun(true);
        }
        else
        {
            stunRemain = Mathf.Max(stunRemain, duration);
            SetStun(true);
        }
    }

    /// <summary>Add a DoT instance (dps, duration, optional tickRate). Ignored if 'immuneToDot' is true.</summary>
    public void AddDot(float dps, float duration, float tickRate = -1f)
    {
        if (immuneToDot) return;
        if (dps <= 0f || duration <= 0f) return;

        dots.Add(new DotEntry
        {
            dps = dps,
            remain = duration,
            tickRate = (tickRate > 0f ? tickRate : defaultDotTickRate),
            tickTimer = 0f
        });
    }

    /// <summary>Toggle DoT immunity at runtime. When enabling, clears all existing DoTs.</summary>
    public void SetDotImmunity(bool enabled)
    {
        immuneToDot = enabled;
        if (immuneToDot && dots.Count > 0) dots.Clear();
    }

    // ===================== Public API: Mark =====================

    /// <summary>Returns true if currently marked.</summary>
    public bool IsMarked() => marked;

    /// <summary>Returns true if currently in lockout (Mark() will be ignored).</summary>
    public bool InLockout() => markLockoutRemain > 0f;

    /// <summary>
    /// Apply a mark. Ignored if in lockout or already marked.
    /// Use this from projectiles like "Mark bullet".
    /// </summary>
    public void Mark()
    {
        if (InLockout() || marked) return;
        marked = true;
        onMarkApplied?.Invoke();
    }

    /// <summary>
    /// Consume a mark (if any) and start lockout, so Mark() cannot apply for 'lockSeconds'.
    /// Returns true if a mark was consumed.
    /// </summary>
    public bool ConsumeMarkAndLock(float lockSeconds)
    {
        if (!marked) return false;

        marked = false;
        onMarkConsumed?.Invoke();

        float sec = (lockSeconds > 0f ? lockSeconds : defaultMarkLockout);
        markLockoutRemain = sec;
        onMarkLockoutStart?.Invoke();
        return true;
    }

    /// <summary>Clear mark without starting lockout (utility for resets).</summary>
    public void ClearMark()
    {
        if (!marked) return;
        marked = false;
        onMarkCleared?.Invoke();
    }

    /// <summary>Force lockout for 'sec' seconds (even if not consuming a mark).</summary>
    public void ForceMarkLock(float sec)
    {
        sec = Mathf.Max(0f, sec);
        if (sec <= 0f) return;
        markLockoutRemain = sec;
        onMarkLockoutStart?.Invoke();
    }
}
