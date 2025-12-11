using Game.Players;
using UnityEngine;

public class BlessingAltar : MonoBehaviour, IInteractable
{
    private bool hasBeenUsed = false;

    public string GetInteractText()
    {
        throw new System.NotImplementedException();
    }

    public Transform GetTransform()
    {
        throw new System.NotImplementedException();
    }

    public void Interact()
    {
        if (hasBeenUsed) return;

        hasBeenUsed = true;

        PlayerStatsManager statsManager = PlayerStatsManager.Instance;
        PlayerStats stats = statsManager.stats;

        statsManager.UpgradeStat(stats.strength, 5);
        statsManager.UpgradeStat(stats.vitality, 5);
        statsManager.UpgradeStat(stats.endurance, 5);
        statsManager.UpgradeStat(stats.stamina, 5);
        statsManager.UpgradeStat(stats.luck, 5);

        stats.goldenPoints++;

        Player player = FindFirstObjectByType<Player>();
        player.UpdateVitals();

        // Optional: play effect or disable altar visually
        Destroy(this); // or disable interaction
    }
}
