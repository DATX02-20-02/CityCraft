using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//From the report "Efficient clipping of arbitrary polygons"
//Assumes there are no degeneracies (each vertex of one polygon does not lie on an edge of the other polygon)
namespace Habrador {
    public static class GreinerHormann {
        public static List<List<Vector2>> ClipPolygons(List<Vector2> polyVector, List<Vector2> clipPolyVector, BooleanOperation booleanOperation) {
            List<List<Vector2>> finalPoly = new List<List<Vector2>>();

            //Step 0. Create the data structure needed
            List<ClipVertex> poly = InitDataStructure(polyVector);

            List<ClipVertex> clipPoly = InitDataStructure(clipPolyVector);

            //Step 1. Find intersection points
            //Need to test if we have found an intersection point, if none is found, the polygons dont intersect, or one polygon is inside the other
            bool hasFoundIntersection = false;

            for (int i = 0; i < poly.Count; i++) {
                ClipVertex currentVertex = poly[i];

                //Important to use iPlusOne because poly.next may change
                int iPlusOne = MathUtility.ClampListIndex(i + 1, poly.Count);

                Vector2 a = poly[i].coordinate;

                Vector2 b = poly[iPlusOne].coordinate;

                for (int j = 0; j < clipPoly.Count; j++) {
                    int jPlusOne = MathUtility.ClampListIndex(j + 1, clipPoly.Count);

                    Vector2 c = clipPoly[j].coordinate;

                    Vector2 d = clipPoly[jPlusOne].coordinate;

                    //Are these lines intersecting?
                    if (Intersections.AreLinesIntersecting(a, b, c, d, true)) {
                        hasFoundIntersection = true;

                        Vector2 intersectionPoint2D = Intersections.GetLineLineIntersectionPoint(a, b, c, d);

                        //We need to insert this intersection vertex into both polygons
                        //Insert into the polygon
                        ClipVertex vertexOnPolygon = InsertIntersectionVertex(a, b, intersectionPoint2D, currentVertex);

                        //Insert into the clip polygon
                        ClipVertex vertexOnClipPolygon = InsertIntersectionVertex(c, d, intersectionPoint2D, clipPoly[j]);

                        //Also connect the intersection vertices with each other
                        vertexOnPolygon.neighbor = vertexOnClipPolygon;

                        vertexOnClipPolygon.neighbor = vertexOnPolygon;
                    }
                }
            }

            //If the polygons are intersecting
            if (hasFoundIntersection) {
                //Step 2. Trace each polygon and mark entry and exit points to the other polygon's interior
                MarkEntryExit(poly, clipPolyVector);

                MarkEntryExit(clipPoly, polyVector);

                //Step 3. Create the desired clipped polygon
                if (booleanOperation == BooleanOperation.Intersection) {
                    //Where the two polygons intersect
                    List<ClipVertex> intersectionVertices = GetClippedPolygon(poly, true);

                    AddPolygonToList(intersectionVertices, finalPoly, false);
                }
                else if (booleanOperation == BooleanOperation.Difference) {
                    //Whats outside of the polygon that doesnt intersect
                    List<ClipVertex> outsidePolyVertices = GetClippedPolygon(poly, false);

                    AddPolygonToList(outsidePolyVertices, finalPoly, true);
                }
                else if (booleanOperation == BooleanOperation.ExclusiveOr) {
                    //Whats outside of the polygon that doesnt intersect
                    List<ClipVertex> outsidePolyVertices = GetClippedPolygon(poly, false);

                    AddPolygonToList(outsidePolyVertices, finalPoly, true);

                    //Whats outside of the polygon that doesnt intersect
                    List<ClipVertex> outsideClipPolyVertices = GetClippedPolygon(clipPoly, false);

                    AddPolygonToList(outsideClipPolyVertices, finalPoly, true);
                }
                else if (booleanOperation == BooleanOperation.Union) {
                    //Where the two polygons intersect
                    List<ClipVertex> intersectionVertices = GetClippedPolygon(poly, true);

                    AddPolygonToList(intersectionVertices, finalPoly, false);

                    //Whats outside of the polygon that doesnt intersect
                    List<ClipVertex> outsidePolyVertices = GetClippedPolygon(poly, false);

                    AddPolygonToList(outsidePolyVertices, finalPoly, true);

                    //Whats outside of the polygon that doesnt intersect
                    List<ClipVertex> outsideClipPolyVertices = GetClippedPolygon(clipPoly, false);

                    AddPolygonToList(outsideClipPolyVertices, finalPoly, true);
                }
            }
            //Check if one polygon is inside the other
            else {
                //Is the polygon inside the clip polygon?
                //Depending on the type of boolean operation, we might get a hole
                if (IsPolygonInsidePolygon(polyVector, clipPolyVector)) {
                    Debug.Log("Poly is inside clip poly");
                }
                else if (IsPolygonInsidePolygon(clipPolyVector, polyVector)) {
                    Debug.Log("Clip poly is inside poly");
                }
                else {
                    Debug.Log("Polygons are not intersecting");
                }
            }

            return finalPoly;
        }

