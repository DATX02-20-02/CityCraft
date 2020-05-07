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

        var lod0 = GenerateLOD0(plot, buildingObject, floorTypes);
        var lod1 = GenerateLOD1(plot, buildingObject, floorTypes);
        SetupLOD(buildingObject, lod0, lod1);

        return buildingObject;
    }

    private GameObject GenerateLOD0(Plot plot, GameObject building, List<ManhattanFloorType> floorTypes) {
        var lod0 = new GameObject("LOD 0");
        lod0.transform.parent = building.transform;

        var floorToSegmentGeneratorDict = segmentGenerators.ToDictionary(sg => sg.floorType, sg => sg.segmentGenerator.GetComponent<IManhattanWallSegmentsGenerator>());
        var segmentToDataDict = segmentToData.ToDictionary(sto => sto.segmentType, sto => sto.data);

        var wallGenerator = new ManhattanBuildingWallGenerator(wallSegmentHeightMeter, floorTypes, floorToSegmentGeneratorDict, segmentToDataDict, lod0);

        foreach (var sg in floorToSegmentGeneratorDict.Values) {
            sg.Init(segmentToDataDict);
        }

        var zero = VectorUtil.Vector3To2(plot.vertices[0]);
        var relativeVertices = plot.vertices.ConvertAll(v => VectorUtil.Vector3To2(v) - zero);

        var ttmSegments = new List<TemporaryTransformedMesh>();

        for (var i = relativeVertices.Count - 1; i >= 0; i--) {
            var cur = relativeVertices[i];
            var next = relativeVertices[i == 0 ? relativeVertices.Count - 1 : (i - 1)];
            wallGenerator.Generate(cur, next, lod0);

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
        ttmSegments.Add(ManhattanBuildingBasementGenerator.Generate(relativeVertices, basementMaterial, biggestYDifference));

        MeshCombiner.Combine(lod0, ttmSegments);

        return lod0;
    }

    private GameObject GenerateLOD1(Plot plot, GameObject building, List<ManhattanFloorType> floorTypes) {
        var lod1 = new GameObject("LOD 1");
        lod1.transform.parent = building.transform;

        var zero = VectorUtil.Vector3To2(plot.vertices[0]);
        var relativeVertices = plot.vertices.ConvertAll(v => VectorUtil.Vector3To2(v) - zero);
        float height = wallSegmentHeightMeter * floorTypes.Count;

        var ttmSegments = new List<TemporaryTransformedMesh>();
        ttmSegments.Add(ManhattanBuildingRoofGenerator.Generate(relativeVertices, roofMaterial, height));

        var basement = ManhattanBuildingBasementGenerator.Generate(relativeVertices, basementMaterial, wallSegmentHeightMeter * floorTypes.Count);
        ttmSegments.Add(new TemporaryTransformedMesh(Matrix4x4.Translate(height * Vector3.up), basement.gameObject));

        MeshCombiner.Combine(lod1, ttmSegments);

        return lod1;
    }

    private void SetupLOD(GameObject building, GameObject lod0, GameObject lod1) {
        LODGroup lodGroup = building.AddComponent<LODGroup>();
        LOD[] lods = new LOD[2];

        Renderer[] renderers = lod0.GetComponentsInChildren<Renderer>();
        lods[0] = new LOD(0.04f, renderers);

        renderers = lod1.GetComponentsInChildren<Renderer>();
        lods[1] = new LOD(0.015f, renderers);

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
