using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Utils.PolygonSplitter.Implementation;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

namespace Utils.PolygonSplitter {

    /**
     * Represents a polygon via the List<Vector3> points
     */
    [Serializable]
    public class Polygon {
        public List<Vector3> points;
        public Polygon(List<Vector3> points) {
            this.points = points;
        }

        public float GetArea() {
            var result = Vector3.zero;
            for (int p = points.Count - 1, q = 0; q < points.Count; p = q++) {
                result += Vector3.Cross(points[q], points[p]);
            }
            result *= 0.5f;

            return result.magnitude;
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

        public bool Contains(Vector3 p) {
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
                if (((pi.z <= p.z && p.z < pj.z) || (pj.z <= p.z && p.z < pi.z)) &&
                    (p.x < (pj.x - pi.x) * (p.z - pi.z) / (pj.z - pi.z) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        public Polygon Difference(Polygon otherPolygon) {
            var vertices = new List<Vector3>();

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
            var hasAdded = new HashSet<Vector3>();
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
