using System.Collections;
using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    [CreateAssetMenu(menuName = "Combat/Status Effects/Stun")]
    public class StunEffect : StatusEffect
    {
        public override void Apply(Enemy target)
        {
            target.StartCoroutine(StunRoutine(target));
        }

        public override void ResetEffect(Enemy target)
        {
            target.ResetMovement();
        }

        private IEnumerator StunRoutine(Enemy target)
        {
            target.ModifySpeed(0f);
            yield return new WaitForSeconds(duration);
            ResetEffect(target);
        }
    }
}
