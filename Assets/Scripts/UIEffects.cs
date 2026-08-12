using UnityEngine;
using DG.Tweening;

public class UIEffects : MonoBehaviour
{
    public static UIEffects Instance;

    [SerializeField] private CanvasGroup damageOverlay;

    private Tween flashTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void DamageFlash()
    {
        flashTween?.Kill();

        damageOverlay.alpha = 0f;

        flashTween = DOTween.Sequence()
            .Append(damageOverlay.DOFade(0.35f, 0.05f))
            .Append(damageOverlay.DOFade(0f, 0.2f));
    }
}
