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

    public List<TemporaryTransformedMesh> Generate(Vector2 start, Vector2 end) {
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
                case ManhattanFloorType.EveryOther:
                    var everyOtherGenerator = floorToSegmentGenerator[ManhattanFloorType.EveryOther];
                    segments.Add(everyOtherGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
                    break;
                case ManhattanFloorType.RepeatWindow:
                    var repeatWindowGenerator = floorToSegmentGenerator[ManhattanFloorType.RepeatWindow];
                    segments.Add(repeatWindowGenerator.Generate(new ManhattanSegmentsGeneratorData(Vector2.Distance(start, end))));
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

                var lookRotation = Quaternion.LookRotation(face);
                var lookRotationMat =
                    Equals(lookRotation, zero) ? Matrix4x4.identity : Matrix4x4.Rotate(lookRotation);

                var transform = Matrix4x4.Translate(end3) * lookRotationMat *
                                Matrix4x4.Rotate(segmentRotation) * Matrix4x4.Translate(segmentPosition) *
                                Matrix4x4.Scale(segmentLocalScale);

                ttmSegments.Add(new TemporaryTransformedMesh(transform, segmentData.wallSegmentObject));
            }

            y++;
        }

        return ttmSegments;
    }
}
