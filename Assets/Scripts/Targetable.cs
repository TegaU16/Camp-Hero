using System.Collections.Generic;
using UnityEngine;

public class Targetable : MonoBehaviour
{
    public enum TargetType
    {
        Campfire,
        Player,
        Structure,
        Wall,
        Defense
    }

    public TargetType targetType;

    private void OnEnable()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterTarget(this);
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterTarget(this);
    }
}
