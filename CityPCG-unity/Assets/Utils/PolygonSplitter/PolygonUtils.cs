using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Utils.PolygonSplitter
{
    public class PolygonUtils 
    {
        public static Polygon CreateTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            return new Polygon(new List<Vector3>() {v1, v2, v3, v1});
        }

        /**
         * Basically adds the first point again to the end to create a loop. 
         */
        public static Polygon CreatePolygon(List<Vector3> vertices)
        {
            if (vertices.Count <= 2)
            {
                return null;
            }
            var copy = new List<Vector3>(vertices) {vertices[0]};
            return new Polygon(copy);
        }


        public static Polygon GetSubPolygon(Polygon polygonToSplit, Vector3 startVertex, Vector3 endVertex) 
        {
            //TODO:
            // Check if startVertex and endVertex actually exists inside of polygonToSplit
            
            var vertices = new List<Vector3>();
            var started = false;

            for (var i = 0; i < polygonToSplit.points.Count; i++)
            {
                var vertex = polygonToSplit.points[i];
                if (vertex.Equals(startVertex))
                {
                    started = true;
                }

                if (started)
                {
                    vertices.Add(vertex);

                    if (vertex.Equals(endVertex))
                    {
                        break;
                    }
                }

                if (i == polygonToSplit.points.Count - 1)
                {
                    i = -1;
                }
            }

            return CreatePolygon(vertices);
        }

        public static LineSegment GetLineSegment(Polygon polygon, int index, bool reversed = false) {
            var segment = new LineSegment(polygon.points[index], polygon.points[index + 1]);
            return reversed ? new LineSegment(segment.end, segment.start) : segment;
        }

        public static List<LineSegment> GetLineSegments(Polygon polygon) {
            var lineSegments = new List<LineSegment>();

            var start = polygon.points[0];
            for (var i = 1; i < polygon.points.Count; i++) {
                var end = polygon.points[i];
                lineSegments.Add(new LineSegment(start, end));
                start = end;
            }
            return lineSegments;
        }
        
        // Distance to point (p) from line segment (end points a b)
        public static float DistanceLineSegmentPoint(Vector3 p, LineSegment line)
        {
            if (line.start == line.end)
                return Vector3.Distance(line.start, p);
     
            // Line segment to point distance equation
            var ba = line.end - line.start;
            var pa = line.start - p;
            return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
        }

        public static bool IsPointOnLineSegment(Vector3 p, LineSegment line)
        {
            return DistanceLineSegmentPoint(p, line) < 0.01;
        }
        
        public static bool IsPointOnLineSegmentExcludingEndpoints(Vector3 point, LineSegment line) {
            if (point.Equals(line.start) || point.Equals(line.end)) {
                return false;
            }
            return IsPointOnLineSegment(point, line);
        }
        
        public static Polygon SlicePolygon(Polygon polygonToSlice, Vector3 startPoint, Vector3 endPoint) {
            var vertices = new List<Vector3>();

            var started = false;
            var finished = false;

            var edges = GetLineSegments(polygonToSlice);
            foreach (var edge in edges) {
                if (!started && IsPointOnLineSegment(startPoint, edge) && !startPoint.Equals(edge.end)) {
                    // if startPoint is on the edge, start building up the sliced part
                    // if it is the endpoint, it will be considered as part of the next edge
                    vertices.Add(startPoint);
                    started = true;
                    continue;
                }

                if (started) {
                    vertices.Add(edge.start);

                    if (IsPointOnLineSegment(endPoint, edge)) {
                        vertices.Add(endPoint);
                        finished = true;
                        break;
                    }
                }
            }

            if (started && !finished) {
                // polygon runs through the first point - continue until endPoint is reached

                foreach (var edge in edges) {
                    vertices.Add(edge.start);
                    if (IsPointOnLineSegment(endPoint, edge)) {
                        vertices.Add(endPoint);
                        finished = true;
                        break;
                    }
                }
            }

            return CreatePolygon(vertices);
        }

        public static bool LineLineIntersection(LineSegment lineA, LineSegment lineB)
        {
            var p1 = lineA.start;
            var p2 = lineA.end;
            var p3 = lineB.start;
            var p4 = lineB.end;
            
            Vector2 a = p2 - p1;
            Vector2 b = p3 - p4;
            Vector2 c = p1 - p3;
   
            var alphaNumerator = b.y*c.x - b.x*c.y;
            var alphaDenominator = a.y*b.x - a.x*b.y;
            var betaNumerator  = a.x*c.y - a.y*c.x;
            var betaDenominator  = a.y*b.x - a.x*b.y;
   
            var doIntersect = true;
   
            if (Math.Abs(alphaDenominator) < float.Epsilon || Math.Abs(betaDenominator) < float.Epsilon) {
                doIntersect = false;
            } else {
       
                if (alphaDenominator > 0) {
                    if (alphaNumerator < 0 || alphaNumerator > alphaDenominator) {
                        doIntersect = false;
               
                    }
                } else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator) {
                    doIntersect = false;
                }
       
                if (doIntersect && betaDenominator > 0) {
                    if (betaNumerator < 0 || betaNumerator > betaDenominator) {
                        doIntersect = false;
                    }
                } else if (betaNumerator > 0 || betaNumerator < betaDenominator) {
                    doIntersect = false;
                }
            }
 
            return doIntersect;
        }
        
        public static IntersectionPosition _GetIntersectionPoint(LineSegment lineA, LineSegment lineB)
        {
            var p1 = lineA.start;
            var p2 = lineA.end;
            var p3 = lineB.start;
            var p4 = lineB.end;
            
            var tmp = (p4.x - p3.x) * (p2.z - p1.z) - (p4.z - p3.z) * (p2.x - p1.x);
 
            if (Math.Abs(tmp) < float.Epsilon)
            {
                return new IntersectionPosition(Vector3.zero, lineA, lineB);
            }
 
            var mu = ((p1.x - p3.x) * (p2.z - p1.z) - (p1.z - p3.z) * (p2.x - p1.x)) / tmp;
            
            return new IntersectionPosition(new Vector3(p3.x + (p4.x - p3.x) * mu, 0, p3.z + (p4.z - p3.z) * mu), lineA, lineB);
        }

        //This function finds out on which side of a line segment the point is located.
        //The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
        //the line segment, project it on the line using ProjectPointOnLine() first.
        //Returns 0 if point is on the line segment.
        //Returns 1 if point is outside of the line segment and located on the side of linePoint1.
        //Returns 2 if point is outside of the line segment and located on the side of linePoint2.
        public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point){
 
            Vector3 lineVec = linePoint2 - linePoint1;
            Vector3 pointVec = point - linePoint1;
 
            float dot = Vector3.Dot(pointVec, lineVec);
 
            //point is on side of linePoint2, compared to linePoint1
            if(dot > 0){
 
                //point is on the line segment
                if(pointVec.magnitude <= lineVec.magnitude){
 
                    return 0;
                }
 
                //point is not on the line segment and it is on the side of linePoint2
                else{
 
                    return 2;
                }
            }
 
            //Point is not on side of linePoint2, compared to linePoint1.
            //Point is not on the line segment and it is on the side of linePoint1.
            else{
 
                return 1;
            }
        }

        //This function returns a point which is a projection from a point to a line.
        //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
        public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point){		
 
            //get vector from point on line to point in space
            Vector3 linePointToPoint = point - linePoint;
 
            float t = Vector3.Dot(linePointToPoint, lineVec);
 
            return linePoint + lineVec * t;
        }
        
        //This function returns a point which is a projection from a point to a line segment.
        //If the projected point lies outside of the line segment, the projected point will 
        //be clamped to the appropriate line edge.
        //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
        public static Vector3 GetProjectedPoint(LineSegment edge, Vector3 point, IntersectionPosition intersectionPosition){
            var vector = edge.end - edge.start;
 
            var projectedPoint = ProjectPointOnLine(edge.start, vector.normalized, point);
 
            var side = PointOnWhichSideOfLineSegment(edge.start, edge.end, projectedPoint);

            switch (side)
            {
                //The projected point is on the line segment
                case 0:
                    return projectedPoint;
                case 1:
                    return edge.start;
                case 2:
                    return edge.end;
                default:
                    //output is invalid
                    return Vector3.zero;
            }
        }	
        
        /**
         * Determines a projection of vertex on opposing edge at an angle perpendicular to angle-bisector of the edges
         * @param vertex
         * @param opposingEdge
         * @param intersectionPoint
         * @return
         */
        /*public static Vector3 GetProjectedPoint(Vector3 position, LineSegment opposingEdge, IntersectionPosition intersectionPoint) {
            if (intersectionPoint != null) {

                //todo: add https://github.com/grimsa/polysplit/blob/master/src/main/java/de/incentergy/geometry/utils/GeometryUtils.java#L91
                if (intersectionPoint.belongsToOneOfTheEdges)
                {
                    Debug.Log("Nope");
                    return Vector3.zero;
                }

                // usual case - when intersection point is somewhere further on the line covering opposing edge
                // Note: projection perpendicular to the angle bisector will be located an equal distance from intersection point

                var distanceOfVertex = Vector3.Distance(position, intersectionPoint.vector);

                // check if the point falls on the edge. I.e. distance from intersection must be between distances of start and end points
                var distOfOpEdgeVertex1 = Vector3.Distance(intersectionPoint.vector, opposingEdge.start);
                var distOfOpEdgeVertex2 = Vector3.Distance(intersectionPoint.vector, opposingEdge.end);

                if (distanceOfVertex >= Math.Max(distOfOpEdgeVertex1, distOfOpEdgeVertex2) || distanceOfVertex <= Math.Min(distOfOpEdgeVertex1, distOfOpEdgeVertex2)) {
                    // the projection falls outside of the opposing edge - ignore it
                    // This also covers cases when projected point matches the vertex
                    Debug.Log("Check if this needs to return null");
                    return Vector3.zero;
                }

                // determine a point along the opposing edge for which distance from intersection point is equal to that of vertex being projected
                var furtherPoint = GetFurtherEnd(intersectionPoint.vector, opposingEdge);
                var extendedOpposingEdge = new LineSegment(intersectionPoint.vector, furtherPoint);
                return extendedOpposingEdge.PointAlong(distanceOfVertex / extendedOpposingEdge.GetLength());
            } else {
                // In case of parallel lines, we do not have an intersection point
                var closestPointOnOpposingLine = opposingEdge.project(position);       // a projection onto opposingEdge (extending to infinity)
                return IsPointOnLineSegmentExcludingEndpoints(closestPointOnOpposingLine, opposingEdge) ? closestPointOnOpposingLine : Vector3.zero;
            }
        }*/
        
        private static Vector3 GetFurtherEnd(Vector3 point, LineSegment lineSegment) {
            return Vector3.Distance(point, lineSegment.start) > Vector3.Distance(point, lineSegment.end) ? lineSegment.start : lineSegment.end;
        }

        public static bool IsIntersectingPolygon(LineSegment line, Polygon polygon)
        {
            var polygonSegments = GetLineSegments(polygon);
            foreach (var segment in polygonSegments)
            {
                if (GetIntersectionPoint(segment, line) != null)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static IntersectionPosition GetIntersectionPoint(LineSegment lineA, LineSegment lineB) {
            var x1 = lineA.start.x;
            var z1 = lineA.start.z;
            var x2 = lineA.end.x;
            var z2 = lineA.end.z;

            var x3 = lineB.start.x;
            var z3 = lineB.start.z;
            var x4 = lineB.end.x;
            var z4 = lineB.end.z;

            var det1And2 = Det(x1, z1, x2, z2);
            var det3And4 = Det(x3, z3, x4, z4);
            var x1LessX2 = x1 - x2;
            var z1LessZ2 = z1 - z2;
            var x3LessX4 = x3 - x4;
            var z3LessZ4 = z3 - z4;

            var det1Less2And3Less4 = Det(x1LessX2, z1LessZ2, x3LessX4, z3LessZ4);
            if (Math.Abs(det1Less2And3Less4) < float.Epsilon) {
                return null;
            }

            var x = Det(det1And2, x1LessX2, det3And4, x3LessX4) / det1Less2And3Less4;
            var z = Det(det1And2, z1LessZ2, det3And4, z3LessZ4) / det1Less2And3Less4;
            return new IntersectionPosition(new Vector3(x, 0, z), lineA, lineB);
        }
        
        private static float Det(float a, float b, float c, float d) {
            return a * d - b * c;
        }

        public static bool IsPolygonIntersectingPolygon(Polygon small, Polygon big)
        {
            foreach (var smallPoint in small.points)
            {
                var found = false;
                foreach (var bigLS in GetLineSegments(big))
                {
                    if (IsPointOnLineSegment(smallPoint, bigLS))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }
        
    }
}