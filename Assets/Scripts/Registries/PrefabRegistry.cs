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

        private static readonly string[] RequiredCategories =
        {
            "Resources",
            "Builds",
            "Drops",
            "Enemies",
            "Animals",
            "General Structures",
            "Important Structures",
            "Effects",
            "Text Notifications"
        };

        public List<PrefabCategory> categories = new();

        public static PrefabRegistry Instance { get; private set; }

        private static Dictionary<string, GameObject> prefabDict;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

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

        private void OnValidate()
        {
            foreach (string categoryName in RequiredCategories)
            {
                if (!categories.Exists(c => c.categoryName == categoryName))
                    categories.Add(new PrefabCategory { categoryName = categoryName });
            }

            categories.Sort((a, b) =>
                Array.IndexOf(RequiredCategories, a.categoryName)
                .CompareTo(Array.IndexOf(RequiredCategories, b.categoryName)));
        }

        public static GameObject GetPrefabByKey(string key) =>
            prefabDict.TryGetValue(key, out GameObject prefab) ? prefab : null;

        public static List<GameObject> GetPrefabsInCategory(string categoryName)
        {
            PrefabCategory category = Instance.categories.Find(c => c.categoryName == categoryName);
            return category != null ? category.prefabs : new List<GameObject>();
        }
    }
}