using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    private Transform target;
    private int damage;

    public void SetTarget(Transform t, int dmg)
    {
        target = t;
        damage = dmg;
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
    }

    void HitTarget()
    {
        if (target.TryGetComponent(out Enemy enemy))
        {
            if (enemy != null)
            {
                enemy.GetComponent<BreakableObject>().TakeDamage(damage, false);
            }
        }

        Destroy(gameObject);
    }
}
