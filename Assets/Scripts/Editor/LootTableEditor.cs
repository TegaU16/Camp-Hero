using System.Collections.Generic;
using System.Linq;
using Game.Saving;
using Game.Storage;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LootTable))]
public class LootTableEditor : Editor
{
    private ReorderableList lootList;
    private SerializedProperty entriesProp;

    private void OnEnable()
    {
        SerializedProperty tableProp = serializedObject.FindProperty("weightedTable");
        entriesProp = tableProp.FindPropertyRelative("entries");

        lootList = new ReorderableList(serializedObject, entriesProp)
        {
            drawHeaderCallback = rect =>
            {
                float line = EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width, line),
                    "Loot Entries"
                );

                rect.y += line + 2;

                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.4f, line), "Item");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.12f, line), "Min");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.12f, line), "Max");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.14f, line), "Weight");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.86f, rect.y, rect.width * 0.14f, line), "Chance %");
            },

            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(index);
                SerializedProperty value = element.FindPropertyRelative("value");
                SerializedProperty weight = element.FindPropertyRelative("weight");

                SerializedProperty item = value.FindPropertyRelative("item");
                SerializedProperty min = value.FindPropertyRelative("minCount");
                SerializedProperty max = value.FindPropertyRelative("maxCount");

                float line = EditorGUIUtility.singleLineHeight;
                rect.y += 2;

                float totalWeight = GetTotalWeight();

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width * 0.4f, line),
                    item,
                    GUIContent.none
                );

                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.12f, line),
                    min,
                    GUIContent.none
                );

                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.12f, line),
                    max,
                    GUIContent.none
                );

                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.14f, line),
                    weight,
                    GUIContent.none
                );

                float chance = totalWeight > 0f
                    ? (weight.floatValue / totalWeight) * 100f
                    : 0f;

                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width * 0.86f, rect.y, rect.width * 0.14f, line),
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
        lootList.DoLayoutList();
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
                "Total weight is 0.\nNo loot can be rolled.",
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
        LootTable table = (LootTable)target;
        return table.weightedTable.Entries
            .Where(entry => entry != null && entry.weight > 0f)
            .Sum(entry => entry.weight);
    }

    // =========================
    // BUTTONS
    // =========================

    private void DrawUtilityButtons()
    {
        LootTable table = (LootTable)target;

        GUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Normalize", GUILayout.Height(20)))
                Normalize(table);

            if (GUILayout.Button("Even Split", GUILayout.Height(20)))
                EvenSplit(table);

            if (GUILayout.Button("Sort (High → Low)", GUILayout.Height(20)))
            {
                Undo.RecordObject(table, "Sort Loot Weights");
                table.weightedTable.Entries.Sort((a, b) => b.weight.CompareTo(a.weight));
                EditorUtility.SetDirty(table);
            }
        }

        if (GUILayout.Button("Preview Loot Roll", GUILayout.Height(22)))
        {
            List<ItemData> loot = table.GetRandomLoot();
            if (loot.Count == 0)
            {
                Debug.Log("No loot rolled.");
            }
            else
            {
                foreach (ItemData item in loot)
                    Debug.Log($"{item.itemName} x{item.count}");
            }
        }
    }

    // =========================
    // ACTIONS
    // =========================

    private void Normalize(LootTable table)
    {
        Undo.RecordObject(table, "Normalize Loot Weights");

        float total = table.weightedTable.Entries.Sum(e => e.weight);
        if (total <= 0f) return;

        foreach (WeightedEntry<LootEntry> entry in table.weightedTable.Entries)
            entry.weight /= total;

        EditorUtility.SetDirty(table);
    }

    private void EvenSplit(LootTable table)
    {
        Undo.RecordObject(table, "Even Split Loot Weights");

        int count = table.weightedTable.Entries.Count;
        if (count == 0) return;

        float value = 1f / count;

        foreach (WeightedEntry<LootEntry> entry in table.weightedTable.Entries)
            entry.weight = value;

        EditorUtility.SetDirty(table);
    }
}
