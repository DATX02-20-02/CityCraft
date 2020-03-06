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
        public Vector3 test;

        private Polygon subPolygon;
        private Polygon subPolygon2;

        private Polygon smallerPolygon;
        private List<Polygon> polygons;
        private List<Color> colors;
        private Polygon differencePolygon;

        void Start()
        {
            // smallerPolygon = CreatePolygon(new List<Vector3>() {new Vector3(1, 0, 1), new Vector3(2, 0, 2), new Vector3(2, 0, 1)});

            //subPolygon = GetSubPolygon(polygon, polygon.points[2], polygon.points[0]);
            polygon = CreatePolygon(polygon.points);

          polygons = Split(polygon, parts);
          // differencePolygon = polygon.Difference(subPolygon);
            //Debug.Log(differencePolygon);

//            subPolygon2 = SlicePolygon(polygon, new Vector3(-5, 0, 0), new Vector3(5, 0, 0));

           // Debug.Log(polygon.Contains(smallerPolygon));
           // Debug.Log(polygon.Contains(subPolygon));
           //  Debug.Log(subPolygon.Contains(smallerPolygon));
           //  Debug.Log(polygon.Contains(polygon));
           //
           //  Debug.Log(smallerPolygon.Contains(polygon));
           //  Debug.Log(subPolygon.Contains(polygon));
           //  Debug.Log(smallerPolygon.Contains(subPolygon));


            colors = new List<Color>();

            if (polygons == null)
            {
                Debug.Log("nope");
            }

            if (polygons != null)
                for (var index = 0; index < polygons.Count; index++)
                {
                    colors.Add(new Color(
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f)
                    ));
                }


            //DrawStuff();
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



            // DrawPolygon(polygon, Color.green, 0);
            // DrawPolygon(subPolygon, Color.magenta, 0);
            // DrawPolygon(differencePolygon, Color.white, 0);
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
            for (var i = 0; i < fidelity; i++) {
                var x = Mathf.Sin(step * i) * radius;
                var z = Mathf.Cos(step * i) * radius;

                var nx = Mathf.Sin(step * (i + 1)) * radius;
                var nz = Mathf.Cos(step * (i + 1)) * radius;

                Debug.DrawLine(pos + new Vector3(x, 0, z), pos + new Vector3(nx, 0, nz), color, d);
            }
        }
        private void DrawStuff()
        {
            polygon = CreatePolygon(polygon.points);
            //DrawPolygon(polygon, Color.blue);
            var segments = GetLineSegments(polygon);

            foreach (var point in polygon.points)
            {
                DrawCircle(point, Color.blue);
            }

            var edgeA = segments[0];
            var edgeB = segments[2];

            DrawEdge(edgeA, Color.green);
            DrawEdge(edgeB, Color.green);

            var edgePair = new EdgePair(edgeA, edgeB);

            DrawCircle(edgePair.intersectionPoint.vector, Color.green);

            DrawCircle(edgePair.projected0.vertex, Color.red);
            DrawCircle(edgePair.projected1.vertex, Color.red);


            var subPolygons = edgePair.GetSubPolygons();
            DrawPolygon(subPolygons.leftTriangle, Color.magenta);
            DrawPolygon(subPolygons.rightTriangle, Color.yellow);
            DrawPolygon(subPolygons.trapezoid, Color.cyan);
            //
            var singlePartArea = polygon.GetArea() / parts;


            // var p = SlicePolygon(polygon, polygon.points[0], edgePair.projected1.vertex);
            // DrawPolygon(p, Color.black);
            // Debug.Log(p);


            var cuts = subPolygons.GetCuts(polygon, singlePartArea);
            // foreach (var cut in cuts)
            // {
            //
            //     Debug.Log(cut);
            //     DrawPolygon(cut.cutAway, new Color(
            //         Random.Range(0f, 1f),
            //         Random.Range(0f, 1f),
            //         Random.Range(0f, 1f)
            //     ));
            // }

            var nextPolygon = polygon.Difference(cuts[0].cutAway);

            //DrawPolygon(cuts[0].cutAway, Color.blue);
            //DrawPolygon(nextPolygon, Color.black);

            var cuts2 = subPolygons.GetCuts(polygon, singlePartArea);
            foreach (var cut in cuts2)
            {

                Debug.Log(cut);
                DrawPolygon(cut.cutAway, new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f)
                ));
            }

            foreach (var p3 in nextPolygon.points)
            {
                DrawCircle(p3, Color.red);
            }

            var nextNextPolygon = nextPolygon.Difference(cuts2[1].cutAway);
            DrawPolygon(nextPolygon, Color.white);
             DrawPolygon(cuts2[1].cutAway, Color.yellow, 1000000, new Vector3(-5, 0,0));
             DrawPolygon(nextNextPolygon, Color.magenta, 1000000, new Vector3(5, 0, 0));

            // DrawEdge(edgeA.start, edgePair.intersectionPoint.vector, Color.white);
            // DrawEdge(edgeB.end, edgePair.intersectionPoint.vector, Color.white);

        }

    }
}
