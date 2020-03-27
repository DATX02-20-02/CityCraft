using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;

// What? Generates the world, including terrain, roads, and cities.
// Why? The many generators need a pipeline that handles the IO between generators.
// How? Splits the workload into many subgenerators, and only manages their inputs/outputs.
public class WorldGenerator : MonoBehaviour {

    [SerializeField] private GameObject terrainGeneratorPrefab = null;
    [SerializeField] private GameObject populationGeneratorPrefab = null;
    [SerializeField] private GameObject roadGeneratorPrefab = null;
    [SerializeField] private GameObject blockGeneratorPrefab = null;
    [SerializeField] private GameObject plotGeneratorPrefab = null;
    [SerializeField] private GameObject buildingGeneratorPrefab = null;
    [SerializeField] private GameObject parkGeneratorPrefab = null;
    [SerializeField] private bool debug = false;
    [SerializeField] private int debugSeed = 0;

    private TerrainGenerator terrainGenerator;
    private NoiseGenerator populationGenerator;
    private RoadGenerator roadGenerator;
    private BlockGenerator blockGenerator;
    private PlotGenerator plotGenerator;
    private BuildingGenerator buildingGenerator;
    private ParkGenerator parkGenerator;

    private Noise populationNoise;
    private RoadNetwork roadNetwork;
    private TerrainModel terrain;

    public void Undo() { }

    public void GenerateTerrain() {
        terrain = terrainGenerator.GenerateTerrain();
    }

    public void GenerateRoads() {
        populationNoise = populationGenerator.Generate();

        roadGenerator.Generate(terrain, populationNoise, (roadNetwork) => blockGenerator.Generate(roadNetwork));
    }

    public void GenerateStreets() { }

    public void GenerateBuildings() {
        foreach (Block block in this.blocks) {
            // Split each block into plots
            var plots = plotGenerator.Generate(block, populationNoise);

            // Generate buildings in each plot.
            foreach (Plot plot in plots) {
                ElevatedPlot elevatedPlot = new ElevatedPlot(
                    plot.vertices.Select(v => roadNetwork.Terrain.GetPosition(v)).ToList()
                );

                buildingGenerator.Generate(elevatedPlot);
            }
        }
    }

    private void GenerateBlocks(RoadNetwork roadNetwork) {
        this.roadNetwork = roadNetwork;
        this.blocks = blockGenerator.Generate(roadNetwork);
    }

    private void InstantiateGenerators() {
        terrainGenerator = Instantiate(terrainGeneratorPrefab, transform).GetComponent<TerrainGenerator>();
        populationGenerator = Instantiate(populationGeneratorPrefab, transform).GetComponent<NoiseGenerator>();
        roadGenerator = Instantiate(roadGeneratorPrefab, transform).GetComponent<RoadGenerator>();
        blockGenerator = Instantiate(blockGeneratorPrefab, transform).GetComponent<BlockGenerator>();
        plotGenerator = Instantiate(plotGeneratorPrefab, transform).GetComponent<PlotGenerator>();
        buildingGenerator = Instantiate(buildingGeneratorPrefab, transform).GetComponent<BuildingGenerator>();
        parkGenerator = Instantiate(parkGeneratorPrefab, transform).GetComponent<ParkGenerator>();
    }

    private void Awake() {
        if (debug) {
            Random.InitState(debugSeed);
        }
    }

    private void Start() {
        InstantiateGenerators();
    }
}
