using System.Collections.Generic;
using UnityEngine;
using Utils.LSystems;

public class RepeatWindowManhattanSegmentsGenerator  : MonoBehaviour, IManhattanWallSegmentsGenerator {
    private LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData> lSystem;

    public void Init(Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentData) {
        lSystem = new LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData>();

        var windowWidth = segmentData[ManhattanWallSegmentType.Window].width;
        var cornerWidth = segmentData[ManhattanWallSegmentType.Corner].width;

        lSystem.ShouldContinue(value => value.widthLeft > 0);

        lSystem.CreateRules(ManhattanWallSegmentType.Corner)
            .Add(1.0f, ManhattanWallSegmentType.Window)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - cornerWidth));

        lSystem.CreateRules(ManhattanWallSegmentType.Window)
            .Add(1.0f, ManhattanWallSegmentType.Window)
            .ShouldAccept(value => value.widthLeft >= windowWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - windowWidth));
    }

    public List<ManhattanWallSegmentType> Generate(ManhattanSegmentsGeneratorData data) {
        var list = lSystem.Run(ManhattanWallSegmentType.Corner, data);
        list.Add(ManhattanWallSegmentType.EndCorner);
        return list;
    }
}
