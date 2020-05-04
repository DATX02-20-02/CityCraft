using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;
using Utils;

// What? Generates the world, including terrain, roads, and cities.
// Why? The many generators need a pipeline that handles the IO between generators.
// How? Splits the workload into many subgenerators, and only manages their inputs/outputs.
public class WorldGenerator : MonoBehaviour {
    [Header("Generators")]
    [SerializeField] private TerrainGenerator terrainGenerator = null;
    [SerializeField] private NoiseGenerator populationGenerator = null;
    [SerializeField] private RoadGenerator roadGenerator = null;
    [SerializeField] private RoadMeshGenerator roadMeshGenerator = null;
    [SerializeField] private BlockGenerator blockGenerator = null;
    [SerializeField] private PlotGenerator plotGenerator = null;
    [SerializeField] private PlotContentGenerator plotContentGenerator = null;

    [Header("Debug")]
    [SerializeField] private bool debug = false;
    [SerializeField] private int debugSeed = 0;

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
    private Vector2 terrainBaseOffset;
    private List<Block> blocks;
    private List<Plot> plots = new List<Plot>();

    private Action buildingsCallback;

    public State CurrentState {
        get { return currentState; }
    }

    public TerrainModel Terrain {
        get { return terrain; }
    }

    public State NextState() {
        State prevState = currentState;

        if (stateMap.ContainsKey(currentStateIndex + 1)) {
            currentState = stateMap[++currentStateIndex];
        }

        if (currentState != prevState) OnStateChanged(prevState);

        return currentState;
    }

    public void Undo() {
        switch (currentState) {
            case State.Terrain:
                ResetBuildings();
                ResetRoads();
                ResetTerrain();
                break;

            case State.Roads:
                ResetBuildings();
                ResetRoads();
                break;

            case State.Streets:
                ResetBuildings();
                ResetStreets();
                break;

            case State.Buildings:
                ResetBuildings();
                break;
        }
    }

    private void ResetTerrain() {
        this.terrainGenerator.Reset();
    }

    private void ResetRoads() {
        this.roadNetwork = this.roadGenerator.Network = null;
        this.roadNetworkSnapshot = null;

        this.roadGenerator.Reset();
        this.roadMeshGenerator.Reset();
    }

    private void ResetStreets() {
        if (this.roadNetworkSnapshot != null) {
            this.roadNetwork = this.roadGenerator.Network = this.roadNetworkSnapshot;
            this.roadNetworkSnapshot = null;
        }

        this.blockGenerator.Reset();
        this.roadMeshGenerator.Generate(this.roadGenerator.Network, terrain, null);
    }

    private void ResetBuildings() {
        this.plotContentGenerator.Reset();
    }

    public State PreviousState() {
        State prevState = currentState;

        if (stateMap.ContainsKey(currentStateIndex - 1)) {
            currentState = stateMap[--currentStateIndex];
        }

        if (currentState != prevState) OnStateChanged(prevState, true);

        return currentState;
    }

    private void OnStateChanged(State prevState, bool previous = false) {
    }

    public void GenerateTerrain(Vector2 localOffset, int width, int depth, bool newBaseOffset = false) {
        this.terrainGenerator.SetWidth(width);
        this.terrainGenerator.SetDepth(depth);

        if (newBaseOffset)
            this.terrainBaseOffset = new Vector2(
                                                 UnityEngine.Random.Range(-10000f, 10000f),
                                                 UnityEngine.Random.Range(-10000f, 10000f)
                                                 );
        terrain = terrainGenerator.GenerateTerrain(this.terrainBaseOffset + localOffset);
    }

    public void ModifyTerrainSea(float sl) {
        float newLevel = terrainGenerator.SetSeaLevel(sl);
        terrain.seaLevel = newLevel;
    }

    public void GenerateRoads(List<CityInput> cityInputs) {
        GenerateRoads(cityInputs, (RoadNetwork network) => { });
    }

    public void GenerateRoads(List<CityInput> cityInputs, Action<RoadNetwork> callback) {
        this.blockGenerator.Reset();
        this.populationNoise = populationGenerator.Generate();

        roadGenerator.Generate(
            this.terrain, this.populationNoise,
            cityInputs,
            (RoadNetwork network) => {
                this.roadNetwork = this.roadGenerator.Network = network;
                callback(network);
            }
        );
    }

    public void GenerateStreets() {
        GenerateStreets((RoadNetwork network) => { });
    }

    public void GenerateStreets(Action<RoadNetwork> callback) {
        if (roadNetwork == null) return;

        if (this.roadNetworkSnapshot != null) {
            this.roadNetwork = this.roadGenerator.Network = this.roadNetworkSnapshot;
        }

        this.roadNetworkSnapshot = roadNetwork.Snapshot();

        this.blockGenerator.Reset();
        roadGenerator.GenerateStreets(
            terrain, populationNoise,
            (roadNetwork) => {
                GenerateBlocks();
                callback(roadNetwork);

            }
        );
    }

    private void GenerateBlocks() {
        this.blocks = blockGenerator.Generate(roadNetwork, populationNoise);
    }

    public void GeneratePlotContent(Action callback) {
        this.buildingsCallback = callback;

        this.plots = plotGenerator.Generate(this.blocks, terrain, populationNoise);
        this.plotContentGenerator.Generate(this.plots, terrain, populationNoise, callback);
    }

    private void Awake() {
        if (debug) {
            UnityEngine.Random.InitState(debugSeed);
        }
    }

    // Just for debug purposes so I don't have to step through
    // generation every single time
    // private void AutoStart() {
    //     if (this.blockGenerator == null || this.buildingGenerator == null) return;
    //     this.roadGenerator.Reset();
    //     this.roadMeshGenerator.Reset();
    //     this.blockGenerator.Reset();
    //     this.buildingGenerator.Reset();

    //     GenerateTerrain();

    //     Vector3 pos = terrain.GetMeshIntersection(300, 300).point;

    //     this.roadUIHandler.AddCityInput(new CityInput(pos, CityType.Manhattan, null, 50));

    //     GenerateRoads(
    //         (RoadNetwork network) => {
    //             // GenerateStreets((RoadNetwork _) => {
    //             //         // GeneratePlotContent();
    //             //     }
    //             // );
    //         }
    //     );
    // }

    // void OnEnable() {
    //     AutoStart();
    // }

    void Update() {
    }
}
