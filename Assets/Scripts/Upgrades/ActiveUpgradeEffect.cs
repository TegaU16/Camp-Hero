using UnityEngine;
using System.Collections;
using Game.Players;

namespace Game.Upgrades
{
    public abstract class ActiveUpgradeEffect : UpgradeEffect
    {
        [Header("Active Upgrade Settings")]
        public float cooldown = 5f;
        public float staminaCost = 0f;
        public KeyCode activationKey;
        public GameObject skillImagePrefab;

        [HideInInspector] public bool isOnCooldown = false;

        public abstract void Activate(Player player);

        public override void OnUnlocked(Player player)
        {
            player.RegisterActiveUpgrade(this);
        }

        public override void OnRemoved(Player player)
        {
            player.UnregisterActiveUpgrade(this);
        }

        public IEnumerator CooldownRoutine()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(cooldown);
            isOnCooldown = false;
        }
    }
}
