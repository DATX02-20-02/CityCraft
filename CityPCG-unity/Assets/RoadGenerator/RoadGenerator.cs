using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RBush;

public class RoadGenerator : MonoBehaviour
{
    [Range(0, 100)]
    public int maxAgentQueueIterations = 1;

    [Range(0, 0.4f)]
    public float generationTickInterval = 0.2f;

    private List<Node> nodes;
    private RBush<Node> tree;
    private PriorityQueue<Agent> queue;

    private bool prevClick;
    private Node prevNode;
    private int increment;

    private List<Vector3> debugPoints = new List<Vector3>();

    private bool areAgentsWorking = false;

    void Start()
    {
        tree = new RBush<Node>();
        nodes = new List<Node>();
        queue = new PriorityQueue<Agent>();

        Node n = null;
        float step = 2 * Mathf.PI / 10.0f;
        // for (int i = 1; i < 2; i++) {
        //     float x = Mathf.Sin(step * i) * 2;
        //     float y = Mathf.Cos(step * i) * 2;

        //     Node node1 = new Node(new Vector3(x, 0, y));
        //     Node node2 = new Node(new Vector3(x * 2, 0, y * 2));

        //     node1.ConnectTo(node2);

        //     AddNode(node1);
        //     AddNode(node2);

        //     n = node2;
        // }

        IAgentFactory generator = new ParisAgentFactory();
        generator.Create(this, new Vector3(0, 0, 0));

        // Node node1 = new Node(new Vector3(-5, 0, 0));
        // Node node2 = new Node(new Vector3(0, 0, 0.5f));
        // Node node3 = new Node(new Vector3(0, 0, 0));
        // Node node4 = new Node(new Vector3(0, 0, -0.5f));

        // // Node nodea = new Node(new Vector3(1.2f, 0, 2));
        // // Node nodeb = new Node(new Vector3(1.2f, 0, 0.2f));

        // AddNode(node1);
        // AddNode(node2);
        // AddNode(node3);
        // AddNode(node4);
        // // AddNode(nodea);
        // // AddNode(nodeb);

        // ConnectNodes(node1, node2);
        // ConnectNodes(node3, node1);
        // ConnectNodes(node4, node1);
        // // ConnectNodes(nodea, nodeb);


        // List<Vector3> sequence = new List<Vector3>() {
        //     new Vector3(-3.8f, 0, -0.2f)
        // };

        // Node prevNode = null;

        // foreach (Vector3 vec in sequence){
        //     print(vec);
        //     Node n1 = prevNode;
        //     Node n2 = new Node(vec);

        //     debugPoints.Add(vec);

        //     if (n1 == null) {
        //         n1 = AddNodeNearby(new Node(new Vector3(0.2f, 0, -1.4f)), 0.2f);
        //     }

        //     ConnectionResult info = ConnectNodesWithIntersect(n1, n2, 0.5f);
        //     print(info.success + " , " + info.didIntersect + " , " + info.didSnap);

        //     if (info.success && !info.didIntersect && !info.didSnap) {
        //         AddNode(n2);
        //     }

        //     prevNode = info.prevNode;
        //     print("prev" + prevNode.pos);
        // }

        // this.prevNode = prevNode;

        //
        // Node node3 = new Node(Vector3.zero);
        // Node node4 = new Node(Vector3.zero);

        // AddNodeNearby(node3, 0.5f);
        // AddNodeNearby(node4, 0.5f);

    }

    public void AddAgent(Agent agent) {
        // if (_.find(this.agents, agent)) return;

        // this.queue.add(agent);
        this.queue.Enqueue(agent);
    }

    public IEnumerable<Node> FindNodesInRadius(Vector3 pos, float radius) {
        Envelope searchBounds = new Envelope(
            pos.x - radius,
            pos.z - radius,
            pos.x + radius,
            pos.z + radius
        );
        IEnumerable<Node> result = tree.Search(searchBounds);

        return result.Where(n => Vector3.Distance(pos, n.pos) < radius);
    }

    public Node AddNode(Node node) {
        nodes.Add(node);
        tree.Insert(node);

        node.added = true;
        node.id = this.increment++;

        return node;
    }

    public Node AddNodeNearby(Node node, float radius) {
        IEnumerable<Node> result = FindNodesInRadius(node.pos, radius);

        Node closestNode = Util.GetClosestNode(node, result);
        if (closestNode != null && Vector3.Distance(closestNode.pos, node.pos) <= radius) {
            return closestNode;
        }

        this.AddNode(node);
        return node;
    }

