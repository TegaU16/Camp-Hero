using System.Collections.Generic;
using UnityEngine;

public class PrefabRegistry : MonoBehaviour
{
    public GameObject[] allPrefabs;

    private static Dictionary<string, GameObject> prefabDict;
    private static Dictionary<GameObject, string> reverseDict;

    void Awake()
    {
        if (prefabDict != null && prefabDict.Count > 0) return; // already initialized

        prefabDict = new();
        reverseDict = new();

        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab == null) continue;

            PrefabID id = prefab.GetComponent<PrefabID>();
            if (id == null || string.IsNullOrEmpty(id.prefabKey))
            {
                Debug.LogWarning($"Prefab {prefab.name} has no PrefabID key!");
                continue;
            }

            prefabDict[id.prefabKey] = prefab;
            reverseDict[prefab] = id.prefabKey;
        }
    }

    public static GameObject GetPrefabByKey(string key) =>
        prefabDict.TryGetValue(key, out GameObject prefab) ? prefab : null;

    public static string GetKeyForPrefab(GameObject prefab) =>
        reverseDict.TryGetValue(prefab, out string key) ? key : null;
}
