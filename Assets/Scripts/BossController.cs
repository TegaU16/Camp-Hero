using UnityEngine;
using System;
using DG.Tweening;

public class BossController : MonoBehaviour
{
    public event Action OnBossDefeated;

    [Header("Dissolve")]
    [SerializeField] private float dissolveStart = 1f;
    [SerializeField] private float dissolveEnd = 0f;
    [SerializeField] private float dissolveDuration = 1.5f;

    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        // Start fully dissolved (hidden)
        SetDissolve(dissolveStart);
    }

    private void Start()
    {
        PlaySpawnDissolve();
    }

    private void PlaySpawnDissolve()
    {
        DOTween.To(
            () => dissolveStart,
            x => SetDissolve(x),
            dissolveEnd,
            dissolveDuration
        ).SetEase(Ease.OutQuad);
    }

    private void SetDissolve(float value)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat(DissolveID, value);
            renderer.SetPropertyBlock(mpb);
        }
    }

    public void Die()
    {
        // Optional: dissolve OUT before destroy
        DOTween.To(
            () => dissolveEnd,
            x => SetDissolve(x),
            dissolveStart,
            0.8f
        )
        .SetEase(Ease.InQuad)
        .OnComplete(() =>
        {
            OnBossDefeated?.Invoke();
            Destroy(gameObject);
        });
    }
}
