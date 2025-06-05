using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Range(0, 24)]
    public float timeOfDay = 12f; // 0 = Midnight, 12 = Noon
    public float dayDurationInSeconds = 120f;
    private int currentDay = 0;
    private bool hasAdvancedDayToday = false;

    public Light sun;
    public Light moon;

    public Gradient sunColor;
    public AnimationCurve lightIntensity;

    public Gradient moonColor;

    public Gradient skyTint;
    public AnimationCurve atmosphereThickness;

    private Material skyboxMaterial;

    public GameObject sunVisual;
    public float sunDistance = 1000f;

    public GameObject moonVisual;
    public float moonDistance = 1000f;

    private GameObject player;

    public DayTextUI dayTextUI;

    void Start()
    {
        skyboxMaterial = RenderSettings.skybox;
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        timeOfDay += (24 / dayDurationInSeconds) * Time.deltaTime;

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

        float normalizedTime = timeOfDay / 24f;

        // Light intensity
        float sunIntensity = lightIntensity.Evaluate(normalizedTime);
        float moonIntensity = 1f - sunIntensity;

        sun.intensity = Mathf.Max(0.01f, sunIntensity);
        moon.intensity = Mathf.Max(0.01f, moonIntensity * 0.3f);

        sun.color = sunColor.Evaluate(normalizedTime);
        moon.color = moonColor.Evaluate(normalizedTime);

        // Skybox properties
        skyboxMaterial.SetColor("_SkyTint", skyTint.Evaluate(normalizedTime));
        skyboxMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness.Evaluate(normalizedTime));

        Color sunAmbient = sunColor.Evaluate(normalizedTime) * sun.intensity;
        Color moonAmbient = moonColor.Evaluate(normalizedTime) * moon.intensity;

        RenderSettings.ambientLight = (sunAmbient + moonAmbient) * 0.5f;

        DynamicGI.UpdateEnvironment();

        if (player != null)
        {
            // Compute the pivot point (center of sky rotation) to be the player
            Vector3 center = player.transform.position;

            // Calculate sun direction
            Quaternion sunRotation = Quaternion.Euler((timeOfDay - 6f) * 15f, 170f, 0);
            Vector3 sunDirection = sunRotation * Vector3.forward;

            // Sun position & rotation
            Vector3 sunPosition = center - sunDirection * sunDistance;
            sun.transform.position = sunPosition;
            sun.transform.LookAt(center); // Ensures the directional light is centered on the player

            sunVisual.transform.position = sunPosition;
            sunVisual.transform.LookAt(player.transform);
            sunVisual.transform.Rotate(0f, 180f, 0f);

            // Moon direction is opposite the sun
            Vector3 moonDirection = -sunDirection;
            Vector3 moonPosition = center - moonDirection * moonDistance;

            moon.transform.position = moonPosition;
            moon.transform.LookAt(center);

            moonVisual.transform.position = moonPosition;
            moonVisual.transform.LookAt(player.transform);
            moonVisual.transform.Rotate(0f, 180f, 0f);
        }
    }

    public float GetSunlightIntensity()
    {
        return sun.intensity;
    }

    public void SetPlayer(GameObject player)
    {
        if (player != null)
        {
            this.player = player;
        }
        else
        {
            Debug.LogWarning("Player is null!");
        }
    }

    public void AdvanceDay()
    {
        currentDay++;
        if (dayTextUI != null)
        {
            dayTextUI.ShowDay(currentDay);
        }
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public bool IsNight()
    {
        return timeOfDay >= 18f || timeOfDay <= 6f;
    }
}
