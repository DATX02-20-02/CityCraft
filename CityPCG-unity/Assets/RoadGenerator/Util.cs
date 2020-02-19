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

    public static Envelope GetEnvelopeFromNodes(IEnumerable<Node> nodes) {
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

        return new Envelope(minX, minZ, maxX, maxZ);
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



    #region Line intersection

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

            public Result(Type type) {
                this.type = type;
            }

            public Result(Type type, Vector2 point) : this(type) {
                this.point = point;
            }
        }

        public static Result CheckIntersection(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB) {
            // x1 -> fromA.x
            // y1 -> fromA.y
            // x2 -> toA.x
            // y2 -> toA.y
            // x3 -> fromB.x
            // y3 -> fromB.y
            // x4 -> toB.x
            // y4 -> toB.y
            float denom = ((toB.y - fromB.y) * (toA.x - fromA.x)) - ((toB.x - fromB.x) * (toA.y - fromA.y));
            float numeA = ((toB.x - fromB.x) * (fromA.y - fromB.y)) - ((toB.y - fromB.y) * (fromA.x - fromB.x));
            float numeB = ((toA.x - fromA.x) * (fromA.y - fromB.y)) - ((toA.y - fromA.y) * (fromA.x - fromB.x));

            if (denom == 0) {
                if (numeA == 0 && numeB == 0) {
                    return new Result(Type.Colinear);
                }
                return new Result(Type.Parallel);
            }

            float uA = numeA / denom;
            float uB = numeB / denom;

            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1) {
                return new Result(Type.Intersecting, new Vector2(
                  fromA.x + (uA * (toA.x - fromA.x)),
                  fromA.y + (uA * (toA.y - fromA.y))
                ));
            }

            return new Result(Type.None);
        }
    }

    #endregion
}
