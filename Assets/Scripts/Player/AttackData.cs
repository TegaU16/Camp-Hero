using Game.StatusEffects;
using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(menuName = "Combat/Attack Data", fileName = "New AttackData")]
    public class AttackData : ScriptableObject
    {
        [Header("Animation")]
        public string animationTrigger;

        [Header("Combat")]
        public float damageMultiplier = 1f;

        [Header("Hitbox Override")]
        public bool overrideHitbox = false;
        public Vector3 hitboxCenter = Vector3.zero;
        public Vector3 hitboxSize = Vector3.one;

        [Header("Impact Effects")]
        public bool applyKnockback = false;
        public float knockbackForce = 5f;
        public float hitStopDuration = 0.15f;

        [Header("Status Effects")]
        public StatusEffect[] statusEffects;

        [Header("VFX / SFX")]
        public GameObject hitEffectPrefab;
        public AudioClip hitSound;

        [Header("Combat Values")]
        public int poiseDamage = 5;
    }
}
