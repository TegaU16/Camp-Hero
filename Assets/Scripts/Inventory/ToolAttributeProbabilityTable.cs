using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(menuName = "Item/Tool Attribute Probability Table")]
    public class ToolAttributeProbabilityTable : ScriptableObject
    {
        public WeightedTable<ToolAttribute> weightedTable = new();

        public ToolAttribute GetRandomToolAttribute(ToolType toolType)
        {
            return weightedTable.Roll(attr => attr != null && (attr.toolType & toolType) != 0);
        }
    }
}
