using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeData))]
public class BiomeDataEditor : Editor
{
    SerializedProperty biomeName;
    SerializedProperty noiseSettings;
    SerializedProperty treePrefabs;
    SerializedProperty rockPrefabs;
    SerializedProperty treeClusterPrefabs;
    SerializedProperty rockClusterPrefabs;

    void OnEnable()
    {
        if (target == null) return; // Avoid SerializedObjectNotCreatableException

        biomeName = serializedObject.FindProperty("biomeName");
        noiseSettings = serializedObject.FindProperty("noiseSettings");
        treePrefabs = serializedObject.FindProperty("treePrefabs");
        rockPrefabs = serializedObject.FindProperty("rockPrefabs");
        treeClusterPrefabs = serializedObject.FindProperty("treeClusterPrefabs");
        rockClusterPrefabs = serializedObject.FindProperty("rockClusterPrefabs");
    }

    public override void OnInspectorGUI()
    {
        if (target == null || serializedObject == null)
            return;

        serializedObject.Update();

        EditorGUILayout.PropertyField(biomeName, new GUIContent("Biome Name"));
        EditorGUILayout.PropertyField(noiseSettings, new GUIContent("Noise Settings"), true);
        EditorGUILayout.PropertyField(treePrefabs, new GUIContent("Tree Prefabs"), true);
        EditorGUILayout.PropertyField(rockPrefabs, new GUIContent("Rock Prefabs"), true);
        EditorGUILayout.PropertyField(treeClusterPrefabs, new GUIContent("Tree Cluster Prefabs"), true);
        EditorGUILayout.PropertyField(rockClusterPrefabs, new GUIContent("Rock Cluster Prefabs"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
