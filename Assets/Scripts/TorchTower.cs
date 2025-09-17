using System.Collections.Generic;
using UnityEngine;

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
        if (GameManager.Instance.isPaused) return;

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

            if (target.TryGetComponent(out CharacterController controller))
                hitPoint = controller.ClosestPoint(transform.position);
            else
                continue;

            hitNormal = (hitPoint - transform.position).normalized;

            // Assign or reuse a beam for this target
            if (!targetToBeam.ContainsKey(target))
            {
                GameObject beam = Instantiate(beamPrefab, firePoint.position, Quaternion.identity, transform);
                targetToBeam[target] = beam;
            }

            GameObject beamObj = targetToBeam[target];
            LineRenderer lr = beamObj.GetComponent<LineRenderer>();
            lr.enabled = true;
            lr.SetPosition(0, firePoint.position);
            lr.SetPosition(1, target.position);

            if (fireCooldown <= 0f)
            {
                if (target.TryGetComponent(out BreakableObject breakable))
                {
                    float damage = damagePerTarget;

                    if (!damageBuffer.ContainsKey(target))
                        damageBuffer[target] = 0f;

                    damageBuffer[target] += damage;

                    int wholeDamage = Mathf.FloorToInt(damageBuffer[target]);
                    if (wholeDamage > 0)
                    {
                        breakable.TakeDamage(wholeDamage, false, hitPoint, hitNormal);
                        damageBuffer[target] -= wholeDamage;
                    }
                }
            }
        }

        // Apply cooldown only once per tick
        if (fireCooldown <= 0f)
        {
            fireCooldown = 1f / fireRate;
        }
    }

    private void CleanUpBeams()
    {
        List<Transform> toRemove = new();
        foreach (var pair in targetToBeam)
        {
            // Clean up if the target is dead or out of range or no longer selected
            bool isDead = !IsTargetAlive(pair.Key);
            bool isOutOfRange = !targets.Contains(pair.Key);

            if (isDead || isOutOfRange || pair.Key == null)
            {
                if (pair.Value != null)
                    pair.Value.GetComponent<LineRenderer>().enabled = false;

                toRemove.Add(pair.Key);
            }
        }

        foreach (Transform key in toRemove)
        {
            targetToBeam.Remove(key);
            damageBuffer.Remove(key);
        }

        // Also remove dead enemies from the targets list
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
            if (hit.GetComponent<Enemy>() != null)
            {
                Transform targetTransform = hit.transform;

                if (IsTargetAlive(targetTransform) && !targets.Contains(targetTransform))
                {
                    targets.Add(targetTransform);
                    if (targets.Count >= maxTargets) break;
                }
            }
        }
    }

    protected override void Fire() { } // Still unused
}
