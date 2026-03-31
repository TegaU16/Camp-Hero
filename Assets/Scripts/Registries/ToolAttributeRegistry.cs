using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Registries
{
    public class ToolAtributeRegistry : MonoBehaviour
    {
        public ToolAttribute[] allAttributes;

        private static Dictionary<string, ToolAttribute> attributeDict;

        private void Awake()
        {
            if (attributeDict != null && attributeDict.Count > 0) return;

            attributeDict = new Dictionary<string, ToolAttribute>();

            foreach (ToolAttribute attribute in allAttributes)
            {
                if (attribute != null && !attributeDict.ContainsKey(attribute.name))
                    attributeDict[attribute.attributeID] = attribute;
            }
        }

        public static ToolAttribute GetToolAttributeByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (attributeDict != null && attributeDict.TryGetValue(name, out ToolAttribute attribute)) return attribute;

            Debug.LogWarning($"ToolAttribute not found: {name}");
            return null;
        }
    }
}
