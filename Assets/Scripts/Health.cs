using System.Collections;
using Game.AI.Enemies;
using Game.Players;
using Game.Terrain.Structures;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;
    [HideInInspector] public bool isImmune = false;

    [Header("Shield Settings")]
    public int maxShield = 50;
    private int currentShield;
    public float shieldRegenRate = 5f; // amount per second
    public float shieldRegenDelay = 3f; // seconds before regen starts
    private Coroutine regenRoutine;
    private bool canRegen = true;
    public bool canHaveShield;
    [HideInInspector] public bool shieldActive;

    [HideInInspector] public HealthBar healthBar;
    [HideInInspector] public ShieldBar shieldBar;

    public delegate void ThornsDamageDelegate(int currentDamage, BreakableObject breakable);
    public event ThornsDamageDelegate OnHit;

    public delegate int PreDamageDelegate(int incomingDamage);
    public event PreDamageDelegate OnPreDamage;

    public bool IsFull => currentHealth >= maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        currentShield = maxShield;

        // Initialize UI
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        if (shieldBar != null)
            shieldBar.SetMaxShield(maxShield);

        UpdateShieldBarVisibility();
    }

    public void TakeDamage(int amount, Transform attacker = null)
    {
        if (isDead || isImmune) return;

        if (TryGetComponent(out Player player) && attacker != null)
        {
            if (attacker.TryGetComponent(out Enemy enemy) && enemy.breakableObject != null)
                OnHit?.Invoke(amount, enemy.breakableObject);

            if (OnPreDamage != null)
                amount = OnPreDamage.Invoke(amount);
        }

        // Shield absorbs first
        int damageToHealth;

        if (currentShield > 0 && shieldActive)
        {
            int absorbed = Mathf.Min(currentShield, amount);
            currentShield -= absorbed;
            damageToHealth = amount - absorbed;
            if (shieldBar != null)
                shieldBar.SetShield(currentShield);
        }
        else
        {
            damageToHealth = amount;
        }

        // Apply leftover damage to health
        currentHealth = Mathf.Max(currentHealth - damageToHealth, 0);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        // Stop and reset shield regen delay
        if (shieldActive)
        {
            if (regenRoutine != null)
                StopCoroutine(regenRoutine);

            regenRoutine = StartCoroutine(ShieldRegenBuffer());
        }

        if (currentHealth <= 0)
            Die();
    }

    public void AddHealth(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    public void SetHealth(int health)
    {
        currentHealth = health;
        if (healthBar != null)
            healthBar.SetHealth(health);
    }

    public int GetHealth() => currentHealth;
    public int GetShield() => currentShield;

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (TryGetComponent(out Campfire campfire))
            campfire.Die();

        if (TryGetComponent(out PlayerDeath playerDeath))
            playerDeath.Die();
    }

    public void ResetHealth(int maxHealth)
    {
        currentHealth = maxHealth;
        currentShield = maxShield;
        isDead = false;
        UpdateShieldBarVisibility();
    }

    private IEnumerator ShieldRegenBuffer()
    {
        canRegen = false;
        yield return new WaitForSeconds(shieldRegenDelay);
        canRegen = true;
        StartCoroutine(ShieldRegen());
    }

    private IEnumerator ShieldRegen()
    {
        while (canRegen && currentShield < maxShield && !isDead)
        {
            currentShield = Mathf.Min(currentShield + Mathf.CeilToInt(shieldRegenRate * Time.deltaTime), maxShield);
            if (shieldBar != null)
                shieldBar.SetShield(currentShield);
            yield return null;
        }
    }

    public void UpdateShieldBarVisibility()
    {
        if (!canHaveShield) return;

        if (shieldBar == null)
            shieldBar = UIManager.Instance.GetShieldBar();

        shieldBar.gameObject.SetActive(shieldActive);
    }
}
