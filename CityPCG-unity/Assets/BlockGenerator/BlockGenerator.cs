using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ClipperLib;

/*
  What? Generates insetted city block areas from a road network.
  Why? These city blocks are suitable to place structures in, and leave room for road meshes.
  How?
  1. Extracts polygons from road network graph by spawning "turtles" that always turn right.
  2. Attempts to expand polygons from streets.
  3. Insets all found polygons and returns them.
*/
public class BlockGenerator : MonoBehaviour {
    [Range(0, 2)]
    public float inset = 0.05f;

    [Range(0, (int)1E5)]
    public int scale = 1024;

    [SerializeField]
    List<Vector2> polygon = new List<Vector2>() {
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(0.5f, 1),
        new Vector2(0.5f, 0.5f),
        new Vector2(0.2f, 0.2f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 1),
        new Vector2(1, 1),
        new Vector2(1, -1),
        new Vector2(0.48f, -1),
        new Vector2(0.51f, -0.8f),
        new Vector2(0.35f, -0.52f),
        new Vector2(0.51f, -0.8f),
        new Vector2(0.64f, -0.67f),
        new Vector2(0.51f, -0.8f),
        new Vector2(0.48f, -1),
        new Vector2(0, -1),
    };

    [SerializeField] private float minBlockArea = 0.0f;
    [SerializeField] private float maxBlockArea = 0.0f;
    [SerializeField] private bool debug = false;
    [SerializeField] private int debugBlock = 0;

    private RoadNetwork roadNetwork;
    private List<Block> blocks;
    private List<Block> insetBlocks;


    // Entrypoint to the generator.
    public List<Block> Generate(RoadNetwork roadNetwork) {
        this.roadNetwork = roadNetwork;
        this.blocks = new List<Block>();
        this.insetBlocks = new List<Block>();

        List<Block> blocks = ExtractPolygons();

        foreach (Block block in blocks) {
            List<Block> newBlocks = InsetBlock(block, this.inset);

            foreach (Block newBlock in newBlocks) {
                float area = BlockArea(newBlock);
                if (minBlockArea <= area && area <= maxBlockArea)
                    this.insetBlocks.Add(newBlock);
            }
        }

        return this.insetBlocks;
    }

    private List<Block> ExtractPolygons() {
        var nodes = roadNetwork.Nodes;

        // Track traversed edges in order to avoid duplications.
        // key: (from, to)
        // Not a good idea since they contain floats
        // TODO: Would be better if each node had UUID e.g. index.
        var traversed = new HashSet<Tuple<Vector3, Vector3>>();

        // Spawn a turtle at every node, for each of its edges.
        foreach (var node in nodes) {
            foreach (var edge in node.connections) {
                var vertices = SpawnTurtle(traversed, node, edge);

                // Ignore empty city blocks.
                if (vertices.Count > 0) {
                    var b = new Block(vertices);
                    float area = BlockArea(b);
                    if (minBlockArea <= area && area <= maxBlockArea)
                        blocks.Add(b);
                }
            }
        }

        return blocks;
    }

    private List<Vector3> SpawnTurtle(HashSet<Tuple<Vector3, Vector3>> traversed, Node node, NodeConnection startEdge) {
        var vertices = new List<Vector3>();
        var curNode = node;
        var nextEdge = startEdge;

        // A bad loop occurs when return to the same node through the same edge.
        bool badloop = false;

        // Simulate turtle until we make a loop, or find another turtle's path.
        while (!traversed.Contains(Tuple.Create(curNode.pos, nextEdge.node.pos))) {

            // Traverse the next edge.
            var curDir = nextEdge.node.pos - curNode.pos;
            traversed.Add(Tuple.Create(curNode.pos, nextEdge.node.pos));
            curNode = nextEdge.node;
            vertices.Add(curNode.pos);

            // Calculate relative angles from current heading direction to other edges
            var options = curNode.connections.Select(c => c.node.pos - curNode.pos).ToList();
            var rightmostIndex = RightmostDirection(curDir, options);
            nextEdge = curNode.connections[rightmostIndex];

            // Track if we return through the same edge
            if (node.pos == nextEdge.node.pos && startEdge.node.pos == curNode.pos) {
                badloop = true;
                break;
            }

        }

        return badloop ? new List<Vector3>() : vertices;
    }

