using System.Collections.Generic;
using UnityEngine;
using Utils;

public interface IAmplifier { }

public class CircularAmplifier : IAmplifier {
    public Vector2 position;
    public float magnitude;
    public float radius;
    public float dropoff; // value 0-1
    public bool linear;

    public CircularAmplifier(Vector2 position, float magnitude, float radius, float dropoff = 1, bool linear = false) {
        this.position = position;
        this.magnitude = magnitude;
        this.radius = radius;
        this.dropoff = dropoff;
        this.linear = linear;
    }
}

public class RectangularAmplifier : IAmplifier {
    public PolygonUtil.Rectangle rectangle;
    public float magnitude;
    public float dropoff; // value 0-1
    public bool linear;

    public RectangularAmplifier(PolygonUtil.Rectangle rectangle, float magnitude, float dropoff = 1, bool linear = false) {
        this.rectangle = rectangle;
        this.magnitude = magnitude;
        this.dropoff = dropoff;
        this.linear = linear;
    }
}

public class Noise {
    private Layer[] layers = null;
    private Vector2 offset;
    private float maxScale = 0;
    private FastNoise noise;

    private List<IAmplifier> amplifiers = new List<IAmplifier>();

    public List<IAmplifier> Amplifiers {
        get { return amplifiers; }
        set { amplifiers = value; }
    }

    public Noise(Layer[] layers, Vector2 offset) {
        this.layers = layers;
        this.offset = offset;

        foreach (Layer layer in layers) {
            if (layer.scale > maxScale) maxScale = layer.scale;
        }

        this.noise = new FastNoise();
        this.noise.SetSeed(Random.Range(0, 1000000));

        this.noise.SetFrequency(1);
    }

    public Noise(Layer[] layers) : this(layers, Vector2.zero) { }

    public float GetValue(float x, float y) {
        float value = 0;
        float maxMagnitude = 0;
        Vector2 pos = new Vector2(x, y);

        foreach (Layer layer in layers) {
            float nx = (x + layer.offset.x + this.offset.x);
            float ny = (y + layer.offset.y + this.offset.y);

            noise.SetFrequency(layer.scale);
            float val = (noise.GetNoise(nx, ny) + 1) / 2;
            float pvalue = Mathf.Pow(2.0f * Mathf.Clamp01(val) * layer.magnitude, layer.exponent) / 2.0f;
            value += pvalue;
            maxMagnitude += layer.magnitude;
        }

        float newValue = value / maxMagnitude;

        foreach (IAmplifier amplifier in amplifiers) {
            if (amplifier is CircularAmplifier) {
                CircularAmplifier cAmp = (CircularAmplifier)amplifier;
                float distance = Vector2.Distance(pos, cAmp.position);
                float t = Mathf.Clamp01(distance / cAmp.radius);
                float v = cAmp.linear ?
                    Mathf.Lerp(cAmp.magnitude, newValue, Mathf.Lerp(0, cAmp.dropoff, t)) :
                    Mathf.SmoothStep(cAmp.magnitude, newValue, Mathf.Lerp(0, cAmp.dropoff, t));

                newValue = distance > cAmp.radius ? newValue : v;
            }
            else if (amplifier is RectangularAmplifier) {
                RectangularAmplifier rAmp = (RectangularAmplifier)amplifier;

                Vector2 cPos = PolygonUtil.GetPointOnCenterLine(rAmp.rectangle, pos);
                float distance = Vector2.Distance(pos, cPos);
                float shortestSide = rAmp.rectangle.width > rAmp.rectangle.height ?
                    rAmp.rectangle.height :
                    rAmp.rectangle.width;
                float maxDistance = shortestSide / 2f;

                float t = Mathf.Clamp01(distance / maxDistance);
                float v = rAmp.linear ?
                    Mathf.Lerp(rAmp.magnitude, newValue, Mathf.Lerp(0, rAmp.dropoff, t)) :
                    Mathf.SmoothStep(rAmp.magnitude, newValue, Mathf.Lerp(0, rAmp.dropoff, t));

                newValue = distance > maxDistance ? newValue : v;
            }
        }

        return Mathf.Clamp01(newValue);
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

    public void AddAmplifier(IAmplifier amplifier) {
        this.amplifiers.Add(amplifier);
    }
}
