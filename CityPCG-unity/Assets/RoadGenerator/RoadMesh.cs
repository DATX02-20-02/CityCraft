using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BezierSpline))]
public class RoadMesh : MonoBehaviour
{
    [SerializeField]
    private Mesh2D crossSectionShape = null;

    [SerializeField]
    [Range(0.01f, 2.0f)]
    private float roadWidth = 0.1f;

    public Mesh2D CrossSectionShape {
        get { return crossSectionShape; }
    }

    public float RoadWidth {
        get { return roadWidth; }
    }

    public void Reset()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter)
        {
            if (meshFilter.sharedMesh) {
                meshFilter.sharedMesh.Clear();
            }
        }
    }

    public void GenerateRoadMesh()
    {
        BezierSpline spline = GetComponent<BezierSpline>();
        if (!spline) {
            return;
        }

        if (spline.ControlPointCount < 4) {
            return;
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateRoadMesh();
    }

    private Mesh CreateRoadMesh() {
        BezierSpline spline = GetComponent<BezierSpline>();
        int ringSubdivisionCount = spline.CurveCount * 20;

        float[] arr = new float[ringSubdivisionCount];
        spline.CalcLengthTableInfo(arr);

        // Vertices
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        for (int ringIndex = 0; ringIndex < ringSubdivisionCount; ringIndex++)
        {
            float t = (ringIndex / (ringSubdivisionCount - 1f));
            OrientedPoint p = spline.GetOrientedPointLocal(t, Vector3.up);

            for (int i = 0; i < crossSectionShape.VertexCount; i++) {
                Vector3 localPoint = CrossSectionShape.vertices[i].point;
                localPoint.x *= roadWidth;
                localPoint.y *= roadWidth;
                verts.Add(p.localToWorld(localPoint));
                normals.Add(p.localToWorldVector(CrossSectionShape.vertices[i].normal));
                uvs.Add(new Vector2(CrossSectionShape.vertices[i].u, spline.Sample(arr, t)));
            }
        }

        // Triangles
        List<int> triangles = new List<int>();
        for (int ringIndex = 0; ringIndex < ringSubdivisionCount - 1; ringIndex++) {
            int rootIndex = ringIndex * crossSectionShape.VertexCount;
            int rootIndexNext = (ringIndex + 1) * CrossSectionShape.VertexCount;

            for (int lineIndex = 0; lineIndex < crossSectionShape.LineCount; lineIndex += 2) {

                int lineStart = crossSectionShape.lineIndices[lineIndex];
                int lineEnd = crossSectionShape.lineIndices[lineIndex + 1];

                int currentA = rootIndex + lineStart;
                int currentB = rootIndex + lineEnd;

                int nextA = rootIndexNext + lineStart;
                int nextB = rootIndexNext + lineEnd;

                triangles.Add(currentA);
                triangles.Add(nextA);
                triangles.Add(nextB);

                triangles.Add(currentA);
                triangles.Add(nextB);
                triangles.Add(currentB);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        return mesh;
    }
}
