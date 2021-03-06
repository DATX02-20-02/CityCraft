using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.PolygonSplitter;
using Utils.PolygonSplitter.Implementation;

namespace Tests {
    public class DifferenceTester : MonoBehaviour {

        public PolygonDifferenceTester p1;
        public PolygonDifferenceTester p2;
        public PolygonDifferenceTester p3;
        public PolygonDifferenceTester p4;
        public PolygonDifferenceTester p5;

        private void Start() {
            p1.Init(Vector3.zero);
            p2.Init(new Vector3(1 * 2.3f, 0, 0));
            p3.Init(new Vector3(2 * 2.3f, 0, 0));
            p4.Init(new Vector3(3 * 2.3f, 0, 0));
            p5.Init(new Vector3(4 * 2.3f, 0, 0));
        }

        private void Update() {
            p1.Render();
            p2.Render();
            p3.Render();
            p4.Render();
            p5.Render();
        }
    }

    [Serializable]
    public class PolygonDifferenceTester {
        // The main polygon to be removed from
        public Polygon polygon;

        // The two points that the subPolygon will slice from polygon
        public Vector2 p1, p2;

        // The subPolygon generated via PolygonUtils.SlicePolygon
        private Polygon subPolygon;

        // The difference polygon called from polygon.Difference(subPolygon)
        private Polygon difference;

        private Polygon outline;

        private Vector3 position;
        private Vector3 level1 = new Vector3(0, 0, -1.1f);
        private Vector3 level2 = new Vector3(1.1f, 0, -1.1f);
        private Vector3 level3 = new Vector3(1.1f, 0, 0);

        public void Init(Vector3 basePosition) {
            //This basically adds the first point again to create a full circle.
            polygon = PolygonUtils.CreatePolygon(polygon.points);

            if (polygon == null) {
                Debug.LogError("polygon for PolygonDifferenceTester");
                return;
            }
            position = basePosition;

            // Slice call
            subPolygon = PolygonUtils.SlicePolygon(polygon, p1, p2);

            //Difference call
            difference = polygon.Difference(subPolygon);

            Debug.Log("Diff polygon");
            foreach (var p in difference.points)
                Debug.Log(p);

            //Outline to clearly differentiate the tests
            outline = new Polygon(new List<Vector2>()
            {
                new Vector2(-0.1f, -1.2f),
                new Vector2(2.2f, -1.2f),
                new Vector2(2.2f, 1.2f),
                new Vector2(-0.1f, 1.2f),
            });
        }

        public void Render() {
            DrawPolygon(outline, Color.black);

            DrawPolygon(polygon, Color.blue);
            DrawCircle(position + new Vector3(p1.x, 0, p1.y), Color.white);
            DrawCircle(position + new Vector3(p2.x, 0, p2.y), Color.white);

            DrawPolygon(subPolygon, Color.red, level1);
            DrawPolygon(difference, Color.green, level2);

            DrawPolygon(subPolygon, Color.red, level3);
            DrawPolygon(difference, Color.green, level3);
        }

        private void DrawPolygon(Polygon p, Color c, Vector3 translation = default) {


            for (var i = 0; i < p.points.Count; i++) {
                var p1 = p.points[i];
                var p2 = p.points[(i + 1) % p.points.Count];

                var cur = new Vector3(p1.x, 0, p1.y) + position + translation;
                var next = new Vector3(p2.x, 0, p2.y) + position + translation;

                Debug.DrawLine(cur, next, c);
            }
        }

        private void DrawCircle(Vector3 pos, Color color, float radius = 0.05f, float fidelity = 10) {
            var step = 2 * Mathf.PI / fidelity;
            for (var i = 0; i < fidelity; i++) {
                var x = Mathf.Sin(step * i) * radius;
                var z = Mathf.Cos(step * i) * radius;

                var nx = Mathf.Sin(step * (i + 1)) * radius;
                var nz = Mathf.Cos(step * (i + 1)) * radius;

                Debug.DrawLine(pos + new Vector3(x, 0, z), pos + new Vector3(nx, 0, nz), color);
            }
        }
    }
}
