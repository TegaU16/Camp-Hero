using Game.StatusEffects;
using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(menuName = "Combat/Attack Data", fileName = "New AttackData")]
    public class AttackData : ScriptableObject
    {
        [Header("Animation")]
        public string animationTrigger;   // e.g. "LightAttack1", "HeavyAttack2"
        public bool useRootMotion;        // does this attack rely on root motion?

        [Header("Combat")]
        public float damageMultiplier = 1f;
        public float attackDistance = 2f;     // how far the hitbox should extend

        [Header("Timing")]
        public float comboWindowOpenTime = 0.3f;   // when you can chain
        public float comboWindowCloseTime = 0.8f;  // when window closes

        [Header("Hitbox Timing (in seconds)")]
        public float hitboxEnableTime = 0.2f;   // when during the anim the hitbox activates
        public float hitboxDuration = 0.4f;     // how long it stays active

        [Header("Hitbox Override")]
        public bool overrideHitbox = false;
        public Vector3 hitboxCenter = Vector3.zero;
        public Vector3 hitboxSize = Vector3.one;

        [Header("Impact Effects")]
        public bool applyKnockback = false;
        public float knockbackForce = 5f;

        [Header("Extra Effects")]
        public StatusEffect[] statusEffects;

        [Header("VFX / SFX")]
        public GameObject hitEffectPrefab;
        public AudioClip hitSound;

        [Header("Combat Values")]
        public float staminaCost = 10f;
        public float poiseDamage = 5f;          // e.g. for stagger systems

        [Header("Procedural Override")]
        public bool overrideLegs = false;
        public bool overrideArms = false;
        public bool overrideTorso = false;
    }
}
