using System.Collections;
using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Regen To Half")]
    public class RegenToHalfUpgrade : PassiveUpgradeEffect
    {
        public float regenRate;

        private Health health;
        private Coroutine updateRoutine;

        public override void OnUnlocked(Player player)
        {
            health = player.GetComponent<Health>();
            if (health == null) return;

            updateRoutine = player.StartCoroutine(RegenerateHealth());
        }

        public override void OnRemoved(Player player)
        {
            if (updateRoutine != null)
                player.StopCoroutine(updateRoutine);
        }

        private IEnumerator RegenerateHealth()
        {
            while (true)
            {
                if (health.GetHealth() <= health.maxHealth / 2f)
                    health.AddHealth((int)(regenRate * Time.deltaTime));

                yield return null; // Updates every frame
            }
        }
    }
}
