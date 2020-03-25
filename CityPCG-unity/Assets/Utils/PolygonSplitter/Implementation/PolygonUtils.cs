using System;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Utils.PolygonSplitter.Implementation {

    /**
     * Contains multiple helper functions that's used throughout PolygonSplitter.
     *
     * Somewhat inspired by the project Polysplit made by Gediminas Rimša, read more in license.txt.
     */
    public static class PolygonUtils {
        public static Polygon CreateTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
            return new Polygon(new List<Vector3>() { v1, v2, v3, v1 });
        }

        /**
         * Basically adds the first point again to the end to create a loop.
         */
        public static Polygon CreatePolygon(List<Vector3> vertices) {
            if (vertices.Count <= 2) {
                return null;
            }
            var copy = new List<Vector3>(vertices) { vertices[0] };
            return new Polygon(copy);
        }


        //TODO: Check if startVertex and endVertex actually exists inside of polygonToSplit
        public static Polygon GetSubPolygon(Polygon polygonToSplit, Vector3 startVertex, Vector3 endVertex) {

            var vertices = new List<Vector3>();
            var started = false;

            for (var i = 0; i < polygonToSplit.points.Count; i++) {
                var vertex = polygonToSplit.points[i];
                if (vertex.Equals(startVertex)) {
                    started = true;
                }

                if (started) {
                    vertices.Add(vertex);

                    if (vertex.Equals(endVertex)) {
                        break;
                    }
                }

                if (i == polygonToSplit.points.Count - 1) {
                    i = -1;
                }
            }

            return CreatePolygon(vertices);
        }

        public static LineSegment GetLineSegment(Polygon polygon, int index, bool reversed = false) {
            var segment = new LineSegment(polygon.points[index], polygon.points[index + 1]);
            return reversed ? new LineSegment(segment.end, segment.start) : segment;
        }

        public static List<LineSegment> GetLineSegments(Polygon polygon) {
            var lineSegments = new List<LineSegment>();

            if (polygon.points.Count == 0) {
                return lineSegments;
            }

            var start = polygon.points[0];
            for (var i = 1; i < polygon.points.Count; i++) {
                var end = polygon.points[i];
                lineSegments.Add(new LineSegment(start, end));
                start = end;
            }
            return lineSegments;
        }

        // Distance to point (p) from line segment (end points a b)
        public static float DistanceLineSegmentPoint(Vector3 p, LineSegment line) {
            if (line.start == line.end) {
                return Vector3.Distance(line.start, p);
            }

            // Line segment to point distance equation
            var ba = line.end - line.start;
            var pa = line.start - p;
            return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
        }

        public static bool IsPointOnLineSegment(Vector3 p, LineSegment line) {
            return DistanceLineSegmentPoint(p, line) < 0.001;
        }

        public static Polygon SlicePolygon(Polygon polygonToSlice, Vector3 startPoint, Vector3 endPoint) {
            var vertices = new List<Vector3>();

            var started = false;
            var finished = false;

            var edges = GetLineSegments(polygonToSlice);
            foreach (var edge in edges) {
                if (!started && IsPointOnLineSegment(startPoint, edge) && !startPoint.Equals(edge.end)) {
                    // if startPoint is on the edge, start building up the sliced part
                    // if it is the endpoint, it will be considered as part of the next edge
                    vertices.Add(startPoint);
                    started = true;
                    continue;
                }

                if (started) {
                    vertices.Add(edge.start);

                    if (IsPointOnLineSegment(endPoint, edge)) {
                        vertices.Add(endPoint);
                        finished = true;
                        break;
                    }
                }
            }

            if (started && !finished) {
                // polygon runs through the first point - continue until endPoint is reached

                foreach (var edge in edges) {
                    vertices.Add(edge.start);
                    if (IsPointOnLineSegment(endPoint, edge)) {
                        vertices.Add(endPoint);
                        break;
                    }
                }
            }

            return CreatePolygon(vertices);
        }

        public static bool LineLineIntersection(LineSegment lineA, LineSegment lineB) {
            var p1 = lineA.start;
            var p2 = lineA.end;
            var p3 = lineB.start;
            var p4 = lineB.end;

            Vector2 a = p2 - p1;
            Vector2 b = p3 - p4;
            Vector2 c = p1 - p3;

            var alphaNumerator = b.y * c.x - b.x * c.y;
            var alphaDenominator = a.y * b.x - a.x * b.y;
            var betaNumerator = a.x * c.y - a.y * c.x;
            var betaDenominator = a.y * b.x - a.x * b.y;

            var doIntersect = true;

            if (Math.Abs(alphaDenominator) < float.Epsilon || Math.Abs(betaDenominator) < float.Epsilon) {
                doIntersect = false;
            }
            else {

                if (alphaDenominator > 0) {
                    if (alphaNumerator < 0 || alphaNumerator > alphaDenominator) {
                        doIntersect = false;

                    }
                }
                else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator) {
                    doIntersect = false;
                }

                if (doIntersect && betaDenominator > 0) {
                    if (betaNumerator < 0 || betaNumerator > betaDenominator) {
                        doIntersect = false;
                    }
                }
                else if (betaNumerator > 0 || betaNumerator < betaDenominator) {
                    doIntersect = false;
                }
            }

            return doIntersect;
        }

        //This function finds out on which side of a line segment the point is located.
        //The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
        //the line segment, project it on the line using ProjectPointOnLine() first.
        //Returns 0 if point is on the line segment.
        //Returns 1 if point is outside of the line segment and located on the side of linePoint1.
        //Returns 2 if point is outside of the line segment and located on the side of linePoint2.
        private static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

            var lineVec = linePoint2 - linePoint1;
            var pointVec = point - linePoint1;

            var dot = Vector3.Dot(pointVec, lineVec);

            //point is on side of linePoint2, compared to linePoint1
            if (dot > 0) {

                //point is on the line segment
                if (pointVec.magnitude <= lineVec.magnitude) {

                    return 0;
                }

                //point is not on the line segment and it is on the side of linePoint2
                return 2;
            }

            //Point is not on side of linePoint2, compared to linePoint1.
            //Point is not on the line segment and it is on the side of linePoint1.
            return 1;
        }

        //This function returns a point which is a projection from a point to a line.
        //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
        private static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point) {

            //get vector from point on line to point in space
            var linePointToPoint = point - linePoint;

            var t = Vector3.Dot(linePointToPoint, lineVec);

            return linePoint + lineVec * t;
        }

        //This function returns a point which is a projection from a point to a line segment.
        //If the projected point lies outside of the line segment, the projected point will
        //be clamped to the appropriate line edge.
        //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
        public static Vector3 GetProjectedPoint(LineSegment edge, Vector3 point) {
            var vector = edge.end - edge.start;

            var projectedPoint = ProjectPointOnLine(edge.start, vector.normalized, point);

            var side = PointOnWhichSideOfLineSegment(edge.start, edge.end, projectedPoint);

            switch (side) {
                //The projected point is on the line segment
                case 0:
                    return projectedPoint;
                case 1:
                    return edge.start;
                case 2:
                    return edge.end;
                default:
                    //output is invalid
                    return Vector3.zero;
            }
        }

        private static float Det(float a, float b, float c, float d) {
            return a * d - b * c;
        }
    }
}
