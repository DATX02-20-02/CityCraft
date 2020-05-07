using System.Collections.Generic;
using UnityEngine;
using Utils.LSystems;

public class FirstManhattanSegmentsGenerator : MonoBehaviour, IManhattanWallSegmentsGenerator {

    private LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData> lSystem;

    public void Init(Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentData) {
        lSystem = new LSystem<ManhattanWallSegmentType, ManhattanSegmentsGeneratorData>();

        var cornerWidth = segmentData[ManhattanWallSegmentType.Corner].width;
        var shopWindowWidth = segmentData[ManhattanWallSegmentType.ShopWindow].width;
        var wallWidth = segmentData[ManhattanWallSegmentType.Wall].width;
        var doorWidth = segmentData[ManhattanWallSegmentType.Door].width;

        lSystem.CreateRules(ManhattanWallSegmentType.Corner)
            .Add(1.0f, ManhattanWallSegmentType.ShopWindow)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - cornerWidth));

        lSystem.CreateRules(ManhattanWallSegmentType.ShopWindow)
            .Add(0.1f, ManhattanWallSegmentType.Wall)
            .Add(0.1f, ManhattanWallSegmentType.ShopWindow)
            .Add(0.8f, ManhattanWallSegmentType.Door)
            .ShouldAccept(value => value.widthLeft >= shopWindowWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - shopWindowWidth));

        lSystem.CreateRules(ManhattanWallSegmentType.Wall)
            .Add(0.35f, ManhattanWallSegmentType.Wall)
            .Add(0.35f, ManhattanWallSegmentType.ShopWindow)
            .Add(0.3f, ManhattanWallSegmentType.Door)
            .ShouldAccept(value => value.widthLeft >= wallWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - wallWidth));

        lSystem.CreateRules(ManhattanWallSegmentType.Door)
            .Add(0.5f, ManhattanWallSegmentType.Wall)
            .Add(0.5f, ManhattanWallSegmentType.ShopWindow)
            .ShouldAccept(value => value.widthLeft >= doorWidth)
            .OnAccepted(value => new ManhattanSegmentsGeneratorData(value.widthLeft - doorWidth));
    }

    public List<ManhattanWallSegmentType> Generate(ManhattanSegmentsGeneratorData data) {
        var list = lSystem.Run(ManhattanWallSegmentType.Corner, data);
        list.Add(ManhattanWallSegmentType.EndCorner);
        return list;
    }

}