        //We may end up with several polygons, so this will split the connected list into one list per polygon
        private static void AddPolygonToList(List<ClipVertex> verticesToAdd, List<List<Vector2>> finalPoly, bool shouldReverse) {
            List<Vector2> thisPolyList = new List<Vector2>();

            finalPoly.Add(thisPolyList);

            for (int i = 0; i < verticesToAdd.Count; i++) {
                ClipVertex v = verticesToAdd[i];

                thisPolyList.Add(v.coordinate);

                //Have we found a new polygon?
                if (v.nextPoly != null) {
                    //If we are finding the !intersection, the vertices will be clockwise
                    //so we should reverse the list to make it easier to triangulate
                    if (shouldReverse) {
                        thisPolyList.Reverse();
                    }

                    thisPolyList = new List<Vector2>();

                    finalPoly.Add(thisPolyList);
                }
            }

            //Reverse the last list added
            if (shouldReverse) {
                finalPoly[finalPoly.Count - 1].Reverse();
            }
        }

        //Get the clipped polygons: either the intersection or the !intersection
        //We might end up with more than one polygon and they are connected via clipvertex nextpoly
        private static List<ClipVertex> GetClippedPolygon(List<ClipVertex> poly, bool getIntersectionPolygon) {
            List<ClipVertex> finalPolygon = new List<ClipVertex>();

            //First we have to reset in case we are repeating this method several times depending on the type of boolean operation
            ResetVertices(poly);

            //Find the first intersection point which is always where we start
            ClipVertex thisVertex = FindFirstEntryVertex(poly);

            //Save this so we know when to stop the algortihm
            ClipVertex firstVertex = thisVertex;

            finalPolygon.Add(thisVertex);

            thisVertex.isTakenByFinalPolygon = true;
            thisVertex.neighbor.isTakenByFinalPolygon = true;

            //These rows is the only part thats different if we want to get the intersection or the !intersection
            //Are needed once again if there are more than one polygon
            bool isMovingForward = getIntersectionPolygon ? true : false;

            thisVertex = getIntersectionPolygon ? thisVertex.next : thisVertex.prev;

            int safety = 0;

            while (true) {
                //This means we are back at the first vertex of this polygon
                if (thisVertex.Equals(firstVertex) || (thisVertex.neighbor != null && thisVertex.neighbor.Equals(firstVertex))) {
                    //Try to find the next intersection point in case we end up with more than one polygon
                    ClipVertex nextVertex = FindFirstEntryVertex(poly);

                    //Stop if we are out of intersection vertices
                    if (nextVertex == null) {
                        //Debug.Log("No more polygons can be found");

                        break;
                    }
                    //Find an entry vertex and start over with another polygon
                    else {
                        //Debug.Log("Find another polygon");

                        //Need to connect the polygons
                        finalPolygon[finalPolygon.Count - 1].nextPoly = nextVertex;

                        //Change to a new polygon
                        thisVertex = nextVertex;

                        firstVertex = nextVertex;

                        finalPolygon.Add(thisVertex);

                        thisVertex.isTakenByFinalPolygon = true;
                        thisVertex.neighbor.isTakenByFinalPolygon = true;

                        //Do we want to get the intersection or the !intersection
                        isMovingForward = getIntersectionPolygon ? true : false;

                        thisVertex = getIntersectionPolygon ? thisVertex.next : thisVertex.prev;
                    }
                }

                //If this is not an intersection, then just add it
                if (!thisVertex.isIntersection) {
                    finalPolygon.Add(thisVertex);

                    //And move in the direction we are moving
                    thisVertex = isMovingForward ? thisVertex.next : thisVertex.prev;
                }
                else {
                    thisVertex.isTakenByFinalPolygon = true;
                    thisVertex.neighbor.isTakenByFinalPolygon = true;

                    //Jump to the other polygon
                    thisVertex = thisVertex.neighbor;

                    finalPolygon.Add(thisVertex);

                    //Move forward/ back depending on if this is an entry / exit vertex and if we want to find the intersection or not
                    if (getIntersectionPolygon) {
                        isMovingForward = thisVertex.isEntry ? true : false;

                        thisVertex = thisVertex.isEntry ? thisVertex.next : thisVertex.prev;
                    }
                    else {
                        isMovingForward = !isMovingForward;

                        thisVertex = isMovingForward ? thisVertex.next : thisVertex.prev;
                    }
                }

                safety += 1;

                if (safety > 100000) {
                    Debug.Log("Endless loop when creating clipped polygon");

                    break;
                }
            }

            return finalPolygon;
        }

        //Reset vertices before we find the final polygon(s)
        private static void ResetVertices(List<ClipVertex> poly) {
            ClipVertex resetVertex = poly[0];

            int safety = 0;

            while (true) {
                //Reset
                resetVertex.isTakenByFinalPolygon = false;
                resetVertex.nextPoly = null;

                //Dont forget to reset the neighbor
                if (resetVertex.isIntersection) {
                    resetVertex.neighbor.isTakenByFinalPolygon = false;
                }

                resetVertex = resetVertex.next;

                //All vertices are reset
                if (resetVertex.Equals(poly[0])) {
                    break;
                }

                safety += 1;

                if (safety > 100000) {
                    Debug.Log("Endless loop in reset vertices");

                    break;
                }
            }
        }

