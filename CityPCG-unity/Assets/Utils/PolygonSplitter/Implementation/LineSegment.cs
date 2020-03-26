using UnityEngine;

namespace Utils.PolygonSplitter.Implementation {

    /**
     * Represents a line from start position to end position.
     *
     * Heavily inspired by the project Polysplit made by Gediminas Rimša, read more in license.txt.
     */
    public class LineSegment {
        public Vector2 start;
        public Vector2 end;

        public LineSegment(Vector2 start, Vector2 end) {
            this.start = start;
            this.end = end;
        }

        public float GetLength() {
            return Vector2.Distance(start, end);
        }

        public Vector2 PointAlong(float segmentLengthFraction) {
            return new Vector2 {
                x = start.x + segmentLengthFraction * (end.x - start.x),
                y = start.y + segmentLengthFraction * (end.y - start.y)
            };
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

            var ls = (LineSegment)obj;
            return start == ls.start && end == ls.end;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public bool EqualsOneTopo(LineSegment other) {
            return start.Equals(other.start) || end.Equals(other.end)
                || start.Equals(other.end) || end.Equals(other.start);
        }
    }
}
