using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float homingStrength = 2f; // Lower = more dodgeable
    public float maxLifetime = 5f;

    private Transform target;
    private int damage;
    private Vector3 currentDirection;
    private bool homing = true;
    private float lifetime;

    public void SetTarget(Transform t, int dmg, bool useHoming = true)
    {
        target = t;
        damage = dmg;
        homing = useHoming;

        if (target != null)
        {
            currentDirection = (target.position - transform.position).normalized;
        }
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 toTarget = (target.position - transform.position).normalized;

        if (homing)
        {
            currentDirection = Vector3.RotateTowards(currentDirection, toTarget, homingStrength * Time.deltaTime, 0f);
        }

        float distanceThisFrame = speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(currentDirection * distanceThisFrame, Space.World);
        transform.rotation = Quaternion.LookRotation(currentDirection);
    }

    void HitTarget()
    {
        Vector3 hitPoint;
        Vector3 hitNormal;

        if (target.TryGetComponent(out Collider collider))
            hitPoint = collider.ClosestPoint(transform.position);
        else if (target.TryGetComponent(out CharacterController controller))
            hitPoint = controller.ClosestPoint(transform.position);
        else
            return;

        hitNormal = (hitPoint - transform.position).normalized;

        if (target.TryGetComponent(out Targetable targetable))
        {
            if (targetable.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(damage);

                if (target.TryGetComponent(out Rigidbody rb))
                {
                    Vector3 knockbackDir = (target.position - transform.position).normalized;
                    rb.AddForce(knockbackDir * 5f, ForceMode.Impulse);
                }
            }
            else if (target.TryGetComponent(out BreakableObject breakable))
            {
                breakable.TakeDamage(damage, false, hitPoint, hitNormal);
            }
        }
        else if (target.TryGetComponent(out BreakableObject breakable))
        {
            breakable.TakeDamage(damage, false, hitPoint, hitNormal);
        }

        Destroy(gameObject);
    }
}
