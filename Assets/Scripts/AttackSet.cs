using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attack Set", fileName = "New AttackSet")]
public class AttackSet : ScriptableObject
{
    public AttackData[] comboAttacks;  // array of attacks, step 1 → step 2 → step 3
}
