using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Utils.PolygonSplitter.Implementation;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

namespace Utils.PolygonSplitter {

    /**
     * Represents a polygon via the List<Vector2> points
     */
    [Serializable]
    public class Polygon {
        public List<Vector2> points;
        public Polygon(List<Vector2> points) {
            this.points = points;
        }
        public float GetArea() {
            var area = 0.0f;
            for (var i = 0; i < points.Count; i++)
                area += points[i].x * (points[(i + 1) % points.Count].y - points[(i - 1 + points.Count) % points.Count].y);

            return Mathf.Abs(area / 2.0f);
        }

        public override string ToString() {
            var sb = new StringBuilder();

            sb.Append("[" + GetArea() + "] ");

            for (var i = 0; i < points.Count; i++) {
                var point = points[i];
                sb.Append(point);

                if (i + 1 < points.Count) {
                    sb.Append(" ---> ");
                }
            }
            return sb.ToString();
        }

        //https://stackoverflow.com/a/4833823
        public bool Contains(Polygon polygon) {
            var p1Segments = GetLineSegments(this);
            var p2Segments = GetLineSegments(polygon);

            // NOTE: This is problematic, because it doesn't allow the polygons to have any coinciding edges.
            foreach (var ls1 in p1Segments) {
                foreach (var ls2 in p2Segments) {
                    if (LineLineIntersection(ls1, ls2)) {
                        return false;
                    }
                }
            }

            foreach (var line in p2Segments) {
                if (Contains(line.end)) {
                    return true;
                }
            }

            return false;

        }

        public bool Contains(Vector2 p) {
            foreach (var point in points) {
                if (point == p) {
                    return true;
                }
            }

            var j = points.Count - 1;
            var inside = false;
            for (var i = 0; i < points.Count; j = i++) {
                var pi = points[i];
                var pj = points[j];
                if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        public Polygon Difference(Polygon otherPolygon) {
            var vertices = new List<Vector2>();

            var segmentsA = GetLineSegments(this);
            var segmentsB = GetLineSegments(otherPolygon);

            // 1. Find all lines from the original that strictly isn't in the slice.
            var segs = new List<LineSegment>();
            foreach (var a in segmentsA) {
                bool isOutside = true;
                foreach (var b in segmentsB) {
                    if (a.EqualsTopo(b))
                        isOutside = false;
                }

                if (isOutside)
                    segs.Add(a);
            }

            // 2. Find overlapping lines (if any) and shorten segments in segs.
            foreach (var a in segmentsA) {
                foreach (var b in segmentsB) {
                    if (IsPointOnLineSegment(b.start, a) && IsPointOnLineSegment(b.end, a)) {
                        // Shorten a, such that it doesn't equal b.
                        if (a.start == b.start)
                            a.start = b.end;
                        else if (a.end == b.end)
                            a.end = b.start;
                        else if (a.start == b.end)
                            a.start = b.start;
                        else if (a.end == b.start)
                            a.end = b.end;
                    }
                }
            }

            // 3. Add all unique vertices.
            var hasAdded = new HashSet<Vector2>();
            foreach (var s in segs) {
                if (!hasAdded.Contains(s.start)) {
                    hasAdded.Add(s.start);
                    vertices.Add(s.start);
                }
                if (!hasAdded.Contains(s.end)) {
                    hasAdded.Add(s.end);
                    vertices.Add(s.end);
                }
            }

            return CreatePolygon(vertices);
        }

    }
}
