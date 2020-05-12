using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate TerrainModel.TerrainHit ProjectOnTerrain(float x, float z);

public class RoadMeshGenerator : MonoBehaviour {
    [Range(1, 1000)]
    [SerializeField] private int maxIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float tickInterval = 0.2f;

    [SerializeField] private bool getFirst = false;

    [SerializeField] private GameObject roadMeshPrefab = null;
    [SerializeField] private GameObject roadIntersectionMeshPrefab = null;

    [SerializeField] private bool debug = false;

    [Header("Instantiate Parents")]
    [SerializeField] private GameObject roadParent = null;
    [SerializeField] private GameObject intersectionParent = null;

    private RoadNetwork network;

    private LinkedList<TraverseUntilIntersection> queue;
    private Dictionary<Node, bool> visited;
    private HashSet<Node> notVisited;
    private Dictionary<Node, Dictionary<Node, bool>> placed;
    private Dictionary<Node, RoadIntersectionMesh> intersections;
    private List<RoadMesh> placedRoads;

    private bool isTraversing = false;

    private TerrainModel terrainModel;
    private ProjectOnTerrain projectOnTerrain;
    private Action<List<RoadMesh>, Dictionary<Node, RoadIntersectionMesh>> callback;

    public void Reset() {
        // Remove previously generated meshes
        if (roadParent != null) {
            foreach (Transform child in roadParent.transform) {
                Destroy(child.gameObject);
            }
        }

        if (intersectionParent != null) {
            foreach (Transform child in intersectionParent.transform) {
                Destroy(child.gameObject);
            }
        }
    }

    public void Generate(RoadNetwork network, TerrainModel terrainModel, Action<List<RoadMesh>, Dictionary<Node, RoadIntersectionMesh>> callback) {
        if (network == null) {
            Debug.LogWarning("Failed to generate road meshes! Given network does not exist.");
            return;
        }
        this.callback = callback;
        this.network = network;
        this.terrainModel = terrainModel;

        this.projectOnTerrain = (float x, float z) => {
            return terrainModel.GetMeshIntersection(x, z);
        };

        if (isTraversing) {
            StopCoroutine(GenerateRoadMesh());
        }

        StartCoroutine(GenerateRoadMesh());
    }

    private IEnumerator GenerateRoadMesh() {
        isTraversing = true;
        Reset();
        this.queue = new LinkedList<TraverseUntilIntersection>();
        this.visited = new Dictionary<Node, bool>();
        this.notVisited = new HashSet<Node>(this.network.Nodes);
        this.placed = new Dictionary<Node, Dictionary<Node, bool>>();
        this.intersections = new Dictionary<Node, RoadIntersectionMesh>();
        this.placedRoads = new List<RoadMesh>();

        while (notVisited.Count > 0) {
            Node startNode = null;
            foreach (Node n in notVisited) {
                if (n.connections.Count != 2) {
                    startNode = n;
                    break;
                }
            }

            // If no startnodes were found, log the remaining ones and exit
            if (startNode == null) {
                Debug.LogWarning("Failed to find starting node when generating road mesh... aborting");
                break;
            }

            foreach (NodeConnection c in startNode.connections) {
                this.queue.AddLast(new TraverseUntilIntersection(startNode, c.node, 0));
            }

            while (this.queue.Count > 0) {
                for (int iteration = 0; iteration < maxIterations; iteration++) {
                    if (this.queue.Count == 0) { break; }
                    TraverseUntilIntersection traverser = getFirst ? this.queue.First.Value : this.queue.Last.Value;

                    Node prevNode = traverser.node;

                    List<Node> path;
                    bool done = traverser.Traverse(out path);

                    if (placed.ContainsKey(traverser.node) && placed[traverser.node].ContainsKey(prevNode) ||
                        placed.ContainsKey(prevNode) && placed[prevNode].ContainsKey(traverser.node)
                    ) {
                        if (getFirst)
                            this.queue.RemoveFirst();
                        else
                            this.queue.RemoveLast();
                        continue;
                    }

                    if (done) {
                        if (getFirst)
                            this.queue.RemoveFirst();
                        else
                            this.queue.RemoveLast();

                        bool shouldPlace = true;

                        Node lastNode = path[path.Count - 1];
                        for (int i = 0; i < path.Count - 1; i++) {
                            Node n = path[i];
                            Node nx = path[i + 1];

                            if (!placed.ContainsKey(n)) {
                                placed[n] = new Dictionary<Node, bool>();
                            }

                            if (placed.ContainsKey(nx) && placed[nx].ContainsKey(n) || placed[n].ContainsKey(nx)) {
                                shouldPlace = false;
                                break;
                            };

                            placed[n][nx] = true;

                            visited[n] = true;
                            visited[nx] = true;
                            notVisited.Remove(n);
                            notVisited.Remove(nx);
                        }

                        if (shouldPlace) {
                            PlaceRoad(path);
                        }

                        foreach (NodeConnection c in lastNode.connections) {
                            if (visited.ContainsKey(c.node)) continue;
                            if (placed.ContainsKey(lastNode) && placed[lastNode].ContainsKey(c.node) ||
                                placed.ContainsKey(c.node) && placed[c.node].ContainsKey(lastNode)
                            ) {
                                continue;
                            }

                            this.queue.AddFirst(new TraverseUntilIntersection(lastNode, c.node, traverser.Priority + 1));
                        }
                    }
                }
                yield return new WaitForSeconds(tickInterval);
            }
        }


        int intersectionCount = 1;
        foreach (var entry in this.intersections) {
            entry.Value.name = roadIntersectionMeshPrefab.name + " " + intersectionCount;
            intersectionCount++;

            // In the application we don't want each road to keep its scripts.
            if (!debug) {
                Destroy(entry.Value);
            }
        }

        if (!debug) {
            // In the application we don't want each road to keep its scripts.
            foreach (RoadMesh road in this.placedRoads) {
                Destroy(road);
                Destroy(road.GetComponent<BezierSpline>());
            }
        }

        isTraversing = false;

        if (this.callback != null)
            this.callback(placedRoads, intersections);
    }


