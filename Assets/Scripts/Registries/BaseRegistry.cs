using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Game.Registries
{
    public abstract class BaseRegistry<T, TKey> : MonoBehaviour
        where T : Object
    {
        public T[] allEntries;
        public string[] requiredCategories;

        protected Dictionary<TKey, T> entryDict;

        // Abstract method: how to get a key from an entry
        protected abstract TKey GetKey(T entry);

        // Optional method: get category name if entry is categorized
        protected virtual string GetCategoryName(T entry) => null;

        protected virtual void Awake()
        {
            entryDict = new Dictionary<TKey, T>();

            foreach (T entry in allEntries)
            {
                if (entry == null) continue;

                TKey key = GetKey(entry);
                if (key == null || key.Equals(default(TKey)))
                {
                    Debug.LogWarning($"Entry {entry.name} has an invalid key!");
                    continue;
                }

                if (!entryDict.ContainsKey(key))
                    entryDict[key] = entry;
            }
        }

        public T GetByKey(TKey key)
        {
            if (entryDict == null)
            {
                Debug.LogError("[PrefabRegistry] Registry not initialized yet!");
                return null;
            }

            if (key == null)
            {
                Debug.LogError("[PrefabRegistry] Requested key is NULL!");
                return null;
            }

            if (entryDict.TryGetValue(key, out T entry)) return entry;

            return null;
        }

        public List<T> GetByKeys(List<TKey> keys)
        {
            List<T> results = new();
            foreach (TKey key in keys)
            {
                T entry = GetByKey(key);
                if (entry != null)
                    results.Add(entry);
            }
            return results;
        }

        protected virtual void OnValidate()
        {
            if (requiredCategories == null || requiredCategories.Length == 0) return;

            foreach (string category in requiredCategories)
            {
                bool exists = allEntries.Any(e => GetCategoryName(e) == category);
                if (!exists)
                    Debug.LogWarning($"Category {category} is required but has no entries!");
            }
        }
    }
}