    public void RemoveNode(Node node) {
        this.nodes.Remove(node);
        this.tree.Delete(node);
    }

    public void UpdateNodeInTree(Node node) {
        // print("before: " + this.tree.Count + " , " + this.nodes.Count);
        this.tree.Delete(node);
        this.tree.Insert(node);
        // print("after: " + this.tree.Count + " , " + this.nodes.Count);
    }

    public bool ConnectNodes(Node node1, Node node2, Node.ConnectionType type = Node.ConnectionType.Street) {
        bool success = node1.ConnectTo(node2, type);

        if (success) {
            if (node1.added) this.UpdateNodeInTree(node1);
            if (node2.added) this.UpdateNodeInTree(node2);
        }

        return success;
    }

    public void DisconnectNodes(Node node1, Node node2) {
        node1.Disconnect(node2);

        if (node1.added) this.UpdateNodeInTree(node1);
        if (node2.added) this.UpdateNodeInTree(node2);
    }

    public class IntersectionInfo {
        public Node from;
        public Node.NodeConnection connection;
        public Vector2 point;
        public bool isProjection;

        public IntersectionInfo(Node from, Node.NodeConnection connection, Vector2 point, bool isProjection) {
            this.from = from;
            this.connection = connection;
            this.point = point;
            this.isProjection = isProjection;
        }
    }

    public class ConnectionResult {
        public bool success;
        public bool didIntersect;
        public bool didSnap;
        public Node prevNode;

        public ConnectionResult(bool success, bool didIntersect, bool didSnap, Node prevNode) {
            this.success = success;
            this.didIntersect = didIntersect;
            this.didSnap = didSnap;
            this.prevNode = prevNode;
        }
    }

