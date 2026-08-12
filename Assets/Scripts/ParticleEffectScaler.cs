using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ParticleEffectScaler : MonoBehaviour
{
    [Tooltip("Optional override. If empty, uses all child renderers.")]
    [SerializeField] private Renderer[] targetRenderers;

    [SerializeField] private float sizeMultiplier = 1f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float emissionMultiplier = 1f;

    private Bounds bounds;

    void Awake()
    {
        CalculateBounds();
    }

    private void CalculateBounds()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        bounds = targetRenderers[0].bounds;
        foreach (Renderer rend in targetRenderers)
            bounds.Encapsulate(rend.bounds);
    }

    public void ApplyTo(ParticleSystem ps)
    {
        float scale = bounds.extents.magnitude;

        MainModule main = ps.main;
        EmissionModule emission = ps.emission;
        ShapeModule shape = ps.shape;

        main.startSizeMultiplier *= scale * sizeMultiplier;
        main.startSpeedMultiplier *= scale * speedMultiplier;

        if (emission.enabled)
            emission.rateOverTimeMultiplier *= scale * emissionMultiplier;

        if (shape.enabled)
            shape.scale = bounds.size;
    }
}
