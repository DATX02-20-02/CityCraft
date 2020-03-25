using UnityEngine;

public struct TerrainModel {
    public float width, depth;
    public float seaLevel;
    private float maxHeight;
    private Noise noise;

    public TerrainModel(float width, float depth, float seaLevel, float maxHeight, Noise noise) {
        this.width = width;
        this.depth = depth;
        this.seaLevel = seaLevel;
        this.maxHeight = maxHeight;
        this.noise = noise;
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
}
