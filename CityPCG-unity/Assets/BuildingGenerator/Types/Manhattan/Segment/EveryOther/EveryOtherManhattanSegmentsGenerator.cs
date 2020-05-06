using System.Collections.Generic;
using UnityEngine;
using Utils.LSystems;
using static ManhattanWallSegmentType;

public class EveryOtherManhattanSegmentsGenerator : MonoBehaviour, IManhattanWallSegmentsGenerator {

    private LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData> lSystem;

    public void Init(Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentData) {
        lSystem = new LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData>();

        var windowWidth = segmentData[Window].width;
        var cornerWidth = segmentData[Corner].width;
        var wallWidth = segmentData[Wall].width;

        lSystem.ShouldContinue(value => value.widthLeft > 0);

        lSystem.CreateRules(Corner)
            .Add(1.0f, Window)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - cornerWidth));

        lSystem.CreateRules(Window)
            .Add(1.0f, Wall)
            .ShouldAccept(value => value.widthLeft >= windowWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - windowWidth));

        lSystem.CreateRules(Wall)
            .Add(1.0f, Window)
            .ShouldAccept(value => value.widthLeft >= windowWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - wallWidth));
    }

    public List<ManhattanWallSegmentType> Generate(ManhattanSegmentsGeneratorData data) {
        var possibleStarts = new[] { Corner, Wall };
        var index = Random.Range(0, possibleStarts.Length);

        var list = lSystem.Run(new List<ManhattanWallSegmentType>() { Corner, possibleStarts[index] }, data);
        list.Add(EndCorner);
        return list;
    }
}
