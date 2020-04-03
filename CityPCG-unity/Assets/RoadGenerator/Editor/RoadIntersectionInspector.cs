using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadIntersectionMesh))]
public class RoadIntersectionInspector : Editor {
    void OnSceneGUI() {
        RoadIntersectionMesh intersection = target as RoadIntersectionMesh;
        if (intersection == null) return;

        intersection.UpdateMesh();

        if (intersection.IntersectionState == null) {
            return;
        }

        if (intersection.IntersectionState.Length < 3) {
            return;
        }

        for (int i = 0; i < intersection.IntersectionState.Length; i ++) {
            RoadIntersectionMesh.RoadSegment c = intersection.IntersectionState[i];
            Handles.color = new Color(0f, (i / (float)intersection.IntersectionState.Length), 0f);
            Handles.DrawDottedLine(intersection.transform.position, intersection.transform.position + c.tangent, 4f);
            Handles.DrawSolidDisc(intersection.transform.position + c.tangent + Vector3.up * i * 0.1f, Vector3.up, 0.1f);

            // Handles.color = Color.red;
            // Handles.DrawWireDisc(c.cornerRight.sidewalkIntersection, Vector3.up, 0.03f);
        }
    }
}
