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

        StartCoroutine(Generate(plots, terrain, populationNoise));
        callback();
    }

    private IEnumerator Generate(List<Plot> plots, TerrainModel terrain, Noise populationNoise) {
        int plotCounter = 0;
        foreach (Plot plot in plots) {
            if (plot.type == PlotType.Apartments || plot.type == PlotType.Skyscraper) {
                buildingGenerator.Generate(plot, terrain, populationNoise);
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
    }
}
