using UnityEngine;

namespace Game.Defenses
{
    public class Mortar : Defense
    {
        public Transform rotatingPart;
        public float mortarArcHeight = 5f;
        public Vector3 rotationOffset = new(0f, -90f, 0f); // if barrel faces +X

        protected override void Fire()
        {
            if (currentTarget == null) return;

            Vector3 start = firePoint.position;
            Vector3 end = currentTarget.position;
            Vector3 direction = end - start;
            Vector3 horizontal = new(direction.x, 0f, direction.z);
            float heightDifference = direction.y;

            float gravity = -Physics.gravity.y;
            float initialYVelocity = Mathf.Sqrt(2 * gravity * mortarArcHeight);
            float timeToApex = initialYVelocity / gravity;

            float totalTime = timeToApex + Mathf.Sqrt(2 * (mortarArcHeight - heightDifference) / gravity);
            Vector3 initialXZVelocity = horizontal / totalTime;
            Vector3 launchVelocity = initialXZVelocity + Vector3.up * initialYVelocity;

            if (rotatingPart != null)
                rotatingPart.rotation = Quaternion.LookRotation(launchVelocity) * Quaternion.Euler(rotationOffset);

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            if (!proj.TryGetComponent(out MortarProjectile mortarProj)) return;

            Collider projectileCollider = proj.GetComponent<Collider>();
            Collider[] mortarColliders = GetComponentsInChildren<Collider>();

            foreach (Collider col in mortarColliders)
            {
                if (projectileCollider != null && col != null)
                    Physics.IgnoreCollision(projectileCollider, col);
            }

            mortarProj.SetTarget(currentTarget);
            mortarProj.SetMortar(transform);
            mortarProj.Launch(currentTarget.position, damage);

            AudioManager.Instance.PlaySFX(shotSound);
        }
    }
}
