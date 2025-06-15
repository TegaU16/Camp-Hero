using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        ResetHealth(maxHealth);
    }

    public void TakeDamage(int amount, HealthBar healthBar = null)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0) Die();
    }

    public void AddHealth(int amount, HealthBar healthBar = null)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    public void SetHealth(int health, HealthBar healthBar)
    {
        currentHealth = health;
        healthBar.SetHealth(health);
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (gameObject.TryGetComponent(out Campfire campfire))
        {
            campfire.Die();
        }

        if (gameObject.TryGetComponent(out Player player))
        {
            player.Die();
        }
    }

    public void ResetHealth(int maxHealth)
    {
        currentHealth = maxHealth;
        isDead = false;
    }
}
