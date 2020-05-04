using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils.PolygonSplitter.Implementation {

    /**
     * Contains multiple helper functions that's used throughout PolygonSplitter.
     *
     * Somewhat inspired by the project Polysplit made by Gediminas Rimša, read more in license.txt.
     */
    public static class PolygonUtils {
        public static Polygon CreateTriangle(Vector2 v1, Vector2 v2, Vector2 v3) {
            return new Polygon(new List<Vector2>() { v1, v2, v3, v1 });
        }

        /**
         * Basically adds the first point again to the end to create a loop.
         */
        public static Polygon CreatePolygon(List<Vector2> vertices) {
            if (vertices.Count <= 2) {
                return null;
            }
            var copy = new List<Vector2>(vertices) { vertices[0] };
            return new Polygon(copy);
        }


        //TODO: Check if startVertex and endVertex actually exists inside of polygonToSplit
        public static Polygon GetSubPolygon(Polygon polygonToSplit, Vector2 startVertex, Vector2 endVertex) {

            var vertices = new List<Vector2>();
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

            //TODO: Investigate reason why polygon or polygon.points can be null.
            if (polygon == null || polygon.points == null || polygon.points.Count == 0) {
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
        // NOTE: This treats the line as an infinite line.
        private static float DistanceLineSegmentPoint(Vector2 p, LineSegment line) {
            if (line.start == line.end) {
                return Vector2.Distance(line.start, p);
            }

            // Line segment to point distance equation
            var ba = line.end - line.start;
            var pa = line.start - p;
            return (pa - ba * (Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba))).magnitude;
        }

        public static bool IsPointOnLineSegment(Vector2 p, LineSegment line) {
            float straight = Vector2.Distance(line.start, line.end);
            float detour = Vector2.Distance(line.start, p) + Vector2.Distance(p, line.end);
            return Mathf.Approximately(straight, detour);
        }

        // NOTE: This returns only one of the two slices. Use Difference() to get the other one.
        public static Polygon SlicePolygon(Polygon polygonToSlice, Vector2 startPoint, Vector2 endPoint) {
            var vertices = new List<Vector2>();

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
        private static int PointOnWhichSideOfLineSegment(Vector2 linePoint1, Vector2 linePoint2, Vector2 point) {

            var lineVec = linePoint2 - linePoint1;
            var pointVec = point - linePoint1;

            var dot = Vector2.Dot(pointVec, lineVec);

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
        private static Vector2 ProjectPointOnLine(Vector2 linePoint, Vector2 lineVec, Vector2 point) {

            //get vector from point on line to point in space
            var linePointToPoint = point - linePoint;

            var t = Vector2.Dot(linePointToPoint, lineVec);

            return linePoint + lineVec * t;
        }

        //This function returns a point which is a projection from a point to a line segment.
        //If the projected point lies outside of the line segment, the projected point will
        //be clamped to the appropriate line edge.
        //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
        public static Vector2 GetProjectedPoint(LineSegment edge, Vector2 point) {
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
                    return Vector2.zero;
            }
        }

        private static float Det(float a, float b, float c, float d) {
            return a * d - b * c;
        }

        public static bool IsIntersectingPolygon(LineSegment line, Polygon polygon) {
            List<LineSegment> edges = GetLineSegments(polygon);

            foreach (LineSegment edge in edges) {
                if ((LineLineIntersection(line, edge) && (!AreLinesConnected(line, edge))) || LinesCoincide(line, edge)) {
                    return true;
                }
            }
            return false;
        }

        public static bool AreLinesConnected(LineSegment l1, LineSegment l2) {
            bool case1 = IsPointOnLineSegment(l1.start, l2);
            bool case2 = IsPointOnLineSegment(l1.end, l2);
            bool case3 = IsPointOnLineSegment(l2.start, l1);
            bool case4 = IsPointOnLineSegment(l2.end, l1);
            return (case1 || case2 || case3 || case4);
        }

        public static bool LinesCoincide(LineSegment l1, LineSegment l2) {
            bool case1 = IsPointOnLineSegment(l1.start, l2) && IsPointOnLineSegment(l1.end, l2);
            bool case2 = IsPointOnLineSegment(l2.start, l1) && IsPointOnLineSegment(l2.end, l1);
            return (case1 || case2);
        }

        public static bool IsLineInsidePolygon(LineSegment l, Polygon p) {
            float t = 0.01f; // tolerance
            Vector2 almost_start = (1f-t) * l.start + t * l.end;
            Vector2 almost_end = t * l.start + (1f-t) * l.end;
            bool b = p.Contains(almost_start) && p.Contains(almost_end);
            return b;
        }
    }
}
