using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ParticleEffectScaler : MonoBehaviour
{
    [Tooltip("Optional override. If empty, uses all child renderers.")]
    public Renderer[] targetRenderers;

    public float sizeMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float emissionMultiplier = 1f;

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
        foreach (Renderer r in targetRenderers)
            bounds.Encapsulate(r.bounds);
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