        //Is a polygon One inside polygon Two?
        private static bool IsPolygonInsidePolygon(List<Vector2> polyOne, List<Vector2> polyTwo) {
            bool isInside = false;

            for (int i = 0; i < polyOne.Count; i++) {
                if (Intersections.IsPointInPolygon(polyTwo, polyOne[i])) {
                    //Is inside if at least one point is inside the polygon (in this case because we run this method after we have tested
                    //if the polygons are intersecting)
                    isInside = true;

                    break;
                }
            }

            return isInside;
        }

        //Find the the first entry vertex in a polygon
        private static ClipVertex FindFirstEntryVertex(List<ClipVertex> poly) {
            ClipVertex thisVertex = poly[0];

            ClipVertex firstVertex = thisVertex;

            int safety = 0;

            while (true) {
                //Is this an available entry vertex?
                if (thisVertex.isIntersection && thisVertex.isEntry && !thisVertex.isTakenByFinalPolygon) {
                    //We have found the first entry vertex
                    break;
                }

                thisVertex = thisVertex.next;

                //We have travelled the entire polygon without finding an available entry vertex
                if (thisVertex.Equals(firstVertex)) {
                    thisVertex = null;

                    break;
                }

                safety += 1;

                if (safety > 100000) {
                    Debug.Log("Endless loop in find first entry vertex");

                    break;
                }
            }

            return thisVertex;
        }

        //Create the data structure needed
        private static List<ClipVertex> InitDataStructure(List<Vector2> polyVector) {
            List<ClipVertex> poly = new List<ClipVertex>();

            for (int i = 0; i < polyVector.Count; i++) {
                poly.Add(new ClipVertex(polyVector[i]));
            }

            //Connect the vertices
            for (int i = 0; i < poly.Count; i++) {
                int iPlusOne = MathUtility.ClampListIndex(i + 1, poly.Count);
                int iMinusOne = MathUtility.ClampListIndex(i - 1, poly.Count);

                poly[i].next = poly[iPlusOne];
                poly[i].prev = poly[iMinusOne];
            }

            return poly;
        }

        //Insert intersection vertex at correct position in the list
        private static ClipVertex InsertIntersectionVertex(Vector2 a, Vector2 b, Vector2 intersectionPoint, ClipVertex currentVertex) {
            //Calculate alpha which is how far the intersection coordinate is between a and b
            //so we can insert this vertex at the correct position
            //pos = start + dir * alpha
            float alpha = (a - intersectionPoint).sqrMagnitude / (a - b).sqrMagnitude;

            //Create a new vertex
            ClipVertex intersectionVertex = new ClipVertex(intersectionPoint);

            intersectionVertex.isIntersection = true;
            intersectionVertex.alpha = alpha;

            //Now we need to insert this intersection point somewhere after currentVertex
            ClipVertex insertAfterThisVertex = currentVertex;

            int safety = 0;

            while (true) {
                //If the next vertex is an intersectionvertex with a higher alpha
                //or if the next vertex is not an intersectionvertex, we cant improve, so break
                if (insertAfterThisVertex.next.alpha > alpha || !insertAfterThisVertex.next.isIntersection) {
                    break;
                }

                insertAfterThisVertex = insertAfterThisVertex.next;

                safety += 1;

                if (safety > 100000) {
                    Debug.Log("Stuck in loop in insert intersection vertices");

                    break;
                }
            }

            //Connect the vertex to the surrounding vertices
            intersectionVertex.next = insertAfterThisVertex.next;

            intersectionVertex.prev = insertAfterThisVertex;

            insertAfterThisVertex.next.prev = intersectionVertex;

            insertAfterThisVertex.next = intersectionVertex;

            return intersectionVertex;
        }

        //Mark entry exit points
        private static void MarkEntryExit(List<ClipVertex> poly, List<Vector2> clipPolyVector) {
            //First see if the first vertex starts inside or outside (we can use the original list)
            bool isInside = Intersections.IsPointInPolygon(clipPolyVector, poly[0].coordinate);

            ClipVertex currentVertex = poly[0];

            ClipVertex firstVertex = currentVertex;

            int safety = 0;

            while (true) {
                if (currentVertex.isIntersection) {
                    //If we were outside, this is an entry
                    currentVertex.isEntry = isInside ? false : true;

                    //Now we know we are either inside or outside
                    isInside = !isInside;
                }

                currentVertex = currentVertex.next;

                //We have travelled around the entire polygon
                if (currentVertex.Equals(firstVertex)) {
                    break;
                }

                safety += 1;

                if (safety > 100000) {
                    Debug.Log("Endless loop in mark entry exit");

                    break;
                }
            }
        }
    }
}
