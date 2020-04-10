using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour {

    public List<BuildingGeneratorType> buildingGenerators;

    public GameObject Generate(Plot plot, BuildingType buildingType, GameObject buildings) {
        var buildingGenerator = buildingGenerators.Find(bg => bg.buildingType == buildingType).buildingGenerator.GetComponent<IBuildingGenerator>();
        return buildingGenerator.Generate(plot, buildings);
    }

    [Serializable]
    public class BuildingGeneratorType {
        public BuildingType buildingType;
        public GameObject buildingGenerator;
    }

}
