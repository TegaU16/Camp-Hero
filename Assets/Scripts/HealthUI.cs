using System.Collections;
using DG.Tweening;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HealthUI : MonoBehaviour
{
    [Header("Bars")]
    [SerializeField] private Slider frontBar;
    [SerializeField] private Slider backBar;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private float uiHeightOffset = 1.5f;

    [Header("Tuning")]
    [SerializeField] private float frontTween = 0.15f;
    [SerializeField] private float backDelay = 0.1f;
    [SerializeField] private float backTween = 0.35f;

    [Header("Timing")]
    [SerializeField] private float displayTime = 5f;
    private float displayTimer;

    private int currentHealth;
    private Tween backTweenRef;

    private CanvasGroup canvasGroup;
    private Transform target;
    private Camera cam;

    private BreakableObject currentBreakable;

    private Vector3 desiredWorldScale;
    private Vector3 lastParentLossyScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        desiredWorldScale = transform.lossyScale;
    }

    private void Update()
    {
        if (displayTimer > 0f)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0f)
                ClearUI();
        }
    }

    private void LateUpdate()
    {
        if (!GameManager.Instance.IsGameActive) return;
        if (target == null || cam == null) return;

        Vector3 parentScale = target.lossyScale;

        if (parentScale != lastParentLossyScale)
        {
            ApplyWorldScale(desiredWorldScale);
            lastParentLossyScale = parentScale;
        }

        FaceCamera();
    }

    public void Setup(BreakableObject breakableObject)
    {
        if (breakableObject == null) return;

        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;

        target = breakableObject.transform;
        cam = Camera.main;

        transform.SetParent(target);
        lastParentLossyScale = target.lossyScale;
        ApplyWorldScale(desiredWorldScale);

        SetMaxHealth(breakableObject.GetMaxHealth());

        if (currentBreakable == breakableObject) return;

        currentBreakable = breakableObject;
        StartCoroutine(SetInitialPosition(moveTime: 0.2f));
    }

    private void SetMaxHealth(int health)
    {
        frontBar.maxValue = health;
        backBar.maxValue = health;
    }

    public void SetHealth(int health)
    {
        displayTimer = displayTime;
        currentHealth = health;

        frontBar.DOKill();
        frontBar.DOValue(currentHealth, frontTween).SetEase(Ease.OutQuad);

        backTweenRef?.Kill();
        backTweenRef = DOVirtual.DelayedCall(backDelay, () =>
        {
            backBar.DOValue(currentHealth, backTween).SetEase(Ease.OutCubic);
        });

        UpdateText(currentHealth);

        if (currentHealth <= 0)
            ClearUI();
    }

    private void UpdateText(int health) => healthText.text = health.ToString();

    private IEnumerator SetInitialPosition(float moveTime)
    {
        Bounds bounds = Utility.GetObjectBounds(target);

        Vector3 startPos = bounds.center + Vector3.up * bounds.extents.y;
        Vector3 finalPos = startPos + Vector3.up * uiHeightOffset;

        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveTime);

            transform.position = Vector3.Lerp(startPos, finalPos, t);

            yield return null;
        }

        transform.position = finalPos;
    }

    private void FaceCamera()
    {
        Vector3 camPos = cam.transform.position;
        camPos.y = transform.position.y;

        transform.LookAt(camPos);
        transform.Rotate(0f, 180f, 0f);
    }

    private void ClearUI()
    {
        currentBreakable = null;
        target = null;
        cam = null;
        transform.SetParent(null);

        UITween.FadeOut(canvasGroup).OnComplete(() =>
        {
            HealthUIPool.Instance.Return(this);
        });
    }

    private void ApplyWorldScale(Vector3 worldScale)
    {
        Vector3 parentScale = transform.parent.lossyScale;

        transform.localScale = new Vector3(
            Mathf.Abs(parentScale.x) > Mathf.Epsilon
                ? worldScale.x / parentScale.x
                : worldScale.x,

            Mathf.Abs(parentScale.y) > Mathf.Epsilon
                ? worldScale.y / parentScale.y
                : worldScale.y,

            Mathf.Abs(parentScale.z) > Mathf.Epsilon
                ? worldScale.z / parentScale.z
                : worldScale.z
        );
    }
}
