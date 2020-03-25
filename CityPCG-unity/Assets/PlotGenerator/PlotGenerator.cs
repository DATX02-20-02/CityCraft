using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;
using static Utils.PolygonSplitter.PolygonSplitter;
using static Utils.PolygonSplitter.PolygonUtils;

public class PlotGenerator : MonoBehaviour {

    public bool debug;
    public int parts = 3;

    public List<Plot> Generate(Block block, Noise populationNoise) {
        var plots = Split(CreatePolygon(block.vertices), parts).ConvertAll((polygon => new Plot(polygon.points)));

        if (debug) {
            plots.ForEach(DrawPlot);
        }

        //Removes the duplicate point at the end that is created via Split function
        plots.ForEach(plot => plot.vertices.RemoveAt(plot.vertices.Count - 1));

        return plots;
    }

    private void DrawPlot(Plot p) {
        for (int i = 0; i < p.vertices.Count; i++) {
            var position = transform.position;

            var cur = p.vertices[i] + position;
            var next = p.vertices[(i + 1) % p.vertices.Count] + position;

            Debug.DrawLine(cur, next, Color.yellow, 10000000);
        }
    }
}
