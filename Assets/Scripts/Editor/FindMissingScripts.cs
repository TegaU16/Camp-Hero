using UnityEngine;
using UnityEditor;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]

    static void Init()
    {
        ShowWindow();
    }

    public static void ShowWindow()
    {
        int missingCount = 0;
        GameObject[] go = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject g in go)
        {
            Component[] components = g.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"Missing script in GameObject: {g.name}", g);
                    missingCount++;
                }
            }
        }

        Debug.Log($"Scan complete. Found {missingCount} missing script references.");
    }
}