    public ConnectionResult ConnectNodesWithIntersect(Node node1, Node node2, float snapRadius, Node.ConnectionType type = Node.ConnectionType.Street) {
        if (Vector3.Distance(node1.pos, node2.pos) <= snapRadius) {
            return new ConnectionResult(false, false, true, node1);
        }

        // Create bounding envelope containing both nodes, including some snapRadius margin
        Envelope bBox = Util.GetEnvelopeFromNodes(new List<Node>() { node1, node2 }, snapRadius);

        List<Node> nearestNodes = tree.Search(bBox).ToList();
        nearestNodes.Remove(node1);
        nearestNodes.Remove(node2);

        List<IntersectionInfo> intersections = new List<IntersectionInfo>();

        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
        bool success = false;
        bool shouldExtend = false;
        foreach (Node other in nearestNodes) {
            if (other == node1 || other == node2) continue;

            if (visited.ContainsKey(other)) continue;
            visited[other] = true;

            foreach (Node.NodeConnection connection in other.connections) {
                if (connection.node == node1 || connection.node == node2) continue;

                // This is to ensure intersection test is not performed on the same connection twice
                // This is due to nodes having bi-directional connections
                if (visited.ContainsKey(connection.node)) continue;


                // Perform a ray-line intersection test
                // This results in three scenarios:
                // 1. No intersection and no snapping should be done.
                // 2. The new connection line is intersecting with another connection, create intersection
                // 3. The new connection line is almost intersecting with another connection (it is within snapRadius),
                //    "extend" the new connection line so that it intersects with the existing connection
                Util.LineIntersection.Result intersection = Util.LineIntersection.RayTest(
                    Util.Vector3To2(other.pos),
                    Util.Vector3To2(connection.node.pos),
                    Util.Vector3To2(node1.pos),
                    Util.Vector3To2(node2.pos - node1.pos)
                );

                bool didIntersect = false;
                if (intersection.type == Util.LineIntersection.Type.Intersecting) {
                    // Scenario #2
                    if (intersection.factorB <= 1) {
                        intersections.Add(new IntersectionInfo(other, connection, intersection.point, false));
                        didIntersect = true;
                    }
                    // Scenario #3
                    else {
                        float distLine = Vector2.Distance(Util.Vector3To2(node2.pos), intersection.point);
                        if (distLine <= snapRadius) {
                            // The ray intersection handles the extension for us, so simply add this result
                            intersections.Add(new IntersectionInfo(other, connection, intersection.point, false));
                            shouldExtend = true;
                        }
                    }
                }

                // Try projecting the end node onto the edge and check if it is within snap radius
                if (!didIntersect) {
                    Vector2 proj = Util.GetProjectedPointOnLine(
                        Util.Vector3To2(node2.pos),
                        Util.Vector3To2(other.pos),
                        Util.Vector3To2(connection.node.pos)
                    );

                    float distProj = Vector2.Distance(proj, Util.Vector3To2(node2.pos));
                    if (distProj <= snapRadius) {
                        intersections.Add(new IntersectionInfo(other, connection, proj, true));
                        didIntersect = true;
                    }
                }
            }
        }

        // Sort intersections by closest, ignore projection intersections if an extension should happen instead
        List<IntersectionInfo> sortedIntersections = intersections
            .Where(n => shouldExtend ? !n.isProjection : true)
            .OrderBy(n => Vector2.Distance(Util.Vector3To2(node1.pos), n.point))
            .ToList();

        Node finalNode = node2;

        // If there is an intersection, create a ghost node at that point
        if (sortedIntersections.Count > 0) {
            IntersectionInfo info = sortedIntersections.First();
            finalNode = new Node(Util.Vector2To3(info.point), node1.type);
        }

        // TODO: These two statements can be merged into one for loop
        bool didSnap = false;
        foreach (Node other in nearestNodes) {
            if (other == node1) continue;

            float dist = Util.GetMinimumDistanceToLine(
                Util.Vector3To2(other.pos),
                Util.Vector3To2(node1.pos),
                Util.Vector3To2(node2.pos)
            );
            if (dist <= snapRadius) {
                if (Vector3.Distance(node1.pos, finalNode.pos) > Vector3.Distance(node1.pos, other.pos)) {
                    finalNode = other;
                    didSnap = true;
                    break;
                }
            }
        }
        // If no nodes were found along the line, try finding one near the end node
        if(!didSnap) {
            Node closestNode = Util.GetClosestNode(finalNode, nearestNodes);
            if (closestNode != null && Vector3.Distance(closestNode.pos, finalNode.pos) <= snapRadius) {
                if (Vector3.Distance(node1.pos, finalNode.pos) > Vector3.Distance(node1.pos, closestNode.pos)) {
                    finalNode = closestNode;
                    didSnap = true;
                }
            }
        }

        // If we haven't snapped yet (no pun intended), and there are intersections, do some more checks
        if (sortedIntersections.Count > 0 && !didSnap) {
            IntersectionInfo info = sortedIntersections.First();

            // We might want to snap to one of the edge nodes
            if (Vector3.Distance(info.from.pos, finalNode.pos) < snapRadius) {
                finalNode = info.from;
                didSnap = true;
            }
            else if (Vector3.Distance(info.connection.node.pos, finalNode.pos) < snapRadius) {
                finalNode = info.connection.node;
                didSnap = true;
            }
            // Our only option is now to just split the line
            else {
                AddNode(finalNode);

                // Split the connection to include the new intersection node
                DisconnectNodes(info.from, info.connection.node);
                ConnectNodes(info.from, finalNode, info.connection.type);
                ConnectNodes(info.connection.node, finalNode, info.connection.type);

                // Connect the origin node to the new intersection node
                ConnectNodes(node1, finalNode, type);

                return new ConnectionResult(false, true, false, finalNode);
            }
        }

        // If the final node is not the original destination node
        if (finalNode != node2) {
            ConnectNodes(node1, finalNode, type);

            return new ConnectionResult(false, false, true, finalNode);
        }

        // Looks like we got nowhere to snap or intersect to, but there are still connections
        // left to check from the origin node since these are ignored in the checks above
        foreach (Node.NodeConnection con in node1.connections) {
            // If we are close to a node on the other side of the connection, snap to it
            if (Vector3.Distance(node2.pos, con.node.pos) <= snapRadius) {
                return new ConnectionResult(false, false, true, con.node);
            }

            Vector2 proj = Util.GetProjectedPointOnLine(
                Util.Vector3To2(node2.pos),
                Util.Vector3To2(node1.pos),
                Util.Vector3To2(con.node.pos)
            );

            // Check if we should cut the connection at the projection point
            float distProj = Vector2.Distance(proj, Util.Vector3To2(node2.pos));
            if (distProj <= snapRadius) {
                Node n = new Node(Util.Vector2To3(proj), node1.type);

                AddNode(n);

                // Split the connection to include the new projected node
                DisconnectNodes(node1, con.node);
                ConnectNodes(node1, n, con.type);
                ConnectNodes(con.node, n, con.type);

                return new ConnectionResult(false, true, false, n);
            }
        }

        // If no intersections or no snapping are found, just connect the desired nodes
        success = ConnectNodes(node1, node2, type);

        return new ConnectionResult(success, false, false, node2);
    }

