using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGeneratorPlayground : MonoBehaviour{
    private void Start() {
        var buildings = new GameObject("Buildings");

        var plot = new Plot(new List<Vector3>(){new Vector3(145.0f, 43f, 263.9f), new Vector3(144.9f, 44.3f, 265.4f), new Vector3(144.8f,46.5f, 266.7f), new Vector3(143.6f,46.3f, 265.3f), new Vector3(143.7f, 46.3f,265.3f), new Vector3(144f, 45, 261)}, PlotType.Manhattan);
        var buildingGenerator =  GetComponent<BuildingGenerator>();
        buildingGenerator.Generate(plot, buildings);
    }
}
