using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(EnemyPool))]
public class EnemyPoolEditor : Editor
{
    SerializedProperty enemyTiers;
    SerializedProperty poolSizePerTier;

    int previewDay = 1;

    void OnEnable()
    {
        enemyTiers = serializedObject.FindProperty("enemyTiers");
        poolSizePerTier = serializedObject.FindProperty("poolSizePerTier");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(poolSizePerTier);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Enemy Tiers", EditorStyles.boldLabel);

        for (int i = 0; i < enemyTiers.arraySize; i++)
        {
            SerializedProperty tier = enemyTiers.GetArrayElementAtIndex(i);
            SerializedProperty prefab = tier.FindPropertyRelative("prefab");
            SerializedProperty unlockDay = tier.FindPropertyRelative("unlockDay");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.PropertyField(prefab, new GUIContent("Prefab"));
            EditorGUILayout.PropertyField(unlockDay, new GUIContent("Unlock Day"));

            if (GUILayout.Button("Remove Tier"))
            {
                enemyTiers.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Enemy Tier"))
        {
            enemyTiers.InsertArrayElementAtIndex(enemyTiers.arraySize);
        }

        EditorGUILayout.Space(10);

        // Duplicate Check
        HashSet<GameObject> seenPrefabs = new();
        bool hasDuplicates = false;
        for (int i = 0; i < enemyTiers.arraySize; i++)
        {
            var prefabProp = enemyTiers.GetArrayElementAtIndex(i).FindPropertyRelative("prefab");
            if (prefabProp.objectReferenceValue is GameObject prefab)
            {
                if (!seenPrefabs.Add(prefab))
                {
                    hasDuplicates = true;
                }
            }
        }

        EditorGUILayout.HelpBox(hasDuplicates ? "⚠️ Duplicate enemy prefabs detected!" : "✓ No duplicate prefabs", MessageType.Info);

        // Preview Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Available Tiers", EditorStyles.boldLabel);
        previewDay = EditorGUILayout.IntSlider("Day", previewDay, 1, 50);

        List<string> availableNames = new();
        for (int i = 0; i < enemyTiers.arraySize; i++)
        {
            var tier = enemyTiers.GetArrayElementAtIndex(i);
            var prefabProp = tier.FindPropertyRelative("prefab");
            var unlockDayProp = tier.FindPropertyRelative("unlockDay");

            if (prefabProp.objectReferenceValue is GameObject prefab && previewDay >= unlockDayProp.intValue)
            {
                availableNames.Add(prefab.name);
            }
        }

        if (availableNames.Count > 0)
        {
            EditorGUILayout.HelpBox("Tiers available on Day " + previewDay + ": " + string.Join(", ", availableNames), MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("No enemy tiers available on this day.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
