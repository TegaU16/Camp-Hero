using UnityEngine;

public class ProceduralQuadruped : MonoBehaviour, IProceduralMotion
{
    [Header("Quadruped Bones")]
    public Transform frontLeft;
    public Transform frontRight;
    public Transform backLeft;
    public Transform backRight;
    public Transform body;

    [Header("Walk Parameters")]
    public float walkSpeed = 2f;
    public float legAmplitude = 25f;
    public float bodyBob = 0.05f;

    private float walkCycle;

    public void Animate(float deltaTime)
    {
        walkCycle += deltaTime * walkSpeed;

        float phaseA = Mathf.Sin(walkCycle);
        float phaseB = Mathf.Sin(walkCycle + Mathf.PI);

        if (frontLeft) 
            frontLeft.localRotation = Quaternion.Euler(phaseA * legAmplitude, 0, 0);

        if (backRight) 
            backRight.localRotation = Quaternion.Euler(phaseA * legAmplitude, 0, 0);

        if (frontRight) 
            frontRight.localRotation = Quaternion.Euler(phaseB * legAmplitude, 0, 0);

        if (backLeft) 
            backLeft.localRotation = Quaternion.Euler(phaseB * legAmplitude, 0, 0);

        if (body)
            body.localPosition = new Vector3(0, Mathf.Sin(walkCycle * 2f) * bodyBob, 0);
    }
}
