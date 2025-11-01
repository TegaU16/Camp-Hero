using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private Material proceduralSkybox;

    [Range(0, 24)]
    [SerializeField] private float timeOfDay = 12f; // 0 = Midnight, 12 = Noon
    [SerializeField] private float dayDurationInSeconds = 120f;
    private int currentDay = 0;
    private bool hasAdvancedDayToday = false;

    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private Gradient sunColor;
    [SerializeField] private AnimationCurve lightIntensity;

    [SerializeField] private Gradient moonColor;

    [SerializeField] private Gradient skyTint;
    [SerializeField] private AnimationCurve atmosphereThickness;

    [SerializeField] private Gradient zenithGradient;   // Zenith/top of sky
    [SerializeField] private Gradient horizonGradient;

    [SerializeField] private float sunDistance = 1000f;

    [SerializeField] private float moonDistance = 1000f;

    [SerializeField] private AnimationCurve starVisibilityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private GameObject player;

    [SerializeField] private DayTextUI dayTextUI;

    [SerializeField] private EnemyPool enemyPool;

    void Start()
    {
        if (proceduralSkybox != null)
            RenderSettings.skybox = proceduralSkybox;
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;

        timeOfDay += (24f / dayDurationInSeconds) * Time.deltaTime;

        if (timeOfDay >= 6f && !hasAdvancedDayToday)
        {
            AdvanceDay();
            hasAdvancedDayToday = true;
        }

        if (timeOfDay >= 24f)
        {
            timeOfDay = 0f;
            hasAdvancedDayToday = false;
        }

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

        if (dayTextUI != null)
            dayTextUI.ShowDay(currentDay);

        if (enemyPool != null)
            enemyPool.AdjustPoolsForNewDay(currentDay);
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public bool IsNight()
    {
        return timeOfDay >= 18f || timeOfDay <= 6f;
    }

    public void SaveDayNight()
    {
        DayNightSaveData data = new()
        {
            timeOfDay = this.timeOfDay,
            currentDay = this.currentDay,
            hasAdvancedDayToday = this.hasAdvancedDayToday
        };

        SaveSystem.SaveDayNight(GameManager.Instance.currentWorldName, data);
    }

    public void LoadDayNight()
    {
        DayNightSaveData data = SaveSystem.LoadDayNight(GameManager.Instance.currentWorldName);
        if (data != null)
        {
            timeOfDay = data.timeOfDay;
            currentDay = data.currentDay;
            hasAdvancedDayToday = data.hasAdvancedDayToday;
        }
    }

    public float GetElapsedTime()
    {
        int daysPassed = currentDay - 1;
        float dayFraction = timeOfDay / 24f;

        return daysPassed + dayFraction;
    }
}
