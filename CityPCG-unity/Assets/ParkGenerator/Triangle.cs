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
}

