using System.Collections.Generic;
using Game.AI.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Defenses
{
    public class TorchTower : Defense
    {
        public GameObject beamPrefab;
        public int baseDamagePerSecond = 10;
        public int maxTargets = 5;

        private readonly List<Transform> targets = new();
        private readonly Dictionary<Transform, GameObject> targetToBeam = new();
        private readonly Dictionary<Transform, float> damageBuffer = new();

        protected override void Update()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;

            fireCooldown -= Time.deltaTime;

            UpdateTargets();
            CleanUpBeams();

            int activeTargets = targets.Count;
            float damagePerTarget = (activeTargets > 0) ? baseDamagePerSecond / Mathf.Max(1f, activeTargets) : 0f;

            foreach (Transform target in targets)
            {
                if (!IsTargetAlive(target)) continue;

                Vector3 hitPoint;
                Vector3 hitNormal;

                if (!target.TryGetComponent(out CharacterController controller)) continue;

                hitPoint = controller.ClosestPoint(transform.position);
                hitNormal = (hitPoint - transform.position).normalized;

                // Assign or reuse a beam for this target
                if (!targetToBeam.ContainsKey(target))
                {
                    GameObject beam = Instantiate(beamPrefab, firePoint.position, Quaternion.identity, transform);
                    targetToBeam[target] = beam;
                }

                GameObject beamObj = targetToBeam[target];
                LineRenderer lineRenderer = beamObj.GetComponent<LineRenderer>();
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, firePoint.position);
                lineRenderer.SetPosition(1, target.position);

                if (fireCooldown > 0f || !target.TryGetComponent(out BreakableObject breakable)) continue;

                fireCooldown = 1f / fireRate;
                float damage = damagePerTarget;

                if (!damageBuffer.ContainsKey(target))
                    damageBuffer[target] = 0f;

                damageBuffer[target] += damage;

                int wholeDamage = Mathf.FloorToInt(damageBuffer[target]);
                if (wholeDamage > 0)
                {
                    breakable.TakeDamage(wholeDamage, crit: false, hitPoint, hitNormal);
                    damageBuffer[target] -= wholeDamage;
                }

                if (target.TryGetComponent(out Enemy enemy))
                    enemy.OnAttacked(transform);
            }
        }

        private void CleanUpBeams()
        {
            List<Transform> toRemove = new();
            foreach (KeyValuePair<Transform, GameObject> pair in targetToBeam)
            {
                if (pair.Key == null) continue;

                bool isAlive = IsTargetAlive(pair.Key);
                bool isWithinRange = targets.Contains(pair.Key);

                if (isAlive && isWithinRange) continue;

                if (pair.Value != null)
                    pair.Value.GetComponent<LineRenderer>().enabled = false;

                toRemove.Add(pair.Key);
                targetToBeam.Remove(pair.Key);
                damageBuffer.Remove(pair.Key);
            }

            targets.RemoveAll(t => t == null || !IsTargetAlive(t));
        }

        protected override void FindTarget() { } // Not used

        private void UpdateTargets()
        {
            targets.Clear();
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, hits);

            if (hitCount == hits.Length)
            {
                Collider[] expandedArray = new Collider[hitCount * 2];
                hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, expandedArray);
                hits = expandedArray;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit.GetComponent<Enemy>() == null) continue;

                Transform targetTransform = hit.transform;
                if (!IsTargetAlive(targetTransform) || targets.Contains(targetTransform)) continue;

                targets.Add(targetTransform);
                if (targets.Count >= maxTargets) break;
            }
        }

        protected override void Fire() { } // Still unused
    }
}
