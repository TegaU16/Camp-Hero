using UnityEngine;

public class Targetable : MonoBehaviour
{
    public enum TargetType
    {
        Campfire,
        Player,
        Structure
    }

    public TargetType targetType;
}
