using UnityEngine;

public class Mortar : Defense
{
    protected override void Fire()
    {
        if (currentTarget == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        if (proj.TryGetComponent(out MortarProjectile mortarProj))
        {
            mortarProj.Launch(currentTarget.position, damage);
        }
    }
}

