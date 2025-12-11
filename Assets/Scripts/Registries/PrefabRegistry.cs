using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Registries
{
    public class PrefabRegistry : MonoBehaviour
    {
        [Serializable]
        public class PrefabCategory
        {
            public string categoryName;
            public List<GameObject> prefabs = new();
        }

        public List<PrefabCategory> categories = new()
    {
        new PrefabCategory { categoryName = "Resources" },
        new PrefabCategory { categoryName = "Builds" },
        new PrefabCategory { categoryName = "Drops" },
        new PrefabCategory { categoryName = "Enemies" },
        new PrefabCategory { categoryName = "Animals" },
        new PrefabCategory { categoryName = "General Structures" },
        new PrefabCategory { categoryName = "Important Structures" },
    };

        private static Dictionary<string, GameObject> prefabDict;

        void Awake()
        {
            if (prefabDict != null && prefabDict.Count > 0) return;

            prefabDict = new();

            foreach (PrefabCategory category in categories)
            {
                foreach (GameObject prefab in category.prefabs)
                {
                    if (prefab == null) continue;

                    PrefabID id = prefab.GetComponent<PrefabID>();
                    if (id == null || string.IsNullOrEmpty(id.prefabKey))
                    {
                        Debug.LogWarning($"Prefab {prefab.name} in {category.categoryName} has no PrefabID key!");
                        continue;
                    }

                    prefabDict[id.prefabKey] = prefab;
                }
            }
        }

        public static GameObject GetPrefabByKey(string key) =>
            prefabDict.TryGetValue(key, out GameObject prefab) ? prefab : null;
    }
}
