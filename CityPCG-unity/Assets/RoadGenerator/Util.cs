using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RBush;

public class Util {
    #region Debug drawing utilities

    public static void DebugDrawCircle(Vector3 pos, float radius, Color color, float fidelity = 10) {
        float step = 2 * Mathf.PI / fidelity;
        for (int i = 0; i < fidelity; i++) {
            float x = Mathf.Sin(step * i) * radius;
            float z = Mathf.Cos(step * i) * radius;

            float nx = Mathf.Sin(step * (i + 1)) * radius;
            float nz = Mathf.Cos(step * (i + 1)) * radius;

            Debug.DrawLine(pos + new Vector3(x, 0, z), pos + new Vector3(nx, 0, nz), color);
        }
    }

    public static void DebugDrawRectangle(float x, float y, float w, float h, Color color) {
        Debug.DrawLine(new Vector3(x,     0, y),     new Vector3(x + w, 0, y),     color);
        Debug.DrawLine(new Vector3(x + w, 0, y),     new Vector3(x + w, 0, y + h), color);
        Debug.DrawLine(new Vector3(x + w, 0, y + h), new Vector3(x,     0, y + h), color);
        Debug.DrawLine(new Vector3(x,     0, y + h), new Vector3(x,     0, y),     color);
    }

    public static void DebugDrawEnvelope(Envelope bounds, Color color) {
        DebugDrawRectangle((float)bounds.MinX, (float)bounds.MinY, (float)(bounds.MaxX - bounds.MinX), (float)(bounds.MaxY - bounds.MinY), color);
    }

    #endregion

    #region Vector utilities

    public static Vector2 Vector3To2(Vector3 vec) {
        return new Vector2(vec.x, vec.z);
    }

    public static Vector3 Vector2To3(Vector2 vec) {
        return new Vector3(vec.x, 0, vec.y);
    }

    #endregion

    public static Envelope GetEnvelopeFromNodes(IEnumerable<Node> nodes, float padding = 0) {
        float minX = float.MaxValue;
        float minZ = float.MaxValue;

        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach (Node node in nodes) {
            minX = Mathf.Min(minX, node.pos.x);
            minZ = Mathf.Min(minZ, node.pos.z);

            maxX = Mathf.Max(maxX, node.pos.x);
            maxZ = Mathf.Max(maxZ, node.pos.z);
        }

        return new Envelope(minX - padding, minZ - padding, maxX + padding, maxZ + padding);
    }

    public static Vector3 GetPlaneMousePos(Vector3 planePos)
    {
        Plane plane = new Plane(Vector3.up, planePos);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        if (plane.Raycast(ray, out dist))
        {
            return ray.GetPoint(dist);
        }
        return Vector3.zero;
    }

    public static Node GetClosestNode(Node node, IEnumerable<Node> nodes) {
        float leastDistance = float.MaxValue;
        Node leastNode = null;

        foreach (Node n in nodes) {
            if (n == node) continue;

            float dist = Vector3.Distance(n.pos, node.pos);
            if (dist < leastDistance) {
                leastNode = n;
                leastDistance = dist;
            }
        }

        return leastNode;
    }

    public static Vector2 GetProjectedPointOnLine(Vector2 point, Vector2 from, Vector2 to) {
        float l2 = (to - from).sqrMagnitude;
        if (l2 == 0) return from;

        // float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(point - from, to - from) / l2));
        float t = Vector2.Dot(point - from, to - from) / l2;
        if (t < 0 || t > 1) return Vector2.negativeInfinity;

        Vector2 proj = from + t * (to - from);
        return proj;
    }

    public static float GetMinimumDistanceToLine(Vector2 point, Vector2 from, Vector2 to) {
        Vector2 proj = GetProjectedPointOnLine(point, from, to);
        return Vector2.Distance(point, proj);
    }



    #region Line intersection

    // Original implementation from Paul Salaets in the Node.JS package "line-intersect"
    // https://github.com/psalaets/line-intersect
    // Ported to C# and modified to also include ray-line intersection
    public class LineIntersection {
        public enum Type {
            None,
            Intersecting,
            Colinear,
            Parallel
        }

        public class Result {
            public Type type;
            public Vector2 point;
            public float factorA;
            public float factorB;

            public Result(Type type) {
                this.type = type;
            }

            public Result(Type type, Vector2 point, float factorA, float factorB) : this(type) {
                this.point = point;
                this.factorA = factorA;
                this.factorB = factorB;
            }
        }

        public static Result RayTest(Vector2 fromA, Vector2 toA, Vector2 origin, Vector2 dir) {
            float denom = (dir.y * (toA.x - fromA.x)) - (dir.x * (toA.y - fromA.y));
            float numeA = (dir.x * (fromA.y - origin.y)) - (dir.y * (fromA.x - origin.x));
            float numeB = ((toA.x - fromA.x) * (fromA.y - origin.y)) - ((toA.y - fromA.y) * (fromA.x - origin.x));

            if (denom == 0) {
                if (numeA == 0 && numeB == 0) {
                    return new Result(Type.Colinear);
                }
                return new Result(Type.Parallel);
            }

            float uA = numeA / denom;
            float uB = numeB / denom;

            if (uA >= 0 && uA <= 1 && uB >= 0) {
                return new Result(Type.Intersecting, origin + uB * dir, uA, uB);
            }

            return new Result(Type.None);
        }

        public static Result LineTest(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB) {
            Result res = RayTest(fromA, toA, fromB, toB - fromB);

            if (res.factorB <= 1) {
                return res;
            }

            return new Result(Type.None);
        }
    }

    #endregion
}
