using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGeneratorPlayground : MonoBehaviour{
    private void Start() {
        var buildings = new GameObject("Buildings");

        //var plot = new Plot(new List<Vector2>(){new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)}, PlotType.Manhattan);
        //var buildingGenerator =  GetComponent<BuildingGenerator>();
        //buildingGenerator.Generate(plot, BuildingType.Manhattan, buildings);
    }
}
