using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshFilter))]
public class NormalsVisualizer : Editor {
    private bool showNormals;
    private Mesh mesh;

    void OnEnable() {
        MeshFilter mf = target as MeshFilter;
        if (mf != null) {
            mesh = mf.sharedMesh;
        }
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        showNormals = GUILayout.Toggle(showNormals, "Show normals");
    }

    void OnSceneGUI() {
        if (!showNormals) return;
        if (mesh == null) return;

        for (int i = 0; i < Mathf.Min(mesh.vertexCount, 50); i++) {
            Vector3 vert = mesh.vertices[i];

            Handles.matrix = (target as MeshFilter).transform.localToWorldMatrix;
            Handles.color = Color.red;
            Handles.DrawLine(
                vert,
                vert + mesh.normals[i]);

        }
    }
}
