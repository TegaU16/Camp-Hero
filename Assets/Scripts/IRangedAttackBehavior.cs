using UnityEngine;

public interface IRangedAttackBehavior
{
    void ExecuteAttack(Transform attacker, Transform target, int damage);
    string AttackName { get; }
}
