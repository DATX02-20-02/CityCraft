using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoadLOD {
    [Header("Configuration")]

    [Range(0.05f, 0.5f)]
    [Tooltip("The approximate distance between generated vertices along the spline")]
    [SerializeField] public float stepSize = 0.15f;

    [SerializeField] public float lodTransitionWidth = 0.1f;

    [Header("Autogenerated objects")]
    [SerializeField] public GameObject group = null;
    [SerializeField] public GameObject roadMesh = null;
    [SerializeField] public GameObject leftSidewalkMesh = null;
    [SerializeField] public GameObject rightSidewalkMesh = null;
}

[RequireComponent(typeof(BezierSpline))]
public class RoadMesh : MonoBehaviour {

    [Header("Road Settings")]
    [SerializeField]
    [Range(0.001f, 1.0f)]
    private float roadWidth = 0.25f;

    [SerializeField]
    [Range(0.001f, 0.5f)]
    private float sidewalkWidth = 0.025f;

    [Header("Road Connections")]
    [SerializeField] private RoadIntersectionMesh roadStart = null;
    [SerializeField] private RoadIntersectionMesh roadEnd = null;

    [Header("Generate Mesh For LODS")]
    [SerializeField] private GameObject roadLODPrefab = null;
    [SerializeField] private RoadLOD[] roadLODs = new RoadLOD[0];


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
        get { return roadLODs[0].roadMesh == null ? null : roadLODs[0].roadMesh.GetComponent<MeshFilter>(); }
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

    private float stepDist;
    public float stepDistance {
        get => stepDist;
        set => stepDist = value;
    }

    public void GenerateRoadMesh(int levelOfDetail = 0) {
        if (this.projectOnTerrain == null) {
            this.projectOnTerrain = (float x, float z) => {
                Vector3 vec = new Vector3(x, transform.position.y, z);
                return new TerrainModel.TerrainHit() { point = vec, normal = Vector3.up };
            };
        }

        GenerateRoadMesh(this.projectOnTerrain, levelOfDetail);
    }

    public void GenerateRoadMesh(ProjectOnTerrain projectOnTerrain, int levelOfDetail = 0) {
        this.projectOnTerrain = projectOnTerrain;

        if (Spline.ControlPointCount < 4) {
            return;
        }

        LODGroup lodGroup = GetComponent<LODGroup>();
        LOD[] lods = new LOD[roadLODs.Length];

        for (int lodLevel = 0; lodLevel < roadLODs.Length; lodLevel++) {
            RoadLOD lod = roadLODs[lodLevel];

            if (lod.group == null) {
                lod.group = Instantiate(roadLODPrefab, transform);
                lod.group.name = "LOD " + lodLevel;
                lod.roadMesh = lod.group.transform.Find("Road Mesh").gameObject;
                lod.leftSidewalkMesh = lod.group.transform.Find("Left Sidewalk Mesh").gameObject;
                lod.rightSidewalkMesh = lod.group.transform.Find("Right Sidewalk Mesh").gameObject;
            }

            lod.roadMesh.GetComponent<MeshFilter>().sharedMesh = ExtrudeQuadFromSpline(Vector3.zero, roadWidth, lod.stepSize);
            lod.leftSidewalkMesh.GetComponent<MeshFilter>().sharedMesh = ExtrudeQuadFromSpline(Vector3.left * (roadWidth + sidewalkWidth) / 2f, sidewalkWidth, lod.stepSize);
            lod.rightSidewalkMesh.GetComponent<MeshFilter>().sharedMesh = ExtrudeQuadFromSpline(Vector3.right * (roadWidth + sidewalkWidth) / 2f, sidewalkWidth, lod.stepSize);

            Renderer[] renderers = lod.group.GetComponentsInChildren<Renderer>();
            lods[lodLevel] = new LOD(lod.lodTransitionWidth, renderers);
        }

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }

    private Mesh ExtrudeQuadFromSpline(Vector3 localCenterOffset, float width, float stepDistance) {
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
        foreach (RoadLOD lod in roadLODs) {
            if (lod.roadMesh) lod.roadMesh.GetComponent<MeshFilter>().mesh = null;
            if (lod.leftSidewalkMesh) lod.leftSidewalkMesh.GetComponent<MeshFilter>().mesh = null;
            if (lod.rightSidewalkMesh) lod.rightSidewalkMesh.GetComponent<MeshFilter>().mesh = null;
        }
    }
}
