using System;
using System.Collections;
using Game;
using Game.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worlds;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Header("Time Settings")]
    public float dayDurationInSeconds = 120f;

    [SerializeField, Range(0, 24)] private float timeOfDay = 12f; // 0 = Midnight, 12 = Noon
    [SerializeField, Range(0, 24)] private float nightStart = 18f;
    [SerializeField, Range(0, 24)] private float nightEnd = 6f;

    private int currentDay = 1;
    private bool hasAdvancedDayToday = false;

    [Header("Sky Settings")]
    [SerializeField] private Material proceduralSkybox;

    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private Transform sunVisual;
    [SerializeField] private Transform moonVisual;

    [SerializeField] private AnimationCurve starVisibilityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField] private AnimationCurve atmosphereThickness;
    [SerializeField] private AnimationCurve sunIntensityCurve;
    [SerializeField] private AnimationCurve moonIntensityCurve;

    [SerializeField] private Gradient sunColor;
    [SerializeField] private Gradient moonColor;
    [SerializeField] private Gradient skyTint;
    [SerializeField] private Gradient zenithGradient;   // Zenith/top of sky
    [SerializeField] private Gradient horizonGradient;

    [SerializeField] private float sunDistance = 1000f;
    [SerializeField] private float moonDistance = 1000f;

    private GameObject player;

    [Header("UI")]
    [SerializeField] private Image sunMoonIcon;
    [SerializeField] private Sprite sunIcon;
    [SerializeField] private Sprite moonIcon;
    [SerializeField] private TextMeshProUGUI dayCountText;
    [SerializeField] private TextMeshProUGUI timeOfDayText;
    private bool isNight;

    [Header("References")]
    [SerializeField] private DayTextUI dayTextUI;

    public Action<int> OnDayAdvanced;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (proceduralSkybox != null)
            RenderSettings.skybox = proceduralSkybox;

        isNight = IsNight();
        UpdateDayUI();
        StartCoroutine(ShowDayTextAfterDelay(3f));

        sunVisual.localPosition = -Vector3.forward * 500f;
        moonVisual.localPosition = -Vector3.forward * 500f;
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameActive) return;
        if (sun == null || moon == null) return;

        timeOfDay += (24f / dayDurationInSeconds) * Time.deltaTime;

        if (timeOfDay >= 24f)
            timeOfDay -= 24f;

        int hours = (int)timeOfDay;
        int minutes = Mathf.RoundToInt((timeOfDay - hours) * 60);
        timeOfDayText.text = $"{(int)timeOfDay}:{minutes:00}";

        bool nightNow = IsNight();

        if (nightNow != isNight)
        {
            isNight = nightNow;
            UpdateDayNightIcon();

            if (!nightNow && timeOfDay >= nightEnd && !hasAdvancedDayToday)
            {
                AdvanceDay();
                hasAdvancedDayToday = true;
            }
        }

        if (nightNow && timeOfDay < nightEnd)
            hasAdvancedDayToday = false;

        float normalizedTime = timeOfDay / 24f;

        Color sunlightColor = sunColor.Evaluate(normalizedTime);
        Color moonlightColor = moonColor.Evaluate(normalizedTime);

        sun.color = sunlightColor;
        moon.color = moonlightColor;

        float sunIntensity = sunIntensityCurve.Evaluate(normalizedTime);
        float moonIntensity = moonIntensityCurve.Evaluate(normalizedTime);

        sun.intensity = sunIntensity;
        moon.intensity = moonIntensity;

        Quaternion sunRotation = Quaternion.Euler((timeOfDay - 6f) * 15f, 170f, 0);
        Vector3 sunDirection = sunRotation * Vector3.forward;
        Vector3 moonDirection = -sunDirection;

        // Apply data to the shader
        if (proceduralSkybox != null)
        {
            proceduralSkybox.SetFloat("_Time_of_Day", normalizedTime);
            
            Color zenithColor = zenithGradient.Evaluate(normalizedTime);
            Color horizonColor = horizonGradient.Evaluate(normalizedTime);

            proceduralSkybox.SetColor("_Zenith_Color", zenithColor);
            proceduralSkybox.SetColor("_Horizon_Color", horizonColor);

            float starIntensity = Mathf.Clamp01(starVisibilityCurve.Evaluate(normalizedTime));
            proceduralSkybox.SetFloat("_Star_Intensity", starIntensity);

            DynamicGI.UpdateEnvironment();
        }

        if (player == null) return;

        Vector3 center = player.transform.position;

        Vector3 sunPosition = center - sunDirection * sunDistance;
        sunVisual.transform.position = sunPosition;
        sun.transform.rotation = Quaternion.LookRotation(sunDirection);

        Vector3 moonPosition = center - moonDirection * moonDistance;
        moonVisual.transform.position = moonPosition;
        moon.transform.rotation = Quaternion.LookRotation(moonDirection);
    }

    public void SetPlayer(GameObject player)
    {
        if (player != null)
            this.player = player;
        else
            Debug.LogWarning("Player is null!");
    }

    public void AdvanceDay()
    {
        currentDay++;
        OnDayAdvanced.Invoke(currentDay);
        UpdateDayUI();
    }

    private void UpdateDayUI()
    {
        if (dayCountText != null)
            dayCountText.text = currentDay.ToString();

        UpdateDayNightIcon();
    }

    private IEnumerator ShowDayTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dayTextUI != null)
            dayTextUI.ShowDay(currentDay);
    }

    public int GetCurrentDay() => currentDay;

    public bool IsNight()
    {
        // Night spans past midnight (18 → 6)
        if (nightStart > nightEnd) return timeOfDay >= nightStart || timeOfDay < nightEnd;

        // Night does NOT span midnight
        return timeOfDay >= nightStart && timeOfDay < nightEnd;
    }

    private void UpdateDayNightIcon() => sunMoonIcon.sprite = isNight ? moonIcon : sunIcon;

    public void SaveDayNight()
    {
        DayNightSaveData data = new()
        {
            timeOfDay = this.timeOfDay,
            currentDay = this.currentDay,
            hasAdvancedDayToday = this.hasAdvancedDayToday
        };

        SaveSystem.SaveDayNight(WorldSession.CurrentWorldName, data);
    }

    public void LoadDayNight()
    {
        DayNightSaveData data = SaveSystem.LoadDayNight(WorldSession.CurrentWorldName);
        if (data != null)
        {
            timeOfDay = data.timeOfDay;
            currentDay = data.currentDay;
            hasAdvancedDayToday = data.hasAdvancedDayToday;
        }

        UpdateDayUI();
    }

    public float GetElapsedTime()
    {
        int daysPassed = currentDay - 1;
        float dayFraction = timeOfDay / 24f;

        return daysPassed + dayFraction;
    }
}
