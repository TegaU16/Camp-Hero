using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Combat/Status Effects/Stun")]
public class StunEffect : StatusEffect
{
    public override void Apply(Enemy target)
    {
        target.StartCoroutine(StunRoutine(target));
    }

    private IEnumerator StunRoutine(Enemy target)
    {
        target.ModifySpeed(0f);
        yield return new WaitForSeconds(duration);
        target.ResetMovement();
    }
}
