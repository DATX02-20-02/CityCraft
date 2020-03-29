using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoadIntersectionMesh : MonoBehaviour {
    [System.Serializable]
    public class RoadMeshConnection {
        public RoadMesh roadMesh;
        public bool isStart;
    }

    [SerializeField] private RoadMeshConnection[] connectedRoads;
    private RoadSegment[] connectionPoints = null;

    [SerializeField] private float arcRadius = 2f;
    [SerializeField] private int arcPrecision = 10;
    [SerializeField] private bool debugView = false;


    public RoadSegment[] IntersectionState {
        get => connectionPoints;
    }

    public class IntersectionCorner {
        public Vector3 sidewalkIntersection;
        public Vector3 sidewalkStartLeft;
        public Vector3 sidewalkStartRight;
        public Vector3 streetIntersection;
        public Vector3 streetStartLeft;
        public Vector3 streetStartRight;
    }

    public class RoadSegment {
        public RoadMesh r;
        public BezierSpline s;
        public Vector3 p;
        public Vector3 tangent;
        public Vector3 binormal;
        public float length;
        public Vector3 left;
        public Vector3 right;
        public Vector3 leftEnd;
        public Vector3 rightEnd;

        public Vector3 sidewalkLeft;
        public Vector3 sidewalkRight;

        public Vector3 startSidewalkLeft;
        public Vector3 startSidewalkRight;
        public Vector3 endSidewalkLeft;
        public Vector3 endSidewalkRight;

        public IntersectionCorner cornerLeft;
        public IntersectionCorner cornerRight;

        public bool isIntersectingLeftCornerFirst;
    }

    private void DrawLine(Vector3 start, Vector3 end, Color color) => Debug.DrawLine(start, end, color);
    private void DrawPoint(Vector3 pos, float radius, Color color) => DrawUtil.DebugDrawCircle(pos, radius, color, 20);

    [ContextMenu("Update intersection mesh")]
    public void UpdateMesh() {
        UpdateIntersectionState();
        GetComponent<MeshFilter>().sharedMesh = CreateMesh();
    }

    private void UpdateIntersectionState() {
        if (connectedRoads.Length < 3) return;

        bool IsStart(RoadMesh r) {
            if (r.RoadStart != this && r.RoadEnd != this) {
                Debug.LogWarning("Interaction and road connection needs to be bidirectional.");
            }

            return r.RoadStart == this;
        }

        Vector3 SplineDirectionOfAttack(RoadSegment r) {
            if (IsStart(r.r)) {
                return r.s.GetTangent(0f) * (-1f);
            }
            else {
                return r.s.GetTangent(1f);
            }
        }

        Vector3 GetSplineConnectionPoint(RoadSegment r) {
            float t = IsStart(r.r) ? 0f : 1f;
            return r.s.GetPoint(t);
        }


        void SetSplineConnectedPosition(RoadSegment c, Vector3 globalPosition) {
            if (IsStart(c.r)) {
                c.s[0] = c.s.transform.InverseTransformPoint(globalPosition);
            }
            else {
                c.s[c.s.ControlPointCount - 1] = c.s.transform.InverseTransformPoint(globalPosition);
            }
            c.r.GenerateRoadMesh();
        }

        connectionPoints = new RoadSegment[connectedRoads.Length];
        for (int i = 0; i < connectionPoints.Length; i++) {
            RoadSegment c  = new RoadSegment();
            c.r = connectedRoads[i].roadMesh;
            c.s = c.r.GetComponent<BezierSpline>();
            c.tangent = SplineDirectionOfAttack(c) * (-1f);
            c.binormal = Vector3.Cross(Vector3.up, c.tangent);

            connectionPoints[i] = c;
        }

        RoadSegment left = connectionPoints[connectionPoints.Length - 1];
        for (int i = 0; i < connectionPoints.Length; i++) {
            RoadSegment right = connectionPoints[i];

            IntersectionCorner corner = new IntersectionCorner();

            LineIntersection.Result GetRoadIntersection(float leftOffset, float rightOffset) {
                Vector3 leftSideWalkOffset = left.binormal * leftOffset;
                Vector3 leftSideWalkStart = transform.position - leftSideWalkOffset;
                // DrawLine(leftSideWalkStart, leftSideWalkStart + left.tangent * 10f, Color.cyan);

                Vector3 rightSideWalkOffset = right.binormal * rightOffset;
                Vector3 rightSideWalkStart = transform.position + rightSideWalkOffset;
                // DrawLine(rightSideWalkStart, rightSideWalkStart + right.tangent * 10f, Color.green);
                return LineIntersection.LineTest(
                    VectorUtil.Vector3To2(leftSideWalkStart - left.tangent * 1000f),
                    VectorUtil.Vector3To2(leftSideWalkStart + left.tangent * 1000f),
                    VectorUtil.Vector3To2(rightSideWalkStart - right.tangent * 1000f),
                    VectorUtil.Vector3To2(rightSideWalkStart + right.tangent * 1000f)
                );
            }

            LineIntersection.Result intersection;

            intersection = GetRoadIntersection(left.r.Width / 2f, right.r.Width/ 2f);
            if (intersection.type == LineIntersection.Type.Intersecting) {
                Vector3 sidewalkIntersectionPoint = VectorUtil.Vector2To3(intersection.point);
                DrawPoint(sidewalkIntersectionPoint, 0.03f, Color.red);
                corner.sidewalkIntersection = sidewalkIntersectionPoint;
            }

            intersection = GetRoadIntersection(left.r.RoadWidth / 2f, right.r.RoadWidth / 2f);
            if (intersection.type == LineIntersection.Type.Intersecting) {
                Vector3 streetIntersectionPoint = VectorUtil.Vector2To3(intersection.point);
                DrawPoint(streetIntersectionPoint, 0.03f, Color.red);
                corner.streetIntersection = streetIntersectionPoint;
            }

            left.cornerRight = corner;
            right.cornerLeft = corner;

            left = right;
        }


        foreach (RoadSegment c in connectionPoints) {
            Vector3 splineEndPoint = new Vector3();
            c.isIntersectingLeftCornerFirst = Vector3.Distance(transform.position, c.cornerLeft.sidewalkIntersection) > Vector3.Distance(transform.position, c.cornerRight.sidewalkIntersection);

            if (c.isIntersectingLeftCornerFirst) {
                splineEndPoint = c.cornerLeft.sidewalkIntersection - c.binormal * c.r.Width / 2f;
            }
            else {
                splineEndPoint = c.cornerRight.sidewalkIntersection + c.binormal * c.r.Width / 2f;
            }

            DrawPoint(splineEndPoint, 0.05f, Color.blue);
            SetSplineConnectedPosition(c, splineEndPoint);

            c.cornerRight.streetStartLeft = splineEndPoint - c.binormal * c.r.RoadWidth / 2f;
            c.cornerLeft.streetStartRight = splineEndPoint + c.binormal * c.r.RoadWidth / 2f;

            c.cornerRight.sidewalkStartLeft = splineEndPoint - c.binormal * c.r.Width / 2f;
            c.cornerLeft.sidewalkStartRight = splineEndPoint + c.binormal * c.r.Width / 2f;

            DrawPoint(c.cornerRight.streetStartLeft, 0.08f, Color.green);
            DrawPoint(c.cornerLeft.streetStartRight, 0.08f, Color.green);
        }

    }

    public Mesh CreateMesh() {
        if (connectedRoads.Length < 3) return null;

        Vector2 whiteUV = new Vector2(0.85f, 0.0f);

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        int AddVertice(Vector3 vert, Vector2 uv) {
            verts.Add(transform.InverseTransformPoint(vert));
            normals.Add(Vector3.up);
            uvs.Add(uv);

            return verts.Count - 1;
        }

        foreach (RoadSegment c in connectionPoints) {
            // Construct right corner
            IntersectionCorner corner = c.cornerRight;
            if (c.cornerRight.sidewalkIntersection == c.cornerRight.sidewalkStartLeft && c.cornerRight.sidewalkIntersection == c.cornerRight.sidewalkStartRight) {
                int idx = AddVertice(corner.streetIntersection, whiteUV);
                AddVertice(corner.streetStartRight, whiteUV);
                AddVertice(corner.streetStartLeft, whiteUV);
                AddVertice(corner.sidewalkIntersection, whiteUV);
                triangles.Add(idx + 0);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);

                triangles.Add(idx + 1);
                triangles.Add(idx + 3);
                triangles.Add(idx + 2);
            }
            else {
                int idx = AddVertice(corner.sidewalkStartLeft, Vector2.one);
                AddVertice(corner.streetStartLeft, Vector2.one);

                AddVertice(corner.sidewalkIntersection, Vector2.one);
                AddVertice(corner.streetIntersection, Vector2.one);

                AddVertice(corner.sidewalkStartRight, Vector2.one);
                AddVertice(corner.streetStartRight, Vector2.one);

                // TODO: This is an unecessary triangle for some cases - figure it out!
                triangles.Add(idx + 0);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);

                triangles.Add(idx + 1);
                triangles.Add(idx + 3);
                triangles.Add(idx + 2);

                triangles.Add(idx + 2);
                triangles.Add(idx + 3);
                triangles.Add(idx + 4);

                triangles.Add(idx + 3);
                triangles.Add(idx + 5);
                triangles.Add(idx + 4);
            }

            // Connect road to center of intersection
            {
                int idx = AddVertice(c.cornerRight.streetStartLeft, Vector2.one);
                AddVertice(c.cornerLeft.streetStartRight, Vector2.one);
                AddVertice(c.cornerRight.streetIntersection, Vector2.one);
                AddVertice(c.cornerLeft.streetIntersection, Vector2.one);

                triangles.Add(idx + 0);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);

                triangles.Add(idx + 1);
                triangles.Add(idx + 3);
                triangles.Add(idx + 2);
              }
        }

        // Build center mesh
        {
            int centerRoot = verts.Count;
            for (int i = connectionPoints.Length - 1; i >= 0; i--) {
                IntersectionCorner corner = connectionPoints[i].cornerRight;
                AddVertice(corner.streetIntersection, Vector2.one);
            }

            for (int tri = 0; tri < connectionPoints.Length - 2; tri++) {
                triangles.Add(centerRoot);
                triangles.Add(centerRoot + tri + 1);
                triangles.Add(centerRoot + tri + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        return mesh;
    }


    public void Reset() {
    }
}
