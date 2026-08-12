using System.Collections.Generic;
using System.Linq;
using Game.Terrain.Structures;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(StructureTable))]
public class StructureTableEditor : Editor
{
    private ReorderableList templateList;
    private SerializedProperty entriesProp;

    private void OnEnable()
    {
        SerializedProperty tableProp = serializedObject.FindProperty("weightedTable");
        entriesProp = tableProp.FindPropertyRelative("entries");

        templateList = new ReorderableList(serializedObject, entriesProp)
        {
            drawHeaderCallback = rect =>
            {
                float line = EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width, line),
                    "Structure Template Entries"
                );

                rect.y += line + 2;

                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width * 0.55f, line),
                    "Template"
                );

                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width * 0.58f, rect.y, rect.width * 0.17f, line),
                    "Weight"
                );

                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width * 0.78f, rect.y, rect.width * 0.22f, line),
                    "Chance %"
                );
            },

            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty templateEntry = entriesProp.GetArrayElementAtIndex(index);
                SerializedProperty structureTemplate = templateEntry.FindPropertyRelative("value");
                SerializedProperty weight = templateEntry.FindPropertyRelative("weight");

                float line = EditorGUIUtility.singleLineHeight;
                rect.y += 2;

                float totalWeight = GetTotalWeight();

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width * 0.55f, line),
                    structureTemplate,
                    GUIContent.none
                );

                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width * 0.58f, rect.y, rect.width * 0.17f, line),
                    weight,
                    GUIContent.none
                );

                float chance = totalWeight > 0f
                    ? (weight.floatValue / totalWeight) * 100f
                    : 0f;

                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width * 0.78f, rect.y, rect.width * 0.22f, line),
                    $"{chance:0.0}%"
                );
            },

            elementHeight = EditorGUIUtility.singleLineHeight + 6,
            headerHeight = EditorGUIUtility.singleLineHeight * 2 + 6
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSummaryBox();
        templateList.DoLayoutList();
        DrawUtilityButtons();

        serializedObject.ApplyModifiedProperties();
    }

    // =========================
    // SUMMARY
    // =========================

    private void DrawSummaryBox()
    {
        float totalWeight = GetTotalWeight();

        if (totalWeight <= 0f)
        {
            EditorGUILayout.HelpBox(
                "Total weight is 0.\nNo template can be rolled.",
                MessageType.Error
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Total Weight: {totalWeight:0.##}",
                MessageType.Info
            );
        }
    }

    private float GetTotalWeight()
    {
        StructureTable table = (StructureTable)target;
        return table.weightedTable.Entries
            .Where(e => e != null && e.weight > 0f)
            .Sum(e => e.weight);
    }

    // =========================
    // BUTTONS
    // =========================

    private void DrawUtilityButtons()
    {
        StructureTable table = (StructureTable)target;

        GUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Normalize", GUILayout.Height(20)))
                NormalizeWeights(table);

            if (GUILayout.Button("Even Split", GUILayout.Height(20)))
                EvenSplit(table);

            if (GUILayout.Button("Sort (High → Low)", GUILayout.Height(20)))
            {
                Undo.RecordObject(table, "Sort Structure Templates");
                table.weightedTable.Entries.Sort(
                    (entryA, entryB) => entryB.weight.CompareTo(entryA.weight)
                );

                EditorUtility.SetDirty(table);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add None Fallback", GUILayout.Height(20)))
                AddNoneFallback(table);

            if (GUILayout.Button("Auto-Populate", GUILayout.Height(20)))
                AutoPopulateTemplates(table);

            if (GUILayout.Button("Test Roll", GUILayout.Height(20)))
            {
                StructureTemplate result = table.GetRandomStructure(hash: 1234567890);
                Debug.Log(result ? $"Rolled: {result.name}" : "Rolled: None");
            }
        }
    }

    // =========================
    // ACTIONS
    // =========================

    private void NormalizeWeights(StructureTable table)
    {
        Undo.RecordObject(table, "Normalize Weights");

        float total = table.weightedTable.Entries.Sum(e => e.weight);
        if (total <= 0f) return;

        foreach (WeightedEntry<StructureTemplate> entry in table.weightedTable.Entries)
            entry.weight /= total;

        EditorUtility.SetDirty(table);
    }

    private void EvenSplit(StructureTable table)
    {
        Undo.RecordObject(table, "Even Split Weights");

        int count = table.weightedTable.Entries.Count;
        if (count == 0) return;

        float value = 1f / count;

        foreach (WeightedEntry<StructureTemplate> entry in table.weightedTable.Entries)
            entry.weight = value;

        EditorUtility.SetDirty(table);
    }

    private void AddNoneFallback(StructureTable table)
    {
        Undo.RecordObject(table, "Add None Fallback");

        table.weightedTable.Add(null, 1f);

        EditorUtility.SetDirty(table);
    }

    private void AutoPopulateTemplates(StructureTable table)
    {
        // Find all StructureTemplate assets in folder
        string[] guids = AssetDatabase.FindAssets(
            "t:StructureTemplate",
            new[] { "Assets/Structure Templates" }
        );

        HashSet<StructureTemplate> existing = table.weightedTable.Entries
            .Where(entry => entry.value != null)
            .Select(entry => entry.value)
            .ToHashSet();

        Undo.RecordObject(table, "Auto Populate Structure Templates");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StructureTemplate template = AssetDatabase.LoadAssetAtPath<StructureTemplate>(path);

            if (template == null || existing.Contains(template)) continue;

            table.weightedTable.Add(template, 1f); // default
        }

        EditorUtility.SetDirty(table);
    }

}
