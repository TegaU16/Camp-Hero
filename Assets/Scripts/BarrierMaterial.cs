using UnityEngine;

public class BarrierMaterial : MonoBehaviour
{
    public static BarrierMaterial Instance { get; private set; }

    [SerializeField] private Material barrierMat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Material GetMaterial() => barrierMat;
}
