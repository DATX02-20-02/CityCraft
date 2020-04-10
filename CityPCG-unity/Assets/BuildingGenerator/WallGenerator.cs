using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Utils.LSystems;
using static UnityEngine.Vector3;

public class WallGenerator : MonoBehaviour {
    /*
    public List<FloorType> floorsType;
    public List<SegmentObject> segmentObjects;

    public float wallWidthMeter;
    public float wallSegmentHeightMeter;

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

        var y = 0;
        foreach (var floorSegments in segments) {
            var x = 0;
            var wallSegmentWidthMeter = wallWidthMeter / floorSegments.Count;
            var r = new Vector3(wallSegmentWidthMeter, 0, 0);
            var u = new Vector3(0, wallSegmentHeightMeter, 0);
            foreach (var wallSegmentType in floorSegments) {
                var obj = Instantiate(segmentObjects.Find(s => s.segmentType == wallSegmentType).gameObject);
                obj.transform.position += (u * (y + 0.5f) + r * (x + 0.5f));
                obj.transform.rotation = transform.rotation;
                obj.transform.localScale = new Vector3(wallSegmentWidthMeter / 10, 1, wallSegmentHeightMeter / 10);
                x++;
            }

            y++;
        }
    }

    private void Update() {
        for (var i = 0; i < segments?.Count; i++) {
            DrawFloor(segments[i], new Vector2(0, i * wallSegmentHeightMeter), wallWidthMeter, wallSegmentHeightMeter);
        }
    }

    private static void DrawFloor(List<WallSegmentType> segs, Vector2 floor, float wallWidthMeter, float wallSegmentHeightMeter) {
        var wallSegmentWidthMeter = wallWidthMeter / segs.Count;
        for (var i = 0; i < segs.Count; i++) {
            var position = new Rect(floor + new Vector2(i * wallSegmentWidthMeter, 0), new Vector2(wallSegmentWidthMeter, wallSegmentHeightMeter));
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
*/
}
