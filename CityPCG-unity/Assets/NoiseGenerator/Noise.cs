using System.Collections.Generic;
using UnityEngine;

public class Noise {
    private Layer[] layers = null;
    private float seed = 0;
    private float maxScale = 0;

    public Noise(Layer[] layers, float seed = 0) {
        this.layers = layers;
        this.seed = seed;

        foreach (Layer layer in layers) {
            if (layer.scale > maxScale) maxScale = layer.scale;
        }
    }

    public float GetValue(float x, float y) {
        float value = 0;
        float maxMagnitude = 0;

        foreach (Layer layer in layers) {
            float nx = x * layer.scale + layer.offset.x + this.seed;
            float ny = y * layer.scale + layer.offset.y + this.seed;

            float pvalue = Mathf.Pow(2.0f * Mathf.Clamp01(Mathf.PerlinNoise(nx, ny)) * layer.magnitude, layer.exponent) / 2.0f;
            value += pvalue;
            maxMagnitude += layer.magnitude;
        }

        return Mathf.Clamp01(value / maxMagnitude);
    }

    public Vector2 GetSlope(float x, float y) {
        float totalValue = 0;
        Vector2 avg = Vector2.zero;
        for (float i = 0; i <= Mathf.PI * 2; i += (Mathf.PI * 2) / 20) {
            Vector2 n = new Vector2(Mathf.Cos(i), Mathf.Sin(i));
            float dist = 0.01f / Mathf.Max(1, maxScale / 20);
            float value = this.GetValue(x + n.x * dist, y + n.y * dist);

            avg += n * value;
            totalValue += value;
        }

        return (avg / totalValue).normalized;
    }

}
