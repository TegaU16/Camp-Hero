using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Renderer[] renderersToExclude;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock propertyBlock;
    private Color[] originalColors;
    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetRenderers();

        propertyBlock = new MaterialPropertyBlock();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].sharedMaterial.GetColor(BaseColorID);
    }

    private Renderer[] GetRenderers()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        List<Renderer> validRenderers = new();

        foreach (Renderer renderer in allRenderers)
        {
            if (renderersToExclude.Contains(renderer)) continue;
            validRenderers.Add(renderer);
        }

        return validRenderers.ToArray();
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            float blend = flashCurve.Evaluate(t);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = Color.Lerp(originalColors[i], flashColor, blend);

                renderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorID, color);
                renderers[i].SetPropertyBlock(propertyBlock);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorID, originalColors[i]);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
