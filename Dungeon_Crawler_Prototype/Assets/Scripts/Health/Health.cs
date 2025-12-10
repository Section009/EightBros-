using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Numeric-health component with optional UI and damage numbers.
/// NEW: Pipes incoming damage through DamageModifier (if present) so other
/// systems can scale/zero damage (e.g., 50% during heavy attack).
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    [Tooltip("Current hit points (initialized to maxHealth on Awake).")]
    public int currentHealth;

    [Header("Events")]
    public UnityEvent onDie;

    [Header("Optional UI")]
    public HealthBarUI healthBar;

    [Header("Optional Damage Numbers")]
    public bool display_damage_numbers = false;
    public GameObject Canvas;
    public GameObject Numbers;

    void Awake()
    {
        // Initialize HP
        currentHealth = maxHealth;
        if (currentHealth <= 0) currentHealth = maxHealth;

        // Auto-find a HealthBarUI in children if not assigned
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBarUI>(true);

        if (healthBar) healthBar.Set(currentHealth, maxHealth);
    }

    /// <summary>
    /// Apply incoming damage. If a DamageModifier component is attached to this
    /// GameObject, the raw damage will be scaled via DamageModifier.Apply(...).
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        // --- NEW: scale incoming damage via DamageModifier if present ---
        var mod = GetComponent<DamageModifier>();   // optional component
        if (mod != null)
            amount = mod.Apply(amount);

        // Ignore non-positive after scaling
        if (amount <= 0) return;

        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - amount);

        // Update UI
        if (healthBar) healthBar.Set(currentHealth, maxHealth);

        // Optional floating numbers
        if (display_damage_numbers && Canvas && Numbers)
        {
            var go = Instantiate(Numbers, Canvas.transform);
            var txt = go.GetComponent<Text>();
            if (txt) txt.text = amount.ToString();
        }

        // Death
        if (currentHealth == 0)
        {
            onDie?.Invoke();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Heal this entity by amount (clamped at maxHealth). No effect if dead.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthBar) healthBar.Set(currentHealth, maxHealth);
    }
}
