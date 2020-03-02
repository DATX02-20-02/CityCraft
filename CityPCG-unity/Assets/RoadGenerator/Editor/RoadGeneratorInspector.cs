using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadGenerator))]
public class RoadGeneratorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        RoadGenerator generator = target as RoadGenerator;

        DrawDefaultInspector();
        if (GUILayout.Button("Generate network")) {
            Undo.RecordObject(generator, "Generate network");

            generator.Generate();

            EditorUtility.SetDirty(generator);
        }
    }
}
