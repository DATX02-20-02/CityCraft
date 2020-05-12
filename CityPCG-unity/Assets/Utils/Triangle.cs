using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle {
    public Vector3 point1;
    public Vector3 point2;
    public Vector3 point3;

    public Triangle(Vector3 point1, Vector3 point2, Vector3 point3) {
        this.point1 = point1;
        this.point2 = point2;
        this.point3 = point3;
    }

    public Vector3 RandomPoint() {
        float r1 = Random.Range(0.0f, 1.0f);
        float r2 = Random.Range(0.0f, 1.0f);

        return point1 * (1 - Mathf.Sqrt(r1)) +
               point2 * (Mathf.Sqrt(r1) * (1 - r2)) +
               point3 * (Mathf.Sqrt(r1) * r2);
    }

    public List<Triangle> Subdivide(int count = 1) {
        if (count == 0) return new List<Triangle>() { this };

        Vector3 mid1 = Vector3.Lerp(point1, point2, 0.5f);
        Vector3 mid2 = Vector3.Lerp(point2, point3, 0.5f);
        Vector3 mid3 = Vector3.Lerp(point3, point1, 0.5f);

        List<Triangle> triangles = new List<Triangle>() {
            new Triangle(mid1, mid2, mid3),
            new Triangle(point1, mid1, mid3),
            new Triangle(point2, mid2, mid1),
            new Triangle(point3, mid3, mid2)
        };

        if (count > 1) {
            List<Triangle> newTriangles = new List<Triangle>();

            foreach (Triangle tri in triangles) {
                newTriangles.AddRange(tri.Subdivide(count - 1));
            }

            return newTriangles;
        }

        return triangles;
    }

    public float Area() {
        Vector3 v = Vector3.Cross(point1 - point2, point1 - point3);

        return v.magnitude * 0.5f;
    }
}

