using System.Collections.Generic;
using UnityEngine;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

namespace Utils.PolygonSplitter.Implementation {

    /**
     * Represents three possible polygons where the minimum cut can be located
     *
     * Heavily inspired by the project Polysplit made by Gediminas Rimša, read more in license.txt.
     */
    public class EdgePairSubPolygons {
        private readonly LineSegment edgeA;
        private readonly LineSegment edgeB;

        private readonly Polygon trapezoid;

        private readonly float leftTriangleArea;
        private readonly float trapezoidArea;
        private readonly float rightTriangleArea;

        private readonly ProjectedVertex leftTriangleProjectedVertex;
        private readonly ProjectedVertex rightTriangleProjectedVertex;

        public EdgePairSubPolygons(LineSegment edgeA, LineSegment edgeB, ProjectedVertex projected0, ProjectedVertex projected1) {
            this.edgeA = edgeA;
            this.edgeB = edgeB;

            // build triangles if corresponding projected points are valid

            var leftTriangle = projected0.valid ? CreateTriangle(edgeA.end, projected0.vertex, edgeB.start) : null;
            leftTriangleProjectedVertex = leftTriangle != null ? projected0 : null;
            leftTriangleArea = leftTriangle?.GetArea() ?? 0;

            var rightTriangle = projected1.valid ? CreateTriangle(edgeA.start, projected1.vertex, edgeB.end) : null;
            rightTriangleProjectedVertex = rightTriangle != null ? projected1 : null;
            rightTriangleArea = rightTriangle?.GetArea() ?? 0;

            // build a trapezoid:
            // 1) if projected1 is on edgeA, add projected1, else add edgeA.start
            // 2) if projected0 is on edgeA, add projected0, else add edgeA.end
            // 3) if projected0 is on edgeB, add projected0, else add edgeB.start
            // 4) if projected1 is on edgeB, add projected1, else add edgeB.end
            // 5) close the polygon
            var coordinate1 = projected1.IsOnEdge(edgeA) ? projected1.vertex : edgeA.start;
            var coordinate2 = projected0.IsOnEdge(edgeA) ? projected0.vertex : edgeA.end;
            var coordinate3 = projected0.IsOnEdge(edgeB) ? projected0.vertex : edgeB.start;
            var coordinate4 = projected1.IsOnEdge(edgeB) ? projected1.vertex : edgeB.end;
            trapezoid = CreatePolygon(new List<Vector2> { coordinate1, coordinate2, coordinate3, coordinate4 });
            trapezoidArea = trapezoid.GetArea();
        }

        private float GetTotalArea() {
            return leftTriangleArea + trapezoidArea + rightTriangleArea;
        }

