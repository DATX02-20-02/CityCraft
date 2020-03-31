using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BezierSpline))]
public class RoadMesh : MonoBehaviour
{
    [SerializeField]
    private RoadIntersectionMesh roadStart = null;
    [SerializeField]
    private RoadIntersectionMesh roadEnd = null;

    [SerializeField]
    [Range(0.001f, 1.0f)]
    private float roadWidth = 0.4f;

    [SerializeField]
    [Range(0.001f, 1.0f)]
    private float sidewalkWidth = 0.005f;

    [SerializeField]
    [Range(0.01f, 0.5f)]
    private float precision = 0.02f;

    public float RoadWidth {
        get { return roadWidth; }
    }

    public float SideWalkWidth {
        get { return sidewalkWidth; }
    }

    public float Width {
        get { return roadWidth + (sidewalkWidth * 2); }
    }

    public BezierSpline Spline {
        get { return GetComponent<BezierSpline>(); }
    }

    public RoadIntersectionMesh RoadEnd {
        get => roadEnd;
        // set => SetRoadEndConnection(ref roadEnd, value);
    }

    public RoadIntersectionMesh RoadStart {
        get => roadStart;
        // set => SetRoadEndConnection(ref roadStart, value);
    }

    private void SetRoadEndConnection(ref RoadIntersectionMesh endConnection, RoadIntersectionMesh newConnection) {
        if (newConnection == null) {
            if (endConnection != null) {
                endConnection.RemoveConnection(this);
                endConnection.UpdateMesh();
                endConnection = null;
            }
        }
        else {
            endConnection = newConnection;
            endConnection.AddConnection(this);
            endConnection.UpdateMesh();
        }
    }


    public void SetStart(RoadIntersectionMesh start) {
        roadStart = start;
    }

    public void SetEnd(RoadIntersectionMesh end) {
        roadEnd = end;
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
        int ringSubdivisionCount = Mathf.RoundToInt(1f / precision);

        float[] arr = new float[ringSubdivisionCount];
        spline.CalcLengthTableInfo(arr);

        // Vertices
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int AddVertex(Vector3 pos, Vector3 normal, Vector2 uv) {
            verts.Add(pos);
            normals.Add(normal);
            uvs.Add(uv);

            return verts.Count - 1;
        }

        for (int ringIndex = 0; ringIndex < ringSubdivisionCount; ringIndex++)
        {
            float t = (ringIndex / (ringSubdivisionCount - 1f));
            OrientedPoint p = spline.GetOrientedPointLocal(t, Vector3.up);
            float splineDistance = spline.Sample(arr, t);

            Vector3 localLeft = Vector3.left * roadWidth / 2f;
            AddVertex(p.localToWorld(localLeft), p.normal, new Vector2(0f, splineDistance));

            Vector3 localRight = Vector3.right * roadWidth / 2f;
            AddVertex(p.localToWorld(localRight), p.normal, new Vector2(1f, splineDistance));

            Vector3 localLeftSidewalkLeft = localLeft + Vector3.left * sidewalkWidth;
            Vector3 localLeftSidewalkRight = localLeft;
            Vector3 localRightSidewalkLeft = localRight;
            Vector3 localRightSidewalkRight = localRight + Vector3.right * sidewalkWidth;

            AddVertex(p.localToWorld(localLeftSidewalkLeft), p.normal, new Vector2(0.2f, splineDistance));
            AddVertex(p.localToWorld(localLeftSidewalkRight), p.normal, new Vector2(0.2f, splineDistance));
            AddVertex(p.localToWorld(localRightSidewalkLeft), p.normal, new Vector2(0.2f, splineDistance));
            AddVertex(p.localToWorld(localRightSidewalkRight), p.normal, new Vector2(0.2f, splineDistance));
        }

        int verticesPerRing = verts.Count / ringSubdivisionCount;

        // Triangles
        List<int> triangles = new List<int>();

        void AddQuad(int currentLeft, int currentRight, int nextLeft, int nextRight) {
            triangles.Add(currentLeft);
            triangles.Add(nextLeft);
            triangles.Add(nextRight);

            triangles.Add(currentLeft);
            triangles.Add(nextRight);
            triangles.Add(currentRight);
        }

        for (int ringIndex = 0; ringIndex < ringSubdivisionCount - 1; ringIndex++) {
            int rootIndex = ringIndex * verticesPerRing;
            int rootIndexNext = (ringIndex + 1) * verticesPerRing;

            AddQuad(rootIndex,     rootIndex + 1, rootIndexNext,     rootIndexNext + 1);
            AddQuad(rootIndex + 2, rootIndex + 3, rootIndexNext + 2, rootIndexNext + 3); // left sidewalk
            AddQuad(rootIndex + 4, rootIndex + 5, rootIndexNext + 4, rootIndexNext + 5); // right sidewalk
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        return mesh;
    }
}
