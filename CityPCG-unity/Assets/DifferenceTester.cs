﻿using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Utils.PolygonSplitter;
using Vector3 = UnityEngine.Vector3;

namespace DefaultNamespace
{
    public class DifferenceTester : MonoBehaviour
    {

        public PolygonDifferenceTester p1;
        public PolygonDifferenceTester p2;
        public PolygonDifferenceTester p3;
        public PolygonDifferenceTester p4;
        public PolygonDifferenceTester p5;
        

        public void Start()
        {
            p1.Init(Vector3.zero);
            p2.Init(new Vector3(2.3f, 0, 0));
            p3.Init(new Vector3(4.6f, 0, 0));
            p4.Init(new Vector3(6.9f, 0, 0));
            p5.Init(new Vector3(9.2f, 0, 0));
        }

        public void Update()
        {   
            p1.Render();
            p2.Render();
            p3.Render();
            p4.Render();
            p5.Render();
        }
    }
    
    [Serializable]
    public class PolygonDifferenceTester
    {
        // The main polygon to be removed from
        public Polygon polygon;
        
        // The two points that the subPolygon will slice from polygon
        public Vector3 p1, p2;

        // The subPolygon generated via PolygonUtils.SlicePolygon
        private Polygon subPolygon;

        // The difference polygon called from polygon.Difference(subPolygon)
        private Polygon difference;

        private Polygon outline;

        private Vector3 position;
        private Vector3 level1 = new Vector3(0, 0, -1.1f);
        private Vector3 level2 = new Vector3(1.1f, 0, -1.1f);
        private Vector3 level3 = new Vector3(1.1f, 0, 0);

        public void Init(Vector3 basePosition)
        {
            //This basically adds the first point again to create a full circle.
            polygon = PolygonUtils.CreatePolygon(polygon.points);
            
            if (polygon == null)
            {
                Debug.LogError("polygon for PolygonDifferenceTester");
                return;
            }
            position = basePosition;
            
            // Slice call
            subPolygon = PolygonUtils.SlicePolygon(polygon, p1, p2);

            //Difference call
            difference = polygon.Difference(subPolygon);
            
            //Outline to clearly differentiate the tests
            outline = new Polygon(new List<Vector3>()
            {
                new Vector3(-0.1f, 0, -1.2f),
                new Vector3(2.2f, 0, -1.2f),
                new Vector3(2.2f, 0, 1.2f),
                new Vector3(-0.1f, 0, 1.2f),
            });
        }

        public void Render()
        {
            DrawPolygon(outline, Color.black);
            
            DrawPolygon(polygon, Color.blue);   
            DrawCircle(position + p1, Color.white);
            DrawCircle(position + p2, Color.white);
            
            DrawPolygon(subPolygon, Color.red, level1);   
            DrawPolygon(difference, Color.green, level2);
            
            DrawPolygon(subPolygon, Color.red, level3);   
            DrawPolygon(difference, Color.green, level3);   
        }
        
        private void DrawPolygon(Polygon p, Color c, Vector3 p2 = default)
        {
            for (var i = 0; i < p.points.Count; i++)
            {
                var cur = p.points[i] + position + p2;
                var next = p.points[(i + 1) % p.points.Count] + position + p2;

                Debug.DrawLine(cur, next, c);
            }
        }

        private void DrawCircle(Vector3 pos, Color color, float radius = 0.05f, float fidelity = 10)
        {
            var step = 2 * Mathf.PI / fidelity;
            for (var i = 0; i < fidelity; i++)
            {
                var x = Mathf.Sin(step * i) * radius;
                var z = Mathf.Cos(step * i) * radius;

                var nx = Mathf.Sin(step * (i + 1)) * radius;
                var nz = Mathf.Cos(step * (i + 1)) * radius;

                Debug.DrawLine(pos + new Vector3(x, 0, z), pos + new Vector3(nx, 0, nz), color);
            }
        }
    }
    
}