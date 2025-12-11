using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain.Structures
{
    [CreateAssetMenu(fileName = "StructureTemplate", menuName = "WorldGen/Structure Template")]
    public class StructureTemplate : ScriptableObject
    {
        public string structureName;
        public List<GameObject> prefabParts;
        public List<Vector3> localOffsets; // Same count as prefabParts
    }
}
