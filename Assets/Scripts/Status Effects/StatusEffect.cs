using Game.AI.Enemies;
using UnityEngine;

namespace Game.StatusEffects
{
    public abstract class StatusEffect : ScriptableObject
    {
        public string effectName;
        public Sprite icon;   // UI, optional
        public float duration = 1f;

        // This is the entry point
        public abstract void Apply(Enemy target);

        public abstract void ResetEffect(Enemy target);
    }
}