    // Index of the direction option closest to the right of the reference.
    private int RightmostDirection(Vector3 reference, List<Vector3> options) {
        int rightmostIndex = 0;
        float rightmostAngle = -180.0f;

        for (int i = 0; i < options.Count; i++) {
            float angle = Vector3.SignedAngle(reference, options[i], Vector3.up);
            // NOTE: "< 180.f" ensures that we prioritize going backwards the least.
            if (Mathf.Abs(angle) < 180.0f && rightmostAngle < angle) {
                rightmostIndex = i;
                rightmostAngle = angle;
            }
        }

        return rightmostIndex;
    }

    private float BlockArea(Block block) {
        var vs = block.vertices;

        float area = 0.0f;
        for (int i = 0; i < vs.Count; i++)
            area += vs[i].x * (vs[(i + 1) % vs.Count].z - vs[(i - 1 + vs.Count) % vs.Count].z);

        return Mathf.Abs(area / 2.0f);
    }

    private List<Block> InsetBlock(Block block, float inset) {
        List<Vector3> vertices = block.vertices;
        List<List<Vector3>> segments = new List<List<Vector3>>();
        HashSet<Vector3> visited = new HashSet<Vector3>();

        List<int> overlaps = new List<int>();
        Vector3 last = Vector3.zero;

        for (int i = 0; i < vertices.Count; i++) {
            Vector3 vec = vertices[i];

            if (visited.Contains(vec)) overlaps.Add(i);
            else visited.Add(vec);

            if (debug) {
                Vector3 end = vertices[(i + 1) % vertices.Count];

                Debug.DrawLine(vec, end, new Color(1, 1, 1));
            }
        }

        if (overlaps.Count > 0) {
            int currOverlapIndex = overlaps.Count - 1;
            int firstIndex = overlaps[currOverlapIndex];
            Vector3 currOverlap = vertices[firstIndex];

            List<Vector3> path = new List<Vector3>();;
            for (int i = firstIndex; i >= 0; i--) {
                Vector3 vec = vertices[i];

                path.Add(vec);

                if (i != firstIndex && Vector3.Equals(vec, currOverlap)) {
                    if (path.Count == 2) {
                        path.RemoveAt(path.Count - 1);
                        segments.Add(path);
                    }
                    else {
                        segments.Add(path);
                    }
                    path = new List<Vector3>();

                    for (int j = currOverlapIndex; j >= 0; j--) {
                        if (overlaps[j] < i) {
                            i = overlaps[j];
                            firstIndex = i;
                            currOverlapIndex = j;
                            currOverlap = vertices[i];
                            path.Add(currOverlap);
                            break;
                        }
                    }
                }
            }
        }

        // Create offsetted line solutions
        List<List<List<IntPoint>>> segmentSolutions = new List<List<List<IntPoint>>>();
        foreach (List<Vector3> path in segments) {
            for (int i = 0; i < path.Count - 1; i++) {
                Vector3 p1 = path[i];
                Vector3 p2 = path[i + 1];

                Debug.DrawLine(p1 + Vector3.up * (0.01f * i), p2 + Vector3.up * (0.01f * (i + 1)),
                               new Color(1, 1, 0));
            }

            List<IntPoint> linePoly = new List<IntPoint>();
            foreach (Vector3 vec in path) {
                Vector2 scaled = VectorUtil.Vector3To2(vec) * scale;
                linePoly.Add(new IntPoint((int) scaled.x, (int) scaled.y));
            }

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset co = new ClipperOffset();
            co.AddPath(linePoly, JoinType.jtSquare, EndType.etOpenSquare);
            co.Execute(ref solution, inset * scale);

            // Add all offsetted polygons to a list
            segmentSolutions.Add(solution);

            if (debug) {
                foreach (List<IntPoint> poly in solution) {
                    for (int i = 0; i < poly.Count; i++) {
                        Vector2 p1 = VectorUtil.IntPointToVector2(poly[i]) / scale;
                        Vector2 p2 = VectorUtil.IntPointToVector2(poly[(i + 1) % poly.Count]) / scale;

                        Debug.DrawLine(VectorUtil.Vector2To3(p1), VectorUtil.Vector2To3(p2), new Color(1, 0, 0));
                    }
                }
            }
        }

        // Get compatible list with original block vertices
        List<IntPoint> s = new List<IntPoint>();
        foreach (Vector3 vec in vertices) {
            Vector2 scaled = VectorUtil.Vector3To2(vec) * scale;
            s.Add(new IntPoint((int) scaled.x, (int) scaled.y));
        }

        List<List<IntPoint>> polygons = Clipper.SimplifyPolygon(
            s,
            PolyFillType.pftEvenOdd
        );

        List<Block> finalBlocks = new List<Block>();

        // Perform subject offset and line segment difference operations
        foreach (List<IntPoint> simplePoly in polygons) {
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset co = new ClipperOffset();
            co.AddPath(simplePoly, JoinType.jtSquare, EndType.etClosedPolygon);
            co.Execute(ref solution, -inset * scale);

            if (solution.Count > 0) {
                List<IntPoint> poly = solution[0];

                List<List<IntPoint>> finalSolution = new List<List<IntPoint>>();
                Clipper cDiff = new Clipper();
                // Add original polygon a subject
                cDiff.AddPath(poly, PolyType.ptSubject, true);
                // Add all line segments to use as difference
                foreach (List<List<IntPoint>> segmentSolution in segmentSolutions) {
                    cDiff.AddPaths(segmentSolution, PolyType.ptClip, true);
                }
                cDiff.Execute(ClipType.ctDifference, finalSolution,
                              PolyFillType.pftPositive, PolyFillType.pftPositive);

                // Draw final solution
                foreach (List<IntPoint> finalPoly in finalSolution) {
                    for (int i = 0; i < finalPoly.Count; i++) {
                        Vector2 p1 = VectorUtil.IntPointToVector2(finalPoly[i]) / scale;
                        Vector2 p2 = VectorUtil.IntPointToVector2(finalPoly[(i + 1) % finalPoly.Count]) / scale;

                        Debug.DrawLine(VectorUtil.Vector2To3(p1), VectorUtil.Vector2To3(p2), new Color(1, 0, 1));
                    }

                    finalBlocks.Add(
                        new Block(
                            finalPoly.Select(v => VectorUtil.IntPointToVector3(v) / scale).ToList()
                        )
                    );
                }
            }
        }

        return finalBlocks;
    }

