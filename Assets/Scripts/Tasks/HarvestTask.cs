using UnityEngine;

[CreateAssetMenu(menuName = "CompanionTasks/Harvest")]
public class HarvestTask : CompanionTaskSO
{
    public BreakableObject.ObjectType resourceToHarvest;
    public int amountToHarvest = 1;

    public override void Execute(Companion companion)
    {
        companion.StartHarvesting(resourceToHarvest, amountToHarvest);
    }
}
