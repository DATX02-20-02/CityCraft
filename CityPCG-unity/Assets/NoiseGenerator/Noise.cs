using UnityEngine;

public class Noise {
    private float[] map;
    private float width;
    private float height;

    private Vector3 position;

    private int textureWidth;
    private int textureHeight;

    public Noise(float[] map, Vector3 position, float width, float height, int textureWidth, int textureHeight) {
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

        int mx = (int)(Mathf.Clamp(x / this.width, 0, 1) * this.textureWidth);
        int my = (int)(Mathf.Clamp(y / this.height, 0, 1) * this.textureHeight);
        return new Vector2Int(mx, my);
    }


    public bool IsInBounds(float x, float y) {
        float mx = x / this.width;
        float my = y / this.height;

        return mx >= 0 && mx < 1 && my >= 0 && my < 1;
    }

    public float GetValue(float x, float y) {
        Vector2Int mapped = this.MapCoordinates(x, y);
        return this.map[mapped.x + mapped.y * this.textureWidth];
    }

    public Vector2 GetSlope(float x, float y) {
        float maxValue = float.MaxValue;
        Vector2 max = Vector2.zero;

        for (float i = 0; i <= Mathf.PI * 2; i += (Mathf.PI * 2) / 10) {
            Vector2 n = new Vector2(Mathf.Cos(i), Mathf.Sin(i));
            float value = this.GetValue(x + n.x * 10, y + n.y * 10);

            if (value > maxValue) {
                max = n;
                maxValue = value;
            }
        }

        return max;
    }

}
