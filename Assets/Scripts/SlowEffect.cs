using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Combat/Status Effects/Slow")]
public class SlowEffect : StatusEffect
{
    [Range(0f, 1f)] public float slowFactor = 0.5f;

    public override void Apply(Enemy target)
    {
        target.StartCoroutine(SlowRoutine(target));
    }

    private IEnumerator SlowRoutine(Enemy target)
    {
        target.ModifySpeed(slowFactor);
        yield return new WaitForSeconds(duration);
        target.ModifySpeed(1f); // reset to normal
    }
}
