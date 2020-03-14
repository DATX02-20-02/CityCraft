using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGeneratorInspector : Editor {
    public override void OnInspectorGUI() {
        NoiseGenerator generator = target as NoiseGenerator;

        DrawDefaultInspector();
        if (GUILayout.Button("Generate map")) {
            Undo.RecordObject(generator, "Generate map");

            generator.Generate();

            EditorUtility.SetDirty(generator);
        }
    }
}
