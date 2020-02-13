using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour {

    [SerializeField] private GameObject terrainGeneratorPrefab;
    [SerializeField] private GameObject populationGeneratorPrefab;
    [SerializeField] private GameObject roadGeneratorPrefab;
    [SerializeField] private GameObject blockGeneratorPrefab;
    [SerializeField] private GameObject plotGeneratorPrefab;
    [SerializeField] private GameObject buildingGeneratorPrefab;
    [SerializeField] private GameObject parkGeneratorPrefab;

    private TerrainGenerator terrainGenerator;
    private PopulationGenerator populationGenerator;
    private RoadGenerator roadGenerator;
    private BlockGenerator blockGenerator;
    private PlotGenerator plotGenerator;
    private BuildingGenerator buildingGenerator;
    private ParkGenerator parkGenerator;


    private void Start() {
        Debug.Log("City generator started...");

        // Spawn all generators
        terrainGenerator    = Instantiate(terrainGeneratorPrefab, transform).GetComponent<TerrainGenerator>();
        populationGenerator = Instantiate(populationGeneratorPrefab, transform).GetComponent<PopulationGenerator>();
        roadGenerator       = Instantiate(roadGeneratorPrefab, transform).GetComponent<RoadGenerator>();
        blockGenerator      = Instantiate(blockGeneratorPrefab, transform).GetComponent<BlockGenerator>();
        plotGenerator       = Instantiate(plotGeneratorPrefab, transform).GetComponent<PlotGenerator>();
        buildingGenerator   = Instantiate(buildingGeneratorPrefab, transform).GetComponent<BuildingGenerator>();
        parkGenerator       = Instantiate(parkGeneratorPrefab, transform).GetComponent<ParkGenerator>();
    }
}
