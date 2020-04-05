using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Return custom type because not all RaycastHit properties are not guranteed to be set
public delegate RaycastHit ProjectOnTerrain(float x, float z);

public class RoadMeshGenerator : MonoBehaviour {
    [Range(1, 1000)]
    [SerializeField] private int maxIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float tickInterval = 0.2f;

    [SerializeField] private bool getFirst = false;

    [SerializeField] private GameObject roadMeshPrefab = null;
    [SerializeField] private GameObject roadIntersectionMeshPrefab = null;
    public GameObject TerrainMesh;

    private RoadNetwork network;
    private GameObject roadParent;
    private GameObject intersectionParent;

    private LinkedList<Traverser> queue;
    private Dictionary<Node, bool> visited;
    private Dictionary<Node, Dictionary<Node, bool>> placed;
    private Dictionary<Node, RoadIntersectionMesh> intersections;
    private List<RoadMesh> placedRoads;

    private bool isTraversing = false;

    private ProjectOnTerrain projectOnTerrain;

    void Start() {
        roadParent = new GameObject("Roads");
        roadParent.transform.parent = this.transform;
        intersectionParent = new GameObject("Intersections");
        intersectionParent.transform.parent = this.transform;
    }

    public void Generate(RoadNetwork network) {
        if (network == null) {
            Debug.LogWarning("Failed to generate road meshes! Given network does not exist.");
            return;
        }
        this.network = network;


        this.projectOnTerrain = (float x, float z) => {
            float rayLength = 10000f;
            Ray ray = new Ray(new Vector3(x, rayLength, z), Vector3.down);

            RaycastHit hit;
            if (TerrainMesh.GetComponent<Collider>().Raycast(ray, out hit, rayLength)) {
                return hit;
            }

            float y = network.Terrain.GetHeight(x, z);
            hit.point = new Vector3(x, y, z);
            hit.normal = network.Terrain.GetNormal(x, z);
            return hit;
        };

        if (isTraversing) {
            StopCoroutine(GenerateRoadMesh());
        }
        StartCoroutine(GenerateRoadMesh());
    }

    private IEnumerator GenerateRoadMesh() {
        isTraversing = true;

        this.queue = new LinkedList<Traverser>();
        this.visited = new Dictionary<Node, bool>();
        this.placed = new Dictionary<Node, Dictionary<Node, bool>>();
        this.intersections = new Dictionary<Node, RoadIntersectionMesh>();
        placedRoads = new List<RoadMesh>();

        // Remove previously generated meshes
        foreach (Transform child in intersectionParent.transform) {
            Destroy(child.gameObject);
        }
        foreach (Transform child in roadParent.transform) {
            Destroy(child.gameObject);
        }

        Node startNode = null;
        { // Get startNode
            foreach (Node n in this.network.Nodes) {
                if (n.connections.Count > 2) {
                    startNode = n;
                    break;
                }
            }
        }

        foreach (NodeConnection c in startNode.connections) {
            this.queue.AddLast(new Traverser(startNode, c.node, 0));
        }

        while(this.queue.Count > 0) {
            for (int iteration = 0; iteration < maxIterations; iteration++) {
                if (this.queue.Count == 0) { break; }
                Traverser traverser = getFirst ? this.queue.First.Value : this.queue.Last.Value;

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

                        this.queue.AddFirst(new Traverser(lastNode, c.node, traverser.Priority + 1));
                    }
                }
            }
            yield return new WaitForSeconds(tickInterval);
        }

        int intersectionCount = 1;
        foreach (var entry in this.intersections) {
            entry.Value.name = roadIntersectionMeshPrefab.name + " " + intersectionCount;
            entry.Value.UpdateMesh(this.projectOnTerrain);
            intersectionCount++;
        }

        foreach (RoadMesh r in placedRoads) {
            r.GenerateRoadMesh(this.projectOnTerrain);
        }

        isTraversing = false;
    }


    private void PlaceRoad(List<Node> path) {
        RoadMesh roadMesh = Instantiate(roadMeshPrefab, roadParent.transform).GetComponent<RoadMesh>();
        roadMesh.name = roadMeshPrefab.name + " " + roadParent.transform.childCount;
        roadMesh.transform.position = path[0].pos;

        for (int i = 0; i < path.Count; i++) {
            RaycastHit hit = this.projectOnTerrain(path[i].pos.x, path[i].pos.z);
            path[i].pos = hit.point + hit.normal * 0.01f;
        }

        for (int i = 0; i < path.Count; i++) {
            roadMesh.Spline.AddPoint(path[i].pos);
        }

        // Try connect to intersections at position
        RoadIntersectionMesh TryCreateIntersection(Node intersectionNode) {
            if (intersectionNode.connections.Count > 2) {
                RoadIntersectionMesh intersection;
                if (!intersections.ContainsKey(intersectionNode)) {
                    intersection = Instantiate(roadIntersectionMeshPrefab, intersectionParent.transform).GetComponent<RoadIntersectionMesh>();
                    intersection.transform.localPosition = intersectionNode.pos;
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
        }

        RoadIntersectionMesh endIntersection = TryCreateIntersection(path[path.Count - 1]);
        if (endIntersection) {
            roadMesh.RoadEnd = endIntersection;
            Vector3 angleOfAttack = (path[path.Count - 1].pos - path[path.Count - 2].pos).normalized;
            endIntersection.AddConnection(roadMesh, angleOfAttack);
        }

        placedRoads.Add(roadMesh);
    }
}

class Traverser : System.IComparable {
    public Node node = null;
    private Node prev = null;
    private List<Node> path = new List<Node>();
    private float priority = 0;

    public float Priority {
        get { return this.priority; }
    }

    public Traverser(Node startNode, Node node, float priority) {
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
        if(obj == null) return 1;

        Traverser traverser = obj as Traverser;
        if(traverser != null) {
            return priority.CompareTo(traverser.priority);
        }
        else {
            throw new System.ArgumentException("Object is not a Traverser");
        }
    }
}
