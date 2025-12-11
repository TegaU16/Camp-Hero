using System.Collections.Generic;
using Game.Storage;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LootTable))]
public class LootTableEditor : Editor
{
    private ReorderableList lootList;

    private void OnEnable()
    {
        SerializedProperty lootEntries = serializedObject.FindProperty("lootEntries");

        lootList = new ReorderableList(serializedObject, lootEntries, true, true, true, true)
        {
            // Header
            drawHeaderCallback = rect =>
            {
                float padding = 4f;
                float lineHeight = EditorGUIUtility.singleLineHeight;

                // Main label
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, lineHeight), "Loot Entries");

                rect.y += lineHeight + 2; // move down for column headers

                // Column headers
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.4f - padding, lineHeight), "Item");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.12f - padding, lineHeight), "Min");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.12f - padding, lineHeight), "Max");
                EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.28f - padding, lineHeight), "Probability");
            },

            // Element display
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = lootEntries.GetArrayElementAtIndex(index);
                SerializedProperty item = element.FindPropertyRelative("item");
                SerializedProperty minCount = element.FindPropertyRelative("minCount");
                SerializedProperty maxCount = element.FindPropertyRelative("maxCount");
                SerializedProperty probability = element.FindPropertyRelative("probability");

                float lineHeight = EditorGUIUtility.singleLineHeight;
                float padding = 4f;

                rect.y += 2;

                // Item field
                Rect itemRect = new(rect.x, rect.y, rect.width * 0.4f - padding, lineHeight);
                EditorGUI.PropertyField(itemRect, item, GUIContent.none);

                // Min/Max count
                Rect minRect = new(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.12f - padding, lineHeight);
                Rect maxRect = new(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.12f - padding, lineHeight);
                EditorGUI.PropertyField(minRect, minCount, GUIContent.none);
                EditorGUI.PropertyField(maxRect, maxCount, GUIContent.none);

                // Probability slider
                Rect probRect = new(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.28f - padding, lineHeight);
                EditorGUI.Slider(probRect, probability, 0f, 1f, GUIContent.none);
            },
            elementHeight = EditorGUIUtility.singleLineHeight + 6,
            headerHeight = EditorGUIUtility.singleLineHeight * 2 + 6
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        lootList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Sort by Probability (High → Low)"))
        {
            LootTable lootTable = (LootTable)target;
            lootTable.lootEntries.Sort((a, b) => b.probability.CompareTo(a.probability));
            EditorUtility.SetDirty(lootTable);
        }

        if (GUILayout.Button("Preview Loot Roll"))
        {
            LootTable lootTable = (LootTable)target;
            List<StoredItem> loot = lootTable.GetRandomLoot();

            if (loot.Count == 0)
            {
                Debug.Log("No loot rolled.");
            }
            else
            {
                foreach (StoredItem item in loot)
                    Debug.Log($"{item.item.name} x{item.count}");
            }
        }
    }
}
