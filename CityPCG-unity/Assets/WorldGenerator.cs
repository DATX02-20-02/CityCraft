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

    // State properties
    public enum State {
        Terrain,
        Roads,
        Streets,
        Buildings,
        Finished
    }

    private Dictionary<int, State> stateMap = new Dictionary<int, State>() {
        { 0, State.Terrain },
        { 1, State.Roads },
        { 2, State.Streets },
        { 3, State.Buildings },
        { 4, State.Finished },
    };

    private int currentStateIndex = 0;
    private State currentState = State.Terrain;

    // Generator input/output properties
    private Noise populationNoise;
    private RoadNetwork roadNetwork;
    private RoadNetwork roadNetworkSnapshot;
    private TerrainModel terrain;
    private float offsetSpeedX = 0;
    private float offsetSpeedZ = 0;
    private bool terrainGenerated = false;
    private List<Block> blocks;
    private List<ElevatedPlot> elevatedPlots = new List<ElevatedPlot>();

    public State NextState() {
        if (stateMap.ContainsKey(currentStateIndex + 1)) {
            currentState = stateMap[++currentStateIndex];
        }

        return currentState;
    }

    public void Undo() {
        switch (currentState) {
            case State.Streets:
                if (this.roadNetworkSnapshot != null) {
                    this.roadNetwork = this.roadGenerator.Network = this.roadNetworkSnapshot;
                    this.roadNetworkSnapshot = null;
                }

                this.blockGenerator.Reset();
                break;

            case State.Roads:
                this.roadNetwork = this.roadGenerator.Network = null;
                this.roadNetworkSnapshot = null;
                break;
        }
    }

    public void PreviousState() {
        if (stateMap.ContainsKey(currentStateIndex - 1)) {
            currentState = stateMap[--currentStateIndex];
        }
    }

    public void GenerateTerrain() {
        terrainGenerated = true;
        terrain = terrainGenerator.GenerateTerrain();
    }

    public void SetOffsetSpeedX(float x) {
        if (terrainGenerated) offsetSpeedX = x;
    }
    public void SetOffsetSpeedZ(float z) {
        if (terrainGenerated) offsetSpeedZ = z * (-1);
    }

    public void ModifyTerrainSea(float sl) {
        terrainGenerator.SetSeaLevel(sl);
    }

    public void GenerateRoads() {
        populationNoise = populationGenerator.Generate();

        roadGenerator.Generate(
            terrain, populationNoise,
            (RoadNetwork network) => {
                this.roadNetwork = this.roadGenerator.Network = network;
            }
        );
    }

    public void GenerateStreets() {
        if (roadNetwork == null) return;

        if (this.roadNetworkSnapshot != null) {
            this.roadNetwork = this.roadGenerator.Network = this.roadNetworkSnapshot;
        }

        this.roadNetworkSnapshot = roadNetwork.Snapshot();

        roadGenerator.GenerateStreets(
            terrain, populationNoise, (roadNetwork) => {
                GenerateBlocks();
            }
        );
    }

    public void GenerateBuildings() {
        this.elevatedPlots = new List<ElevatedPlot>();

        foreach (Block block in this.blocks) {
            // Split each block into plots
            var plots = plotGenerator.Generate(block, populationNoise);

            // Generate buildings in each plot.
            foreach (Plot plot in plots) {
                ElevatedPlot elevatedPlot = new ElevatedPlot(
                    plot.vertices.Select(v => roadNetwork.Terrain.GetPosition(v)).ToList()
                );
                this.elevatedPlots.Add(elevatedPlot);

                buildingGenerator.Generate(elevatedPlot);
            }
        }
    }

    private void GenerateBlocks() {
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

    private void Update() {
        if (plotGenerator != null) {
            foreach (ElevatedPlot plot in this.elevatedPlots) {
                plotGenerator.DrawPlot(plot);
            }
        }

        if (offsetSpeedX != 0 || offsetSpeedZ != 0) {
            Vector2 speedamp = new Vector2(offsetSpeedX * Time.deltaTime, offsetSpeedZ * Time.deltaTime);
            Vector2 speed = terrainGenerator.NoiseOffset + speedamp;
            terrain = terrainGenerator.GenerateTerrain(speed);
        }

    }
}
