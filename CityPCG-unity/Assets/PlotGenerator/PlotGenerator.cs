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

    [SerializeField] private float parkingProbability = 0.1f;

    private List<Plot> plots = new List<Plot>();

    public void Reset() {
        plots = new List<Plot>();
    }

    public List<Plot> Generate(List<Block> blocks, TerrainModel terrain, Noise populationNoise) {
        Reset();

        foreach (Block block in blocks)
            plots.AddRange(Generate(block, terrain, populationNoise));

        return plots;
    }

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
        float skyscraperProb = skyscraperGradient.Evaluate(population);

        if (type == BlockType.Industrial || type == BlockType.Downtown) {
            if (UnityEngine.Random.value < parkingProbability) return PlotType.Parking;
        }

        switch (type) {
            case BlockType.Industrial:
                return (UnityEngine.Random.value < skyscraperProb) ? PlotType.Skyscraper : PlotType.Apartments;

            case BlockType.Downtown:
                return (UnityEngine.Random.value < skyscraperProb) ? PlotType.Skyscraper : PlotType.Apartments;

            case BlockType.Suburbs:
                return PlotType.Apartments;

            case BlockType.Skyscrapers:
                return PlotType.Skyscraper;

            case BlockType.Apartments:
                return PlotType.Apartments;

            case BlockType.Parks:
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

    void Update() {
        if (this.debug) {
            foreach (Plot plot in plots) {
                DrawPlot(plot);
            }
        }
    }
}
