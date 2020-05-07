using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour {

    public List<BuildingGeneratorType> buildingGenerators;

    public void Reset() {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
    }

    public GameObject Generate(Plot plot, GameObject buildings) {
        var buildingGenerator = buildingGenerators.Find(bg => bg.buildingType == plot.type).buildingGenerator.GetComponent<IBuildingGenerator>();
        var building = buildingGenerator.Generate(plot, buildings, 1.0f);

        var highestY = plot.vertices.Aggregate(plot.vertices[0], (v1, v2) => v1.y > v2.y ? v1 : v2).y;
        building.transform.position = new Vector3(plot.vertices[0].x, highestY, plot.vertices[0].z);

        return building;
    }

    public GameObject Generate(Plot plot, TerrainModel terrain, Noise populationNoise, GameObject buildings) {
        var center = VectorUtil.Vector3To2(plot.Center);
        var population = populationNoise.GetValue(center.x / terrain.width, center.y / terrain.depth);
        var buildingGenerator = buildingGenerators.Find(bg => bg.buildingType == plot.type).buildingGenerator.GetComponent<IBuildingGenerator>();

        var building = buildingGenerator.Generate(plot, buildings, population);

        return building;
    }

    [Serializable]
    public class BuildingGeneratorType {
        public PlotType buildingType;
        public GameObject buildingGenerator;
    }

}
