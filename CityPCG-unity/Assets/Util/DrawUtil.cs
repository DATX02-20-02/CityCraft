using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RBush;
using Utils;

public class DrawUtil {

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
        Debug.DrawLine(new Vector3(x, 0, y), new Vector3(x + w, 0, y), color);
        Debug.DrawLine(new Vector3(x + w, 0, y), new Vector3(x + w, 0, y + h), color);
        Debug.DrawLine(new Vector3(x + w, 0, y + h), new Vector3(x, 0, y + h), color);
        Debug.DrawLine(new Vector3(x, 0, y + h), new Vector3(x, 0, y), color);
    }

    public static void DebugDrawRectangle(PolygonUtil.Rectangle rect, Color color) {
        Debug.DrawLine(VectorUtil.Vector2To3(rect.topLeft), VectorUtil.Vector2To3(rect.topRight), color);
        Debug.DrawLine(VectorUtil.Vector2To3(rect.topRight), VectorUtil.Vector2To3(rect.botRight), color);
        Debug.DrawLine(VectorUtil.Vector2To3(rect.botRight), VectorUtil.Vector2To3(rect.botLeft), color);
        Debug.DrawLine(VectorUtil.Vector2To3(rect.botLeft), VectorUtil.Vector2To3(rect.topLeft), color);
    }

    public static void DebugDrawEnvelope(Envelope bounds, Color color) {
        DebugDrawRectangle((float)bounds.MinX, (float)bounds.MinY, (float)(bounds.MaxX - bounds.MinX), (float)(bounds.MaxY - bounds.MinY), color);
    }

}
