using System.Collections.Generic;
using UnityEngine;
using static Utils.PolygonSplitter.PolygonUtils;

namespace Utils.PolygonSplitter
{
    public static class PolygonSplitter
    {

        public static List<Polygon> Split(Polygon originalPolygon, int parts)
        {
            var singlePartArea = originalPolygon.GetArea() / parts;
            
            var polygonParts = new List<Polygon>(parts);
            var remainingPoly = originalPolygon;
            for (var i = 0; i < parts - 1; i++)
            {
                remainingPoly = Split(remainingPoly, polygonParts, singlePartArea);
            }
            polygonParts.Add(remainingPoly);
            
            return polygonParts;
        }

        private static Polygon Split(Polygon polygon, List<Polygon> resultList, float singlePartArea)
        {
            var segments = GetLineSegments(polygon);
            if (segments.Count < 3)
            {
                Debug.Log(polygon);
                foreach (var segment in segments)
                {
                    Debug.Log(segment);
                }
                return polygon;
            }
            
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

            Cut shortestCut = null;
            if (possibleCuts.Count == 0)
            {
                var edgeA = segments[0];
                var edgeB = segments[2];
                var edgePair = new EdgePair(edgeA, edgeB);
                var subPolygons = edgePair.GetSubPolygons();
                var cuts = subPolygons.GetCuts(polygon, singlePartArea);

                if (cuts.Count == 0)
                {
                    return polygon;
                }
                else
                {
                    shortestCut = cuts[0];
                }
            }
            else
            {
                Debug.Log("Possible cuts:");

                shortestCut = possibleCuts[0];
                for (var i = 1; i < possibleCuts.Count; i++)
                {
                    Debug.Log(possibleCuts[i]);
                    if (possibleCuts[i].length < shortestCut.length && possibleCuts[i].cutAway != null && polygon.Contains(possibleCuts[i].cutAway))
                    {
                        shortestCut = possibleCuts[i];
                    }
                }
            }
            
            resultList.Add(shortestCut.cutAway);
            return polygon.Difference(shortestCut.cutAway);
        }

    }
}
