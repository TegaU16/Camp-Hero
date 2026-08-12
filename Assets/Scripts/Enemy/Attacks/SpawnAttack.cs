using UnityEngine;
using System.Collections;

namespace Game.AI.Enemies.Attacks
{
    public class SpawnAttack : MonoBehaviour, IRangedAttackBehavior
    {
        [SerializeField] private GameObject warningPrefab; // The visual warning indicator
        [SerializeField] private GameObject damagePrefab;  // The object that deals damage
        [SerializeField] private float warningDuration = 1f; // How long the warning lasts
        [SerializeField] private int damage;

        public RangedAttackType AttackType => RangedAttackType.Spawn;

        public void ExecuteAttack(Transform attacker, Transform target, int damage)
        {
            if (warningPrefab == null || damagePrefab == null || target == null) return;

            // Start the attack coroutine
            damage = (int)(this.damage * DifficultyManager.Instance.GetDamageMultiplier());
            StartCoroutine(SpawnSequence(target.position, damage, attacker));
        }

        private IEnumerator SpawnSequence(Vector3 position, int damage, Transform attacker)
        {
            // Step 1: Show warning prefab
            GameObject warning = Instantiate(warningPrefab, position, Quaternion.identity);

            // Determine target scale based on DamageArea radius
            float targetRadius = 1f; // default
            if (damagePrefab.TryGetComponent(out DamageArea areaPrefab))
                targetRadius = areaPrefab.Radius;

            // Animate warning growth
            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 endScale = 2f * targetRadius * Vector3.one;

            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / warningDuration;
                warning.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            Destroy(warning);

            // Step 2: Spawn damage object **from the ground**
            GameObject damageObj = Instantiate(damagePrefab, position, Quaternion.identity);
            if (damageObj.TryGetComponent(out DamageArea area))
            {
                area.SetDamage(damage);
                area.Activate(attacker);
            }
        }
    }
}
