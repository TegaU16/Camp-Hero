using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Registries
{
    [Serializable]
    public class PrefabCategory
    {
        public string categoryName;
        public List<GameObject> prefabs = new();
    }

    public class PrefabRegistry : BaseRegistry<GameObject, string>
    {
        public static PrefabRegistry Instance;
        public List<PrefabCategory> categories = new();

        private new void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            allEntries = categories
            .SelectMany(c => c.prefabs)
            .Where(p => p != null)
            .Distinct()
            .ToArray();

            base.Awake();
        }

        protected override string GetKey(GameObject entry)
        {
            if (!entry.TryGetComponent(out PrefabID id))
            {
                Debug.LogWarning($"{entry.name} has NO PrefabID component!");
                return null;
            }

            return id.prefabKey;
        }

        protected override string GetCategoryName(GameObject entry)
        {
            foreach (PrefabCategory category in categories)
            {
                if (category.prefabs.Contains(entry)) return category.categoryName;
            }

            return null;
        }

        protected override void OnValidate()
        {
            if (requiredCategories == null || requiredCategories.Length == 0) return;

            foreach (string catName in requiredCategories)
            {
                if (!categories.Exists(c => c.categoryName == catName))
                    categories.Add(new PrefabCategory { categoryName = catName });
            }

            categories.Sort((a, b) =>
                Array.IndexOf(requiredCategories, a.categoryName)
                .CompareTo(Array.IndexOf(requiredCategories, b.categoryName)));
        }

        public List<GameObject> GetPrefabsInCategory(string categoryName)
        {
            PrefabCategory category = categories.Find(c => c.categoryName == categoryName);
            return category != null ? category.prefabs : new List<GameObject>();
        }
    }
}
