using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils {
    public class PolygonUtil {

        public struct Rectangle {
            public Vector2 topLeft;
            public Vector2 topRight;
            public Vector2 botLeft;
            public Vector2 botRight;

            public float angle; // counter-clockwise in radians
            public float width;
            public float height;
        }

        public static Rectangle CreateRectangle(float x, float y, float angle, float width, float height) {
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

        public static Rectangle ApproximateLargestRectangle(
                                                    List<Vector2> polygon,
                                                      float ratio,
                                                      float stepSize,
                                                      int angleResolution,
                                                      int widthResolution) {
            var best = new Rectangle();

            // Find center point of given polygon.
            // We'll try to center the rectangle here.
            Vector2 center = Vector2.zero;
            foreach (var p in polygon)
                center += p;
            center /= polygon.Count;

            // For each angle and width we consider...
            for (int a = 0; a < angleResolution; a++) {
                for (int w = 0; w < widthResolution; w++) {

                    // Compute attempted rectangle size.
                    float width = stepSize * (w + 1);
                    float height = width / ratio;

                    // For each angle we create a new coordinate system.
                    float rightAngle = (Mathf.PI / angleResolution) * a;
                    float upAngle = rightAngle + Mathf.PI / 2.0f;

                    // We then turn our angles into vectors that span the new coordinate system.
                    Vector2 rightDir = new Vector2(Mathf.Cos(rightAngle), Mathf.Sin(rightAngle));
                    Vector2 upDir = new Vector2(Mathf.Cos(upAngle), Mathf.Sin(upAngle));

                    // The 4 corners of our rotated rectangle.
                    Vector2 topLeft = center - (width * rightDir) + (height * upDir);
                    Vector2 topRight = center + (width * rightDir) + (height * upDir);
                    Vector2 botLeft = center - (width * rightDir) - (height * upDir);
                    Vector2 botRight = center + (width * rightDir) - (height * upDir);

                    // Check if our rectangle's lines intersects with the given polygon's lines.
                    bool intersects = false;
                    for (int i = 0; i < polygon.Count; i++) {
                        int i2 = (i + 1) % polygon.Count;

                        intersects =
                            // Check if rectangle surrounds polygon.
                            (LineIntersection.LineTest(center, topRight, polygon[i], polygon[i2]).type != LineIntersection.Type.None) ||

                            // Checks rectangle lines against polygon lines.
                            (LineIntersection.LineTest(topLeft, topRight, polygon[i], polygon[i2]).type != LineIntersection.Type.None) ||
                            (LineIntersection.LineTest(topRight, botRight, polygon[i], polygon[i2]).type != LineIntersection.Type.None) ||
                            (LineIntersection.LineTest(botRight, botLeft, polygon[i], polygon[i2]).type != LineIntersection.Type.None) ||
                            (LineIntersection.LineTest(botLeft, topLeft, polygon[i], polygon[i2]).type != LineIntersection.Type.None);

                        if (intersects) {
                            w = widthResolution; // larger widths won't help, so we break the outer loop.
                            break;
                        }
                    }

                    // If they don't intersect, then we can consider updating our best.
                    if (!intersects && best.width * best.height < width * height) {
                        best.topLeft = topLeft;
                        best.topRight = topRight;
                        best.botLeft = botLeft;
                        best.botRight = botRight;
                        best.width = width;
                        best.height = height;
                        best.angle = rightAngle;
                    }
                }
            }

            return best;
        }

        public static Vector3 PolygonCenter(List<Vector3> vertices) {
            return vertices.Aggregate(Vector3.zero, (s, v) => s + v) / (float)vertices.Count;
        }

        public static float PolygonArea(List<Vector3> vertices) {
            List<Vector3> vs = vertices;

            float area = 0.0f;
            for (int i = 0; i < vs.Count; i++)
                area += vs[i].x * (vs[(i + 1) % vs.Count].z - vs[(i - 1 + vs.Count) % vs.Count].z);

            return Mathf.Abs(area / 2.0f);
        }

        public static Vector2 GetPointOnCenterLine(PolygonUtil.Rectangle rect, Vector2 pos) {
            Vector2 center = (rect.topLeft + rect.topRight + rect.botLeft + rect.botRight) / 4f;

            if (rect.width == rect.height)
                return center;

            Vector2 dir = pos - center;
            Vector2 forward = rect.topLeft - rect.topRight;
            Vector2 right = rect.topLeft - rect.botLeft;

            if (forward.magnitude > right.magnitude) {
                float dot = Vector2.Dot(dir, forward.normalized);
                float len = forward.magnitude - right.magnitude;

                return center + forward.normalized * Mathf.Clamp(dot, -len / 2, len / 2);
            }
            else {
                float dot = Vector2.Dot(dir, right.normalized);
                float len = right.magnitude - forward.magnitude;

                return center + right.normalized * Mathf.Clamp(dot, -len / 2, len / 2);
            }
        }
    }
}
