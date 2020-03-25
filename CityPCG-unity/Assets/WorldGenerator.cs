using System;
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

    private TerrainGenerator terrainGenerator;
    private NoiseGenerator populationGenerator;
    private RoadGenerator roadGenerator;
    private BlockGenerator blockGenerator;
    private PlotGenerator plotGenerator;
    private BuildingGenerator buildingGenerator;
    private ParkGenerator parkGenerator;

    private Noise populationNoise;
    private List<Plot> plots;

    public void Undo() { }

    public void GenerateTerrain() {
        terrainGenerator.GenerateTerrain();
    }

    public void GenerateRoads() {
        populationNoise = populationGenerator.Generate();

        roadGenerator.Generate(populationNoise, GenerateBlocks);
    }

    private void GenerateBlocks(RoadNetwork roadNetwork) {
        var blocks = blockGenerator.Generate(roadNetwork);
        plots = new List<Plot>();
        blocks.ForEach(GeneratePlots);
    }

    private void GeneratePlots(Block block) {
        plots.AddRange(plotGenerator.Generate(block, populationNoise));
    }

    public void GenerateBuildings() {
        foreach (var plot in plots) {
            buildingGenerator.Generate(plot);
        }
    }

    public void GenerateStreets() { }
    private void InstantiateGenerators() {
        terrainGenerator = Instantiate(terrainGeneratorPrefab, transform).GetComponent<TerrainGenerator>();
        populationGenerator = Instantiate(populationGeneratorPrefab, transform).GetComponent<NoiseGenerator>();
        roadGenerator = Instantiate(roadGeneratorPrefab, transform).GetComponent<RoadGenerator>();
        blockGenerator = Instantiate(blockGeneratorPrefab, transform).GetComponent<BlockGenerator>();
        plotGenerator = Instantiate(plotGeneratorPrefab, transform).GetComponent<PlotGenerator>();
        buildingGenerator = Instantiate(buildingGeneratorPrefab, transform).GetComponent<BuildingGenerator>();
        parkGenerator = Instantiate(parkGeneratorPrefab, transform).GetComponent<ParkGenerator>();
    }

    private void Start() {
        InstantiateGenerators();
    }
}
