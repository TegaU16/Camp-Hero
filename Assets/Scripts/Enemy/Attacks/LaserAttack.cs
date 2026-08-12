using System.Collections.Generic;
using UnityEngine;
using static BreakableObject;

namespace Game.AI.Enemies.Attacks
{
    public class LaserAttack : MonoBehaviour, IRangedAttackBehavior
    {
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private Transform laserOrigin;

        [SerializeField] private float laserSpeed = 30f;
        [SerializeField] private float spinSpeed = 360f;
        [SerializeField] private float laserDuration = 2f;
        [SerializeField] private int baseDamagePerSecond = 10;

        private float currentLength = 0f;

        private GameObject laserInstance;
        private Transform target;
        private Transform attacker;

        private float elapsedTime = 0f;
        private float damageBuffer = 0f;

        private Transform beamVisual;
        private readonly float beamThickness = 0.2f;

        public RangedAttackType AttackType => RangedAttackType.Laser;

        public void ExecuteAttack(Transform attacker, Transform target, int damage)
        {
            if (laserPrefab == null || attacker == null || target == null) return;

            this.attacker = attacker;
            this.target = target;
            elapsedTime = 0f;
            damageBuffer = 0f;

            currentLength = 0f;

            laserInstance = Instantiate(laserPrefab, attacker.position, Quaternion.identity, attacker);
            beamVisual = laserInstance.transform;

            StartCoroutine(TrackAndDamage());
        }

        private IEnumerator<WaitForEndOfFrame> TrackAndDamage()
        {
            Vector3 origin = laserOrigin.position;
            Vector3 lockedDirection = (Utility.GetTargetPoint(target) - origin).normalized;

            currentLength = 0f;
            damageBuffer = 0f;

            while (elapsedTime < laserDuration)
            {
                if (attacker == null || target == null) break;

                origin = laserOrigin.position;

                currentLength += laserSpeed * Time.deltaTime;
                currentLength = Mathf.Min(currentLength, 100f);

                Vector3 endPoint = origin + lockedDirection * currentLength;

                if (Physics.Raycast(origin, lockedDirection, out RaycastHit hit, currentLength, hitMask))
                {
                    endPoint = hit.point;

                    float deltaDamage =
                        baseDamagePerSecond *
                        DifficultyManager.Instance.GetDamageMultiplier() *
                        Time.deltaTime;

                    damageBuffer += deltaDamage;

                    int wholeDamage = Mathf.FloorToInt(damageBuffer);

                    if (wholeDamage > 0)
                    {
                        if (hit.collider.TryGetComponent(out Health health))
                        {
                            health.TakeDamage(wholeDamage);
                        }
                        else if (hit.collider.TryGetComponent(out BreakableObject breakable))
                        {
                            DamageInfo attackDamageInfo = new(
                                damage: wholeDamage,
                                hitPoint: hit.point,
                                hitNormal: hit.normal,
                                fromEnemy: true
                            );

                            breakable.TakeDamage(attackDamageInfo);
                        }

                        damageBuffer -= wholeDamage;
                    }
                }

                Vector3 beamDir = endPoint - origin;
                float distance = beamDir.magnitude;

                if (distance > 0.001f)
                {
                    Vector3 center = origin + beamDir * 0.5f;

                    Quaternion baseRotation = Quaternion.LookRotation(beamDir.normalized);

                    Quaternion spin = Quaternion.AngleAxis(
                        Time.time * spinSpeed,
                        beamDir.normalized
                    );

                    beamVisual.SetPositionAndRotation(center, baseRotation * spin);

                    beamVisual.localScale = new Vector3(
                        beamThickness,
                        beamThickness,
                        distance
                    );
                }

                elapsedTime += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            if (laserInstance != null)
                Destroy(laserInstance);
        }
    }
}
