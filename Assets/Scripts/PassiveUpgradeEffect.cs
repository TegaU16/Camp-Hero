using UnityEngine;

public abstract class PassiveUpgradeEffect : UpgradeEffect
{
    // Optionally track whether it’s currently active
    protected bool isActive;

    public override void OnUnlocked(Player player)
    {
        isActive = true;
    }

    public override void OnRemoved(Player player)
    {
        isActive = false;
    }
}
