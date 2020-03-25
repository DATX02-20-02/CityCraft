using UnityEngine;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

namespace Utils.PolygonSplitter.Implementation {
    /**
     * Represents a pair of edges on a polygon. Assumes the direction of the polygon is clockwise.
     *
     * Possible lines of cut are located in one of:
     *
     * T1 - First triangle, may not exist in some cases
     * Trapezoid - Trapezoid, always present
     * T2 - Second triangle, may not exist in some cases
     *
     *
     *
     *                                edgeA
     *            edgeA.p0 .____________________________. edgeA.p1
     *                    /|                            |\
     *                   /                                \
     *   outsideEdge2   /  |                            |  \   outsideEdge1
     *                 /                                    \
     *                / T2 |        Trapezoid           | T1 \
     *               /                                        \
     *              .______.____________________________|______.
     *        edgeB.p1                edgeB                    edgeB.p0
     *                     ^                            ^
     *                 projected1                  projected0
     *
     *
     * Heavily inspired by the project Polysplit made by Gediminas Rimša, read more in license.txt.
     */
    public class EdgePair {
        private readonly LineSegment edgeA;
        private readonly LineSegment edgeB;

        private readonly ProjectedVertex projected0;
        private readonly ProjectedVertex projected1;

        public EdgePair(LineSegment edgeA, LineSegment edgeB) {
            this.edgeA = edgeA;
            this.edgeB = edgeB;

            projected0 = GetProjectedVertex(edgeA.end, edgeB);
            if (!projected0.valid) {
                projected0 = GetProjectedVertex(edgeB.start, edgeA);
            }
            projected1 = GetProjectedVertex(edgeB.end, edgeA);
            if (!projected1.valid) {
                projected1 = GetProjectedVertex(edgeA.start, edgeB);
            }
        }

        private static ProjectedVertex GetProjectedVertex(Vector3 point, LineSegment edge) {
            var projectionPoint = GetProjectedPoint(edge, point);
            return projectionPoint != Vector3.zero ? new ProjectedVertex(projectionPoint, edge) : ProjectedVertex.INVALID;
        }

        public EdgePairSubPolygons GetSubPolygons() {
            return new EdgePairSubPolygons(edgeA, edgeB, projected0, projected1);
        }
    }
}
