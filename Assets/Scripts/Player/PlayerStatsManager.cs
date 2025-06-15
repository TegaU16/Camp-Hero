using TMPro;
using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance;
    public TextMeshProUGUI availablePointsText;
    public PlayerStats stats = new();
    private Player player;

    private void Awake()
    {
        Instance = this;
    }

    public void AllocatePoint(string statName)
    {
        if (stats.availablePoints <= 0)
            return;

        switch (statName.ToLower())
        {
            case "strength":
                stats.strength.Value++;
                stats.strength.LevelText.text = $"Lv. {stats.strength.Value + 1}";
                break;
            case "vitality":
                stats.vitality.Value++;
                stats.vitality.LevelText.text = $"Lv. {stats.vitality.Value + 1}";
                break;
            case "endurance":
                stats.endurance.Value++;
                stats.endurance.LevelText.text = $"Lv. {stats.endurance.Value + 1}";
                break;
            case "stamina":
                stats.stamina.Value++;
                stats.stamina.LevelText.text = $"Lv. {stats.stamina.Value + 1}";
                break;
            case "luck":
                stats.luck.Value++;
                stats.luck.LevelText.text = $"Lv. {stats.luck.Value + 1}";
                break;
            default:
                Debug.LogWarning("Invalid stat name");
                return;
        }

        stats.availablePoints--;
        UpdateAvailablePoints();

        if (player != null)
        {
            player.UpdateVitals();
        }
    }

    public void AddPoints(int points)
    {
        stats.availablePoints += points;
        UpdateAvailablePoints();
    }

    private void UpdateAvailablePoints()
    {
        availablePointsText.text = $"Available Points: {stats.availablePoints}";
    }

    public void SetPlayer(GameObject playerObj)
    {
        if (playerObj.TryGetComponent(out Player playerScript))
        {
            player = playerScript;
        }
    }
}
