using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Health))]
public class LowHealthVFX : MonoBehaviour
{
    private Health health;
    private Volume volume;
    private AudioLowPassFilter lowPassFilter;

    private Vignette vignette;
    private ChromaticAberration chromatic;

    [Header("Effect Strength")]
    [SerializeField] private float maxVignette = 0.45f;
    [SerializeField] private float maxChromatic = 0.6f;

    [Header("Audio Settings")]
    [SerializeField] private float normalCutoff = 22000f;
    [SerializeField] private float lowHealthCutoff = 800f;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;
    private float currentIntensity;

    void Start()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (volume == null)
            volume = FindFirstObjectByType<Volume>();

        if (lowPassFilter == null)
            lowPassFilter = FindFirstObjectByType<AudioLowPassFilter>();

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out chromatic);
        }
    }

    void Update()
    {
        if (health == null) return;

        float healthPercent = health.HealthPercent;
        float targetIntensity = 1f - healthPercent;

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);

        ApplyEffects(currentIntensity);
        ApplyAudio(currentIntensity);
    }

    private void ApplyEffects(float value)
    {
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxVignette, value);
            vignette.smoothness.value = Mathf.Lerp(0.2f, 0.8f, value);
        }

        if (chromatic != null)
            chromatic.intensity.value = Mathf.Lerp(0f, maxChromatic, value);
    }

    private void ApplyAudio(float value)
    {
        if (lowPassFilter == null) return;

        lowPassFilter.cutoffFrequency = Mathf.Lerp(
            normalCutoff,
            lowHealthCutoff,
            value
        );
    }
}
