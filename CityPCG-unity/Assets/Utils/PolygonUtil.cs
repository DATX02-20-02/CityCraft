using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils {
    public class PolygonUtil {
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
    }
}