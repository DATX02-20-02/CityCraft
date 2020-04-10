using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BezierSpline))]
public class RoadMesh : MonoBehaviour {

    [Header("Road Settings")]
    [SerializeField]
    [Range(0.001f, 1.0f)]
    private float roadWidth = 0.25f;

    [SerializeField]
    [Range(0.001f, 0.5f)]
    private float sidewalkWidth = 0.025f;

    [SerializeField]
    [Range(0.05f, 0.5f)]
    [Tooltip("The approximate distance between generated vertices along the spline")]
    private float stepDistance = 0.15f;

    [Header("Road Connections")]
    [SerializeField] private RoadIntersectionMesh roadStart = null;
    [SerializeField] private RoadIntersectionMesh roadEnd = null;

    [Header("Generate Mesh For")]
    [SerializeField] private MeshFilter roadMesh = null;
    [SerializeField] private MeshFilter leftSidewalkMesh = null;
    [SerializeField] private MeshFilter rightSidewalkMesh = null;


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

    public MeshFilter RoadMeshFilter {
        get { return roadMesh; }
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
        if (this.projectOnTerrain == null) {
            this.projectOnTerrain = (float x, float z) => {
                Vector3 vec = new Vector3(x, transform.position.y, z);
                return new TerrainModel.TerrainHit() { point = vec, normal = Vector3.up };
            };
        }

        GenerateRoadMesh(this.projectOnTerrain);
    }

    public void GenerateRoadMesh(ProjectOnTerrain projectOnTerrain) {
        this.projectOnTerrain = projectOnTerrain;

        if (Spline.ControlPointCount < 4) {
            return;
        }

        roadMesh.sharedMesh = ExtrudeQuadFromSpline(Vector3.zero, roadWidth);

        if (leftSidewalkMesh) {
            leftSidewalkMesh.sharedMesh = ExtrudeQuadFromSpline(Vector3.left * (roadWidth + sidewalkWidth) / 2f, sidewalkWidth);
        }

        if (rightSidewalkMesh) {
            rightSidewalkMesh.sharedMesh = ExtrudeQuadFromSpline(Vector3.right * (roadWidth + sidewalkWidth) / 2f, sidewalkWidth);
        }
    }

    private Mesh ExtrudeQuadFromSpline(Vector3 localCenterOffset, float width) {
        int sampleCount = 100 * Spline.CurveCount; // sample 100 points per spline curve
        BezierSplineDistanceLUT splineDistanceLUT = new BezierSplineDistanceLUT(Spline, sampleCount);

        // Vertices
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int AddVertex(Vector3 pos, Vector3 normal, Vector2 uv) {
            Vector3 worldPos = transform.TransformPoint(pos);
            TerrainModel.TerrainHit hit = this.projectOnTerrain(worldPos.x, worldPos.z);
            verts.Add(transform.InverseTransformPoint(hit.point + hit.normal * 0.005f));
            // verts.Add(pos);

            normals.Add(normal);
            uvs.Add(uv);

            return verts.Count - 1;
        }

        int ringSubdivisionCount = 0;
        bool exceedsSplineDistance = false;
        float targetDistance = 0f;
        int i = 0;
        while (!exceedsSplineDistance) {
            exceedsSplineDistance = targetDistance > splineDistanceLUT.TotalDistance;

            // Find t at targetdistance
            float t = 0;
            {
                if (!exceedsSplineDistance) {
                    for (; i < sampleCount; i++) {
                        float d = splineDistanceLUT.GetDistance(i);
                        if (d > targetDistance) {
                            i--;
                            break;
                        }
                    }
                    t = splineDistanceLUT.IndexToT(i);
                }
                else {
                    t = 1f;
                }
            }


            Vector3 globalSplinePosition = Spline.GetPoint(t);
            TerrainModel.TerrainHit hit = this.projectOnTerrain(globalSplinePosition.x, globalSplinePosition.z);
            OrientedPoint p = Spline.GetOrientedPointLocal(t, hit.normal);

            float splineDistance = splineDistanceLUT.Sample(t);

            Vector3 localLeft = localCenterOffset + Vector3.left * width / 2f;
            AddVertex(p.localToWorld(localLeft), p.normal, new Vector2(0f, splineDistance));

            Vector3 localRight = localCenterOffset + Vector3.right * width / 2f;
            AddVertex(p.localToWorld(localRight), p.normal, new Vector2(1f, splineDistance));

            ringSubdivisionCount++;
            targetDistance += stepDistance;
        }

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

        int verticesPerRing = verts.Count / ringSubdivisionCount;
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
        if (roadMesh) { roadMesh.mesh = null; }
        if (leftSidewalkMesh) { leftSidewalkMesh.mesh = null; }
        if (rightSidewalkMesh) { rightSidewalkMesh.mesh = null; }
    }
}
