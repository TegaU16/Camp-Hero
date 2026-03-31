using Game.Terrain;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainPreview))]
public class TerrainPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainPreview preview = (TerrainPreview)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Preview"))
        {
            preview.GeneratePreview();
        }

        if (GUILayout.Button("Clear Preview"))
        {
            preview.ClearPreview();
        }
    }
}
