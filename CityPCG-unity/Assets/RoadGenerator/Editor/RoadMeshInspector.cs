using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadMesh))]
public class RoadMeshInspector : Editor {
    private static float testT = 0.0f;

    public override void OnInspectorGUI() {
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

        // testT = GUILayout.HorizontalSlider(testT, 0f, 1f);
        float newTestT = EditorGUILayout.Slider(testT, 0f, 1f);
        if (testT != newTestT) {
            testT = newTestT;
            EditorUtility.SetDirty(road);
        }
    }

    void OnSceneGUI() {
        RoadMesh road = target as RoadMesh;
        if (road == null) return;

        BezierSpline spline = road.GetComponent<BezierSpline>();
        if (spline == null) return;
        if (spline.ControlPointCount <= 3) return;

        OrientedPoint orientedPoint = spline.GetOrientedPoint(testT, Vector3.up);
        Handles.PositionHandle(orientedPoint.position, Quaternion.LookRotation(orientedPoint.tangent, orientedPoint.normal));

        if (road.CrossSectionShape == null) return;

        void DrawLine(Vector2 localPosA, Vector2 localPosB) => Handles.DrawLine(orientedPoint.localToWorld(localPosA), orientedPoint.localToWorld(localPosB));

        Mesh2D shape = road.CrossSectionShape;
        Handles.color = Color.white;
        for (int i = 0; i < road.CrossSectionShape.lineIndices.Length; i += 2) {
            Vector3 a = shape.vertices[shape.lineIndices[i]].point;
            Vector3 b = shape.vertices[shape.lineIndices[i + 1]].point;
            DrawLine(a, b);
        }
    }
}
