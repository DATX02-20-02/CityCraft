using UnityEngine;

public struct Rectangle {
    public Vector2 topLeft;
    public Vector2 topRight;
    public Vector2 botLeft;
    public Vector2 botRight;

    public float angle; // counter-clockwise in radians
    public float width;
    public float height;

    public Vector2 Center {
        get => (topLeft + topRight + botLeft + botRight) / 4.0;
    }

    public static Rectangle Create(float x, float y, float angle, float width, float height) {
        Rectangle rect = new Rectangle();
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        Vector2 pos = new Vector2(x, y);
        Vector2 forward = new Vector2(cos, sin);
        Vector2 right = new Vector2(sin, -cos);

        rect.topLeft = pos - forward * width / 2 - right * height / 2;
        rect.topRight = pos + forward * width / 2 - right * height / 2;
        rect.botLeft = pos - forward * width / 2 + right * height / 2;
        rect.botRight = pos + forward * width / 2 + right * height / 2;

        rect.angle = angle;
        rect.width = width;
        rect.height = height;

        return rect;
    }
}