    private void Log(object msg) {
        if (debug)
            Debug.Log(msg);
    }

    private void Update() {
        /*
        polygon = new List<Vector2>() {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(0.5f, 1),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.2f, 0.2f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 1),
            new Vector2(1, 1),
            new Vector2(1, -1),
            new Vector2(0.48f, -1),
            new Vector2(0.51f, -0.8f),
            new Vector2(0.35f, -0.52f),
            new Vector2(0.51f, -0.8f),
            new Vector2(0.64f, -0.67f),
            new Vector2(0.51f, -0.8f),
            new Vector2(0.48f, -1),
            new Vector2(0, -1),
        };
        */

        List<Block> blocks = this.insetBlocks;

        if (debug && blocks != null) {
            if (debugBlock < 0 || debugBlock > blocks.Count) return;
            foreach (var v in blocks[debugBlock].vertices) {
                Debug.DrawLine(v, v + 0.5f * Vector3.up, Color.yellow, 0.1f);
            }
            Log("Block area: " + BlockArea(blocks[debugBlock]));
        }

        if (debug)
            InsetBlock(new Block(polygon.Select(v => VectorUtil.Vector2To3(v)).ToList()), this.inset);

        if (blocks != null) {
            if (debug) {
                foreach (Block block in this.blocks) {
                    List<Block> newBlocks = InsetBlock(block, this.inset);

                    foreach (Block newBlock in newBlocks) {
                        for (int i = 0; i < newBlock.vertices.Count; i++) {
                            Vector3 p1 = newBlock.vertices[i];
                            Vector3 p2 = newBlock.vertices[(i + 1) % newBlock.vertices.Count];

                            Debug.DrawLine(p1, p2, new Color(1, 0, 1));
                        }
                    }
                }
            }
            else {
                foreach (Block block in blocks) {
                    for (int i = 0; i < block.vertices.Count; i++) {
                        Vector3 p1 = block.vertices[i];
                        Vector3 p2 = block.vertices[(i + 1) % block.vertices.Count];

                        Debug.DrawLine(p1, p2, new Color(1, 0, 1));
                    }
                }
            }
        }
    }
}
