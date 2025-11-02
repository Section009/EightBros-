using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Events")]
    public UnityEvent onDie;

    [Header("Optional UI")]
    public HealthBarUI healthBar;

    void Awake()
    {
        currentHealth = maxHealth;
        if (healthBar) healthBar.Set(currentHealth, maxHealth);
        if (currentHealth <= 0) currentHealth = maxHealth;
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBarUI>(true); // 自动找子物体
        if (healthBar) healthBar.Set(currentHealth, maxHealth);

    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (healthBar) healthBar.Set(currentHealth, maxHealth);
        if (currentHealth == 0)
        {
            onDie?.Invoke();
            Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthBar) healthBar.Set(currentHealth, maxHealth);
    }
}
