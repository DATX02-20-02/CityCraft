using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BezierSpline))]
public class RoadMesh : MonoBehaviour
{
    [SerializeField]
    [Range(0.01f, 1.0f)]
    private float roadWidth = 0.05f;

    public void Reset()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh.Clear();
    }

    public void GenerateRoadMesh()
    {
        BezierSpline spline = GetComponent<BezierSpline>();
        if (!spline)
        {
            return;
        }

        if (spline.ControlPointCount < 4)
        {
            return;
        }

        int steps = spline.CurveCount * 10;
        Vector3[] points = new Vector3[steps + 1];
        points[0] = transform.InverseTransformPoint(spline.GetPoint(0.0f));
        for (int i = 1; i < steps; i++)
        {
            float t = ((float)i / (float)steps);
            points[i] = transform.InverseTransformPoint(spline.GetPoint(t));
        }
        points[steps] = transform.InverseTransformPoint(spline.GetPoint(1f));
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateRoadMesh(points, false);
    }


    private Mesh CreateRoadMesh(Vector3[] nodes, bool isClosed)
    {
        Vector3[] verts = new Vector3[nodes.Length * 2];
        Vector2[] uvs = new Vector2[verts.Length];
        int numTris = 2 * (nodes.Length - 1) + ((isClosed) ? 2 : 0);
        int[] tris = new int[numTris * 3];
        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < nodes.Length; i++)
        {
            Vector3 forward = Vector2.zero;
            if (i < nodes.Length - 1 || isClosed)
            {
                forward += nodes[(i + 1)%nodes.Length] - nodes[i];
            }
            if (i > 0 || isClosed)
            {
                forward += nodes[i] - nodes[(i - 1 + nodes.Length)%nodes.Length];
            }
            forward.y = 0;

            forward.Normalize();
            Vector3 left = new Vector3(-forward.z, 0, forward.x);

            verts[vertIndex] = nodes[i] + left * roadWidth * .5f;
            verts[vertIndex + 1] = nodes[i] - left * roadWidth * .5f;

            float completionPercent = i / (float)(nodes.Length - 1);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertIndex] = new Vector2(0, v);
            uvs[vertIndex + 1] = new Vector2(1, v);

            if (i < nodes.Length - 1 || isClosed)
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 3)  % verts.Length;
            }

            vertIndex += 2;
            triIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        return mesh;
    }
}
