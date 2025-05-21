using UnityEngine;

public class DefenderTotem : Defense
{
    public float pulseRadius = 5f;
    public int pulseDamage = 15;
    public ParticleSystem pulseEffect;

    protected override void Fire()
    {
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
            if (hit.CompareTag("Enemy") && hit.TryGetComponent(out Enemy enemy))
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

    protected override void FindTarget() { } // Doesn’t track single target
}
