using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGeneratorPlayground : MonoBehaviour{
    private void Start() {
        var buildings = new GameObject("Buildings");

        var plot = new Plot(new List<Vector3>(){new Vector3(0, 0, 0), new Vector3(1,0, 0), new Vector3(1,0, 1), new Vector3(0, 0,1)}, PlotType.Manhattan);
        var buildingGenerator =  GetComponent<BuildingGenerator>();
        buildingGenerator.Generate(plot, buildings);
    }
}
