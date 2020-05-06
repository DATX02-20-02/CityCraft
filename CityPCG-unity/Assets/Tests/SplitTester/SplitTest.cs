using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils.PolygonSplitter;
using Utils.PolygonSplitter.Implementation;

public class SplitTest : MonoBehaviour {

    [System.Serializable]
    public struct SplitUnitTest {
        public int parts;
        public List<Vector2> vertices;
        public bool show;
    }

    public List<SplitUnitTest> tests;


    void Update() {
        Debug.Log("");
        Debug.Log("");
        Debug.Log("");

        foreach (var test in tests)
            if (test.show)
                RunTest(test);
    }

    private void RunTest(SplitUnitTest t) {
        Debug.Log("");
        var fixed_vertices = new List<Vector2>(t.vertices);
        fixed_vertices.Add(t.vertices[0]);

        var polygon = new Polygon(fixed_vertices);
        List<Polygon> subPolygons = PolygonSplitter.Split(polygon, t.parts);

        foreach (var p in subPolygons) {
            Debug.Log(p);
            DrawPolygon(p, Color.green);
        }
    }

    private void DrawPolygon(Polygon polygon, Color c, float duration = 0.01f) {
        int count = polygon.points.Count;
        for (int i = 0; i < count; i++)
            Debug.DrawLine(polygon.points[i], polygon.points[(i + 1) % count], c, duration);
    }
}
