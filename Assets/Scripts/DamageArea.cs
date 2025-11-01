using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageArea : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float activeDuration = 0.5f; // how long it stays active
    [SerializeField] private float riseHeight = 1.5f; // how far it comes out of ground
    [SerializeField] private float riseDuration = 0.3f; // how fast it rises/sinks
    [SerializeField] private float damageTickInterval = 0.1f; // check every frame/interval

    public float Radius => radius;

    private int damage;
    private readonly HashSet<Health> damagedTargets = new();

    public void SetDamage(int dmg) => damage = dmg;

    public void Activate(Transform attacker)
    {
        StartCoroutine(AttackLifecycle(attacker));
    }

    private IEnumerator AttackLifecycle(Transform attacker)
    {
        Vector3 basePos = transform.position;
        Vector3 startPos = basePos - Vector3.up * riseHeight;
        Vector3 peakPos = basePos;

        // Start below ground
        transform.position = startPos;

        // Rise up
        yield return MoveOverTime(startPos, peakPos, riseDuration);

        // Active phase: damage window
        float elapsed = 0f;
        while (elapsed < activeDuration)
        {
            DealDamage(attacker);
            elapsed += damageTickInterval;
            yield return new WaitForSeconds(damageTickInterval);
        }

        // Sink back down
        yield return MoveOverTime(peakPos, startPos, riseDuration);

        Destroy(gameObject);
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time;
            t = t * t * (3f - 2f * t); // smoothstep easing
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.position = to;
    }

    private void DealDamage(Transform attacker = null)
    {
        Collider[] hits = new Collider[20];
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, targetLayer);

        for (int i = 0; i < numHits; i++)
        {
            Collider hit = hits[i];
            if (hit.TryGetComponent(out Health health))
            {
                // Skip if already damaged once
                if (damagedTargets.Contains(health)) continue;

                health.TakeDamage(damage, attacker);
                damagedTargets.Add(health);
            }
        }
    }
}
