using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    [CreateAssetMenu(menuName = "Combat/Status Effects/Burn")]
    public class BurnEffect : StatusEffect
    {
        public float tickSpeed = 1f;
        public float damagePerTick = 2f;

        public override void Apply(Enemy target) => target.ApplyBurn(this);

        public override void ResetEffect(Enemy target) => target.StopBurn(this);
    }
}
