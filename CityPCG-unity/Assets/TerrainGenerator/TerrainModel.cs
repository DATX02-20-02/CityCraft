using UnityEngine;

public struct TerrainModel {
    public float width, depth;
    private float maxHeight;
    private Noise noise;

    public TerrainModel(float width, float depth, float maxHeight, Noise noise) {
        this.width = width;
        this.depth = depth;
        this.maxHeight = maxHeight;
        this.noise = noise;
    }

    public float GetHeight(float x, float z) {
        return maxHeight * noise.GetValue(x, z);
    }

    public Vector2 GetSlope(float x, float z) {
        return noise.GetSlope(x, z);
    }
}
