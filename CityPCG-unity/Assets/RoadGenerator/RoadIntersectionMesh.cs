using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoadIntersectionMesh : MonoBehaviour {

    private class RoadConnection {
        public RoadMesh road;
        public Vector3 angleOfAttack;

        public RoadConnection(RoadMesh road, Vector3 angleOfAttack) {
            this.road = road;
            this.angleOfAttack = angleOfAttack;
        }
    };

    [SerializeField] private List<RoadConnection> connectedRoads;
    [SerializeField] private bool debugView = false;

    private RoadSegment[] connectionPoints = null;
    private ProjectOnTerrain projectOnTerrain;
    private Vector3 intersectionNormal;
    private bool isValid = false;

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
        public Vector3 tangent;
        public Vector3 binormal;

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

    private void DrawLine(Vector3 start, Vector3 end, Color color) {
        if (debugView) Debug.DrawLine(start, end, color);
    }
    private void DrawPoint(Vector3 pos, float radius, Color color) {
        if (debugView) DrawUtil.DebugDrawCircle(pos, radius, color, 20);
    }

    public void Awake() {
        connectedRoads = new List<RoadConnection>();
    }

    public void AddConnection(RoadMesh toAdd, Vector3 angleOfAttack) {
        connectedRoads.Add(new RoadConnection(toAdd, angleOfAttack));
    }

    [ContextMenu("Update intersection mesh")]
    public void UpdateMesh() {
        UpdateMesh(this.projectOnTerrain);
    }

    public void UpdateMesh(ProjectOnTerrain projectOnTerrain) {
        this.projectOnTerrain = projectOnTerrain;

        UpdateIntersectionState();
        Mesh result = CreateMesh();
        if (result != null) {
            GetComponent<MeshFilter>().sharedMesh = result;
        }
    }

    private bool IsStart(RoadMesh r) {
        if (r.RoadStart != this && r.RoadEnd != this) {
            Debug.LogWarning("Interaction and road connection needs to be bidirectional.");
        }

        return r.RoadStart == this;
    }

    private Vector3 SplineDirectionOfAttack(RoadMesh r) {
        if (IsStart(r)) {
            return r.Spline.GetTangent(0f) * (-1f);
        }
        else {
            return r.Spline.GetTangent(1f);
        }
    }

    private void UpdateIntersectionState() {
        if (connectedRoads == null) return;
        if (connectedRoads.Count < 3) {
            Debug.LogWarning("Tried to generate intersection with fewer than 3 connected roads, aborting");
            return;
        }

        isValid = true;

        // sort roads in cw order
        {
            connectedRoads.Sort(delegate(RoadConnection r1, RoadConnection r2) {
                Vector3 r1Dir = r1.angleOfAttack;
                float r1Angle = Mathf.Atan2(r1Dir.z, r1Dir.x) * Mathf.Rad2Deg;
                Vector3 r2Dir = r2.angleOfAttack;
                float r2Angle = Mathf.Atan2(r2Dir.z, r2Dir.x) * Mathf.Rad2Deg;
                return r1Angle.CompareTo(r2Angle);
            });
        }

        void SetSplineConnectedPosition(RoadMesh r, Vector3 globalPosition) {
            Vector3 localEndPoint = r.Spline.transform.InverseTransformPoint(globalPosition);
            if (IsStart(r)) {
                r.Spline[0] = localEndPoint;
            }
            else {
                r.Spline[r.Spline.ControlPointCount - 1] = localEndPoint;
            }

            r.Spline.AutoConstructSpline();
        }

        connectionPoints = new RoadSegment[connectedRoads.Count];
        this.intersectionNormal = this.projectOnTerrain(transform.position.x, transform.position.z).normal;
        for (int i = 0; i < connectionPoints.Length; i++) {
            RoadSegment c  = new RoadSegment();
            c.r = connectedRoads[i].road;
            c.s = c.r.Spline;
            c.tangent = connectedRoads[i].angleOfAttack * (-1f);
            c.binormal = Vector3.Cross(intersectionNormal, c.tangent);

            connectionPoints[i] = c;
        }

        RoadSegment left = connectionPoints[connectionPoints.Length - 1];
        for (int i = 0; i < connectionPoints.Length; i++) {
            RoadSegment right = connectionPoints[i];

            IntersectionCorner corner = new IntersectionCorner();

            LineIntersection.Result GetRoadIntersection(float leftOffset, float rightOffset) {
                Vector3 leftSideWalkOffset = left.binormal * leftOffset;
                Vector3 leftSideWalkStart = transform.position - leftSideWalkOffset;
                Vector3 leftLineStart = leftSideWalkStart - left.tangent * 1000f;
                Vector3 leftLineEnd = leftSideWalkStart + left.tangent * 1000f;
                DrawLine(leftLineStart, leftLineEnd, Color.cyan);

                Vector3 rightSideWalkOffset = right.binormal * rightOffset;
                Vector3 rightSideWalkStart = transform.position + rightSideWalkOffset;
                Vector3 rightLineStart = rightSideWalkStart - right.tangent * 1000f;
                Vector3 rightLineEnd = rightSideWalkStart + right.tangent * 1000f;
                DrawLine(rightLineStart, rightLineEnd, Color.green);

                return LineIntersection.LineTest(
                    VectorUtil.Vector3To2(leftLineStart),
                    VectorUtil.Vector3To2(leftLineEnd),
                    VectorUtil.Vector3To2(rightLineStart),
                    VectorUtil.Vector3To2(rightLineEnd)
                );
            }


            if (Vector3.Angle(left.tangent, right.tangent) > 170f) {
                corner.sidewalkIntersection = transform.position + right.binormal * right.r.Width / 2f;
                corner.streetIntersection =  transform.position + right.binormal * right.r.RoadWidth / 2f;
                DrawPoint(corner.sidewalkIntersection, 0.03f, Color.yellow);
                DrawPoint(corner.streetIntersection, 0.03f, Color.yellow);
            }
            else {
                LineIntersection.Result intersection;

                intersection = GetRoadIntersection(left.r.Width / 2f, right.r.Width/ 2f);
                if (intersection.type == LineIntersection.Type.Intersecting) {
                    Vector3 sidewalkIntersectionPoint = VectorUtil.Vector2To3(intersection.point) + Vector3.up * transform.position.y;
                    DrawPoint(sidewalkIntersectionPoint, 0.03f, Color.red);
                    corner.sidewalkIntersection = sidewalkIntersectionPoint;
                }
                else {
                    Debug.LogError("Sidewalks do not intersect! " + intersection.type, left.r);
                    isValid = false;
                }

                intersection = GetRoadIntersection(left.r.RoadWidth / 2f, right.r.RoadWidth / 2f);
                if (intersection.type == LineIntersection.Type.Intersecting) {
                    Vector3 streetIntersectionPoint = VectorUtil.Vector2To3(intersection.point) + Vector3.up * transform.position.y;
                    DrawPoint(streetIntersectionPoint, 0.03f, Color.red);
                    corner.streetIntersection = streetIntersectionPoint;
                }
                else {
                    Debug.LogError("Streets do not intersect! " + intersection.type, left.r);
                    isValid = false;
                }
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
            SetSplineConnectedPosition(c.r, this.projectOnTerrain(splineEndPoint.x, splineEndPoint.z).point);

            c.cornerRight.streetStartLeft = splineEndPoint - c.binormal * c.r.RoadWidth / 2f;
            c.cornerLeft.streetStartRight = splineEndPoint + c.binormal * c.r.RoadWidth / 2f;

            c.cornerRight.sidewalkStartLeft = splineEndPoint - c.binormal * c.r.Width / 2f;
            c.cornerLeft.sidewalkStartRight = splineEndPoint + c.binormal * c.r.Width / 2f;
        }
    }

    public Mesh CreateMesh() {
        if (connectedRoads == null || connectionPoints == null) return null;
        if (connectedRoads.Count < 3) return null;
        if (!isValid) return null;

        Vector2 whiteUV = new Vector2(0.85f, 0.0f);

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        int AddVertice(Vector3 vert, Vector2 uv) {
            RaycastHit hit = this.projectOnTerrain(vert.x, vert.z);
            verts.Add(transform.InverseTransformPoint(hit.point + hit.normal * 0.01f));
            normals.Add(this.intersectionNormal);
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
