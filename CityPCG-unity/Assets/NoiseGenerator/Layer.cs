using System;
using UnityEngine;

[System.Serializable]
public class Layer {
    public Vector2 offset;
    public float scale = 1;
    public float magnitude = 1;
    public float exponent = 1;

    public Layer(Vector2 offset, float scale, float exponent) {
        this.offset = offset;
        this.scale = scale;
        this.exponent = exponent;
    }
}
