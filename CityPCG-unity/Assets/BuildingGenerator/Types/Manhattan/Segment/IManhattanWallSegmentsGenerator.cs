using System;
using System.Collections.Generic;

public interface IManhattanWallSegmentsGenerator {
    void Init(Dictionary<ManhattanWallSegmentType, ManhattanSegmentData> segmentData);
    List<ManhattanWallSegmentType> Generate(ManhattanSegmentsGeneratorData data);
}
