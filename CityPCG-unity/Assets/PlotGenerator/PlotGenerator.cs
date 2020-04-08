using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;
using static Utils.PolygonSplitter.PolygonSplitter;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

public class PlotGenerator : MonoBehaviour {

    [SerializeField] private bool debug = false;
    [SerializeField] private int parts = 3;


    public List<Plot> Generate(Block block, TerrainModel terrain, Noise populationNoise) {
        var plots = Split(CreatePolygon(block.Vertices2D()), parts)
            .Where(polygon => polygon != null).ToList()
            .ConvertAll(
                // TODO: Hook into population map and generate based on that
                // for apartments / skyscrapers. For now, skyscraper is random chance
                polygon => new Plot(
                    polygon.points.Select(v => terrain.GetPosition(v)).ToList(),
                    Plot.DecidePlotType(block.type)
                )
            );

        //Removes the duplicate point at the end that is created via Split function
        plots.ForEach(plot => plot.vertices.RemoveAt(plot.vertices.Count - 1));

        return plots;
    }

    public void DrawPlot(Plot p) {
        if (this.debug) {
            var position = transform.position;

            for (int i = 0; i < p.vertices.Count; i++) {
                var cur = p.vertices[i] + position;
                var next = p.vertices[(i + 1) % p.vertices.Count] + position;

                Debug.DrawLine(cur, next, Color.yellow);
            }
        }
    }

    public void DrawPlots(List<Plot> plots) {
        if (this.debug) {
            foreach (Plot plot in plots) {
                DrawPlot(plot);
            }
        }
    }
}
