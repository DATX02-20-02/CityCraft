using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
  What? Generates insetted city block areas from a road network.
  Why? These city blocks are suitable to place structures in, and leave room for road meshes.
  How?
  1. Extracts polygons from road network graph by spawning "turtles" that always turn right.
  2. Attempts to expand polygons from streets.
  3. Insets all found polygons and returns them.
*/
public class BlockGenerator : MonoBehaviour {

    [SerializeField] private float minBlockArea = 0.0f;
    [SerializeField] private float maxBlockArea = 0.0f;
    [SerializeField] private bool debug = false;
    [SerializeField] private int debugBlock = 0;

    private RoadNetwork roadNetwork;
    private List<Block> blocks;


    // Entrypoint to the generator.
    public List<Block> Generate(RoadNetwork roadNetwork) {
        this.roadNetwork = roadNetwork;
        this.blocks = new List<Block>();

        return ExtractPolygons();
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

    private void Log(object msg) {
        if (debug)
            Debug.Log(msg);
    }

    private void Update() {
        if (debug && this.blocks != null) {
            if (debugBlock < 0 || debugBlock > this.blocks.Count) return;
            foreach (var v in this.blocks[debugBlock].vertices) {
                Debug.DrawLine(v, v + 0.5f * Vector3.up, Color.yellow, 0.1f);
            }
            Log("Block area: " + BlockArea(this.blocks[debugBlock]));
        }
    }
}