        public List<Cut> GetCuts(Polygon polygon, float singlePartArea) {
            var cuts = new List<Cut>(2);

            var segments = GetLineSegments(polygon);
            var indexOfEdgeA = segments.IndexOf(edgeA);
            var indexOfEdgeB = segments.IndexOf(edgeB);
            var segmentsCovered = indexOfEdgeB - indexOfEdgeA;            // number of segments covered by a LineRing starting with edgeA and ending with edgeB (including)

            // Polygon's exterior ring is equal to [edgeA + segmentsBetweenEdgePair + edgeB + segmentsOutsideEdgePair]
            var segmentCountBetweenEdgePair = segmentsCovered - 2;
            var segmentCountOutsideEdgePair = segments.Count - segmentsCovered;

            // if edges are not connected directly, polygon has extra area adjacent to them
            Polygon polygonOutside1 = null;
            Polygon polygonOutside2 = null;
            if (segmentCountBetweenEdgePair > 1) {
                // calculate extra area bounded by segmentsBetweenEdgePair
                polygonOutside1 = GetSubPolygon(polygon, edgeA.end, edgeB.start);
            }
            if (segmentCountOutsideEdgePair > 1) {
                // calculate extra area bounded by segmentsOutsideEdgePair
                polygonOutside2 = GetSubPolygon(polygon, edgeB.end, edgeA.start);
            }
            var areaOutside1 = polygonOutside1?.GetArea() ?? 0;
            var areaOutside2 = polygonOutside2?.GetArea() ?? 0;

            // check first direction (areaOutside1 + T1 + Trapezoid + T2)
            if (areaOutside1 <= singlePartArea) {
                LineSegment lineOfCut = null;                       // line of cut goes from edgeA to edgeB

                if (areaOutside1 + leftTriangleArea > singlePartArea) {
                    // produce a Cut in leftTriangle

                    var areaToCutAwayInTriangle = singlePartArea - areaOutside1;
                    var fraction = areaToCutAwayInTriangle / leftTriangleArea;

                    var edgeWithPointOfCut = leftTriangleProjectedVertex.IsOnEdge(edgeA)
                        ? new LineSegment(edgeA.end, leftTriangleProjectedVertex.vertex)
                        : new LineSegment(edgeB.start, leftTriangleProjectedVertex.vertex);
                    var pointOfCut = edgeWithPointOfCut.PointAlong(fraction);
                    lineOfCut = IsPointOnLineSegment(pointOfCut, edgeA) ? new LineSegment(pointOfCut, edgeB.start) : new LineSegment(edgeA.end, pointOfCut);
                }
                else if (areaOutside1 + leftTriangleArea + trapezoidArea >= singlePartArea) {
                    // produce cut in Trapezoid

                    var areaToCutAway = singlePartArea - (areaOutside1 + leftTriangleArea);
                    var fraction = areaToCutAway / trapezoidArea;

                    var trapezoidEdgeOnEdgeA = GetLineSegment(trapezoid, 0, true); // problem? this edge is reversed so it has the same direction as edgeB
                    var trapezoidEdgeOnEdgeB = GetLineSegment(trapezoid, 2);

                    var pointOfCutOnEdgeA = trapezoidEdgeOnEdgeA.PointAlong(fraction);
                    var pointOfCutOnEdgeB = trapezoidEdgeOnEdgeB.PointAlong(fraction);
                    lineOfCut = new LineSegment(pointOfCutOnEdgeA, pointOfCutOnEdgeB);

                }
                else if (areaOutside1 + GetTotalArea() >= singlePartArea) {
                    // produce cut in rightTriangle

                    var areaToCutAwayInTriangle = singlePartArea - (areaOutside1 + leftTriangleArea + trapezoidArea);
                    var fraction = areaToCutAwayInTriangle / rightTriangleArea;

                    var edgeWithPointOfCut = rightTriangleProjectedVertex.IsOnEdge(edgeA)
                        ? new LineSegment(rightTriangleProjectedVertex.vertex, edgeA.start)
                        : new LineSegment(rightTriangleProjectedVertex.vertex, edgeB.end);
                    var pointOfCut = edgeWithPointOfCut.PointAlong(fraction);
                    lineOfCut = IsPointOnLineSegment(pointOfCut, edgeA) ? new LineSegment(pointOfCut, edgeB.end) : new LineSegment(edgeA.start, pointOfCut);
                }

                if (lineOfCut != null && !IsIntersectingPolygon(lineOfCut, polygon) && IsLineInsidePolygon(lineOfCut, polygon)) {
                    // only consider cuts that do not intersect the exterior ring of the polygon
                    var cutAwayPolygon = SlicePolygon(polygon, lineOfCut.start, lineOfCut.end);
                    cuts.Add(new Cut(lineOfCut.GetLength(), cutAwayPolygon));
                }
            }

            // check another direction (areaOutside2 + T2 + Trapezoid + T1)
            if (areaOutside2 <= singlePartArea) {
                LineSegment lineOfCut = null;                       // line of cut goes from edgeB to edgeA

                if (areaOutside2 + rightTriangleArea > singlePartArea) {
                    // produce a Cut in rightTriangle
                    var areaToCutAwayInTriangle = singlePartArea - areaOutside2;
                    var fraction = areaToCutAwayInTriangle / rightTriangleArea;

                    var edgeWithPointOfCut = rightTriangleProjectedVertex.IsOnEdge(edgeA)
                        ? new LineSegment(edgeA.start, rightTriangleProjectedVertex.vertex)
                        : new LineSegment(edgeB.end, rightTriangleProjectedVertex.vertex);
                    var pointOfCut = edgeWithPointOfCut.PointAlong(fraction);
                    lineOfCut = IsPointOnLineSegment(pointOfCut, edgeA) ? new LineSegment(edgeB.end, pointOfCut) : new LineSegment(pointOfCut, edgeA.start);

                }
                else if (areaOutside2 + rightTriangleArea + trapezoidArea >= singlePartArea) {
                    // produce cut in Trapezoid

                    var areaToCutAway = singlePartArea - (areaOutside2 + rightTriangleArea);
                    var fraction = areaToCutAway / trapezoidArea;

                    var trapezoidEdgeOnEdgeA = GetLineSegment(trapezoid, 0);
                    var trapezoidEdgeOnEdgeB = GetLineSegment(trapezoid, 2, true);  // this edge is reversed so it has the same direction as edgeA

                    var pointOfCutOnEdgeA = trapezoidEdgeOnEdgeA.PointAlong(fraction);
                    var pointOfCutOnEdgeB = trapezoidEdgeOnEdgeB.PointAlong(fraction);
                    lineOfCut = new LineSegment(pointOfCutOnEdgeB, pointOfCutOnEdgeA);

                }
                else if (areaOutside2 + GetTotalArea() >= singlePartArea) {
                    // produce cut in leftTriangle

                    var areaToCutAwayInTriangle = singlePartArea - (areaOutside2 + rightTriangleArea + trapezoidArea);
                    var fraction = areaToCutAwayInTriangle / leftTriangleArea;

                    var edgeWithPointOfCut = leftTriangleProjectedVertex.IsOnEdge(edgeA)
                        ? new LineSegment(leftTriangleProjectedVertex.vertex, edgeA.end)
                        : new LineSegment(leftTriangleProjectedVertex.vertex, edgeB.start);
                    var pointOfCut = edgeWithPointOfCut.PointAlong(fraction);
                    lineOfCut = IsPointOnLineSegment(pointOfCut, edgeA) ? new LineSegment(edgeB.start, pointOfCut) : new LineSegment(pointOfCut, edgeA.end);
                }

                if (lineOfCut != null && !IsIntersectingPolygon(lineOfCut, polygon) && IsLineInsidePolygon(lineOfCut, polygon)) {
                    // only consider cuts that do not intersect the exterior ring of the polygon
                    var cutAwayPolygon = SlicePolygon(polygon, lineOfCut.start, lineOfCut.end);
                    if (cutAwayPolygon == null || cutAwayPolygon.points.Count > 3) {
                        cuts.Add(new Cut(lineOfCut.GetLength(), cutAwayPolygon));
                    }
                }
            }

            return cuts;
        }

    }
}
