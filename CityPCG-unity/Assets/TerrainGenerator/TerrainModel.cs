using UnityEngine;

public struct TerrainModel {
    public float width, depth;
    public float seaLevel;
    private float maxHeight;
    private Noise noise;

    private int xResolution;
    private int zResolution;

    public Noise Noise {
        get { return noise; }
    }

    public struct TerrainHit {
        public Vector3 point;
        public Vector3 normal;
    }

    public TerrainModel(float width, float depth, float seaLevel, float maxHeight, Noise noise, int xResolution, int zResolution) {
        this.width = width;
        this.depth = depth;
        this.seaLevel = seaLevel;
        this.maxHeight = maxHeight;
        this.noise = noise;
        this.xResolution = xResolution;
        this.zResolution = zResolution;
    }

    public float GetHeight(float x, float z) {
        return maxHeight * noise.GetValue(x / width, z / depth);
    }

    public Vector3 GetPosition(float x, float y) {
        return new Vector3(x, GetHeight(x, y), y);
    }

    public Vector3 GetPosition(Vector2 pos) {
        return GetPosition(pos.x, pos.y);
    }

    public Vector2 GetSlope(float x, float z) {
        return noise.GetSlope(x / width, z / depth);
    }

    public Vector3 GetNormal(float x, float z) {
        Vector3 pos = GetPosition(x, z);
        Vector2 slope = -GetSlope(x, z);

        Vector3 forward = GetPosition(x + slope.x, z + slope.y) - pos;
        Vector3 right = GetPosition(x + slope.y, z - slope.x) - pos;

        if (slope == Vector2.zero) return Vector3.up;

        return Vector3.Cross(forward, right).normalized;
    }

    // Performs a "raycast" from above on the terrain _mesh_
    public TerrainHit GetMeshIntersection(float x, float z) {
        int xStep = (int)((x / width) * xResolution);
        int zStep = (int)((z / depth) * zResolution);

        Vector3 quadStart = GetTriangleCornerPos(xStep + 0, zStep + 0);
        Vector3 quadEnd = GetTriangleCornerPos(xStep + 1, zStep + 1);

        Vector3 p0, p1, p2;
        Vector2 given = new Vector2(x, z);
        bool isLowerRightTriangle = Vector2.Distance(given, VectorUtil.Vector3To2(quadStart)) < Vector2.Distance(given, VectorUtil.Vector3To2(quadEnd));
        if (isLowerRightTriangle) {
            p0 = quadStart;
            p1 = GetTriangleCornerPos(xStep + 1, zStep + 0);
            p2 = GetTriangleCornerPos(xStep + 0, zStep + 1);
        }
        else {
            p0 = quadEnd;
            p1 = GetTriangleCornerPos(xStep + 0, zStep + 1);
            p2 = GetTriangleCornerPos(xStep + 1, zStep + 0);
        }

        // The quad spans up a coordinate system starting from p0
        Vector3 right = (p1 - p0);
        Vector3 up = (p2 - p0);
        Vector3 normal = Vector3.Cross(up, right).normalized;

        // From equation of plane we can solve for y:
        //     A(x-x0) + B(y-y0) + C(z-z0) = 0   ==>   y = -(A(x - x0) - B y0 + C(z - z0)) / B, where B != 0
        float y = -(normal.x * (x - p0.x) - normal.y * p0.y + normal.z * (z - p0.z)) / normal.y;
        Vector3 point = new Vector3(x, y, z);

        TerrainHit hit = new TerrainHit();
        hit.point = point;
        hit.normal = normal;

        return hit;
    }

    private Vector3 GetTriangleCornerPos(int x, int z) {
        float xPos = (float)(x / (float)this.xResolution) * this.width;
        float zPos = (float)(z / (float)this.zResolution) * this.depth;
        float yPos = this.GetHeight(xPos, zPos);
        return new Vector3(xPos, yPos, zPos);
    }
}
