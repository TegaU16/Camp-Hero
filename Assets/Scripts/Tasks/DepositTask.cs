using UnityEngine;

[CreateAssetMenu(menuName = "CompanionTasks/Deposit")]
public class DepositTask : CompanionTaskSO
{
    public BreakableObject.ObjectType resourceToDeposit;
    public int amountToDeposit = 1;

    public override void Execute(Companion companion)
    {
        Debug.Log($"Depositing {amountToDeposit} {resourceToDeposit}...");
        // You can call companion.Deposit(resourceToDeposit, amountToDeposit) here
    }
}
