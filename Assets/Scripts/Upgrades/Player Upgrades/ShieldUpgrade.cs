using Game.Level;
using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Shield")]
    public class ShieldUpgrade : PassiveUpgradeEffect
    {
        [Tooltip("Base shield value before level scaling.")]
        public int baseShield = 50;

        [Tooltip("Extra shield gained per player level.")]
        public int shieldPerLevel = 10;

        private Health health;

        public override void OnUnlocked(Player player)
        {
            base.OnUnlocked(player);

            health = player.GetComponent<Health>();
            if (health == null) return;

            // Subscribe to level-up event
            LevelManager.Instance.OnLevelUp += HandleLevelUp;

            ActivateShield();
        }

        public override void OnRemoved(Player player)
        {
            base.OnRemoved(player);

            if (health == null) return;

            // Unsubscribe from event
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelUp -= HandleLevelUp;

            // Deactivate shield
            health.shieldActive = false;
            health.UpdateShieldBarVisibility();
        }

        private void ActivateShield()
        {
            health.shieldBar = UIManager.Instance.GetShieldBar();

            int level = LevelManager.Instance.GetLevel();
            int scaledMaxShield = baseShield + (level * shieldPerLevel);

            health.shieldActive = true;
            health.maxShield = scaledMaxShield;

            health.shieldBar.SetMaxShield(health.maxShield);

            health.ResetHealth(health.maxHealth);
            health.UpdateShieldBarVisibility();
        }

        private void HandleLevelUp(int newLevel)
        {
            if (health == null) return;

            int scaledMaxShield = baseShield + (newLevel * shieldPerLevel);

            // Update max shield but preserve current shield proportion
            float shieldRatio = (float)health.GetShield() / health.maxShield;
            health.maxShield = scaledMaxShield;
            int newShield = Mathf.RoundToInt(health.maxShield * shieldRatio);

            health.shieldBar.SetMaxShield(newShield);
            health.SetHealth(health.GetHealth()); // refresh UI
            health.ResetHealth(health.maxHealth); // optional if you want to refill shield on level-up
            health.UpdateShieldBarVisibility();
        }
    }
}
