using UnityEngine;

public class Cannon : Defense
{
    public Transform rotatingPart; // The part that should turn to face the enemy

    protected override void Update()
    {
        base.Update();

        if (currentTarget != null && rotatingPart != null)
        {
            RotateTowardTarget();
        }
    }

    private void RotateTowardTarget()
    {
        Vector3 direction = currentTarget.position - rotatingPart.position;
        direction.y = 0; // Optional: keep rotation only on Y-axis

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rotatingPart.rotation = Quaternion.Slerp(rotatingPart.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
