using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain.Structures
{
    [CreateAssetMenu(fileName = "StructureTemplate", menuName = "WorldGen/Structure Template")]
    public class StructureTemplate : ScriptableObject
    {
        public string structureName;
        public List<PrefabPart> prefabParts;
    }

    [System.Serializable]
    public struct PrefabPart
    {
        public GameObject prefab;
        public Vector3 localOffset;
    }
}
