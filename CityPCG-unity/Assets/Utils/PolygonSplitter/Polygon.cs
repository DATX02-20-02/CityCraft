using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Utils.PolygonSplitter.PolygonUtils;

namespace Utils.PolygonSplitter
{
    [Serializable]
    public class Polygon
    {
        public List<Vector3> points;
        public Polygon(List<Vector3> points)
        {
            this.points = points;
        }

        public float GetArea()
        {
            var result = Vector3.zero;
            for(int p = points.Count-1, q = 0; q < points.Count; p = q++) {
                result += Vector3.Cross(points[q], points[p]);
            }
            result *= 0.5f;
            
            return result.magnitude;
            //return GeometryUtility.CalculateBounds(points, Matrix4x4.identity);
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("[" + GetArea() + "] ");

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                sb.Append(point);

                if (i + 1 < points.Count)
                {
                    sb.Append(" ---> ");
                }
            }
            return sb.ToString();
        }

        //https://stackoverflow.com/a/4833823
        public bool Contains(Polygon polygon)
        {
            var p1Segments = GetLineSegments(this);
            var p2Segments = GetLineSegments(polygon);

            foreach (var ls1 in p1Segments)
            {
                foreach (var ls2 in p2Segments)
                {
                    if (LineLineIntersection(ls1, ls2))
                    {
                        return false;
                    }
                }
            }

            foreach (var line in p2Segments)
            {
                if (Contains(line.end))
                {
                    return true;
                }
            }

            return false;

        }
        
        public bool Contains(Vector3 p)
        {
            foreach (var point in points)
            {
                if (point == p)
                {
                    return true;
                }
            }
            
            var j = points.Count - 1;
            var inside = false;
            for (var i = 0; i < points.Count; j = i++)
            {
                var pi = points[i];
                var pj = points[j];
                if (((pi.z <= p.z && p.z < pj.z) || (pj.z <= p.z && p.z < pi.z)) &&
                    (p.x < (pj.x - pi.x) * (p.z - pi.z) / (pj.z - pi.z) + pi.x))
                    inside = !inside;
            }
            return inside;
        }
        
        //TODO: Optimize
        public Polygon Difference(Polygon otherPolygon)
        {
            var vertices = new List<Vector3>();

            var segmentsA = GetLineSegments(this);
            var segmentsB = GetLineSegments(otherPolygon);
            foreach (var segA in segmentsA)
            {
                LineSegment foundSegB = null;
                var found = 0;
                foreach (var segB in segmentsB)
                {
                    if (segA.EqualsTopo(segB))
                    {
                        found = 0;
                        foundSegB = null;
                        break;
                    }

                    if (found == 0 && IsPointOnLineSegmentExcludingEndpoints(segB.start, segA) || IsPointOnLineSegmentExcludingEndpoints(segB.end, segA) && segA.EqualsOneTopo(segB))
                    {
                        found = 1;
                        foundSegB = segB;
                        continue;
                    }

                    if (found == 0 && segA.EqualsOneTopo(segB))
                    {
                        found = 2;
                        foundSegB = segB;
                    }
                }

                if (found == 1 && foundSegB != null)
                {
                    var segB = foundSegB;
                    if (segA.start.Equals(segB.start))
                    {
                        vertices.Add(segB.end);
                        vertices.Add(segA.end);
                    }

                    else if (segA.start.Equals(segB.end))
                    {
                        vertices.Add(segB.start);
                        vertices.Add(segA.end);
                    }

                    else if (segA.end.Equals(segB.start))
                    {
                        vertices.Add(segA.start);
                        vertices.Add(segB.end);
                    }

                    else if (segA.end.Equals(segB.end))
                    {
                        vertices.Add(segA.start);
                        vertices.Add(segB.start);
                    }

                }

                else if (found == 2)
                {
                    vertices.Add(segA.start);
                }
            }
            
            return CreatePolygon(vertices);	
        }
        
    }
}
