using UnityEngine;

namespace Game.Terrain.Structures
{
    [CreateAssetMenu(fileName = "StructureTable", menuName = "WorldGen/Structure Table")]
    public class StructureTable : ScriptableObject
    {
        public WeightedTable<StructureTemplate> weightedTable = new();

        public StructureTemplate GetRandomStructure(int hash)
        {
            if (weightedTable.Entries.Count == 0) return null;
            return weightedTable.Roll(hash);
        }
    }
}
