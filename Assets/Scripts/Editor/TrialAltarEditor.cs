using Game.Terrain.Structures.Trials;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrialAltar))]
public class TrialAltarEditor : Editor
{
    private SerializedProperty wavesProp;

    private void OnEnable()
    {
        wavesProp = serializedObject.FindProperty("waves");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(wavesProp, new GUIContent("Waves"), true);

        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspectorExcept("waves");
    }

    private void DrawDefaultInspectorExcept(string exclude)
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == exclude) continue;

            EditorGUILayout.PropertyField(iterator, true);
        }
    }
}
