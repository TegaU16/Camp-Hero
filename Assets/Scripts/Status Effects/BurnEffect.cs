using System.Collections;
using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    [CreateAssetMenu(menuName = "Combat/Status Effects/Burn")]
    public class BurnEffect : StatusEffect
    {
        public float tickSpeed = 1f;
        public float damagePerTick = 2f;

        public override void Apply(Enemy target)
        {
            target.StartCoroutine(BurnRoutine(target));
        }

        public override void ResetEffect(Enemy target)
        {
            target.StopCoroutine(BurnRoutine(target));
        }

        private IEnumerator BurnRoutine(Enemy target)
        {
            float elapsed = 0f;
            BreakableObject breakable = target.GetComponent<BreakableObject>();

            if (breakable.GetHealth() <= 0) yield break;

            while (elapsed < duration)
            {
                breakable.TakeDamage(Mathf.RoundToInt(damagePerTick), false, target.transform.position, Vector3.zero);
                yield return new WaitForSeconds(tickSpeed);
                elapsed += tickSpeed;
            }
        }
    }
}
