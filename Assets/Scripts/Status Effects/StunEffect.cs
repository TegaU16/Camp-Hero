using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    [CreateAssetMenu(menuName = "Combat/Status Effects/Stun")]
    public class StunEffect : StatusEffect
    {
        public override void Apply(Enemy target) => target.ApplyStun(this);

        public override void ResetEffect(Enemy target) => target.StopStun(this);
    }
}
