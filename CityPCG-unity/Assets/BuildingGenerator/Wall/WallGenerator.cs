using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Utils.LSystems;

public class WallGenerator : MonoBehaviour {

    public List<FloorType> floorsType;

    private List<List<WallSegmentType>> segments;


    public List<List<WallSegmentType>> Generate(List<FloorType> input) {
        var output = new List<List<WallSegmentType>>();

        var normalSegmentSystem = GetNormalSegmentSystem();
        var firstFloorSegmentSystem = GetFirstFloorSegmentSystem();

        foreach (var floorType in input) {
            switch (floorType) {
                case FloorType.First:
                    var firstSegments = new List<WallSegmentType>();

                    var halfFirstResult = firstFloorSegmentSystem.Run(WallSegmentType.Corner, new WallData(1.5f));
                    firstSegments.AddRange(halfFirstResult);

                    //Other half
                    halfFirstResult.Add(WallSegmentType.Door);
                    halfFirstResult.Reverse();
                    firstSegments.AddRange(halfFirstResult);

                    output.Add(firstSegments);
                    break;
                case FloorType.Normal:
                    var normalSegments = new List<WallSegmentType>();

                    var halfNormalResult = normalSegmentSystem.Run(WallSegmentType.Corner, new WallData(1.5f));
                    normalSegments.AddRange(halfNormalResult);

                    //Other half
                    halfNormalResult.Reverse();
                    normalSegments.AddRange(halfNormalResult);

                    output.Add(normalSegments);
                    break;
                case FloorType.Roof:
                    output.Add(new List<WallSegmentType>() { WallSegmentType.Roof });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return output;
    }

    private void Start() {
        segments = Generate(floorsType);
    }

    private void Update() {
        for (var i = 0; i < segments?.Count; i++) {
            DrawFloor(segments[i], new Vector2(0, i));
        }
    }

    private static void DrawFloor(List<WallSegmentType> segs, Vector2 floor) {
        var size = 1.0f / segs.Count;
        for (var i = 0; i < segs.Count; i++) {
            var position = new Rect(floor + new Vector2(i * size, 0), new Vector2(size, 1.0f));
            Color color;
            switch (segs[i]) {
                case WallSegmentType.Corner:
                    color = Color.black;
                    break;
                case WallSegmentType.Wall:
                    color = Color.blue;
                    break;
                case WallSegmentType.Window:
                    color = Color.red;
                    break;
                case WallSegmentType.EndCorner:
                    color = Color.cyan;
                    break;
                case WallSegmentType.Roof:
                    color = Color.yellow;
                    break;
                case WallSegmentType.ShopWindow:
                    color = Color.magenta;
                    break;
                case WallSegmentType.Door:
                    color = Color.green;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            DrawSegment(position, color);
        }
    }

    private static void DrawSegment(Rect rect, Color color) {
        var p = rect.position;
        var s = rect.size;
        var w = new Vector2(s.x, 0);
        var h = new Vector2(0, s.y);

        var lines = new List<Line>() {
            new Line(p, p + w),
            new Line(p + w, p + s),
            new Line(p + s, p + h),
            new Line(p + h, p),
            new Line(p, p + s),
            new Line(p + h, p + w)
        };

        foreach (var line in lines) {
            DrawLine(line, color);
        }
    }

    private static void DrawLine(Line line, Color color) {
        Debug.DrawLine(new Vector3(line.start.x, line.start.y, 0), new Vector3(line.end.x, line.end.y, 0), color);
    }

    private struct Line {
        public Vector2 start;
        public Vector2 end;

        public Line(Vector2 start, Vector2 end) {
            this.start = start;
            this.end = end;
        }
    }

    private class WallData {
        public readonly float widthLeft;

        public WallData(float widthLeft) {
            this.widthLeft = widthLeft;
        }
    }

    private LSystem<WallSegmentType, WallData> GetNormalSegmentSystem() {
        var normalSegmentSystem = new LSystem<WallSegmentType, WallData>();

        const float cornerWidth = 0.5f;
        const float windowWidth = 0.5f;
        const float wallWidth = 0.5f;

        normalSegmentSystem.ShouldContinue(value => value.widthLeft > 0);

        normalSegmentSystem.CreateRules(WallSegmentType.Corner)
            .Add(0.5f, WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.Window)
            .OnAccepted(value => new WallData(value.widthLeft - cornerWidth));

        normalSegmentSystem.CreateRules(WallSegmentType.Window)
            .Add(0.5f, WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.Window)
            .ShouldAccept(value => value.widthLeft >= windowWidth)
            .OnAccepted(value => new WallData(value.widthLeft - windowWidth));

        normalSegmentSystem.CreateRules(WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.Window)
            .ShouldAccept(value => value.widthLeft >= wallWidth)
            .OnAccepted(value => new WallData(value.widthLeft - wallWidth));

        return normalSegmentSystem;
    }

    private LSystem<WallSegmentType, WallData> GetFirstFloorSegmentSystem() {
        var firstFloorSegmentSystem = new LSystem<WallSegmentType, WallData>();

        const float cornerWidth = 0.5f;
        const float shopWindowWidth = 1.5f;
        const float wallWidth = 0.5f;

        firstFloorSegmentSystem.CreateRules(WallSegmentType.Corner)
            .Add(1.0f, WallSegmentType.ShopWindow)
            .OnAccepted(value => new WallData(value.widthLeft - cornerWidth));

        firstFloorSegmentSystem.CreateRules(WallSegmentType.ShopWindow)
            .Add(0.5f, WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.ShopWindow)
            .ShouldAccept(value => value.widthLeft >= shopWindowWidth)
            .OnAccepted(value => new WallData(value.widthLeft - shopWindowWidth));

        firstFloorSegmentSystem.CreateRules(WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.ShopWindow)
            .ShouldAccept(value => value.widthLeft > wallWidth)
            .OnAccepted(value => new WallData(value.widthLeft - wallWidth));


        return firstFloorSegmentSystem;
    }

}
