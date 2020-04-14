using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class ManhattanBuildingWallGenerator {

    private float wallSegmentHeightMeter;
    private List<ManhattanFloorType> floorTypes;
    private Dictionary<ManhattanFloorType, IManhattanWallSegmentsGenerator> floorToSegmentGenerator;
    private Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentToData;
    private GameObject buildingObject;

    public ManhattanBuildingWallGenerator(float wallSegmentHeightMeter, List<ManhattanFloorType> floorTypes,
        Dictionary<ManhattanFloorType, IManhattanWallSegmentsGenerator> floorToSegmentGenerator,
        Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentToData, GameObject buildingObject) {
        this.wallSegmentHeightMeter = wallSegmentHeightMeter;
        this.floorTypes = floorTypes;
        this.floorToSegmentGenerator = floorToSegmentGenerator;
        this.segmentToData = segmentToData;
        this.buildingObject = buildingObject;
    }

    public void Generate(Vector2 start, Vector2 end) {
        var wallObject = new GameObject("Wall");
        wallObject.transform.parent = buildingObject.transform;
        var segments = new List<List<ManhattanWallSegmentType>>();

        foreach (var floorType in floorTypes) {
            switch (floorType) {
                case ManhattanFloorType.First:
                    var firstGenerator = floorToSegmentGenerator[ManhattanFloorType.First];
                    segments.Add(firstGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    break;
                case ManhattanFloorType.Normal:
                    var normalGenerator = floorToSegmentGenerator[ManhattanFloorType.Normal];
                    segments.Add(normalGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var start3 = new Vector3(start.x, 0, start.y);
        var end3 = new Vector3(end.x, 0, end.y);
        var dir3 = start3 - end3;
        var face = Vector3.Cross(dir3, Vector3.up).normalized;

        var y = 0;
        foreach (var floorSegments in segments) {
            //The size won't be exact what is specified.
            var totalSpecifiedWidth = floorSegments.Aggregate(0f, (prod, next) => prod + segmentToData[next].width);

            var x = 0f;
            var u = new Vector3(0, wallSegmentHeightMeter, 0);

            var floorObject = new GameObject("floor");

            foreach (var wallSegmentType in floorSegments) {
                var segmentData = segmentToData[wallSegmentType];
                var obj = Object.Instantiate(segmentToData[wallSegmentType].wallSegmentObject, floorObject.transform);
                obj.transform.position = new Vector3(x + segmentData.width / 2, 0, 0);
                x += segmentData.width;
                obj.transform.rotation = Quaternion.LookRotation(Vector3.up);
                obj.transform.localScale = new Vector3(segmentData.width / 10, 1, wallSegmentHeightMeter / 10);
            }

            MeshCombiner.Combine(floorObject);

            var floorPosition = u * (y + 0.5f);
            floorObject.transform.parent = wallObject.transform;
            floorObject.transform.rotation = Quaternion.identity;
            floorObject.transform.position = floorPosition;
            floorObject.transform.localScale = new Vector3(dir3.magnitude / totalSpecifiedWidth, 1, 1);


            y++;
        }

        MeshCombiner.Combine(wallObject);

        wallObject.transform.position = end3;
        wallObject.transform.rotation = Quaternion.LookRotation(face);
    }
}
