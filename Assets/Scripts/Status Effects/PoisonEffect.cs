using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    [CreateAssetMenu(menuName = "Combat/Status Effects/Poison")]
    public class PoisonEffect : StatusEffect
    {
        public int damagePerTick;
        public float tickSpeed;

        [Range(0f, 0.8f)] public float attackDamageMult;

        public override void Apply(Enemy target) => target.ApplyPoison(this);

        public override void ResetEffect(Enemy target) => target.StopPoison(this);
    }
}
