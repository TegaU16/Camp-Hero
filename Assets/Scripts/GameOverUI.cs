using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private GameObject categoryHeaderPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayStats(RunStats stats)
    {
        // --- Score ---
        scoreText.text = $"Score\n{stats.totalExpGained}";

        // --- Clear old UI entries ---
        foreach (Transform child in statsContainer)
            Destroy(child.gameObject);

        // --- Categories ---
        AddCategoryHeader("Survival");
        AddStat("Days Survived", stats.daysSurvived.ToString("0.0"));
        AddStat("Player Deaths", stats.playerDeaths.ToString());
        AddStat("Damage Taken", stats.damageTaken.ToString());

        AddCategoryHeader("Combat");
        AddStat("Enemies Defeated", stats.totalEnemiesDefeated.ToString());
        AddStat("Elite Enemies Defeated", stats.eliteEnemiesDefeated.ToString());
        AddStat("Blight Enemies Defeated", stats.blightEnemiesDefeated.ToString());
        AddStat("Animals Killed", stats.animalsKilled.ToString());
        AddStat("Cows Violated", stats.cowsViolated.ToString());
        AddStat("Total Damage Dealt", stats.totalDamageDealt.ToString());
        AddStat("Damage to Enemies", stats.damageDealtToEnemies.ToString());
        AddStat("Damage to Resources", stats.damageDealtToResources.ToString());

        AddCategoryHeader("Progression");
        AddStat("Experience Gained", stats.totalExpGained.ToString());
        AddStat("Structures Built", stats.structuresBuilt.ToString());
        AddStat("Resources Collected", stats.resourcesCollected.ToString());
    }

    private void AddStat(string label, string value)
    {
        GameObject row = Instantiate(statRowPrefab, statsContainer);
        row.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = label;
        row.transform.Find("Value").GetComponent<TextMeshProUGUI>().text = value;
    }

    private void AddCategoryHeader(string title)
    {
        GameObject header = Instantiate(categoryHeaderPrefab, statsContainer);
        header.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = title;
    }
}
