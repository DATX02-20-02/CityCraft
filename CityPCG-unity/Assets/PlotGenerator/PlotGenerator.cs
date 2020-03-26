using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;
using static Utils.PolygonSplitter.PolygonSplitter;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

public class PlotGenerator : MonoBehaviour {

    [SerializeField] private bool debug = false;
    [SerializeField] private int parts = 3;


    public List<Plot> Generate(Block block, Noise populationNoise) {
        var vec2Vertices = block.vertices.ConvertAll(vec3 => new Vector2(vec3.x, vec3.z));
        var polygons = Split(CreatePolygon(vec2Vertices), parts);
        var plots = polygons.ConvertAll(polygon => new Plot(polygon.points));
        
        if (debug) {
            plots.ForEach(DrawPlot);
        }

        //Removes the duplicate point at the end that is created via Split function
        plots.ForEach(plot => plot.vertices.RemoveAt(plot.vertices.Count - 1));

        return plots;
    }

    private void DrawPlot(Plot p) {
        var position = new Vector2(transform.position.x, transform.position.z);

        for (int i = 0; i < p.vertices.Count; i++) {
            var cur = p.vertices[i] + position;
            var next = p.vertices[(i + 1) % p.vertices.Count] + position;

            Debug.DrawLine(new Vector3(cur.x, 0, cur.y),  new Vector3(next.x, 0, next.y), Color.yellow, 10000000);
        }
    }
}