    private RoadMesh PlaceRoad(List<Node> path) {
        GameObject roadObj = Instantiate(roadMeshPrefab, roadParent.transform);

        RoadMesh roadMesh = roadObj.GetComponent<RoadMesh>();
        roadMesh.name = roadMeshPrefab.name + " " + (placedRoads.Count + 1);
        roadMesh.transform.position = path[0].pos;

        for (int i = 0; i < path.Count; i++) {
            TerrainModel.TerrainHit hit = this.projectOnTerrain(path[i].pos.x, path[i].pos.z);
            path[i].pos = hit.point + hit.normal * 0.01f;
            roadMesh.Spline.AddPoint(path[i].pos);
        }

        // Try connect to intersections at position
        RoadIntersectionMesh TryCreateIntersection(Node intersectionNode) {
            if (intersectionNode.connections.Count > 2) {
                RoadIntersectionMesh intersection;
                if (!intersections.ContainsKey(intersectionNode)) {
                    GameObject intersectionObj = Instantiate(roadIntersectionMeshPrefab, intersectionParent.transform);
                    intersectionObj.transform.localPosition = intersectionNode.pos;
                    intersection = intersectionObj.GetComponent<RoadIntersectionMesh>();
                    intersections[intersectionNode] = intersection;
                }
                else {
                    intersection = intersections[intersectionNode];
                }

                return intersection;
            }

            return null;
        }

        RoadIntersectionMesh startIntersection = TryCreateIntersection(path[0]);
        if (startIntersection) {
            roadMesh.RoadStart = startIntersection;
            Vector3 angleOfAttack = (path[0].pos - path[1].pos).normalized;
            startIntersection.AddConnection(roadMesh, angleOfAttack);

            if (startIntersection.ConnectedRoads.Count == path[0].connections.Count) {
                startIntersection.UpdateMesh(this.projectOnTerrain);
                foreach (var roadConnection in startIntersection.ConnectedRoads) {
                    roadConnection.road.GenerateRoadMesh(this.projectOnTerrain);
                }
            }
        }

        RoadIntersectionMesh endIntersection = TryCreateIntersection(path[path.Count - 1]);
        if (endIntersection) {
            roadMesh.RoadEnd = endIntersection;
            Vector3 angleOfAttack = (path[path.Count - 1].pos - path[path.Count - 2].pos).normalized;
            endIntersection.AddConnection(roadMesh, angleOfAttack);

            if (endIntersection.ConnectedRoads.Count == path[path.Count - 1].connections.Count) {
                endIntersection.UpdateMesh(this.projectOnTerrain);
                foreach (var roadConnection in endIntersection.ConnectedRoads) {
                    roadConnection.road.GenerateRoadMesh(this.projectOnTerrain);
                }
            }
        }

        placedRoads.Add(roadMesh);

        if (startIntersection == null && endIntersection == null)
            roadMesh.GenerateRoadMesh(this.projectOnTerrain);

        return roadMesh;
    }
}

// Traverses road network until an intersection is found
class TraverseUntilIntersection : System.IComparable {
    public Node node = null;
    private Node prev = null;
    private List<Node> path = new List<Node>();
    private float priority = 0;

    public float Priority {
        get { return this.priority; }
    }

    public TraverseUntilIntersection(Node startNode, Node node, float priority) {
        this.node = node;
        this.priority = priority;

        if (startNode != node) {
            path.Add(startNode);
            prev = startNode;
        }
    }

    public bool Traverse(out List<Node> nodes) {
        path.Add(node);
        nodes = path;

        if (node.connections.Count == 2) {
            foreach (NodeConnection c in node.connections) {
                if (c.node == prev) continue;

                prev = node;
                node = c.node;

                break;
            }
        }
        else {
            return true;
        }

        return false;
    }

    public int CompareTo(object obj) {
        if (obj == null) return 1;

        TraverseUntilIntersection traverser = obj as TraverseUntilIntersection;
        if (traverser != null) {
            return priority.CompareTo(traverser.priority);
        }
        else {
            throw new System.ArgumentException("Object is not a Traverser");
        }
    }
}
