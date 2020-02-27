using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private TerrainGenerator terrainGenerator;
    private PopulationGenerator populationGenerator;
    private RoadGenerator roadGenerator;
    private BlockGenerator blockGenerator;
    private PlotGenerator plotGenerator;
    private BuildingGenerator buildingGenerator;
    private ParkGenerator parkGenerator;


    public void Undo() {}
    public void GenerateTerrain() {}
    public void GenerateRoads() {}
    public void GenerateStreets() {}
    public void GenerateBuildings() {}

    private void SpawnGenerators() {
        terrainGenerator    = Instantiate(terrainGeneratorPrefab, transform).GetComponent<TerrainGenerator>();
        populationGenerator = Instantiate(populationGeneratorPrefab, transform).GetComponent<PopulationGenerator>();
        roadGenerator       = Instantiate(roadGeneratorPrefab, transform).GetComponent<RoadGenerator>();
        blockGenerator      = Instantiate(blockGeneratorPrefab, transform).GetComponent<BlockGenerator>();
        plotGenerator       = Instantiate(plotGeneratorPrefab, transform).GetComponent<PlotGenerator>();
        buildingGenerator   = Instantiate(buildingGeneratorPrefab, transform).GetComponent<BuildingGenerator>();
        parkGenerator       = Instantiate(parkGeneratorPrefab, transform).GetComponent<ParkGenerator>();
    }

    private void Start() {
        SpawnGenerators();
    }
}
