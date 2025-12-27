using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    [CreateAssetMenu(menuName = "Combat/Status Effects/Slow")]
    public class SlowEffect : StatusEffect
    {
        [Range(0f, 1f)] public float slowFactor = 0.5f;
        public bool noTimer;

        public override void Apply(Enemy target) => target.ApplySlow(this);

        public override void ResetEffect(Enemy target) => target.StopSlow(this);
    }
}
