using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public class RoadMeshGenerator : MonoBehaviour {
    [Range(1, 1000)]
    [SerializeField] private int maxIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float tickInterval = 0.2f;

    [SerializeField] private bool getFirst = false;

    [SerializeField] private GameObject roadMeshPrefab = null;
    [SerializeField] private GameObject roadIntersectionMeshPrefab = null;

    private RoadNetwork network;

    private LinkedList<Traverser> queue;
    private Dictionary<Node, bool> visited;
    private Dictionary<Node, Dictionary<Node, bool>> placed;
    private Dictionary<Node, RoadIntersectionMesh> intersections;

    private bool isTraversing = false;

    private ProjectVertex projector;

    public void Generate(RoadNetwork network) {
        if (network == null) {
            Debug.LogWarning("Failed to generate road meshes! Given network does not exist.");
            return;
        }
        this.network = network;

        this.queue = new LinkedList<Traverser>();
        this.visited = new Dictionary<Node, bool>();
        this.placed = new Dictionary<Node, Dictionary<Node, bool>>();
        this.intersections = new Dictionary<Node, RoadIntersectionMesh>();

        // this.projector = (Vector3 vec) => { return network.Terrain.GetPosition(vec.x, vec.z); };
        this.projector = (Vector3 vec) => { return network.Terrain.GetNormal(vec.x, vec.z); };
        // this.projector = (Vector3 vec) => vec;

        // Remove previously generated meshes
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        StartCoroutine("DoTraverse");
    }

    IEnumerator DoTraverse() {
        isTraversing = true;

        Node startNode = null;
        foreach (Node n in this.network.Nodes) {
            if (n.connections.Count != 2) {
                startNode = n;
                break;
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

        int intersectionCount = 0;
        foreach (var entry in this.intersections) {
            Camera.main.transform.position = entry.Value.transform.position + Vector3.up * 1.5f;
            Camera.main.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            entry.Value.name = roadIntersectionMeshPrefab.name + " " + intersectionCount;
            entry.Value.UpdateMesh(projector);
            intersectionCount++;
        }

        isTraversing = false;
    }


    private void PlaceRoad(List<Node> path) {
        GameObject roadGameObject = (GameObject)Instantiate(roadMeshPrefab);
        roadGameObject.name = roadMeshPrefab.name + " " + transform.childCount;
        roadGameObject.transform.parent = transform;
        roadGameObject.transform.localPosition = path[0].pos;
        BezierSpline spline = roadGameObject.GetComponent<BezierSpline>();
        RoadMesh roadMesh = roadGameObject.GetComponent<RoadMesh>();
        spline.Reset();
        roadMesh.Reset();

        for (int i = 0; i < path.Count; i++) {
            spline.AddPoint(path[i].pos);
        }

        // Try connect to intersections at position
        RoadIntersectionMesh TryCreateIntersection(Node intersectionNode) {
            if (intersectionNode.connections.Count > 2) {
                RoadIntersectionMesh intersection;
                if (!intersections.ContainsKey(intersectionNode)) {
                    intersection = Instantiate(roadIntersectionMeshPrefab).GetComponent<RoadIntersectionMesh>();
                    intersection.transform.parent = transform;
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
            roadMesh.SetStart(startIntersection);
            startIntersection.AddConnection(roadMesh);
        }

        RoadIntersectionMesh endIntersection = TryCreateIntersection(path[path.Count - 1]);
        if (endIntersection) {
            roadMesh.SetEnd(endIntersection);
            endIntersection.AddConnection(roadMesh);
        }

        roadMesh.GenerateRoadMesh(projector);
    }


    void Update() {
        // if (network != null) {
        //     Vector3 prevPoint = Vector3.zero;
        //     for (float i = 0; i < 100; i+=0.1f) {
        //         Vector2 p = new Vector2(i, 0);
        //         Vector3 tp = network.Terrain.GetPosition(p);

        //         Debug.DrawLine(prevPoint, tp, new Color(1, 0, 0));
        //         Debug.DrawLine(tp, tp + Vector3.up, new Color(1, 0, 0));

        //         prevPoint = tp;
        //     }
        // }
    }
}
