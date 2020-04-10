using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManhattanBuildingGenerator : MonoBehaviour, IBuildingGenerator {

    public Material roofMaterial;
    public List<ManhattanFloorGenerator> floorGenerators;
    public List<ManhattanSegmentGenerator> segmentGenerators;
    public List<ManhattanSegmentToSegmentData> segmentToData;
    public float wallSegmentHeightMeter = 2;

    public GameObject Generate(Plot plot, GameObject buildings) {
        var buildingObject = new GameObject("ManhattanBuilding");
        buildingObject.transform.parent = buildings.transform;

        //Here you can change the building type, right now it will always be Straight
        const ManhattanBuildingType buildingType = ManhattanBuildingType.Straight;
        var floorGenerator = floorGenerators.Find(a => a.buildingType == buildingType).floorsGenerator.GetComponent<IManhattanFloorsGenerator>();
        var floorTypes = floorGenerator.Generate();

        var floorToSegmentGeneratorDict = segmentGenerators.ToDictionary(sg => sg.floorType, sg => sg.segmentGenerator.GetComponent<IManhattanWallSegmentsGenerator>());
        var segmentToDataDict = segmentToData.ToDictionary(sto => sto.segmentType, sto => sto.data);

        var wallGenerator = new ManhattanBuildingWallGenerator(wallSegmentHeightMeter, floorTypes, floorToSegmentGeneratorDict, segmentToDataDict, buildingObject);


        foreach (var sg in floorToSegmentGeneratorDict.Values)
        {
            sg.Init(segmentToDataDict);
        }

        for (var i = plot.vertices.Count - 1; i >= 0; i--) {
            var cur = plot.vertices[i];
            var next = plot.vertices[i == 0 ? plot.vertices.Count - 1 : (i - 1)];

            wallGenerator.Generate(cur, next);
        }

        ManhattanBuildingRoofGenerator.Generate(plot.vertices, roofMaterial, buildingObject, wallSegmentHeightMeter * floorTypes.Count);

        return buildingObject;
    }

    [Serializable]
    public class ManhattanFloorGenerator {
        public ManhattanBuildingType buildingType;
        public GameObject floorsGenerator;
    }

    [Serializable]
    public class ManhattanSegmentGenerator {
        public ManhattanFloorType floorType;
        public GameObject segmentGenerator;
    }

    [Serializable]
    public class ManhattanSegmentToSegmentData {
        public ManhattanWallSegmentType segmentType;
        public ManhattanSegmentData data;
    }
}
