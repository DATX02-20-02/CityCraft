using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

// What? Generates content within plots
// Why?
// How?
public class PlotContentGenerator : MonoBehaviour {
    [Header("Generators")]
    [SerializeField] private BuildingGenerator buildingGenerator = null;
    [SerializeField] private ParkGenerator parkGenerator = null;
    [SerializeField] private ParkingGenerator parkingGenerator = null;

    [Header("Content Generation Params")]
    [SerializeField] private int intervalSize = 0;
    [SerializeField] private float intervalDelay = 0;

    public void Reset() {
        buildingGenerator.Reset();
        parkGenerator.Reset();
        parkingGenerator.Reset();
    }

    public void Generate(List<Plot> plots, TerrainModel terrain, Noise populationNoise, Action callback) {
        Reset();

        StartCoroutine(GenerateRoutine(plots, terrain, populationNoise, callback));
    }

    private IEnumerator GenerateRoutine(List<Plot> plots, TerrainModel terrain, Noise populationNoise, Action callback) {
        int plotCounter = 0;
        foreach (Plot plot in plots) {
            if (plot.type == PlotType.Manhattan || plot.type == PlotType.Skyscraper) {
                GameObject blockObject = new GameObject("Block");
                blockObject.transform.parent = buildingGenerator.transform;

                buildingGenerator.Generate(plot, terrain, populationNoise, blockObject);
            }
            else if (plot.type == PlotType.Park) {
                parkGenerator.Generate(terrain, plot);
            }
            else if (plot.type == PlotType.Parking) {
                parkingGenerator.Generate(terrain, plot);
            }

            plotCounter++;
            if (intervalSize <= plotCounter) {
                plotCounter = 0;
                yield return new WaitForSeconds(intervalDelay);
            }
        }
        callback();
    }
}
