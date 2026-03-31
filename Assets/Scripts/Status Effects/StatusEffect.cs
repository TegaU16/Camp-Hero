using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    public abstract class StatusEffect : ScriptableObject
    {
        public float duration = 1f;
        public ParticleSystem particleSystem;
        public bool noTimer;

        // This is the entry point
        public abstract void Apply(Enemy target);

        public abstract void ResetEffect(Enemy target);
    }
}
