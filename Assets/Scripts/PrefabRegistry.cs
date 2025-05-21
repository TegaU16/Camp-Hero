using System.Collections.Generic;
using UnityEngine;

public class PrefabRegistry : MonoBehaviour
{
    public GameObject[] allPrefabs;

    private static Dictionary<string, GameObject> prefabDict;

    void Awake()
    {
        if (prefabDict != null && prefabDict.Count > 0) return; // Avoid overwriting
        prefabDict = new();
        foreach (GameObject prefab in allPrefabs)
            if (prefab != null)
                prefabDict[prefab.name] = prefab;
    }

    public static GameObject GetPrefabByName(string name)
    {
        return prefabDict.TryGetValue(name, out GameObject prefab) ? prefab : null;
    }
}
