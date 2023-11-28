using UnityEngine;
using UnityEditor;

// Enable terrain modification in editor mode
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            mapGen.DrawMapInEditor();
        }

        // Press the button then call Generate Map function
        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
