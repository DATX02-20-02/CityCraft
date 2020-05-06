using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManhattanBuildingGenerator : MonoBehaviour, IBuildingGenerator {

    public Material basementMaterial;
    public Material roofMaterial;
    public List<ManhattanFloorGenerator> floorGenerators;
    public List<ManhattanSegmentGenerator> segmentGenerators;
    public List<ManhattanSegmentToSegmentData> segmentToData;
    public float wallSegmentHeightMeter = 2;

    public GameObject Generate(Plot plot, GameObject buildings, float population) {
        var buildingObject = new GameObject("ManhattanBuilding");
        buildingObject.transform.parent = buildings.transform;

        //Here you can change the building type, right now it will always be Straight
        const ManhattanBuildingType buildingType = ManhattanBuildingType.Straight;
        var floorGenerator = floorGenerators.Find(a => a.buildingType == buildingType).floorsGenerator.GetComponent<IManhattanFloorsGenerator>();
        var floorTypes = floorGenerator.Generate(population);

        var floorToSegmentGeneratorDict = segmentGenerators.ToDictionary(sg => sg.floorType, sg => sg.segmentGenerator.GetComponent<IManhattanWallSegmentsGenerator>());
        var segmentToDataDict = segmentToData.ToDictionary(sto => sto.segmentType, sto => sto.data);

        var wallGenerator = new ManhattanBuildingWallGenerator(wallSegmentHeightMeter, floorTypes, floorToSegmentGeneratorDict, segmentToDataDict, buildingObject);

        foreach (var sg in floorToSegmentGeneratorDict.Values) {
            sg.Init(segmentToDataDict);
        }

        var zero = VectorUtil.Vector3To2(plot.vertices[0]);
        var relativeVertices = plot.vertices.ConvertAll(v => VectorUtil.Vector3To2(v) - zero);

        var ttmSegments = new List<TemporaryTransformedMesh>();

        for (var i = relativeVertices.Count - 1; i >= 0; i--) {
            var cur = relativeVertices[i];
            var next = relativeVertices[i == 0 ? relativeVertices.Count - 1 : (i - 1)];
            wallGenerator.Generate(cur, next, buildingObject);

            //ttmSegments.AddRange();
        }

        var biggestYDifference = 0.0f;
        foreach (var v1 in plot.vertices) {
            foreach (var v2 in plot.vertices) {
                if (v1 != v2) {
                    biggestYDifference = Math.Max(biggestYDifference, Math.Abs(v1.y - v2.y));
                }
            }
        }

        ttmSegments.Add(ManhattanBuildingRoofGenerator.Generate(relativeVertices, roofMaterial, wallSegmentHeightMeter * floorTypes.Count));
        ttmSegments.Add(ManhattanBuildingBasementGenerator.Generate(relativeVertices, basementMaterial,
            biggestYDifference));

        MeshCombiner.Combine(buildingObject, ttmSegments);
        AddLOD(buildingObject);

        return buildingObject;
    }

    private void AddLOD(GameObject building) {
        LODGroup lodGroup = building.AddComponent<LODGroup>();
        LOD[] lods = new LOD[1];
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        lods[0] = new LOD(0.025f, renderers);
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
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
