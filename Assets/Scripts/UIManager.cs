using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly Dictionary<string, HealthBar> healthBars = new();
    private StaminaBar staminaBar;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void RegisterHealthBar(string id, HealthBar hb)
    {
        healthBars[id] = hb;
        CheckReady();
    }

    public void RegisterStaminaBar(StaminaBar sb)
    {
        staminaBar = sb;
        CheckReady();
    }

    private void CheckReady()
    {
        if (healthBars.ContainsKey("Player") && staminaBar != null)
        {
            IsInitialized = true;
        }
    }

    public HealthBar GetHealthBar(string id)
    {
        healthBars.TryGetValue(id, out HealthBar hb);
        return hb;
    }

    public StaminaBar GetStaminaBar() => staminaBar;
}
