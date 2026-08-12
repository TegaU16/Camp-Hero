using System.Collections.Generic;
using Game.Storage;
using UnityEngine;

namespace Game.Terrain.Structures
{
    [CreateAssetMenu(fileName = "StructureTemplate", menuName = "WorldGen/Structure Template")]
    public class StructureTemplate : ScriptableObject
    {
        public string structureName;
        public List<PrefabPart> prefabParts;

        [Header("Optional")]
        public LootTable lootTable;
    }

    [System.Serializable]
    public struct PrefabPart
    {
        public GameObject prefab;
        public Vector3 localOffset;
    }
}
