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
        var plots = Split(CreatePolygon(block.vertices), parts)
            .Where(polygon => polygon != null).ToList()
            .ConvertAll(
                polygon => new Plot(polygon.points)
            );

        //Removes the duplicate point at the end that is created via Split function
        plots.ForEach(plot => plot.vertices.RemoveAt(plot.vertices.Count - 1));

        return plots;
    }

    public void DrawPlot(ElevatedPlot p) {
        if (this.debug) {
            var position = transform.position;

            for (int i = 0; i < p.vertices.Count; i++) {
                var cur = p.vertices[i] + position;
                var next = p.vertices[(i + 1) % p.vertices.Count] + position;

                Debug.DrawLine(cur, next, Color.yellow);
            }
        }
    }
}
