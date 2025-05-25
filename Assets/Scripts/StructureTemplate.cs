using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StructureTemplate", menuName = "WorldGen/Structure Template")]
public class StructureTemplate : ScriptableObject
{
    public string structureName;
    public List<GameObject> prefabParts;
    public List<Vector3> localOffsets; // Same count as prefabParts
    public List<Vector2Int> localSizes; // Same count as prefabParts
}
