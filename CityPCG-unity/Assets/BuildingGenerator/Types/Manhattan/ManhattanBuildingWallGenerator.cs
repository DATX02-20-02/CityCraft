using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static UnityEngine.Vector3;
using Debug = UnityEngine.Debug;
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

    public List<TemporaryTransformedMesh> Generate(Vector2 start, Vector2 end, GameObject parent) {
        var everyOtherGenerated = new List<ManhattanWallSegmentType>();
        var normalGenerated = new List<ManhattanWallSegmentType>();
        var repeatWindowGenerated = new List<ManhattanWallSegmentType>();

        var segments = new List<List<ManhattanWallSegmentType>>();
        var wallObject = new GameObject("Wall");
        wallObject.transform.parent = parent.transform;

        foreach (var floorType in floorTypes) {
            switch (floorType) {
                case ManhattanFloorType.First:
                    var firstGenerator = floorToSegmentGenerator[ManhattanFloorType.First];
                    segments.Add(firstGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    break;
                case ManhattanFloorType.Normal:
                    var normalGenerator = floorToSegmentGenerator[ManhattanFloorType.Normal];
                    if (normalGenerated.Count == 0) {
                        normalGenerated.AddRange(normalGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    }
                    segments.Add(normalGenerated);
                    break;
                case ManhattanFloorType.EveryOther:
                    var everyOtherGenerator = floorToSegmentGenerator[ManhattanFloorType.EveryOther];
                    if (everyOtherGenerated.Count == 0) {
                        everyOtherGenerated.AddRange(everyOtherGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    }
                    segments.Add(everyOtherGenerated);
                    break;
                case ManhattanFloorType.RepeatWindow:
                    var repeatWindowGenerator = floorToSegmentGenerator[ManhattanFloorType.RepeatWindow];
                    if (repeatWindowGenerated.Count == 0) {
                        repeatWindowGenerated.AddRange(repeatWindowGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    }
                    segments.Add(repeatWindowGenerated);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var start3 = new Vector3(start.x, 0, start.y);
        var end3 = new Vector3(end.x, 0, end.y);
        var dir3 = start3 - end3;
        var face = Cross(dir3, up).normalized;

        var ttmSegments = new List<TemporaryTransformedMesh>();

        var length = Vector2.Distance(start, end);

        var y = 0;
        foreach (var floorSegments in segments) {
            //The size won't be exact what is specified.
            var totalSpecifiedWidth = floorSegments.Aggregate(0f, (prod, next) => prod + segmentToData[next].width);

            var x = 0f;
            var u = new Vector3(0, 0, wallSegmentHeightMeter);

            var floorPosition = u * (y + 0.5f);

            var scl = length / totalSpecifiedWidth;

            foreach (var wallSegmentType in floorSegments) {
                var segmentData = segmentToData[wallSegmentType];
                var segmentPosition = new Vector3(x + (segmentData.width * scl) / 2, 0, 0) + floorPosition;
                x += segmentData.width * scl;
                var segmentRotation = Quaternion.LookRotation(up);
                var segmentLocalScale = new Vector3((segmentData.width / 10) * scl, 1, wallSegmentHeightMeter / 10);

                var transform = Matrix4x4.Rotate(segmentRotation) * Matrix4x4.Translate(segmentPosition) *
                                Matrix4x4.Scale(segmentLocalScale);

                ttmSegments.Add(new TemporaryTransformedMesh(transform, segmentData.wallSegmentObject));
            }

            y++;
        }

        wallObject.transform.position = end3;
        wallObject.transform.rotation = Quaternion.LookRotation(face);
        MeshCombiner.Combine(wallObject, ttmSegments);


        return ttmSegments;
    }
}
