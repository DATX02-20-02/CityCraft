using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class ParkingGeneratorTest : MonoBehaviour {
    [SerializeField] private Plot plot = new Plot(
        new List<Vector3>() {
            new Vector3(1, 0, 1),
            new Vector3(2.5f, 0, 1),
            new Vector3(3, 0, 3),
            new Vector3(1.5f, 0, 2.5f)
        },
        PlotType.Parking
    );

    [SerializeField] private ParkingGenerator parkingGenerator = null;
    [SerializeField] private TerrainGenerator terrainGen = null;
    [SerializeField] private bool generate = false;

    private Rectangle rect;

    private TerrainModel terrain;

    private void Reset() {
        parkingGenerator.Reset();
    }

    private void Generate() {
        terrain = terrainGen.GenerateTerrain();

        plot.vertices = plot.vertices.Select(v => terrain.GetMeshIntersection(v.x, v.z).point).ToList();

        parkingGenerator.Reset();
        rect = parkingGenerator.Generate(terrain, plot);
    }

    void Update() {
        if (generate) {
            generate = false;
            Generate();
        }

        DrawUtil.DebugDrawPlot(plot, Color.yellow);
        DrawUtil.DebugDrawRectangle(rect, Color.red);
    }
}
