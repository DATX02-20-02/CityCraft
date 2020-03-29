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

    private RoadNetwork network;

    private LinkedList<Traverser> queue;
    private Dictionary<Node, bool> visited;
    private Dictionary<Node, Dictionary<Node, bool>> placed;

    private bool isTraversing = false;

    public void Generate(RoadNetwork network) {
        if (this.network == null) {
            Debug.LogWarning("Failed to generate road meshes! Given network does not exist.");
            return;
        }

        this.queue = new LinkedList<Traverser>();
        this.visited = new Dictionary<Node, bool>();
        this.placed = new Dictionary<Node, Dictionary<Node, bool>>();

        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        this.network = network;

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
    }

    IEnumerator DoTraverse() {
        if(this.queue.Count == 0) yield break;

        isTraversing = true;

        int iterations = 0;
        while(this.queue.Count > 0 && iterations < maxIterations) {
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

                Node lastNode = path[path.Count - 1];
                if (lastNode != null) {

                    GameObject roadGameObject = (GameObject)Instantiate(roadMeshPrefab);
                    roadGameObject.transform.parent = transform;
                    roadGameObject.transform.localPosition = path[0].pos;
                    BezierSpline spline = roadGameObject.GetComponent<BezierSpline>();
                    RoadMesh roadMesh = roadGameObject.GetComponent<RoadMesh>();
                    spline.Reset();
                    roadMesh.Reset();

                    for (int i = 0; i < path.Count; i++) {
                        spline.AddPoint(path[i].pos);
                    }
                    roadMesh.GenerateRoadMesh();


                    for (int i = 0; i < path.Count - 1; i++) {
                        Node n = path[i];
                        Node nx = path[i + 1];

                        if (!placed.ContainsKey(n)) {
                            placed[n] = new Dictionary<Node, bool>();
                        }

                        if (placed.ContainsKey(nx) && placed[nx].ContainsKey(n) ||
                            placed[n].ContainsKey(nx)) {
                            break;
                        };

                        placed[n][nx] = true;

                        visited[n] = true;
                        visited[nx] = true;
                    }

                    foreach (NodeConnection c in lastNode.connections) {
                        if (visited.ContainsKey(c.node)) continue;
                        if (placed.ContainsKey(lastNode) && placed[lastNode].ContainsKey(c.node) ||
                            placed.ContainsKey(c.node) && placed[c.node].ContainsKey(lastNode)
                        ) {
                            continue;
                        }

                        // visited[c.node] = true;
                        this.queue.AddFirst(new Traverser(lastNode, c.node, traverser.Priority + 1));
                    }
                }
            }

            iterations++;
        }

        yield return new WaitForSeconds(tickInterval);
        isTraversing = false;
    }

    private void Update() {
        if(this.queue != null && !isTraversing) {
            if (this.queue.Count > 0)
                StartCoroutine("DoTraverse");
        }
    }
}
