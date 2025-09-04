using TMPro;
using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance;
    public TextMeshProUGUI availablePointsText;
    public TMP_InputField pointsToAddField;
    public PlayerStats stats = new();
    private Player player;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        availablePointsText.text = stats.availablePoints.ToString();
    }

    public void AllocatePoint(string statName)
    {
        if (stats.availablePoints <= 0)
            return;

        int pointsToAdd = 1;

        string enteredPointsToAdd = pointsToAddField.text;
        if (int.TryParse(enteredPointsToAdd, out int num))
        {
            pointsToAdd = Mathf.Min(num, stats.availablePoints);
        }

        switch (statName.ToLower())
        {
            case "strength":
                stats.strength.Value += pointsToAdd;
                stats.strength.LevelText.text = $"Lv. {stats.strength.Value + 1}";
                break;
            case "vitality":
                stats.vitality.Value += pointsToAdd;
                stats.vitality.LevelText.text = $"Lv. {stats.vitality.Value + 1}";
                break;
            case "endurance":
                stats.endurance.Value += pointsToAdd;
                stats.endurance.LevelText.text = $"Lv. {stats.endurance.Value + 1}";
                break;
            case "stamina":
                stats.stamina.Value += pointsToAdd;
                stats.stamina.LevelText.text = $"Lv. {stats.stamina.Value + 1}";
                break;
            case "luck":
                stats.luck.Value += pointsToAdd;
                stats.luck.LevelText.text = $"Lv. {stats.luck.Value + 1}";
                break;
            default:
                Debug.LogWarning("Invalid stat name");
                return;
        }

        stats.availablePoints -= pointsToAdd;
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

    public void UpdateAvailablePoints()
    {
        availablePointsText.text = $"{stats.availablePoints}";
    }

    public void SetPlayer(GameObject playerObj)
    {
        if (playerObj.TryGetComponent(out Player playerScript))
        {
            player = playerScript;
        }
    }
}
