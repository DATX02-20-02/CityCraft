using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor {
    private BezierSpline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const float handleSize = 0.08f;
    private const float pickSize = 0.10f;
    private int selectedIndex = -1;

    public override void OnInspectorGUI() {
        spline = target as BezierSpline;

        DrawDefaultInspector();

        if (GUILayout.Button("Add Curve")) {
            Undo.RecordObject(spline, "Add Curve");
            spline.AddCurve();
            TryUpdateRoad();
            EditorUtility.SetDirty(spline);
        }

        if (GUILayout.Button("Expand Spline With Random Point")) {
            Undo.RecordObject(spline, "Add Point");

            if (spline.ControlPointCount == 0) {
                spline.AddPoint(new Vector3(0, 0, 0));
            }
            else if (spline.ControlPointCount == 1) {
                spline.AddCurve();
            }
            else {
                Vector3 end = spline.GetPoint(1f);
                Vector3 tangent = spline.GetTangent(1f);
                Vector3 binormal = spline.GetBinormal(1f, Vector3.up);

                spline.AddPoint(end + (tangent * 3f) + binormal * Random.Range(-10f, 10f));
            }

            TryUpdateRoad();
            EditorUtility.SetDirty(spline);
        }

        if (GUILayout.Button("Reset")) {
            Undo.RecordObject(spline, "Spline reset");
            spline.Reset();
            TryUpdateRoad();
            EditorUtility.SetDirty(spline);
        }
    }

    void OnSceneGUI() {
        spline = target as BezierSpline;
        handleTransform = spline.transform;
        handleRotation = handleTransform.rotation;

        if (spline.ControlPointCount < 4) {
            return;
        }

        Vector3 p0 = ShowPoint(0);
        for (int i = 1; i < spline.ControlPointCount; i += 3) {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i + 1);
            Vector3 p3 = ShowPoint(i + 2);

            Handles.color = Color.gray;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            if (spline.debugDrawSpline) {
                Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
            }
            p0 = p3;
        }

        const float lineLength = 0.1f;

        int samplePoints = spline.CurveCount * 80;
        if (spline.debugNormals) {
            for (float t = 0.0f; t < 1f; t += (1f / (float)samplePoints)) {
                Vector3 start = spline.GetPoint(t);
                Vector3 tangent = spline.GetTangent(t);
                Vector3 normal = spline.GetNormal(t, Vector3.up);
                Vector3 binormal = spline.GetBinormal(t, Vector3.up);

                Handles.color = Color.green;
                Handles.DrawLine(start, start + tangent * lineLength);

                Handles.color = Color.red;
                Handles.DrawLine(start, start + normal * lineLength);

                Handles.color = Color.red;
                Handles.DrawLine(start, start + binormal * lineLength);
            }
        }
    }

    private Vector3 ShowPoint(int index) {
        Vector3 point = handleTransform.TransformPoint(spline[index]);

        float size = HandleUtility.GetHandleSize(point);
        if (index == 0) {
            size *= 2f;
        }

        Handles.color = (index % 3) == 0 ? Color.green : Color.yellow;
        if (Handles.Button(point, Quaternion.LookRotation(Vector3.up), size * handleSize, size * pickSize, Handles.CircleHandleCap)) {
            selectedIndex = index;
            Repaint();
        }

        if (selectedIndex == index) {
            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Moved Spline Control point");
                point = newPoint;
                spline.AutoConstructSpline();
                TryUpdateRoad();
            }

            spline[index] = handleTransform.InverseTransformPoint(point);
        }
        return point;
    }

    private void TryUpdateRoad() {
        RoadMesh road = spline.GetComponent<RoadMesh>();
        if (road) {
            road.GenerateRoadMesh();
        }
    }
}
