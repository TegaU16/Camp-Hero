using UnityEngine;
using UnityEditor;

public static class FindAllMissingReferences
{
    [MenuItem("Tools/Find Missing Scripts & References (Assets + Scene)")]
    public static void FindEverything()
    {
        int assetMissingScripts = 0;
        int assetMissingRefs = 0;
        int sceneMissingScripts = 0;
        int sceneMissingRefs = 0;

        // =============================
        // ASSETS: PREFABS & GAMEOBJECTS
        // =============================
        string[] gameObjectGUIDs = AssetDatabase.FindAssets("t:GameObject");

        for (int i = 0; i < gameObjectGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(gameObjectGUIDs[i]);
            if (path.StartsWith("Packages/")) continue;

            EditorUtility.DisplayProgressBar(
                "Checking Asset GameObjects",
                path,
                (float)i / gameObjectGUIDs.Length
            );

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;

            ScanGameObject(
                root,
                path,
                isSceneObject: false,
                ref assetMissingScripts,
                ref assetMissingRefs
            );
        }

        // =============================
        // ASSETS: SCRIPTABLEOBJECTS
        // =============================
        string[] soGUIDs = AssetDatabase.FindAssets("t:ScriptableObject");

        for (int i = 0; i < soGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(soGUIDs[i]);
            if (path.StartsWith("Packages/")) continue;

            EditorUtility.DisplayProgressBar(
                "Checking ScriptableObjects",
                path,
                (float)i / soGUIDs.Length
            );

            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) continue;

            ScanSerializedObject(
                asset,
                $"[ASSET][SO] {path}",
                ref assetMissingRefs
            );
        }

        // =============================
        // ASSETS: MISSING SCRIPTABLEOBJECT SCRIPTS
        // =============================
        string[] allAssetGUIDs = AssetDatabase.FindAssets("");

        for (int i = 0; i < allAssetGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allAssetGUIDs[i]);

            if (!path.StartsWith("Assets/")) continue;

            Object[] assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (Object asset in assetsAtPath)
            {
                if (asset == null)
                {
                    Debug.LogWarning(
                        $"[ASSET][SO] Missing ScriptableObject script at path: {path}"
                    );
                    assetMissingScripts++;
                    break;
                }
            }
        }

        // =============================
        // SCENE OBJECTS
        // =============================
        EditorUtility.DisplayProgressBar("Checking Scene Objects", "Scanning open scenes...", 1f);

        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in sceneObjects)
        {
            ScanGameObject(
                go,
                go.scene.name,
                isSceneObject: true,
                ref sceneMissingScripts,
                ref sceneMissingRefs
            );
        }

        EditorUtility.ClearProgressBar();

        Debug.Log(
            $"Scan Complete\n\n" +
            $"[ASSETS]\n" +
            $"  Missing Scripts: {assetMissingScripts}\n" +
            $"  Missing References: {assetMissingRefs}\n\n" +
            $"[SCENE]\n" +
            $"  Missing Scripts: {sceneMissingScripts}\n" +
            $"  Missing References: {sceneMissingRefs}"
        );
    }

    // -----------------------------
    // GAMEOBJECT SCAN (ASSET / SCENE)
    // -----------------------------
    private static void ScanGameObject(
        GameObject root,
        string context,
        bool isSceneObject,
        ref int missingScripts,
        ref int missingRefs
    )
    {
        Component[] components = root.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            // Missing MonoBehaviour
            if (component == null)
            {
                Debug.LogWarning(
                    $"{(isSceneObject ? "[SCENE]" : "[ASSET]")} Missing script in {context}",
                    root
                );
                missingScripts++;
                break;
            }

            ScanSerializedObject(
                component,
                $"{(isSceneObject ? "[SCENE]" : "[ASSET]")} {context} ? {component.GetType().Name}",
                ref missingRefs
            );
        }
    }

    // -----------------------------
    // SERIALIZED PROPERTY SCAN
    // -----------------------------
    private static void ScanSerializedObject(
        Object obj,
        string context,
        ref int missingRefs
    )
    {
        SerializedObject so = new(obj);
        SerializedProperty prop = so.GetIterator();

        while (prop.NextVisible(true))
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                prop.objectReferenceValue == null &&
                prop.objectReferenceInstanceIDValue != 0)
            {
                Debug.LogWarning(
                    $"Missing reference: {context}\nField: {prop.displayName}",
                    obj
                );
                missingRefs++;
                break;
            }
        }
    }
}
