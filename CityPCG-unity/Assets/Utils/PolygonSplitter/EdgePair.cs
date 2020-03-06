using UnityEngine;
using static Utils.PolygonSplitter.PolygonUtils;

namespace Utils.PolygonSplitter
{
    public class EdgePair
    {
        private readonly LineSegment edgeA;
        private readonly LineSegment edgeB;

        public ProjectedVertex projected0;
        public ProjectedVertex projected1;

        public IntersectionPosition intersectionPoint;
        
        public EdgePair(LineSegment edgeA, LineSegment edgeB)
        {
            intersectionPoint = GetIntersectionPoint(edgeA, edgeB);
            
            this.edgeA = edgeA;
            this.edgeB = edgeB;
            
            projected0 = GetProjectedVertex(edgeA.end, edgeB, intersectionPoint);
            if (!projected0.valid) {
                projected0 = GetProjectedVertex(edgeB.start, edgeA, intersectionPoint);
            }
            projected1 = GetProjectedVertex(edgeB.end, edgeA, intersectionPoint);
            if (!projected1.valid) {
                projected1 = GetProjectedVertex(edgeA.start, edgeB, intersectionPoint);
            }
        }
        
        private static ProjectedVertex GetProjectedVertex(Vector3 point, LineSegment edge, IntersectionPosition intersectionPoint) {
            var projectionPoint = GetProjectedPoint(edge, point, intersectionPoint);
            return projectionPoint != Vector3.zero ? new ProjectedVertex(projectionPoint, edge) : ProjectedVertex.INVALID;
        }
        
        public EdgePairSubPolygons GetSubPolygons() {
            return new EdgePairSubPolygons(edgeA, edgeB, projected0, projected1);
        }
    }
}