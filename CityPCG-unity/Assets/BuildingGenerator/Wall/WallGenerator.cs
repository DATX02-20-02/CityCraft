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
                    output.Add(firstFloorSegmentSystem.Run(WallSegmentType.Corner));
                    break;
                case FloorType.Normal:
                    output.Add(normalSegmentSystem.Run(WallSegmentType.Corner));
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

        Debug.Log(segments);
    }

    private void Update() {
        for (var i = 0; i < segments.Count; i++) {
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

    private LSystem<WallSegmentType> GetNormalSegmentSystem() {
        var normalSegmentSystem = new LSystem<WallSegmentType>();

        normalSegmentSystem.CreateRules(WallSegmentType.Corner)
            .Add(0.5f, WallSegmentType.Wall)
            .Add(0.5f, WallSegmentType.Window);

        normalSegmentSystem.CreateRules(WallSegmentType.Window)
            .Add(0.4f, WallSegmentType.Wall)
            .Add(0.4f, WallSegmentType.Window)
            .Add(0.2f, WallSegmentType.EndCorner);

        normalSegmentSystem.CreateRules(WallSegmentType.Wall)
            .Add(0.4f, WallSegmentType.Wall)
            .Add(0.4f, WallSegmentType.Window)
            .Add(0.2f, WallSegmentType.EndCorner);

        normalSegmentSystem.CreateRules(WallSegmentType.EndCorner);

        return normalSegmentSystem;
    }

    private LSystem<WallSegmentType> GetFirstFloorSegmentSystem() {
        var firstFloorSegmentSystem = new LSystem<WallSegmentType>();

        firstFloorSegmentSystem.CreateRules(WallSegmentType.Corner)
            .Add(1.0f, WallSegmentType.ShopWindow);

        firstFloorSegmentSystem.CreateRules(WallSegmentType.ShopWindow)
            .Add(0.4f, WallSegmentType.Wall)
            .Add(0.4f, WallSegmentType.ShopWindow)
            .Add(0.2f, WallSegmentType.Door);

        firstFloorSegmentSystem.CreateRules(WallSegmentType.Wall)
            .Add(0.4f, WallSegmentType.Wall)
            .Add(0.4f, WallSegmentType.ShopWindow)
            .Add(0.2f, WallSegmentType.Door);

        firstFloorSegmentSystem.CreateRules(WallSegmentType.Door)
            .Add(1.0f, WallSegmentType.EndCorner);

        firstFloorSegmentSystem.CreateRules(WallSegmentType.EndCorner);

        return firstFloorSegmentSystem;
    }

}
