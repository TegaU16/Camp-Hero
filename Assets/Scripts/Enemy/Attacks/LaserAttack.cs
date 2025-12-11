using UnityEngine;
using System.Collections.Generic;

namespace Game.AI.Enemies.Attacks
{
    public class LaserAttack : MonoBehaviour, IRangedAttackBehavior
    {
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private float laserDuration = 2f;
        [SerializeField] private int baseDamagePerSecond = 10;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private Transform laserOrigin;

        private GameObject laserInstance;
        private LineRenderer laser;
        private Transform target;
        private Transform attacker;

        private float elapsedTime = 0f;
        private float damageBuffer = 0f;

        public string AttackName => "Laser";

        public void ExecuteAttack(Transform attacker, Transform target, int damage)
        {
            if (laserPrefab == null || attacker == null || target == null) return;

            this.attacker = attacker;
            this.target = target;
            this.elapsedTime = 0f;
            this.damageBuffer = 0f;

            laserInstance = Instantiate(laserPrefab, attacker.position, Quaternion.identity, attacker);
            laser = laserInstance.GetComponent<LineRenderer>();
            laser.enabled = true;

            StartCoroutine(TrackAndDamage());
        }

        private IEnumerator<WaitForEndOfFrame> TrackAndDamage()
        {
            while (elapsedTime < laserDuration)
            {
                if (attacker == null || target == null || laser == null)
                    break;

                Vector3 origin = laserOrigin.position;
                Vector3 targetPos = target.position;

                Vector3 direction = (targetPos - origin).normalized;

                Quaternion lookRotation = Quaternion.LookRotation(direction);
                attacker.rotation = Quaternion.Slerp(attacker.rotation, lookRotation, Time.deltaTime * 10f);

                if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f, hitMask))
                {
                    laser.SetPosition(0, origin);
                    laser.SetPosition(1, hit.point);

                    float deltaDamage = baseDamagePerSecond * DifficultyManager.Instance.GetDamageMultiplier() * Time.deltaTime;
                    damageBuffer += deltaDamage;

                    int wholeDamage = Mathf.FloorToInt(damageBuffer);

                    if (wholeDamage > 0)
                    {
                        if (hit.collider.TryGetComponent(out Health health))
                        {
                            health.TakeDamage(wholeDamage);
                        }
                        else if (target.TryGetComponent(out BreakableObject breakable))
                        {
                            Vector3 hitPoint = hit.collider.ClosestPoint(transform.position);
                            Vector3 hitNormal = (hitPoint - transform.position).normalized;

                            breakable.TakeDamage(wholeDamage, false, hitPoint, hitNormal, true);
                        }

                        damageBuffer -= wholeDamage;
                    }
                }
                else
                {
                    // Nothing hit — laser reaches max distance
                    laser.SetPosition(0, origin);
                    laser.SetPosition(1, origin + direction * 100f);
                }

                elapsedTime += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            if (laserInstance != null)
                Destroy(laserInstance);
        }
    }
}
