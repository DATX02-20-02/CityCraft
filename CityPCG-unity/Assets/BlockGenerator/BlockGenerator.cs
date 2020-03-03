using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClipperLib;

// using Path = List<IntPoint>;
// using Paths = List<List<IntPoint>>;

public class BlockGenerator : MonoBehaviour
{
    [Range(-2, 2)]
    public float offset = 0.2f;

    [Range(0, (int)1E5)]
    public int scale = 1024;

    [SerializeField]
    List<Vector2> polygon = new List<Vector2>() {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 1),
    };

    // Start is called before the first frame update
    void Start()
    {

    }

    public static Vector2 Vector3To2(Vector3 vec) {
        return new Vector2(vec.x, vec.z);
    }

    public static Vector3 Vector2To3(Vector2 vec) {
        return new Vector3(vec.x, 0, vec.y);
    }

    public static Vector2 IntPointToVector2(IntPoint point) {
        return new Vector2(point.X, point.Y);
    }

    // Update is called once per frame
    void Update()
    {
        List<IntPoint> s = new List<IntPoint>();
        foreach (Vector2 vec in polygon) {
            Vector2 scaled = vec * scale;
            s.Add(new IntPoint((int) scaled.x, (int) scaled.y));
        }

        List<List<IntPoint>> polygons = Clipper.SimplifyPolygon(
            s,
            PolyFillType.pftEvenOdd
        );

        foreach (List<IntPoint> simplePoly in polygons) {
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset co = new ClipperOffset();
            co.AddPath(simplePoly, JoinType.jtRound, EndType.etClosedPolygon);
            co.Execute(ref solution, offset * scale);

            for (int i = 0; i < simplePoly.Count; i++) {
                Vector2 p1 = IntPointToVector2(simplePoly[i]) / scale;
                Vector2 p2 = IntPointToVector2(simplePoly[(i + 1) % simplePoly.Count]) / scale;

                Debug.DrawLine(Vector2To3(p1), Vector2To3(p2), new Color(0, 1, 0));
            }

            foreach (List<IntPoint> poly in solution) {
                for (int i = 0; i < poly.Count; i++) {
                    Vector2 p1 = IntPointToVector2(poly[i]) / scale;
                    Vector2 p2 = IntPointToVector2(poly[(i + 1) % poly.Count]) / scale;

                    Debug.DrawLine(Vector2To3(p1), Vector2To3(p2), new Color(1, 0, 0));
                }
            }
        }
    }
}
