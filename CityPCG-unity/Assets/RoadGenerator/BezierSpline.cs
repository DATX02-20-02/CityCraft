using UnityEngine;
using System;

public class BezierSpline : MonoBehaviour {
#if UNITY_EDITOR
    public bool debugNormals = false;
    public bool debugDrawSpline = false;
#endif

    [SerializeField] private Vector3[] points;

    public Vector3 this[int index] {
        get => points[index];
        set => SetControlPoint(index, value);
    }

    public void SetControlPoint(int index, Vector3 point) {
        if (index % 3 == 0) {
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
            if (points == null) return 0;
            return (points.Length - 1) / 3;
        }
    }

    public int ControlPointCount {
        get {
            if (points == null) return 0;
            return points.Length;
        }
    }


    public Vector3 GetPointLocal(float t) {
        int i = GetCurveIndex(ref t);
        Vector3 worldPosition = PointOnBezier(points[i], points[i + 1], points[i + 2], points[i + 3], t);
        return worldPosition;
    }

    public Vector3 GetTangentLocal(float t) {
        int i = GetCurveIndex(ref t);
        Vector3 worldTangent = DerivOnBezier(points[i], points[i + 1], points[i + 2], points[i + 3], t);
        return worldTangent.normalized;
    }

    public Vector3 GetBinormalLocal(float t, Vector3 localUp) {
        Vector3 tangent = GetTangentLocal(t);
        Vector3 binormal = Vector3.Cross(localUp, tangent).normalized;
        return binormal;
    }

    public Vector3 GetNormalLocal(float t, Vector3 localUp) {
        Vector3 tangent = GetTangentLocal(t);
        Vector3 binormal = Vector3.Cross(localUp, tangent).normalized;
        return Vector3.Cross(tangent, binormal);
    }

    public OrientedPoint GetOrientedPointLocal(float t, Vector3 worldUp) {
        OrientedPoint point = new OrientedPoint();
        Vector3 localUp = transform.InverseTransformDirection(worldUp);

        point.position = GetPointLocal(t);
        point.tangent = GetTangentLocal(t);
        point.normal = GetNormalLocal(t, localUp);
        point.binormal = GetBinormalLocal(t, localUp);

        return point;
    }

    public Vector3 GetPoint(float t) {
        Vector3 worldPosition = transform.TransformPoint(GetPointLocal(t));
        return worldPosition;
    }

    public Vector3 GetTangent(float t) {
        int i = GetCurveIndex(ref t);
        Vector3 worldTangent = transform.TransformDirection(GetTangentLocal(t));
        return worldTangent.normalized;
    }

    public Vector3 GetBinormal(float t, Vector3 worldUp) {
        Vector3 tangent = GetTangent(t);
        Vector3 binormal = Vector3.Cross(worldUp, tangent).normalized;
        return binormal;
    }

    public Vector3 GetNormal(float t, Vector3 worldUp) {
        Vector3 tangent = GetTangent(t);
        Vector3 binormal = Vector3.Cross(worldUp, tangent).normalized;
        return Vector3.Cross(tangent, binormal);
    }



    public OrientedPoint GetOrientedPoint(float t, Vector3 worldUp) {
        OrientedPoint point = new OrientedPoint();

        point.position = GetPoint(t);
        point.tangent = GetTangent(t);
        point.normal = GetNormal(t, worldUp);
        point.binormal = GetBinormal(t, worldUp);

        return point;
    }


    public void AddCurve() {
        Vector3 point = points[points.Length - 1];
        Array.Resize(ref points, points.Length + 3);
        point.x += 1f;
        points[points.Length - 3] = point;
        point.x += 1f;
        points[points.Length - 2] = point;
        point.x += 1f;
        points[points.Length - 1] = point;
    }

    public void AddPoint(Vector3 newPoint) {
        newPoint = transform.InverseTransformPoint(newPoint);

        if (points == null || points.Length == 0) {
            Array.Resize(ref points, 1);
            points[0] = newPoint;
            return;
        }

        Array.Resize(ref points, points.Length + 3);
        points[points.Length - 1] = newPoint;
        AutoConstructSpline();
    }

