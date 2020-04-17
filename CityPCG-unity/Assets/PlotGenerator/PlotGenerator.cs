using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;
using Utils;

using static Utils.PolygonSplitter.PolygonSplitter;
using static Utils.PolygonSplitter.Implementation.PolygonUtils;

public class PlotGenerator : MonoBehaviour {

    [SerializeField] private AnimationCurve skyscraperGradient = null;
    [SerializeField] private bool debug = false;
    [SerializeField] private int parts = 3;


    public List<Plot> Generate(Block block, TerrainModel terrain, Noise populationNoise) {
        var plots = Split(CreatePolygon(block.Vertices2D()), parts)
            .Where(polygon => polygon != null).ToList()
            .ConvertAll(
                polygon => {
                    List<Vector3> points = polygon.points.Select(v => terrain.GetMeshIntersection(v.x, v.y).point).ToList();
                    Vector2 center = VectorUtil.Vector3To2(PolygonUtil.PolygonCenter(points));
                    float plotPopulation = populationNoise.GetValue(center.x / terrain.width, center.y / terrain.depth);

                    return new Plot(
                        points,
                        DecidePlotType(block.type, plotPopulation)
                    );
                }
            );

        //Removes the duplicate point at the end that is created via Split function
        plots.ForEach(plot => plot.vertices.RemoveAt(plot.vertices.Count - 1));

        return plots;
    }

    private PlotType DecidePlotType(BlockType type, float population) {
        switch (type) {
            case BlockType.Building:
                float skyscraperProb = skyscraperGradient.Evaluate(population);
                return (UnityEngine.Random.value < skyscraperProb) ? PlotType.Skyscraper : PlotType.Apartments;
            case BlockType.Park:
                return PlotType.Park;
            default:
                return PlotType.Empty;
        }
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

    [SerializeField] List<Vector3> vertices = new List<Vector3>() {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1),
    };

    [SerializeField] float a = 1.0f;
    [SerializeField] float b = 0.1f;
    [SerializeField] int c = 6;
    [SerializeField] int d = 10;

    void Update() {
        // if (prev != null) {
        //     Destroy(prev);
        //     prev = null;
        // }

        Plot plot = new Plot(vertices, PlotType.Empty);
        DrawPlot(plot);

        Rectangle rect = PolygonUtil.ApproximateLargestRectangle(
            plot.vertices.Select(v => VectorUtil.Vector3To2(v)).ToList(),
            a,
            b,
            c,
            d
        );

        DrawUtil.DebugDrawRectangle(rect, Color.yellow);

        Debug.DrawLine(
            VectorUtil.Vector2To3(rect.topLeft) + Vector3.up * 0.1f,
            VectorUtil.Vector2To3(rect.topLeft + (rect.topRight - rect.topLeft).normalized * rect.width) + Vector3.up * 0.1f,
            Color.green
        );

        // if (prev == null) {
        //     prev = Instantiate(objGen, transform);
        //     prev.GetComponent<SkyscraperGenerator>().Generate(plot);
        // }
    }
}
