using System;
using UnityEngine;

namespace Utils.PolygonSplitter {
    public class LineSegment {
        public Vector3 start;
        public Vector3 end;

        public LineSegment(Vector3 start, Vector3 end) {
            this.start = start;
            this.end = end;
        }

        public float GetLength() {
            return Vector3.Distance(start, end);
        }

        public Vector3 PointAlong(float segmentLengthFraction) {
            return new Vector3 {
                x = start.x + segmentLengthFraction * (end.x - start.x),
                z = start.z + segmentLengthFraction * (end.z - start.z)
            };
        }

        public Vector3 project(Vector3 p) {
            if (p.Equals(start) || p.Equals(end)) return new Vector3(p.x, p.y, p.z);

            var r = ProjectionFactor(p);
            return new Vector3 { x = start.x + r * (end.x - start.x), z = start.z + r * (end.z - start.z) };
        }

        /**
   * Computes the Projection Factor for the projection of the point p
   * onto this LineSegment.  The Projection Factor is the constant r
   * by which the vector for this segment must be multiplied to
   * equal the vector for the projection of p on the line
   * defined by this segment.
   */
        public float ProjectionFactor(Vector3 p) {
            if (p.Equals(start)) return 0.0f;
            if (p.Equals(end)) return 1.0f;
            // Otherwise, use comp.graphics.algorithms Frequently Asked Questions method
            /*     	      AC dot AB
                           r = ---------
                                 ||AB||^2
                        r has the following meaning:
                        r=0 P = A
                        r=1 P = B
                        r<0 P is on the backward extension of AB
                        r>1 P is on the forward extension of AB
                        0<r<1 P is interior to AB
                */
            var dx = end.x - start.x;
            var dz = end.z - start.z;
            var len2 = dx * dx + dz * dz;
            var r = ((p.x - start.x) * dx + (p.z - start.z) * dz)
                    / len2;
            return r;
        }

        public bool EqualsTopo(LineSegment other) {
            return
                start.Equals(other.start) && end.Equals(other.end)
                || start.Equals(other.end) && end.Equals(other.start);
        }

        public override string ToString() {
            return start + " ---> " + end;
        }

        public override bool Equals(object obj) {      //Check for null and compare run-time types.
            if ((obj == null) || GetType() != obj.GetType()) {
                return false;
            }

            LineSegment ls = (LineSegment)obj;
            return start == ls.start && end == ls.end;
        }

        public bool EqualsOneTopo(LineSegment other) {
            return start.Equals(other.start) || end.Equals(other.end)
                || start.Equals(other.end) || end.Equals(other.start);
        }
    }
}
