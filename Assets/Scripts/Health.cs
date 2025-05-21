using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount, HealthBar healthBar = null)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(0);
            }
            Die();
        }
        else
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }
        }
    }

    void Die()
    {
        // If it's the campfire
        if (gameObject.TryGetComponent(out Campfire campfire))
        {
            campfire.Die();
        }

        // If it's the player
        if (gameObject.TryGetComponent(out Player player))
        {
            player.Die();
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}
