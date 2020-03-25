using System.Collections.Generic;
using UnityEngine;
using static Utils.PolygonSplitter.PolygonUtils;

namespace Utils.PolygonSplitter {
    public static class PolygonSplitter {

        public static List<Polygon> Split(Polygon originalPolygon, int parts) {
            var singlePartArea = originalPolygon.GetArea() / parts;

            var polygonParts = new List<Polygon>(parts);
            var remainingPoly = originalPolygon;
            for (var i = 0; i < parts - 1; i++) {
                remainingPoly = Split(remainingPoly, polygonParts, singlePartArea);
            }
            polygonParts.Add(remainingPoly);

            return polygonParts;
        }

        //TODO: Add a random element in selecting splits
        private static Polygon Split(Polygon polygon, List<Polygon> resultList, float singlePartArea) {
            var segments = GetLineSegments(polygon);

            var possibleCuts = new List<Cut>();

            // for each unique edge pair
            for (var i = 0; i < segments.Count - 2; i++) {

                // generate unique edge pairs (e.g. 2 pairs for any rectangle)
                for (var j = i + 2; j < segments.Count; j++) {
                    var segmentsCovered = j - i + 1;            // number of segments covered by a LineRing starting with edgeA and ending with edgeB (including)
                    if (segments.Count == segmentsCovered) {
                        break;
                    }

                    var edgeA = segments[i];
                    var edgeB = segments[j];
                    var edgePair = new EdgePair(edgeA, edgeB);
                    var subPolygons = edgePair.GetSubPolygons();
                    var cutForCurrentEdgePair = subPolygons.GetCuts(polygon, singlePartArea);
                    possibleCuts.AddRange(cutForCurrentEdgePair);
                }
            }

            //TODO This shouldn't really never happen. Debug why.
            if (possibleCuts.Count == 0) {
                return polygon;
            }

            var shortestCut = possibleCuts[0];
            for (var i = 1; i < possibleCuts.Count; i++) {
                if (possibleCuts[i].length < shortestCut.length && possibleCuts[i].cutAway != null && polygon.Contains(possibleCuts[i].cutAway)) {
                    shortestCut = possibleCuts[i];
                }
            }

            if(shortestCut.cutAway == null) {
                Debug.Log(polygon);
                Debug.Log("Cutout is null");
            }

            resultList.Add(shortestCut.cutAway);
            return polygon.Difference(shortestCut.cutAway);
        }

    }
}
