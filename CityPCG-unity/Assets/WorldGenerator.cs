using System.Collections;
using System.Collections.Generic;
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
    private List<Block> blocks;


    public void Undo() { }

    public void GenerateTerrain() {
        terrainGenerator.GenerateTerrain();
    }

    public void GenerateRoads() {
        populationNoise = populationGenerator.Generate();

        roadGenerator.Generate(populationNoise, GenerateBlocks);
    }

    public void GenerateStreets() { }

    public void GenerateBuildings() {
        foreach (var block in this.blocks) {
            // Split each block into plots
            var plots = plotGenerator.Generate(block, populationNoise);

            // Generate buildings in each plot.
            foreach (var plot in plots) {
                buildingGenerator.Generate(plot);
            }
        }
    }

    private void GenerateBlocks(RoadNetwork roadNetwork) {
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
        if(debug)
            Random.InitState(debugSeed);
    }

    private void Start() {
        InstantiateGenerators();
    }
}
