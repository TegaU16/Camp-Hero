using System.Collections.Generic;
using UnityEngine;

public enum GemColor
{
    Blue,
    Green,
    Red,
    Yellow
}

public class Campfire : MonoBehaviour, IInteractable
{
    public Health health;

    [Header("Gem References")]
    public GameObject blueGem;
    public GameObject greenGem;
    public GameObject redGem;
    public GameObject yellowGem;

    private readonly HashSet<GemColor> unlockedGems = new();
    private Dictionary<GemColor, GameObject> gemObjects;

    private void Awake()
    {
        gemObjects = new Dictionary<GemColor, GameObject>
        {
            { GemColor.Blue, blueGem },
            { GemColor.Green, greenGem },
            { GemColor.Red, redGem },
            { GemColor.Yellow, yellowGem }
        };
    }

    public void UnlockGem(GemColor color)
    {
        if (unlockedGems.Add(color))
        {
            UpdateGemVisibility();
        }
    }

    public void RemoveGem(GemColor color)
    {
        if (unlockedGems.Remove(color))
        {
            UpdateGemVisibility();
        }
    }

    private void UpdateGemVisibility()
    {
        foreach (KeyValuePair<GemColor, GameObject> pair in gemObjects)
        {
            pair.Value.SetActive(unlockedGems.Contains(pair.Key));
        }
    }

    public void Interact()
    {
        InventoryManager.Instance.campfireMenuUI.SetActive(true);
    }

    public string GetInteractText() => "Open Campfire Menu";

    public Transform GetTransform() => transform;

    public void Die()
    {
        GameManager.Instance.GameOver();
    }

    public CampfireSaveData GetSaveData()
    {
        return new CampfireSaveData
        {
            currentHealth = health.GetHealth(),
            unlockedGems = new List<GemColor>(unlockedGems)
        };
    }

    public void LoadFromSaveData(CampfireSaveData data)
    {
        health.SetHealth(data.currentHealth);

        unlockedGems.Clear();
        foreach (GemColor color in data.unlockedGems)
        {
            unlockedGems.Add(color);
        }
        UpdateGemVisibility();
    }

    public void LoadDefault()
    {
        health.SetHealth(health.maxHealth);
        UpdateGemVisibility();
    }
}
