using UnityEngine;
using System;

public class BezierSpline : MonoBehaviour
{
    public bool debugNormals = false;

    [SerializeField]
    private Vector3[] points;

    public Vector3 this[int index]
    {
        get => points[index];
        set => SetControlPoint(index, value);
    }

    public void SetControlPoint (int index, Vector3 point)
    {
        if (index % 3 == 0)
        {
            Vector3 delta = point - points[index];
            if (index > 0) {
                points[index - 1] += delta;
            }
            if (index + 1 < points.Length) {
                points[index + 1] += delta;
            }
        }
        points[index] = point;
    }

    public int CurveCount {
        get {
            return (points.Length - 1) / 3;
        }
    }

    public int ControlPointCount {
        get {
            return points.Length;
        }
    }

    private int GetCurveIndex(ref float t)
    {
        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }

        return i;
    }

    public Vector3 GetPoint(float t)
    {
        int i = GetCurveIndex(ref t);
        Vector3 worldPosition = transform.TransformPoint(PointOnBezier(points[i], points[i + 1], points[i + 2], points[i + 3], t));
        return worldPosition;
    }

    public Vector3 GetTangent(float t)
    {
        int i = GetCurveIndex(ref t);
        Vector3 worldTangent = transform.TransformPoint(DerivOnBezier(points[i], points[i + 1], points[i + 2], points[i + 3], t));
        return worldTangent.normalized;
    }

    public Vector3 GetBinormal(float t, Vector3 up)
    {
        Vector3 tangent = GetTangent(t);
        Vector3 binormal = transform.TransformPoint(Vector3.Cross(up, tangent)).normalized;
        return binormal;
    }

    public Vector3 GetNormal(float t, Vector3 up)
    {
        Vector3 tangent = GetTangent(t);
        Vector3 binormal = (Vector3.Cross(up, tangent)).normalized;
        return Vector3.Cross(tangent, binormal);
    }

    public Quaternion GetOrientation(float t, Vector3 up)
    {
        Vector3 tangent = GetTangent(t);
        Vector3 normal = GetNormal(t, up);
        return Quaternion.LookRotation(tangent, normal);
    }

    public void CalcLengthTableInfo(float[] arr)
    {
        arr[0] = 0f;
        float totalLength = 0f;
        Vector3 prev = points[0];
        for (int i = 1; i < arr.Length; i++)
        {
            float t = (float)i / (arr.Length - 1);
            Vector3 pt = GetPoint(t);
            float distanceDiff = (prev - pt).magnitude;
            totalLength += distanceDiff;
            arr[i] = totalLength;
            prev = pt;
        }
    }

    public float Sample(float[] arr, float t)
    {
        float iFloat = t * (arr.Length - 1);
        int idLower = Mathf.FloorToInt(iFloat);
        int idUpper = Mathf.FloorToInt(iFloat + 1);
        if (idUpper >= arr.Length)
        {
            return arr[arr.Length - 1];
        }
        if (idLower < 0)
        {
            return arr[0];
        }
        return Mathf.Lerp(arr[idLower], arr[idUpper], iFloat - idLower);
    }

    public void AddCurve()
    {
        Vector3 point = points[points.Length - 1];
        Array.Resize(ref points, points.Length + 3);
        point.x += 1f;
        points[points.Length - 3] = point;
        point.x += 1f;
        points[points.Length - 2] = point;
        point.x += 1f;
        points[points.Length - 1] = point;
    }

    public void AddPoint(Vector3 newPoint)
    {
        Vector3 dir;
        float length;

        if (points == null || points.Length == 0)
        {
            Array.Resize(ref points, 1);
            points[0] = newPoint;
            return;
        }
        else if(points.Length < 4)
        {
            Array.Resize(ref points, 4);
            dir = (newPoint - points[0]).normalized;
            length = (newPoint - points[0]).magnitude;
            points[1] = points[0] + dir * length;
            points[2] = newPoint - dir * length;
            points[3] = newPoint;
            return;
        }


        Vector3 prevprev0 = points.Length < 7 ? points[points.Length - 4] : points[points.Length - 7];

        Vector3 prev0 = points[points.Length - 4];
        Vector3 prev1 = points[points.Length - 3];
        Vector3 prev2 = points[points.Length - 2];
        Vector3 prev3 = points[points.Length - 1];

        dir = (newPoint - prevprev0).normalized;
        length = (newPoint - prev3).magnitude / 2f;
        // length = 0f; //nochecking

        points[points.Length - 2] = prev3 - dir * length;

        Array.Resize(ref points, points.Length + 3);

        Vector3 p0 = prev3;
        Vector3 p1 = p0 + dir * length;
        Vector3 p2 = newPoint - dir * length;
        Vector3 p3 = newPoint;

        points[points.Length - 3] = p1;
        points[points.Length - 2] = p2;
        points[points.Length - 1] = p3;
    }

    public void Reset()
    {
        points = new Vector3[0];
    }

    // Third Order (Four point) bezier solver
    // Equation: (1-t)^3*P_0 + 3*t*(1-t)^2*P_1 + 3*t^2*(1-t)*P_2 + t^3*P_3
    public static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return (1-t)*(1-t)*(1-t)*p0 + 3*t*(1-t)*(1-t)*p1 + 3*t*t*(1-t)*p2 + t*t*t*p3;
    }

    // First derivative of Third Order (Four point) bezier solver.
    // Equation: 3*(1-t)^2*(P_1-P_0) + 6*t*(1-t)*(P_2-P_1) + 3*t^2*(P_3-P_2)
    public static Vector3 DerivOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 3*(1-t)*(1-t)*(p1-p0) + 6*t*(1-t)*(p2-p1) + 3*t*t*(p3-p2);
    }

    // Finds the value t after moving a "real" distance `dist`.  This can be used to move along a curve at a constant
    // velocity.
    public static float GetTWithRealDistance(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float dist) {
        return dist / DerivOnBezier(p0,p1,p2,p3,t0).magnitude;
    }
}
