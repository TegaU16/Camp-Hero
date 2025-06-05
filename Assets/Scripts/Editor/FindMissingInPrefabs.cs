using UnityEngine;
using UnityEditor;
using System.IO;

public class FindMissingInPrefabs
{
    [MenuItem("Tools/Find Missing Scripts In All Assets")]
    public static void FindMissingScriptsInAssets()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:GameObject");
        int count = 0;

        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            Component[] components = prefab.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"Missing script in prefab: {path}", prefab);
                    count++;
                    break;
                }
            }
        }

        Debug.Log($"Finished checking ALL GameObjects. Found {count} with missing scripts.");
    }
}
