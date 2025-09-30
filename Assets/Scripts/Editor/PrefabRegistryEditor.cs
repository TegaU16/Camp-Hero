using UnityEditor;

[CustomEditor(typeof(PrefabRegistry))]
public class PrefabRegistryEditor : Editor
{
    private SerializedProperty categoriesProp;

    private void OnEnable()
    {
        categoriesProp = serializedObject.FindProperty("categories");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        for (int i = 0; i < categoriesProp.arraySize; i++)
        {
            SerializedProperty category = categoriesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = category.FindPropertyRelative("categoryName");
            SerializedProperty prefabsProp = category.FindPropertyRelative("prefabs");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(nameProp.stringValue, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(prefabsProp, includeChildren: true);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
