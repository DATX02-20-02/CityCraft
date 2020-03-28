using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoadIntersectionMesh : MonoBehaviour {
    [SerializeField] private float roadWidth = 1f;
    [SerializeField] private float[] angles = new float[] {0f, 120f, 180f};
    [SerializeField] private bool debugView = false;

    enum IntersectionCornerType {
        Overlap,
        Center,
    }

    class IntersectionCorner {
        public IntersectionCornerType type;
        public Vector3 start;
        public Vector3 end;
        public Vector3 center;
        public Vector3 intersection;

        public IntersectionCorner(IntersectionCornerType type, Vector3 center, Vector3 start, Vector3 end) {
            this.type = type;
            this.center = center;
            this.start = start;
            this.end = end;
        }

        public List<Vector3> GetPoints() {
            List<Vector3> ps = new List<Vector3>();
            float circleRadius = Vector3.Distance(center, start);
            Vector3 startDir = (start - center).normalized;
            Vector3 endDir = (end - center).normalized;
            int steps = 10;
            float angleStep = Vector3.SignedAngle(startDir, endDir, Vector3.up) / (float)steps ;

            Vector3 curDir = startDir;
            for (int i = 0; i < steps; i++) {
                Vector3 p = center + curDir * circleRadius;
                DrawUtil.DebugDrawCircle(p, 0.02f, Color.red, 20);
                ps.Add(p);
                curDir = Quaternion.Euler(Vector3.up * angleStep) * curDir;
            }
            ps.Add(end);

            return ps;
        }
    }

    class RoadSegment {
        public Vector3 p;
        public Vector3 tangent;
        public Vector3 binormal;
        public float length;
        public Vector3 left;
        public Vector3 right;
        public Vector3 leftEnd;
        public Vector3 rightEnd;

        public IntersectionCorner cornerLeft;
        public IntersectionCorner cornerRight;
    }

    public float RoadWidth {
        get => roadWidth;
    }

    public float[] Angles {
        get => angles;
    }

    private RoadIntersectionMesh(float roadWidth, float[] angles) {
        this.roadWidth = roadWidth;
        this.angles = angles;
    }

    public RoadIntersectionMesh(float roadWidth, float angle1, float angle2, float angle3)
        : this(roadWidth, new float[] {angle1, angle2, angle3}) {}

    public RoadIntersectionMesh(float roadWidth, float angle1, float angle2, float angle3, float angle4)
        : this(roadWidth, new float[] {angle1, angle2, angle3, angle4}) {}

    public void Update() {
        GetComponent<MeshFilter>().sharedMesh = GenerateMesh();
    }

    public Mesh GenerateMesh() {

        void DrawLine(Vector3 start, Vector3 end, Color color) => Debug.DrawLine(start, end, color);
        void DrawPoint(Vector3 pos, float radius, Color color) => DrawUtil.DebugDrawCircle(pos, radius, color, 20);

        float intersectionRadius = roadWidth * 2f;
        float innerIntersectionRadius = roadWidth / 2f;

        if (debugView) {
            DrawPoint(transform.position, intersectionRadius, Color.green);
            DrawPoint(transform.position, innerIntersectionRadius, Color.cyan);
        }

        Vector3 center = transform.position;

        RoadSegment[] connectionPoints = new RoadSegment[angles.Length];
        for (int i = 0; i < angles.Length; i++) {
            RoadSegment c = new RoadSegment();

            float angle = angles[i];
            float radians = Mathf.Deg2Rad * angle;
            c.p = center + (new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians))) * intersectionRadius;
            c.tangent = (center - c.p).normalized;

            c.binormal = Vector3.Cross(Vector3.up, c.tangent);
            c.left = c.p - c.binormal * innerIntersectionRadius;
            c.right = c.p + c.binormal * innerIntersectionRadius;

            c.length = Vector3.Distance(center, c.p);
            c.leftEnd = c.left + c.tangent * c.length;
            c.rightEnd = c.right + c.tangent * c.length;

            if (debugView) {
                DrawLine(c.left, c.right, Color.black);
                DrawLine(c.left, c.leftEnd, Color.black);
                DrawLine(c.right, c.rightEnd, Color.black);
                DrawLine(c.leftEnd, c.rightEnd, Color.black);
            }

            connectionPoints[i] = c;
        }

        RoadSegment left = connectionPoints[connectionPoints.Length - 1];
        for (int i = 0; i < connectionPoints.Length; i++) {
            RoadSegment right = connectionPoints[i];

            Vector2 intersectionOut = new Vector2();
            if (LineSegmentsIntersection(VectorUtil.Vector3To2(left.right), VectorUtil.Vector3To2(left.rightEnd), VectorUtil.Vector3To2(right.left), VectorUtil.Vector3To2(right.leftEnd), out intersectionOut)) {
                Vector3 intersection = VectorUtil.Vector2To3(intersectionOut);

                if (debugView) DrawPoint(intersection, 0.01f, Color.gray);

                Vector3 bisectorPoint = left.right + (right.left - left.right) * 0.5f;
                Vector3 projectedPointLeft = VectorUtil.Vector2To3(VectorUtil.GetProjectedPointOnLine(VectorUtil.Vector3To2(bisectorPoint), intersectionOut, VectorUtil.Vector3To2(left.right)));
                float distanceToProj = Vector3.Distance(intersection, projectedPointLeft);
                Vector3 projectedPointRight = intersection + (right.left - intersection).normalized * distanceToProj;

                if (debugView) {
                    DrawPoint(bisectorPoint, Vector3.Distance(bisectorPoint, projectedPointLeft), Color.gray);
                    DrawPoint(projectedPointLeft, 0.01f, Color.red);
                    DrawPoint(projectedPointRight, 0.01f, Color.red);
                }

                IntersectionCorner corner = new IntersectionCorner(IntersectionCornerType.Overlap, bisectorPoint, projectedPointLeft, projectedPointRight);
                left.cornerRight = corner;
                right.cornerLeft = corner;
                corner.intersection = intersection;
            }
            else {
                IntersectionCorner corner = new IntersectionCorner(IntersectionCornerType.Center, center, left.rightEnd, right.leftEnd);
                left.cornerRight = corner;
                right.cornerLeft = corner;
            }

            left = right;
        }

        List<Vector3> verts = new List<Vector3>();
        List<int> triangles = new List<int>();

        void AddVertice(Vector3 vert) => verts.Add(transform.InverseTransformPoint(vert));

        foreach (RoadSegment c in connectionPoints) {


            if (c.cornerRight.type == IntersectionCornerType.Overlap) {

            }

            int rootIdx = verts.Count;
            AddVertice(c.left);
            AddVertice(c.right);
            AddVertice(c.leftEnd);
            AddVertice(c.rightEnd);

            triangles.Add(rootIdx);
            triangles.Add(rootIdx + 2);
            triangles.Add(rootIdx + 1);

            triangles.Add(rootIdx + 1);
            triangles.Add(rootIdx + 2);
            triangles.Add(rootIdx + 3);

            if (c.cornerRight.type == IntersectionCornerType.Overlap) {
                if (c.cornerLeft.type == IntersectionCornerType.Center) {

                }

                int cornerCenterIdx = verts.Count;
                AddVertice(c.cornerRight.intersection);
                List<Vector3> ps = c.cornerRight.GetPoints();
                AddVertice(ps[0]);
                for (int arcIdx = 1; arcIdx < ps.Count; arcIdx++) {
                    AddVertice(ps[arcIdx]);
                    triangles.Add(verts.Count - 2);
                    triangles.Add(cornerCenterIdx);
                    triangles.Add(verts.Count - 1);
                    if (debugView) {
                        float a = (ps.Count - arcIdx) / (float)ps.Count;
                        DrawLine(ps[arcIdx - 1], ps[arcIdx], new Color(a, 0f, 0f, 1f));
                    }
                }

            }
            else {
                int cornerCenterIdx = verts.Count;
                AddVertice(center);
                List<Vector3> ps = c.cornerRight.GetPoints();
                AddVertice(ps[0]);
                for (int arcIdx = 1; arcIdx < ps.Count; arcIdx++) {
                    AddVertice(ps[arcIdx]);
                    triangles.Add(verts.Count - 2);
                    triangles.Add(cornerCenterIdx);
                    triangles.Add(verts.Count - 1);

                    if (debugView) {
                        float a = (ps.Count - arcIdx) / (float)ps.Count;
                        DrawLine(ps[arcIdx - 1], ps[arcIdx], new Color(a, 0f, 0f, 1f));
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }


    public void Reset() {
    }
}
