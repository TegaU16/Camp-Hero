using Unity.Cinemachine;
using UnityEngine;

public class CameraControlToggle : MonoBehaviour
{
    public static CameraControlToggle Instance;

    [SerializeField] private CinemachineCamera cinemachineCamera;

    private CinemachineOrbitalFollow orbitalFollow;
    private bool allowCameraControl = true;

    private float lockedHorizontalValue;
    private float lockedVerticalValue;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

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
    }
}
