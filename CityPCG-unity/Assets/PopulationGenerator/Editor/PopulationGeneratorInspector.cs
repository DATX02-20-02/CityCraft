using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PopulationGenerator))]
public class PopulationGeneratorInspector : Editor {
    public override void OnInspectorGUI() {
        PopulationGenerator generator = target as PopulationGenerator;

        DrawDefaultInspector();
        if (GUILayout.Button("Generate map")) {
            Undo.RecordObject(generator, "Generate map");

            generator.Generate();

            EditorUtility.SetDirty(generator);
        }
    }
}
