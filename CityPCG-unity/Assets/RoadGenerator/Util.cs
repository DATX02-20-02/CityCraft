using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTree;

public class Util {
    public static void DebugDrawCircle(Vector3 pos, float radius, Color color, float fidelity = 10) {
        float step = 2 * Mathf.PI / fidelity;
        for (int i = 0; i < fidelity; i++) {
            float x = Mathf.Sin(step * i) * radius;
            float z = Mathf.Cos(step * i) * radius;

            float nx = Mathf.Sin(step * (i + 1)) * radius;
            float nz = Mathf.Cos(step * (i + 1)) * radius;

            Debug.DrawLine(pos + new Vector3(x, 0, z), pos + new Vector3(nx, 0, nz), new Color(0, 1, 0));
        }
    }

    public static void DebugDrawRectangle(float x, float y, float w, float h, Color color) {
        Debug.DrawLine(new Vector3(x,     0, y),     new Vector3(x + w, 0, y),     color);
        Debug.DrawLine(new Vector3(x + w, 0, y),     new Vector3(x + w, 0, y + h), color);
        Debug.DrawLine(new Vector3(x + w, 0, y + h), new Vector3(x,     0, y + h), color);
        Debug.DrawLine(new Vector3(x,     0, y + h), new Vector3(x,     0, y),     color);
    }

    public static void DebugDrawEnvelope(Envelope bounds) {
        DebugDrawRectangle((float)bounds.MinX, (float)bounds.MinY, (float)(bounds.MaxX - bounds.MinX), (float)(bounds.MaxY - bounds.MinY), new Color(0, 0, 1));
    }

    public static Envelope getEnvelopeFromNodes(IEnumerable<Node> nodes) {
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
}
