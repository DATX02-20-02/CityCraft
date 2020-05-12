using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
namespace Habrador {
    public class Intersections {
        public static bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints) {
            bool isIntersecting = false;

            float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

            //Make sure the denominator is > 0, if not the lines are parallel
            if (denominator != 0f) {
                float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
                float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

                //Are the line segments intersecting if the end points are the same
                if (shouldIncludeEndPoints) {
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                    if (u_a >= 0f && u_a <= 1f && u_b >= 0f && u_b <= 1f) {
                        isIntersecting = true;
                    }
                }
                else {
                    //Is intersecting if u_a and u_b are between 0 and 1
                    if (u_a > 0f && u_a < 1f && u_b > 0f && u_b < 1f) {
                        isIntersecting = true;
                    }
                }
            }

            return isIntersecting;
        }

        // Whats the coordinate of an intersection point between two lines in 2d space if we know they are intersecting
        // http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
        public static Vector2 GetLineLineIntersectionPoint(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2) {
            float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

            float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

            Vector2 intersectionPoint = l1_p1 + u_a * (l1_p2 - l1_p1);

            return intersectionPoint;
        }

        // The list describing the polygon has to be sorted either clockwise or counter-clockwise because we have to identify its edges
        public static bool IsPointInPolygon(List<Vector2> polygonPoints, Vector2 point) {
            //Step 1. Find a point outside of the polygon
            //Pick a point with a x position larger than the polygons max x position, which is always outside
            Vector2 maxXPosVertex = polygonPoints[0];

            for (int i = 1; i < polygonPoints.Count; i++) {
                if (polygonPoints[i].x > maxXPosVertex.x) {
                    maxXPosVertex = polygonPoints[i];
                }
            }

            //The point should be outside so just pick a number to make it outside
            Vector2 pointOutside = maxXPosVertex + new Vector2(10f, 0f);

            //Step 2. Create an edge between the point we want to test with the point thats outside
            Vector2 l1_p1 = point;
            Vector2 l1_p2 = pointOutside;

            //Step 3. Find out how many edges of the polygon this edge is intersecting
            int numberOfIntersections = 0;

            for (int i = 0; i < polygonPoints.Count; i++) {
                //Line 2
                Vector2 l2_p1 = polygonPoints[i];

                int iPlusOne = MathUtility.ClampListIndex(i + 1, polygonPoints.Count);

                Vector2 l2_p2 = polygonPoints[iPlusOne];

                //Are the lines intersecting?
                if (AreLinesIntersecting(l1_p1, l1_p2, l2_p1, l2_p2, true)) {
                    numberOfIntersections += 1;
                }
            }

            //Step 4. Is the point inside or outside?
            bool isInside = true;

            //The point is outside the polygon if number of intersections is even or 0
            if (numberOfIntersections == 0 || numberOfIntersections % 2 == 0) {
                isInside = false;
            }

            return isInside;
        }
    }
}
