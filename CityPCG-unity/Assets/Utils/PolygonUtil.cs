using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils {
    public class PolygonUtil {
        private static void AddIfIntersecting(List<LineIntersection.Result> results, LineIntersection.Result res) {
            if (res.type == LineIntersection.Type.Intersecting)
                results.Add(res);
        }

        public static bool IsConvex(List<Vector2> polygon) {
            for (int i = 0; i < polygon.Count; i++) {
                Vector2 v1 = polygon[i];
                Vector2 v2 = polygon[(i + 1) % polygon.Count];
                Vector2 v3 = polygon[(i + 2) % polygon.Count];

                Vector2 d1 = v2 - v1;
                Vector2 d2 = v3 - v2;

                float cross = d1.x * d2.y - d1.y * d2.x;
                if (cross < 0) {
                    return false;
                }
            }

            return true;
        }

        public static Rectangle ApproximateLargestRectangle(
            List<Vector2> polygon,
            float ratio,
            float stepSize,
            int angleResolution,
            int widthResolution,
            int centerIterations = 4
        ) {
            var best = new Rectangle();

            if (polygon.Count == 4) {
                // Check if perfectly square
                if (Mathf.Abs(Vector2.Dot((polygon[1] - polygon[0]).normalized, (polygon[3] - polygon[2]).normalized)) == 1 &&
                    Mathf.Abs(Vector2.Dot((polygon[2] - polygon[1]).normalized, (polygon[0] - polygon[3]).normalized)) == 1) {
                    best.topLeft = polygon[0];
                    best.topRight = polygon[1];
                    best.botRight = polygon[2];
                    best.botLeft = polygon[3];

                    best.width = Vector2.Distance(best.topLeft, best.topRight);
                    best.height = Vector2.Distance(best.botLeft, best.botRight);

                    Vector2 d = best.topRight - best.topLeft;
                    best.angle = Mathf.Atan2(d.y, d.x);

                    return best;
                }

                if (IsConvex(polygon)) {
                    for (int i = 0; i < polygon.Count; i++) {
                        Vector2 v1 = polygon[i];
                        Vector2 v2 = polygon[(i + 1) % polygon.Count];
                        Vector2 v3 = polygon[(i + 2) % polygon.Count];
                        Vector2 v4 = polygon[(i + 3) % polygon.Count];

                        Vector2 dir = (v2 - v1).normalized;
                        Vector2 perp = new Vector2(-dir.y, dir.x);

                        Vector2 o1 = v3;
                        Vector2 o2 = v4;

                        Vector2 oDir = (o2 - o1).normalized;
                        Vector2 oPerp = new Vector2(oDir.y, -oDir.x);

                        List<LineIntersection.Result> candidatesA = new List<LineIntersection.Result>();
                        List<LineIntersection.Result> candidatesB = new List<LineIntersection.Result>();

                        AddIfIntersecting(candidatesA, LineIntersection.RayTest(o1, o2, v1, perp));
                        AddIfIntersecting(candidatesA, LineIntersection.RayTest(o1, o2, v2, perp));

                        AddIfIntersecting(candidatesA, LineIntersection.RayTest(v1, v2, o1, -perp));
                        AddIfIntersecting(candidatesA, LineIntersection.RayTest(v1, v2, o2, -perp));

                        // There will always be either 0 or 2 candidates
                        if (candidatesA.Count == 2) {
                            LineIntersection.Result res0 = candidatesA[0];
                            LineIntersection.Result res1 = candidatesA[1];

                            Vector2 topLeft;
                            Vector2 topRight;
                            Vector2 botLeft;
                            Vector2 botRight;

                            if (res0.factorB < res1.factorB) {
                                topLeft = res0.origin;
                                topRight = res0.point;
                                botLeft = VectorUtil.GetProjectedPointOnLine(res0.origin, res1.point, res1.origin, false);
                                botRight = VectorUtil.GetProjectedPointOnLine(res0.point, res1.point, res1.origin, false);
                            }
                            else {
                                topLeft = res1.origin;
                                topRight = res1.point;
                                botLeft = VectorUtil.GetProjectedPointOnLine(res1.origin, res0.point, res0.origin, false);
                                botRight = VectorUtil.GetProjectedPointOnLine(res1.point, res0.point, res0.origin, false);
                            }

                            float width = Vector2.Distance(topLeft, topRight);
                            float height = Vector2.Distance(topLeft, botLeft);

                            if (width * height > best.width * best.height) {
                                best.topLeft = topLeft;
                                best.topRight = topRight;
                                best.botLeft = botLeft;
                                best.botRight = botRight;
                                best.width = width;
                                best.height = height;

                                Vector2 d = best.topRight - best.topLeft;
                                best.angle = Mathf.Atan2(d.y, d.x);
                            }
                        }
                    }

                    return best;
                }
            }

            // To calculate random center we have to triangulate first, unfortunately
            Vector3[] poly3D = polygon.Select(VectorUtil.Vector2To3).ToArray();

            Triangulator triangulator = new Triangulator(poly3D);
            int[] triangulated = triangulator.Triangulate();

            for (int c = 0; c < centerIterations; c++) {
                int randIndex = Random.Range(0, triangulated.Length / 3) * 3 % triangulated.Length;

                float r1 = Random.Range(0.3f, 0.7f);
                float r2 = Random.Range(0.3f, 0.7f);

                Vector2 center = polygon[triangulated[randIndex]] * (1 - Mathf.Sqrt(r1)) +
                    polygon[triangulated[randIndex + 1]] * (Mathf.Sqrt(r1) * (1 - r2)) +
                    polygon[triangulated[randIndex + 2]] * (Mathf.Sqrt(r1) * r2);

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
                            best.width = width * 2;
                            best.height = height * 2;
                            best.angle = rightAngle;
                        }
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
