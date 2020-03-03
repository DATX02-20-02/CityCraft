using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadMesh))]
public class RoadMeshInspector : Editor
{
    public override void OnInspectorGUI()
    {
        RoadMesh road = target as RoadMesh;

        DrawDefaultInspector();
        if (GUILayout.Button("Generate Mesh")) {
            Undo.RecordObject(road, "Generate Mesh");
            road.GenerateRoadMesh();
            EditorUtility.SetDirty(road);
        }

        if (GUILayout.Button("Reset Mesh")) {
            Undo.RecordObject(road, "Reset Mesh");
            road.Reset();
            EditorUtility.SetDirty(road);
        }
    }
}
