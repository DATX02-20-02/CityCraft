using UnityEngine;
using static Utils.PolygonSplitter.PolygonUtils;

namespace Utils.PolygonSplitter {
    public class IntersectionPosition {
        public readonly bool belongsToOneOfTheEdges;
        public Vector3 vector;

        public IntersectionPosition(Vector3 vector, LineSegment edgeA, LineSegment edgeB) {
            this.vector = vector;
            belongsToOneOfTheEdges = IsPointOnLineSegmentExcludingEndpoints(vector, edgeA) || IsPointOnLineSegmentExcludingEndpoints(vector, edgeB);
        }

    }
}
