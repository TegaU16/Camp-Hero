using Game.Players;
using UnityEngine;
using static Game.Players.PlayerStats;

public class BlessingAltar : MonoBehaviour, IInteractable
{
    private bool hasBeenUsed = false;

    [SerializeField] private Sprite altarIcon;
    public Sprite ObjectIcon => altarIcon;

    public string GetInteractText() => "Receive Blessing\n<color=#27ef60>\"E\"</color>";

    public Transform GetTransform() => transform;

    public void Interact()
    {
        if (hasBeenUsed) return;

        hasBeenUsed = true;

        PlayerStatsManager statsManager = PlayerStatsManager.Instance;

        foreach (Stat stat in statsManager.AllStats)
            statsManager.UpgradeStat(stat, 5);

        statsManager.AddPoints(regularPoints: 0, goldenPoints: 5);

        Player player = FindFirstObjectByType<Player>();
        player.UpdateVitals();

        // Optional: play effect or disable altar visually
        Destroy(this); // or disable interaction
    }
}
