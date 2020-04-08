using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BezierSpline))]
public class RoadMesh : MonoBehaviour {
    [SerializeField] private Material roadMaterial = null;
    [SerializeField] private Material sidewalkMaterial = null;
    [SerializeField] private RoadIntersectionMesh roadStart = null;
    [SerializeField] private RoadIntersectionMesh roadEnd = null;

    [SerializeField]
    [Range(0.001f, 1.0f)]
    private float roadWidth = 0.4f;

    [SerializeField]
    [Range(0.001f, 1.0f)]
    private float sidewalkWidth = 0.005f;

    [SerializeField]
    [Range(0.01f, 0.5f)]
    private float precision = 0.02f;


    private ProjectOnTerrain projectOnTerrain;


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
        set => roadEnd = value;
    }

    public RoadIntersectionMesh RoadStart {
        get => roadStart;
        set => roadStart = value;
    }

    public void GenerateRoadMesh() {
        GenerateRoadMesh(this.projectOnTerrain);
    }

    public void GenerateRoadMesh(ProjectOnTerrain projectOnTerrain) {
        this.projectOnTerrain = projectOnTerrain;

        if (Spline.ControlPointCount < 4) {
            return;
        }

        // remove any previous mesh
        foreach (Transform child in this.transform) {
            Destroy(child.gameObject);
        }

        MeshFilter CreateEmptyRenderable(string name, Material material) {
            GameObject go = new GameObject(name);
            go.transform.parent = this.transform;
            go.transform.localPosition = Vector3.zero;
            go.AddComponent<MeshRenderer>().material = material;
            return go.AddComponent<MeshFilter>();
        }

        MeshFilter roadMesh = CreateEmptyRenderable("Road Mesh", roadMaterial);
        MeshFilter leftSidewalkMesh = CreateEmptyRenderable("Left Sidewalk Mesh", sidewalkMaterial);
        MeshFilter rightSidewalkMesh = CreateEmptyRenderable("Right Sidewalk Mesh", sidewalkMaterial);

        roadMesh.sharedMesh = ExtrudeQuadFromSpline(Vector3.zero, roadWidth);
        leftSidewalkMesh.sharedMesh = ExtrudeQuadFromSpline(Vector3.left * (roadWidth + sidewalkWidth) / 2f, sidewalkWidth);
        rightSidewalkMesh.sharedMesh = ExtrudeQuadFromSpline(Vector3.right * (roadWidth + sidewalkWidth) / 2f, sidewalkWidth);
    }

    private Mesh ExtrudeQuadFromSpline(Vector3 localCenterOffset, float width) {
        int ringSubdivisionCount = Mathf.RoundToInt(1f / precision) * Spline.CurveCount;
        BezierSplineDistanceLUT splineDistanceLUT = new BezierSplineDistanceLUT(Spline, ringSubdivisionCount);

        // Vertices
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int AddVertex(Vector3 pos, Vector3 normal, Vector2 uv) {
            Vector3 worldPos = transform.TransformPoint(pos);
            RaycastHit hit = this.projectOnTerrain(worldPos.x, worldPos.z);
            verts.Add(transform.InverseTransformPoint(hit.point + hit.normal * 0.005f));
            // verts.Add(pos);

            normals.Add(normal);
            uvs.Add(uv);

            return verts.Count - 1;
        }

        for (int ringIndex = 0; ringIndex < ringSubdivisionCount; ringIndex++) {
            // TODO(anton): Sample curve at evenly spaced intervals (https://pomax.github.io/bezierinfo/#tracing)
            float t = (ringIndex / (ringSubdivisionCount - 1f));
            Vector3 globalSplinePosition = Spline.GetPoint(t);
            RaycastHit hit = this.projectOnTerrain(globalSplinePosition.x, globalSplinePosition.z);
            OrientedPoint p = Spline.GetOrientedPointLocal(t, hit.normal);

            float splineDistance = splineDistanceLUT.Sample(t);

            Vector3 localLeft = localCenterOffset + Vector3.left * width / 2f;
            AddVertex(p.localToWorld(localLeft), p.normal, new Vector2(0f, splineDistance));

            Vector3 localRight = localCenterOffset + Vector3.right * width / 2f;
            AddVertex(p.localToWorld(localRight), p.normal, new Vector2(1f, splineDistance));
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

            AddQuad(rootIndex, rootIndex + 1, rootIndexNext, rootIndexNext + 1);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        return mesh;
    }

    public void Reset() {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter) {
            if (meshFilter.sharedMesh) {
                meshFilter.sharedMesh.Clear();
            }
        }

        this.projectOnTerrain = (float x, float z) => {
            Vector3 vec = new Vector3(x, transform.position.y, z);
            RaycastHit hit = new RaycastHit();
            hit.point = vec;
            hit.normal = Vector3.up;
            return hit;
        };
    }
}
