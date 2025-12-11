using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Game.AI.Enemies;

[CustomEditor(typeof(EnemyPool))]
public class EnemyPoolEditor : Editor
{
    SerializedProperty enemyTiers;

    SerializedProperty dayNightCycle;
    SerializedProperty poolGraveyardPosition;

    SerializedProperty dailyGrowthRate;
    SerializedProperty maxGrowthMultiplier;

    int previewDay = 1;

    void OnEnable()
    {
        enemyTiers = serializedObject.FindProperty("enemyTiers");

        dayNightCycle = serializedObject.FindProperty("dayNightCycle");
        poolGraveyardPosition = serializedObject.FindProperty("poolGraveyardPosition");

        dailyGrowthRate = serializedObject.FindProperty("dailyGrowthRate");
        maxGrowthMultiplier = serializedObject.FindProperty("maxGrowthMultiplier");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dayNightCycle, new GUIContent("Day Night Cycle"));
        EditorGUILayout.PropertyField(poolGraveyardPosition, new GUIContent("Pool Graveyard Position"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Pool Growth Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dailyGrowthRate, new GUIContent("Daily Growth Rate"));
        EditorGUILayout.PropertyField(maxGrowthMultiplier, new GUIContent("Max Growth Multiplier"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Enemy Tiers", EditorStyles.boldLabel);

        for (int i = 0; i < enemyTiers.arraySize; i++)
        {
            SerializedProperty tier = enemyTiers.GetArrayElementAtIndex(i);
            SerializedProperty prefab = tier.FindPropertyRelative("prefab");
            SerializedProperty unlockDay = tier.FindPropertyRelative("unlockDay");
            SerializedProperty basePoolSize = tier.FindPropertyRelative("poolSize");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.PropertyField(prefab, new GUIContent("Prefab"));
            EditorGUILayout.PropertyField(unlockDay, new GUIContent("Unlock Day"));
            EditorGUILayout.PropertyField(basePoolSize, new GUIContent("Base Pool Size"));

            if (GUILayout.Button("Remove Tier"))
            {
                enemyTiers.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Enemy Tier"))
            enemyTiers.InsertArrayElementAtIndex(enemyTiers.arraySize);

        EditorGUILayout.Space(10);

        // Duplicate Check
        HashSet<GameObject> seenPrefabs = new();
        bool hasDuplicates = false;
        for (int i = 0; i < enemyTiers.arraySize; i++)
        {
            SerializedProperty prefabProp = enemyTiers.GetArrayElementAtIndex(i).FindPropertyRelative("prefab");
            if (prefabProp.objectReferenceValue is GameObject prefab && !seenPrefabs.Add(prefab))
                hasDuplicates = true;
        }

        EditorGUILayout.HelpBox(hasDuplicates ? "⚠️ Duplicate enemy prefabs detected!" : "✓ No duplicate prefabs", MessageType.Info);

        // Preview Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Available Tiers", EditorStyles.boldLabel);
        previewDay = EditorGUILayout.IntSlider("Day", previewDay, 1, 50);

        List<string> availableNames = new();
        for (int i = 0; i < enemyTiers.arraySize; i++)
        {
            SerializedProperty tier = enemyTiers.GetArrayElementAtIndex(i);
            SerializedProperty prefabProp = tier.FindPropertyRelative("prefab");
            SerializedProperty unlockDayProp = tier.FindPropertyRelative("unlockDay");

            if (prefabProp.objectReferenceValue is GameObject prefab && previewDay >= unlockDayProp.intValue)
                availableNames.Add(prefab.name);
        }

        if (availableNames.Count > 0)
            EditorGUILayout.HelpBox("Tiers available on Day " + previewDay + ": " + string.Join(", ", availableNames), MessageType.None);
        else
            EditorGUILayout.HelpBox("No enemy tiers available on this day.", MessageType.Warning);

        serializedObject.ApplyModifiedProperties();
    }
}
