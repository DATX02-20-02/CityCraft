using System.Collections.Generic;
using UnityEngine;

public class GeneratedNoise {
    private float[] map;
    private float width;
    private float height;

    private Vector3 position;

    private int textureWidth;
    private int textureHeight;

    public GeneratedNoise(float[] map, Vector3 position, float width, float height, int textureWidth, int textureHeight) {
        this.map = map;
        this.position = position;

        this.width = width;
        this.height = height;

        this.textureWidth = textureWidth;
        this.textureHeight = textureHeight;
    }

    public float[] Map {
        get { return this.map; }
    }

    public float Width { get { return this.width; } }
    public float Height { get { return this.height; } }

    public int TextureWidth { get { return this.textureWidth; } }
    public int TextureHeight { get { return this.textureHeight; } }

    public Vector3 Position { get { return this.position; } }


    public Vector2Int MapCoordinates(float x, float y, bool useRelative = false) {
        if (useRelative) {
            x = x - this.position.x;
            y = y - this.position.z;
        }

        int mx = (int)(Mathf.Clamp(x / this.width, 0, 1) * (this.textureWidth - 1));
        int my = (int)(Mathf.Clamp(y / this.height, 0, 1) * (this.textureHeight - 1));
        return new Vector2Int(mx, my);
    }


    public bool IsInBounds(float x, float y) {
        float mx = x / this.textureWidth;
        float my = y / this.textureHeight;

        return mx >= 0 && mx < 1 && my >= 0 && my < 1;
    }

    public float GetValue(int x, int y) {
        if (!IsInBounds(x, y)) return 0;

        return this.map[x + y * this.textureWidth];
    }

    public Vector2 GetSlope(float x, float y) {
        if (!IsInBounds(x, y)) return Vector2.zero;

        List<WeightedDirection> directions = new List<WeightedDirection>();

        float totalValue = 0;
        for (float i = 0; i <= Mathf.PI * 2; i += (Mathf.PI * 2) / 20) {
            Vector2 n = new Vector2(Mathf.Cos(i), Mathf.Sin(i));
            float dist = Mathf.Sqrt(2) * 10;
            float value = this.GetValue((int)(x + n.x * dist), (int)(y + n.y * dist));

            WeightedDirection direction;
            direction.value = value;
            direction.dir = n;
            directions.Add(direction);

            totalValue += value;
        }

        Vector2 avg = Vector2.zero;
        foreach (WeightedDirection direction in directions) {
            avg += direction.dir * direction.value / totalValue;
        }

        return avg.normalized;
    }

}
