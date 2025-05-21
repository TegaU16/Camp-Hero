using System.Collections.Generic;
using UnityEngine;

public class TorchTower : Defense
{
    public GameObject beamPrefab;
    private readonly List<GameObject> activeBeams = new();
    public int baseDamagePerSecond = 10;
    public int maxTargets = 5;

    private readonly List<Transform> targets = new();

    protected override void Update()
    {
        if (GameManager.Instance.isPaused) return;

        fireCooldown -= Time.deltaTime;

        UpdateTargets();

        int activeTargets = targets.Count;
        float damagePerTarget = (activeTargets > 0) ? baseDamagePerSecond / Mathf.Max(1f, activeTargets) : 0f;

        for (int i = 0; i < activeTargets; i++)
        {
            if (i >= activeBeams.Count)
            {
                GameObject newBeam = Instantiate(beamPrefab, firePoint.position, Quaternion.identity, transform);
                activeBeams.Add(newBeam);
            }

            LineRenderer lr = activeBeams[i].GetComponent<LineRenderer>();
            lr.enabled = true;
            lr.SetPosition(0, firePoint.position);
            lr.SetPosition(1, targets[i].position);

            Transform target = targets[i];

            if (target.TryGetComponent(out Enemy enemy) &&
                    enemy.TryGetComponent(out BreakableObject breakable))
            {
                breakable.TakeDamage((int)(damagePerTarget * Time.deltaTime), false);
            }
        }

        for (int i = activeTargets; i < activeBeams.Count; i++)
        {
            activeBeams[i].GetComponent<LineRenderer>().enabled = false;
        }
    }

    protected override void FindTarget() { } // Disabled in favor of multi-targeting

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
            if (hit.CompareTag("Enemy"))
            {
                targets.Add(hit.transform);
                if (targets.Count >= maxTargets) break;
            }
        }
    }

    protected override void Fire() { } // Still unused
}
