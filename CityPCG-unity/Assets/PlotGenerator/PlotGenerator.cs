using System.Collections.Generic;
using UnityEngine;
using Utils.PolygonSplitter;
using static Utils.PolygonSplitter.PolygonSplitter;
using static Utils.PolygonSplitter.PolygonUtils;

namespace PlotGenerator
{
    public class PlotGenerator : MonoBehaviour
    {

        public Polygon polygon;
        public int parts;
    
        private List<Polygon> polygons;
        private List<Color> colors;
        
        void Start()
        {    
            polygon = CreatePolygon(polygon.points);
            polygons = Split(polygon, parts);
            colors = new List<Color>();
            
            if (polygons != null)
                for (var index = 0; index < polygons.Count; index++)
                {
                    colors.Add(new Color(
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f)
                    ));
                }
        }
    
        void Update()
        {
            if (polygons != null)
            {
                for (var index = 0; index < polygons.Count; index++)
                {
                    var p = polygons[index];
                    if (p == null)    
                    {
                        Debug.Log("A polygon is null");
                    }
                    else
                    {
                        DrawPolygon(p, colors[index]);
                    }
                }
            }
        }

        private void DrawPolygon(Polygon p, Color c, int d = 1000000, Vector3 p2 = default)
        {
            for (int i = 0; i < p.points.Count; i++)
            {
                var position = transform.position + p2;
                
                var cur = p.points[i] + position;
                var next = p.points[(i + 1) % p.points.Count] + position;

                Debug.DrawLine(cur, next, c, d);
            }
        }

        private void DrawEdge(LineSegment line, Color c)
        {    
            DrawEdge(line.start, line.end, c);
        }
        
        private void DrawEdge(Vector3 start, Vector3 end, Color c)
        {
            var d = 1000000;
            
            Debug.DrawLine(start, end, c, d);
        }

        public static void DrawCircle(Vector3 pos, Color color, float radius = 0.1f, float fidelity = 10)
        {
            var d = 1000000;

            var step = 2 * Mathf.PI / fidelity;
            for (var i = 0; i < fidelity; i++)
            {
                var x = Mathf.Sin(step * i) * radius;
                var z = Mathf.Cos(step * i) * radius;

                var nx = Mathf.Sin(step * (i + 1)) * radius;
                var nz = Mathf.Cos(step * (i + 1)) * radius;

                Debug.DrawLine(pos + new Vector3(x, 0, z), pos + new Vector3(nx, 0, nz), color, d);
            }
        }
    }
}