    public void AutoConstructSpline() {
        if (CurveCount == 1) {
            points[1] = points[0] + (points[3] - points[0]) / 6f;
            points[2] = points[3] + (points[0] - points[3]) / 6f;
            return;
        }

        int numEndPoints = CurveCount + 1;
        for (int i = 0; i < numEndPoints; i++) {
            Vector3 pMinus1, p1, p2;
            Vector3 p0 = points[i * 3];

            if (i == 0) {
                pMinus1 = points[0];
            }
            else {
                pMinus1 = points[(i - 1) * 3];
            }

            if (i < numEndPoints - 2) {
                p1 = points[(i + 1) * 3];
                p2 = points[(i + 2) * 3];
            }
            else if (i == numEndPoints - 2) {
                p1 = p2 = points[(i + 1) * 3];
            }
            else {
                p1 = p2 = p0;
            }

            if (i < numEndPoints - 1) {
                points[i * 3 + 1] = p0 + (p1 - pMinus1) / 6f;
                points[i * 3 + 2] = p1 - (p2 - p0) / 6f;
            }
            else {
                points[i * 3 - 1] = p0 - (p1 - pMinus1) / 6f;
            }
        }
    }

    private int GetCurveIndex(ref float t) {
        int i;
        if (t >= 1f) {
            t = 1f;
            i = points.Length - 4;
        }
        else {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }

        return i;
    }

    // Third Order (Four point) bezier solver
    // Equation: (1-t)^3*P_0 + 3*t*(1-t)^2*P_1 + 3*t^2*(1-t)*P_2 + t^3*P_3
    public static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        return (1 - t) * (1 - t) * (1 - t) * p0 + 3 * t * (1 - t) * (1 - t) * p1 + 3 * t * t * (1 - t) * p2 + t * t * t * p3;
    }

    // First derivative of Third Order (Four point) bezier solver.
    // Equation: 3*(1-t)^2*(P_1-P_0) + 6*t*(1-t)*(P_2-P_1) + 3*t^2*(P_3-P_2)
    public static Vector3 DerivOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        return 3 * (1 - t) * (1 - t) * (p1 - p0) + 6 * t * (1 - t) * (p2 - p1) + 3 * t * t * (p3 - p2);
    }

    // Finds the value t after moving a "real" distance `dist`.  This can be used to move along a curve at a constant
    // velocity.
    public static float GetTWithRealDistance(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float dist) {
        return dist / DerivOnBezier(p0, p1, p2, p3, t0).magnitude;
    }

    public void Reset() {
        points = new Vector3[0];
    }
}

public class OrientedPoint {
    public Vector3 position;
    public Vector3 tangent;
    public Vector3 normal;
    public Vector3 binormal;

    public Vector3 localToWorld(Vector3 localPos) {
        return position + Quaternion.LookRotation(tangent, normal) * localPos;
    }

    public Vector3 localToWorldVector(Vector3 localPos) {
        return Quaternion.LookRotation(tangent, normal) * localPos;
    }
}

public class BezierSplineDistanceLUT {
    BezierSpline spline;
    float[] distanceLUT;

    public float TotalLength {
        get { return distanceLUT[distanceLUT.Length - 1]; }
    }

    public BezierSplineDistanceLUT(BezierSpline spline, int sampleCount) {
        this.spline = spline;
        this.distanceLUT = new float[sampleCount];
        FillLengthTableInfo();
    }

    public float Sample(float t) {
        float iFloat = t * (distanceLUT.Length - 1);
        int idLower = Mathf.FloorToInt(iFloat);
        int idUpper = Mathf.FloorToInt(iFloat + 1);

        if (idUpper >= distanceLUT.Length) {
            return distanceLUT[distanceLUT.Length - 1];
        }

        if (idLower < 0) {
            return distanceLUT[0];
        }

        return Mathf.Lerp(distanceLUT[idLower], distanceLUT[idUpper], iFloat - idLower);
    }

    private void FillLengthTableInfo() {
        distanceLUT[0] = 0f;
        float totalLength = 0f;
        Vector3 prev = spline[0];

        for (int i = 1; i < distanceLUT.Length; i++) {
            float t = i / (distanceLUT.Length - 1f);
            Vector3 pt = spline.GetPointLocal(t);
            float distanceDiff = Vector3.Distance(prev, pt);
            totalLength += distanceDiff;
            distanceLUT[i] = totalLength;
            prev = pt;
        }
    }
}
