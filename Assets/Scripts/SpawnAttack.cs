using UnityEngine;

public class SpawnAttack : MonoBehaviour, IRangedAttackBehavior
{
    [SerializeField] private GameObject spawnPrefab;

    public string AttackName => "SpawnOnTarget";

    public void ExecuteAttack(Transform attacker, Transform target, int damage)
    {
        if (spawnPrefab == null || target == null) return;

        Instantiate(spawnPrefab, target.position, Quaternion.identity);
    }
}
