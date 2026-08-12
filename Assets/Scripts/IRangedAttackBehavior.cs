using Game.AI.Enemies;
using UnityEngine;

public interface IRangedAttackBehavior
{
    void ExecuteAttack(Transform attacker, Transform target, int damage);
    RangedAttackType AttackType { get; }
}
