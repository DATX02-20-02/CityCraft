using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClipperLib;

public class VectorUtil {

    public static Vector2 Vector3To2(Vector3 vec) {
        return new Vector2(vec.x, vec.z);
    }

    public static Vector3 Vector2To3(Vector2 vec) {
        return new Vector3(vec.x, 0, vec.y);
    }

    public static Vector2 IntPointToVector2(IntPoint point) {
        return new Vector2(point.X, point.Y);
    }

    public static Vector3 IntPointToVector3(IntPoint point) {
        return new Vector3(point.X, 0, point.Y);
    }

    public static bool IsInBounds(Vector2 vec, float width, float height) {
        float mx = vec.x / width;
        float my = vec.y / height;

        return mx >= 0 && mx < 1 && my >= 0 && my < 1;
    }

    public static Vector2 GetProjectedPointOnLine(Vector2 point, Vector2 from, Vector2 to, bool limit = true) {
        float l2 = (to - from).sqrMagnitude;
        if (l2 == 0) return from;

        float t = Vector2.Dot(point - from, to - from) / l2;
        if (limit && (t < 0 || t > 1)) return Vector2.negativeInfinity;

        Vector2 proj = from + t * (to - from);
        return proj;
    }

    public static float GetMinimumDistanceToLine(Vector2 point, Vector2 from, Vector2 to) {
        Vector2 proj = GetProjectedPointOnLine(point, from, to);
        return Vector2.Distance(point, proj);
    }

    public static Vector3 GetPlaneMousePos(Vector3 planePos) {
        Plane plane = new Plane(Vector3.up, planePos);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist;
        if (plane.Raycast(ray, out dist)) {
            return ray.GetPoint(dist);
        }
        return Vector3.zero;
    }

    public static Vector2 GetPointOnCenterLine(Rectangle rect, Vector2 pos) {
        Vector2 center = (rect.topLeft + rect.topRight + rect.botLeft + rect.botRight) / 4f;

        if (rect.width == rect.height)
            return center;

        Vector2 dir = pos - center;
        Vector2 forward = rect.topLeft - rect.topRight;
        Vector2 right = rect.topLeft - rect.botLeft;

        if (forward.magnitude > right.magnitude) {
            float dot = Vector2.Dot(dir, forward.normalized);
            float len = forward.magnitude - right.magnitude;

            return center + forward.normalized * Mathf.Clamp(dot, -len / 2, len / 2);
        }
        else {
            float dot = Vector2.Dot(dir, right.normalized);
            float len = right.magnitude - forward.magnitude;

            return center + right.normalized * Mathf.Clamp(dot, -len / 2, len / 2);
        }
    }
}
