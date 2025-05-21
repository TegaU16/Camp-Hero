using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public DayNightCycle dayNightCycle;

    private float sunIntensity;

    private static readonly int LightMultiplierID = Shader.PropertyToID("_LightMultiplier");

    // Register local override materials (torch, furnace, etc.)
    private readonly HashSet<Material> localOverrideMaterials = new();

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        sunIntensity = dayNightCycle.GetSunlightIntensity();
        Shader.SetGlobalFloat(LightMultiplierID, sunIntensity);

        // Optionally update local override materials (for animation, flicker, etc.)
        foreach (Material mat in localOverrideMaterials)
        {
            // Do nothing unless you want flicker, etc.
            // Example flicker:
            // float flicker = 0.9f + 0.1f * Mathf.Sin(Time.time * 10f);
            // mat.SetFloat(LightMultiplierID, flicker);
        }
    }

    /// <summary>
    /// Call this for torches, furnaces, glowing crystals, etc.
    /// </summary>
    public void SetLocalLight(Material mat, float intensity)
    {
        mat.SetFloat(LightMultiplierID, intensity);
        localOverrideMaterials.Add(mat);
    }

    /// <summary>
    /// Reverts the material back to following global lighting.
    /// </summary>
    public void RemoveLocalOverride(Material mat)
    {
        localOverrideMaterials.Remove(mat);
        // Reset to global immediately
        mat.SetFloat(LightMultiplierID, sunIntensity);
    }
}
