using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadIntersectionMesh))]
public class RoadIntersectionInspector : Editor {
    void OnSceneGUI() {
        RoadIntersectionMesh i = target as RoadIntersectionMesh;
        if (i == null) return;

        i.UpdateMesh();

        if (i.IntersectionState == null) {
            return;
        }

        if (i.IntersectionState.Length < 3) {
            return;
        }

        foreach (RoadIntersectionMesh.RoadSegment c in i.IntersectionState) {
            Handles.color = Color.green;
            Handles.DrawDottedLine(i.transform.position, i.transform.position + c.tangent, 4f);
            Handles.DrawSolidDisc(i.transform.position + c.tangent, Vector3.up, 0.1f);

            // Handles.color = Color.red;
            // Handles.DrawWireDisc(c.cornerRight.sidewalkIntersection, Vector3.up, 0.03f);
        }
    }
}
