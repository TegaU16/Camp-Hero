using Unity.Cinemachine;
using UnityEngine;

public class CameraControlToggle : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;

    private CinemachineOrbitalFollow orbitalFollow;
    private bool allowCameraControl = true;

    private float lockedHorizontalValue;
    private float lockedVerticalValue;

    void Start()
    {
        if (cinemachineCamera != null)
            orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
    }

    void LateUpdate()
    {
        if (orbitalFollow == null) return;

        if (!allowCameraControl)
        {
            orbitalFollow.HorizontalAxis.Value = lockedHorizontalValue;
            orbitalFollow.VerticalAxis.Value = lockedVerticalValue;
        }
    }

    public void SetCameraControl(bool enabled)
    {
        allowCameraControl = enabled;

        if (orbitalFollow == null) return;

        if (!enabled)
        {
            lockedHorizontalValue = orbitalFollow.HorizontalAxis.Value;
            lockedVerticalValue = orbitalFollow.VerticalAxis.Value;
        }
        // No need to restore anything — values will be updated automatically by Cinemachine
    }
}