    IEnumerator DoAgentWork() {
        if (this.queue.Count == 0) yield break;

        areAgentsWorking = true;

        int iterations = 0;
        while (this.queue.Count > 0 && iterations < maxAgentQueueIterations) {
            Agent agent = this.queue.Dequeue();

            if (!agent.started) {
                agent.Start();
                agent.started = true;
            }

            agent.Work();

            Util.DebugDrawCircle(agent.pos, 0.2f, new Color(0, 1, 0));

            if (agent.terminated) {
                // this.queue.Dequeue();
            }
            else {
                this.queue.Enqueue(agent);
            }

            iterations++;
        }

        yield return new WaitForSeconds(generationTickInterval);
        areAgentsWorking = false;
    }

    void Update()
    {
        Vector3 mousePos = Util.GetPlaneMousePos(new Vector3(0, 0, 0));

        bool click = Input.GetButtonDown("Fire1");
        if (click && !prevClick) {

            Node node1 = this.prevNode;
            Node node2 = new Node(mousePos);

            if (node1 == null) {
                 node1 = AddNodeNearby(new Node(Vector3.zero), 0.2f);
            }


            ConnectionResult info = ConnectNodesWithIntersect(node1, node2, 0.2f);

            if (info.success && !info.didIntersect && !info.didSnap) {
                AddNode(node2);
            }

            prevNode = info.prevNode;
        }
        prevClick = click;

        if (!areAgentsWorking){
            StartCoroutine("DoAgentWork");
        }


        float padding = 1f;
        Envelope searchBounds = new Envelope(mousePos.x - padding, mousePos.z - padding, mousePos.x + padding, mousePos.z + padding);
        IEnumerable<Node> result = tree.Search(searchBounds);

        int count = 0;
        foreach (Node n in result) {
            n.hovering = true;
            count++;


            foreach (Node.NodeConnection c in n.connections)
            {
                Debug.DrawLine(n.pos, c.node.pos, new Color(1, 0, 0));
            }
        }

        Util.DebugDrawEnvelope(searchBounds, new Color(1, 1, 1, 0.1f));

        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();

        int idx = 0;
        foreach (Node n in nodes)
        {
            Util.DebugDrawCircle(n.pos, 0.025f, n.hovering ? new Color(0, 1, 0) : new Color(0, 1, 1), 3);

            // Util.DebugDrawEnvelope(n.Envelope, new Color(0, 0, 1, 0.1f));

            // Vector2 mousePos2 = Util.Vector3To2(mousePos);
            // Debug.DrawLine(this.prevNode != null ? this.prevNode.pos : Vector3.zero, mousePos);
            //
            visited[n] = true;

            foreach (Node.NodeConnection c in n.connections)
            {
                if (visited.ContainsKey(c.node)) continue;

                Debug.DrawLine(n.pos, c.node.pos, new Color(1, 0, 0));

                // Util.LineIntersection.Result intersection = Util.LineIntersection.RayTest(
                //     Util.Vector3To2(n.pos),
                //     Util.Vector3To2(c.node.pos),
                //     Vector2.zero,
                //     Util.Vector3To2(mousePos)
                // );

                // if (intersection.type == Util.LineIntersection.Type.Intersecting) {
                //     Util.DebugDrawCircle(Util.Vector2To3(intersection.point), 0.02f, new Color(1, 0, 1));
                //     print(intersection.factorB);
                // }
                // Vector2 proj = Util.GetProjectedPointOnLine(
                //     Util.Vector3To2(mousePos),
                //     Util.Vector3To2(c.node.pos),
                //     Util.Vector3To2(n.pos)
                // );
                // Util.DebugDrawCircle(Util.Vector2To3(proj), 0.02f, new Color(1, 0, 1));
            }

            idx++;
            n.hovering = false;
        }

        // foreach (Vector3 p in debugPoints){
        //     Util.DebugDrawCircle(p, 0.03f, new Color(1, 0.5f, 0));
        // }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), "node count: " + nodes.Count);
        GUI.Label(new Rect(10, 40, 100, 20), "tree count: " + tree.Count);
    }
}
