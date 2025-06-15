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
}
