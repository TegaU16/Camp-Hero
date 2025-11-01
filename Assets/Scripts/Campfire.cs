using System.Collections.Generic;
using UnityEngine;

public enum GemColor
{
    Blue,
    Green,
    Red,
    Yellow
}

public class Campfire : MonoBehaviour, IInteractable, ISaveableObject
{
    public Health health;

    [Header("Gem References")]
    public GameObject blueGem;
    public GameObject greenGem;
    public GameObject redGem;
    public GameObject yellowGem;

    private readonly List<GemColor> unlockedGems = new();
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
        if (!unlockedGems.Contains(color))
        {
            unlockedGems.Add(color);
            UpdateGemVisibility();
        }
    }

    public void RemoveGem(GemColor color)
    {
        if (unlockedGems.Remove(color))
            UpdateGemVisibility();
    }

    private void UpdateGemVisibility()
    {
        foreach (KeyValuePair<GemColor, GameObject> pair in gemObjects)
            pair.Value.SetActive(unlockedGems.Contains(pair.Key));
    }

    public void Interact()
    {
        InventoryManager.Instance.campfireMenuUI.SetActive(true);
        GameManager.Instance.ToggleCameraFollow(false);
    }

    public string GetInteractText() => "Open Campfire Menu";

    public Transform GetTransform() => transform;

    public void Die()
    {
        GameManager.Instance.GameOver();
    }

    public string SaveState()
    {
        CampfireSaveData data = new()
        {
            currentHealth = health.GetHealth(),
            unlockedGems = new List<GemColor>(unlockedGems)
        };

        return JsonUtility.ToJson(data);
    }

    public void LoadState(string json)
    {
        CampfireSaveData data = JsonUtility.FromJson<CampfireSaveData>(json);
        GameManager.Instance.SetCampfire(gameObject, data);

        if (data == null)
        {
            health.SetHealth(health.maxHealth);
            UpdateGemVisibility();
        }
        else
        {
            health.SetHealth(data.currentHealth);

            unlockedGems.Clear();
            foreach (GemColor color in data.unlockedGems)
                unlockedGems.Add(color);

            UpdateGemVisibility();
        }
    }
}
