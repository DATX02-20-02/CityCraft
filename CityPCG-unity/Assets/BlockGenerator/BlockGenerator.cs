using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ClipperLib;
using Utils;

/*
  What? Generates insetted city block areas from a road network.
  Why? These city blocks are suitable to place structures in, and leave room for road meshes.
  How?
  1. Extracts polygons from road network graph by spawning "turtles" that always turn right.
  2. Attempts to expand polygons from streets.
  3. Insets all found polygons and returns them.
*/
public class BlockGenerator : MonoBehaviour {
    // The distance the blocks should be inset
    [Range(0, 2)]
    public float inset = 0.05f;

    // Since ClipperLib only uses IntPoint, we have to scale it first
    // to work with floats, and after Clipper is done, we reverse the scale
    [Range(0, (int)1E5)]
    public int scale = 1024;

    // This is primarily for testing the inset algorithm on a configurable polygon
    [SerializeField]
    List<Vector2> debugPolygon = new List<Vector2>() {
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
    [SerializeField] private float minParkArea = 0.0f;
    [SerializeField] private float maxBlockArea = 0.0f;
    [SerializeField] private bool debug = false;
    [SerializeField] private int debugBlock = 0;
    [SerializeField] private bool debugInset = false;

    private RoadNetwork roadNetwork;
    private List<Block> blocks;
    private List<Block> insetBlocks;
    private Noise populationNoise;

    public void Reset() {
        this.blocks = new List<Block>();
        this.insetBlocks = new List<Block>();
    }

    // Entrypoint to the generator.
    public List<Block> Generate(RoadNetwork roadNetwork, Noise populationNoise) {
        this.Reset();

        this.roadNetwork = roadNetwork;
        this.populationNoise = populationNoise;

        ExtractPolygons();
        InsetBlocks();

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
                    float area = PolygonUtil.PolygonArea(vertices);
                    if (area <= maxBlockArea) {
                        blocks.Add(new Block(vertices, BlockType.Empty));
                    }
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

    // This operation can return multiple blocks because the inset could
    // create polygons that are isolated because of overlapping inset lines
    private List<Block> InsetBlock(Block block, float inset) {
        List<Vector3> vertices = block.vertices;

        // Segments are paths that represent lines that occur when
        // points are overlapping
        List<List<Vector3>> segments = new List<List<Vector3>>();
        HashSet<Vector3> visited = new HashSet<Vector3>();

        // Overlaps are a list of indices for vertices that overlap
        // other vertices in the same polygon
        List<int> overlaps = new List<int>();
        Vector3 last = Vector3.zero;

        for (int i = 0; i < vertices.Count; i++) {
            Vector3 vec = vertices[i];

            // Find all overlapping points
            if (visited.Contains(vec)) overlaps.Add(i);
            else visited.Add(vec);

            if (debugInset) {
                Vector3 end = vertices[(i + 1) % vertices.Count];

                Debug.DrawLine(vec, end, new Color(1, 1, 1));
            }
        }

        // If we found overlaps, it means we have to create line segments
        // that will be used as difference on the simplified polygon
        if (overlaps.Count > 0) {
            int currOverlapIndex = overlaps.Count - 1;
            int firstIndex = overlaps[currOverlapIndex];
            Vector3 currOverlap = vertices[firstIndex];

            // Iterate through all overlaps in reverse order
            List<Vector3> path = new List<Vector3>();
            for (int i = firstIndex; i >= 0; i--) {
                Vector3 vec = vertices[i];

                path.Add(vec);

                if (i != firstIndex && Vector3.Equals(vec, currOverlap)) {
                    if (path.Count == 2) {
                        // If the path is only two long, that means we went to a point
                        // and then directly back to the point we came from,
                        // this is unnecessary
                        path.RemoveAt(path.Count - 1);
                        segments.Add(path);
                    }
                    else {
                        segments.Add(path);
                    }
                    path = new List<Vector3>();

                    // Find next overlap that is not part of the current line segment
                    // This is to repeat the line segment creation for all overlaps
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
            if (debugInset) {
                for (int i = 0; i < path.Count - 1; i++) {
                    Vector3 p1 = path[i];
                    Vector3 p2 = path[i + 1];

                    Debug.DrawLine(p1 + Vector3.up * (0.01f * i), p2 + Vector3.up * (0.01f * (i + 1)),
                                   new Color(1, 1, 0));
                }
            }

            // This is necessary because Clipper only works with IntPoint, so
            // our Vector3 must be converted to Vector2, scaled and then
            // changed into an IntPoint
            List<IntPoint> linePoly = new List<IntPoint>();
            foreach (Vector3 vec in path) {
                Vector2 scaled = VectorUtil.Vector3To2(vec) * scale;
                linePoly.Add(new IntPoint((int)scaled.x, (int)scaled.y));
            }

            // Perform the Clipper offset on all the line segments
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset co = new ClipperOffset();
            co.AddPath(linePoly, JoinType.jtSquare, EndType.etOpenSquare);
            co.Execute(ref solution, inset * scale);

            // Add all offsetted polygons to a list
            segmentSolutions.Add(solution);

            // Debug draw the line segments
            if (debugInset) {
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
            s.Add(new IntPoint((int)scaled.x, (int)scaled.y));
        }

        // Simplify polygon, removing overlapping lines
        // These will be reintroduced by the line segments using
        // a difference operation
        List<List<IntPoint>> polygons = Clipper.SimplifyPolygon(
            s,
            PolyFillType.pftEvenOdd
        );

        List<Block> finalBlocks = new List<Block>();

        // Perform subject offset and line segment difference operations
        foreach (List<IntPoint> simplePoly in polygons) {
            // Perform the offset on the simplified polygon
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset co = new ClipperOffset();
            co.AddPath(simplePoly, JoinType.jtSquare, EndType.etClosedPolygon);
            co.Execute(ref solution, -inset * scale);

            // The offset operation can return multiple polygons, but we first need to
            // check if there are any to do any more operations
            if (solution.Count > 0) {
                List<IntPoint> poly = solution[0];

                // Perform difference operation on the insetted simplified polygon
                // and the line segments
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

                // Compose final solution
                foreach (List<IntPoint> finalPoly in finalSolution) {
                    // Debug draw the final polygons
                    if (debugInset) {
                        for (int i = 0; i < finalPoly.Count; i++) {
                            Vector2 p1 = VectorUtil.IntPointToVector2(finalPoly[i]) / scale;
                            Vector2 p2 = VectorUtil.IntPointToVector2(finalPoly[(i + 1) % finalPoly.Count]) / scale;

                            Debug.DrawLine(VectorUtil.Vector2To3(p1), VectorUtil.Vector2To3(p2), new Color(1, 0, 1));
                        }
                    }

                    finalBlocks.Add(
                        new Block(
                            finalPoly.Select(v => roadNetwork.Terrain.GetPosition(VectorUtil.IntPointToVector2(v) / scale)).ToList(),
                            block.type
                        )
                    );
                }
            }
        }

        return finalBlocks;
    }

    private List<Block> InsetBlocks() {
        // Go through all blocks and perform the inset algorithm on each
        int decider = 0;
        foreach (Block block in this.blocks) {
            List<Block> newBlocks = InsetBlock(block, this.inset);

            foreach (Block newBlock in newBlocks) {
                float area = PolygonUtil.PolygonArea(newBlock.vertices);
                if (area <= maxBlockArea) {
                    if (area <= minBlockArea)
                        this.insetBlocks.Add(new Block(newBlock.vertices, BlockType.Empty));
                    else if (area >= minParkArea)
                        this.insetBlocks.Add(new Block(newBlock.vertices, BlockType.Parks));
                    else {
                        float rng = UnityEngine.Random.value;
                        BlockType t = BlockType.Skyscrapers;

                        if (rng < 0.15f)
                            t = BlockType.Apartments;
                        else if (rng < 0.25f)
                            t = BlockType.Downtown;
                        else if (rng < 0.75f)
                            t = BlockType.Industrial;
                        else if (rng < 0.9f)
                            t = BlockType.Suburbs;

                        this.insetBlocks.Add(new Block(newBlock.vertices, t));
                    }
                }
            }
        }

        return this.insetBlocks;
    }

    private void Log(object msg) {
        if (debug)
            Debug.Log(msg);
    }

    private void DrawBlock(Block block) {
        Vector3 avg = Vector3.zero;

        foreach (Vector3 vert in block.vertices) {
            avg += vert;
        }
        avg /= block.vertices.Count;

        for (int i = 0; i < block.vertices.Count; i++) {
            Vector3 p1 = block.vertices[i];
            Vector3 p2 = block.vertices[(i + 1) % block.vertices.Count];

            Debug.DrawLine(p1, p2, new Color(1, 0, 1));
        }

        Color c = Color.black;
        Debug.Log(block.type);
        switch (block.type) {
            case BlockType.Industrial:
                c = Color.white;
                break;
            case BlockType.Suburbs:
                c = Color.blue;
                break;
            case BlockType.Downtown:
                c = Color.magenta;
                break;
            case BlockType.Skyscrapers:
                c = Color.cyan;
                break;
            case BlockType.Apartments:
                c = Color.yellow;
                break;
            case BlockType.Parks:
                c = Color.red;
                break;
        }
        DrawUtil.DebugDrawCircle(avg, 0.1f, c, 4);
    }

    private void Update() {
        if (!debug) return;

        List<Block> blocks = this.insetBlocks;
        if (blocks != null) {
            if (debugBlock < 0 || debugBlock > blocks.Count) return;
            foreach (var v in blocks[debugBlock].vertices) {
                Debug.DrawLine(v, v + 0.5f * Vector3.up, Color.yellow, 0.1f);
            }
            Log("Block area: " + PolygonUtil.PolygonArea(blocks[debugBlock].vertices));
        }

        if (debugInset)
            InsetBlock(
                new Block(debugPolygon.Select(v => VectorUtil.Vector2To3(v)).ToList(), BlockType.Empty),
                this.inset
            );

        if (blocks != null) {
            if (debugInset) {
                foreach (Block block in this.blocks) {
                    List<Block> newBlocks = InsetBlock(block, this.inset);

                    foreach (Block newBlock in newBlocks) {
                        DrawBlock(newBlock);
                    }
                }
            }
            else {
                foreach (Block block in blocks) {
                    DrawBlock(block);
                }
            }
        }
    }
}
