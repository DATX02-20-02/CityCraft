using System.Collections.Generic;
using UnityEngine;
using Utils.LSystems;

public class NormalManhattanSegmentsGenerator : MonoBehaviour, IManhattanWallSegmentsGenerator {

    private LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData> lSystem;

    public void Init(Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentData) {
        lSystem = new LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData>();

        var cornerWidth = segmentData[ManhattanWallSegmentType.Corner].width;
        var windowWidth = segmentData[ManhattanWallSegmentType.Window].width;
        var wallWidth = segmentData[ManhattanWallSegmentType.Wall].width;

        lSystem.ShouldContinue(value => value.widthLeft > 0);

        lSystem.CreateRules(ManhattanWallSegmentType.Corner)
            .Add(0.5f, ManhattanWallSegmentType.Wall)
            .Add(0.5f, ManhattanWallSegmentType.Window)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - cornerWidth));

        lSystem.CreateRules(ManhattanWallSegmentType.Window)
            .Add(0.5f, ManhattanWallSegmentType.Wall)
            .Add(0.5f, ManhattanWallSegmentType.Window)
            .ShouldAccept(value => value.widthLeft >= windowWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - windowWidth));

        lSystem.CreateRules(ManhattanWallSegmentType.Wall)
            .Add(0.5f, ManhattanWallSegmentType.Wall)
            .Add(0.5f, ManhattanWallSegmentType.Window)
            .ShouldAccept(value => value.widthLeft >= wallWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - wallWidth));
    }

    public List<ManhattanWallSegmentType> Generate(ManhattanSegmentsGeneratorData data) {
        var list = lSystem.Run(ManhattanWallSegmentType.Corner, new ManhattanSegmentsGeneratorData(data.widthLeft / 2));
        var newList = new List<ManhattanWallSegmentType>(list);
        newList.RemoveAt(0);
        newList.Reverse();
        list.AddRange(newList);
        list.Add(ManhattanWallSegmentType.EndCorner);
        return list;
    }
}

