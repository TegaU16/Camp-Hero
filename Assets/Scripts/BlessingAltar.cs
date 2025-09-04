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
        if (hasBeenUsed)
            return;

        hasBeenUsed = true;

        PlayerStatsManager statsManager = PlayerStatsManager.Instance;
        statsManager.stats.strength.Value += 5;
        statsManager.stats.vitality.Value += 5;
        statsManager.stats.endurance.Value += 5;
        statsManager.stats.stamina.Value += 5;
        statsManager.stats.luck.Value += 5;

        statsManager.stats.availablePoints += 0; // optional

        Player player = FindFirstObjectByType<Player>();
        player.UpdateVitals();

        Debug.Log("Received Blessing: +5 to all stats");

        // Optional: play effect or disable altar visually
        Destroy(this); // or disable interaction
    }
}
