using UnityEngine;

public class DefenderTotem : Defense
{
    public float pulseRadius = 5f;
    public int pulseDamage = 15;
    public float pulseInterval = 2f; // Time between pulses
    public ParticleSystem pulseEffect;

    private float pulseTimer = 0f;

    protected override void Update()
    {
        pulseTimer -= Time.deltaTime;

        if (pulseTimer <= 0f && HasEnemiesInRange())
        {
            Fire();
            pulseTimer = pulseInterval;
        }
    }

    protected override void Fire()
    {
        Collider[] hits = new Collider[20];
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, pulseRadius, hits);

        if (hitCount == hits.Length)
        {
            Collider[] expandedArray = new Collider[hitCount * 2];
            hitCount = Physics.OverlapSphereNonAlloc(transform.position, pulseRadius, expandedArray);
            hits = expandedArray;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            if (hit.TryGetComponent(out Enemy enemy))
            {
                if (enemy.TryGetComponent(out BreakableObject breakable))
                {
                    breakable.TakeDamage(pulseDamage, false);
                }
            }
        }

        if (pulseEffect != null)
            pulseEffect.Play();
    }

    private bool HasEnemiesInRange()
    {
        Collider[] hits = new Collider[20];
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, pulseRadius, hits);

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].TryGetComponent(out Enemy enemy) && IsTargetAlive(enemy.gameObject.transform))
                return true;
        }

        return false;
    }

    protected override void FindTarget() { } // Not used
}
