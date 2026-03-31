using System;
using System.Collections;
using Game;
using Game.AI.Enemies;
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

    [SerializeField] private AnimationCurve starVisibilityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField] private AnimationCurve atmosphereThickness;

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
    public DayTextUI dayTextUI;
    public EnemyPool enemyPool;

    public Action<int> OnDayAdvanced;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (proceduralSkybox != null)
            RenderSettings.skybox = proceduralSkybox;

        isNight = IsNight();
        UpdateDayUI();
        StartCoroutine(ShowDayTextAfterDelay(3f));
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameActive) return;
        if (sun == null || moon == null) return;

        // Update time
        timeOfDay += (24f / dayDurationInSeconds) * Time.deltaTime;

        if (timeOfDay >= 24f)
            timeOfDay -= 24f;

        int hours = (int)timeOfDay;
        int minutes = Mathf.RoundToInt((timeOfDay - hours) * 60);
        timeOfDayText.text = $"{(int)timeOfDay}:{minutes:00}";

        bool nightNow = IsNight();

        // Detect transition
        if (nightNow != isNight)
        {
            isNight = nightNow;
            UpdateDayNightIcon();

            // Day advancing logic (only when entering daytime)
            if (!nightNow && timeOfDay >= nightEnd && !hasAdvancedDayToday)
            {
                AdvanceDay();
                hasAdvancedDayToday = true;
            }
        }

        // Reset the day advance lock at midnight
        if (nightNow && timeOfDay < nightEnd)
            hasAdvancedDayToday = false;

        // Normalized time (0 = midnight, 0.5 = noon, 1 = next midnight)
        float normalizedTime = timeOfDay / 24f;

        // Set sun and moon directions (already done above)
        Quaternion sunRotation = Quaternion.Euler((timeOfDay - 6f) * 15f, 170f, 0);
        Vector3 sunDirection = sunRotation * Vector3.forward;
        Vector3 moonDirection = -sunDirection;

        // Apply data to the shader
        if (proceduralSkybox != null)
        {
            // Set time of day
            proceduralSkybox.SetFloat("_Time_of_Day", normalizedTime);
            
            // Set zenith and horizon colors from gradients
            Color zenithColor = zenithGradient.Evaluate(normalizedTime);
            Color horizonColor = horizonGradient.Evaluate(normalizedTime);

            proceduralSkybox.SetColor("_Zenith_Color", zenithColor);
            proceduralSkybox.SetColor("_Horizon_Color", horizonColor);

            // Sun and moon
            proceduralSkybox.SetVector("_Sun_Direction", sunDirection);
            proceduralSkybox.SetVector("_Moon_Direction", moonDirection);
            proceduralSkybox.SetColor("_Sun_Color", sunColor.Evaluate(normalizedTime));
            proceduralSkybox.SetColor("_Moon_Color", moonColor.Evaluate(normalizedTime));

            // Stars
            float starIntensity = Mathf.Clamp01(starVisibilityCurve.Evaluate(normalizedTime));
            proceduralSkybox.SetFloat("_Star_Intensity", starIntensity);

            DynamicGI.UpdateEnvironment();
        }

        if (player != null)
        {
            // Compute the pivot point (center of sky rotation) to be the player
            Vector3 center = player.transform.position;

            // Sun position & rotation
            Vector3 sunPosition = center - sunDirection * sunDistance;
            sun.transform.position = sunPosition;
            sun.transform.LookAt(center); // Ensures the directional light is centered on the player

            // Moon direction is opposite the sun
            Vector3 moonPosition = center - moonDirection * moonDistance;

            moon.transform.position = moonPosition;
            moon.transform.LookAt(center);
        }
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
