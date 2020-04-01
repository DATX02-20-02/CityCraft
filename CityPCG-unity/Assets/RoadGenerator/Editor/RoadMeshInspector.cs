using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadMesh))]
public class RoadMeshInspector : Editor {
    public override void OnInspectorGUI() {
        RoadMesh road = target as RoadMesh;

        DrawDefaultInspector();
        if (GUILayout.Button("Generate Mesh")) {
            Undo.RecordObject(road, "Generate Mesh");
            road.GenerateRoadMesh((Vector3 vec) => vec);
            EditorUtility.SetDirty(road);
        }

        if (GUILayout.Button("Reset Mesh")) {
            Undo.RecordObject(road, "Reset Mesh");
            road.Reset();
            EditorUtility.SetDirty(road);
        }
    }

    void OnSceneGUI() {
        RoadMesh road = target as RoadMesh;
        if (road == null) return;

        BezierSpline spline = road.GetComponent<BezierSpline>();
        if (spline == null) return;
        if (spline.ControlPointCount <= 3) return;
    }
}
