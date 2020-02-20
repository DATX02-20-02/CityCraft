using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RBush;

public class RoadGenerator : MonoBehaviour
{
    public const int MAX_AGENT_QUEUE_ITERATIONS = 10;

    private List<Node> nodes;
    private RBush<Node> tree;
    private PriorityQueue<Agent> queue;

    private bool prevClick;
    private Node prevNode;

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

        // var generator = new ParisCityGenerator();
        // generator.Generate(this, new Vector3(0, 0, 0));

        Node node1 = new Node(new Vector3(1.1f, 0, 1));
        Node node2 = new Node(new Vector3(1, 0, -1));
        // Node node3 = new Node(new Vector3(-0.5f, 0, 0));

        AddNode(node1);
        AddNode(node2);
        // AddNode(node3);

        ConnectNodes(node1, node2);
        // ConnectNodes(node2, node3);
        //
        Node node3 = new Node(Vector3.zero);
        Node node4 = new Node(Vector3.zero);

        AddNodeNearby(node3, 0.5f);
        AddNodeNearby(node4, 0.5f);

    }

    public void AddAgent(Agent agent) {
        // if (_.find(this.agents, agent)) return;

        // this.queue.add(agent);
        this.queue.Enqueue(agent);
        agent.Start();
        print("Adding agent");
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

        print("now: " + this.tree.Count + " , " + nodes.Count);
        return node;
    }

    public Node AddNodeNearby(Node node, float radius) {
        IEnumerable<Node> result = FindNodesInRadius(node.pos, radius);

        Node closestNode = Util.GetClosestNode(node, result);
        print(closestNode);
        if (closestNode != null && Vector3.Distance(closestNode.pos, node.pos) <= radius) {
            print("found closest");
            return closestNode;
        }

        print("new node");
        this.AddNode(node);
        return node;
    }

    public void RemoveNode(Node node) {
        this.nodes.Remove(node);
        this.tree.Delete(node);
    }

    public void UpdateNodeInTree(Node node) {
        print("before: " + this.tree.Count + " , " + this.nodes.Count);
        this.tree.Delete(node);
        this.tree.Insert(node);
        print("after: " + this.tree.Count + " , " + this.nodes.Count);
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
        public Util.LineIntersection.Result result;

        public IntersectionInfo(Node from, Node.NodeConnection connection, Util.LineIntersection.Result result) {
            this.from = from;
            this.connection = connection;
            this.result = result;
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

        IEnumerable<Node> results = tree.Search(bBox);

        // Sort by closest nodes
        List<Node> sortedRes = results
            .OrderBy(n => Vector3.Distance(node1.pos, n.pos))
            .ToList();

        List<IntersectionInfo> intersections = new List<IntersectionInfo>();

        Dictionary<Node, bool> added = new Dictionary<Node, bool>();
        bool success = false;
        foreach (Node other in sortedRes) {
            if (other == node1 || other == node2) continue;

            // First, check if the node is within snapRadius of the destination node
            // If so, snap to it
            if (Vector3.Distance(other.pos, node2.pos) <= snapRadius) {
                ConnectionResult res = ConnectNodesWithIntersect(node1, other, snapRadius, type);

                return new ConnectionResult(false, res.didIntersect, res.didSnap, res.prevNode);
            }

            // Check nodes along the desired connection line
            // If found, snap to that node
            float dist = Util.GetMinimumDistanceToLine(
                Util.Vector3To2(other.pos),
                Util.Vector3To2(node1.pos),
                Util.Vector3To2(node2.pos)
            );
            if (dist <= snapRadius && Vector3.Distance(other.pos, node1.pos) > snapRadius) {
                success = this.ConnectNodes(node1, other, type);

                return new ConnectionResult(false, false, true, other);
            }

            // This is to ensure intersection test is not performed on the same connection twice
            // This is due to nodes having bi-directional connections
            if (added.ContainsKey(other)) continue;
            added[other] = true;

            foreach (Node.NodeConnection connection in other.connections) {
                if (connection.node == node1 || connection.node == node2) continue;
                if (added.ContainsKey(connection.node)) continue;
                added[connection.node] = true;

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

                if (intersection.type == Util.LineIntersection.Type.Intersecting) {
                    // Scenario #2
                    if (intersection.factorB <= 1) {
                        intersections.Add(new IntersectionInfo(other, connection, intersection));
                    }
                    // Scenario #3
                    else {
                        float distLine = Vector2.Distance(Util.Vector3To2(node2.pos), intersection.point);
                        if (distLine <= snapRadius) {
                            // The ray intersection handles the extension for us, so simply add this result
                            intersections.Add(new IntersectionInfo(other, connection, intersection));
                        }
                    }
                }
            }
        }

        // Resolve potential intersections
        if (intersections.Count > 0) {
            // Multiple intersections can occur, make sure we take the closest one
            List<IntersectionInfo> sorted = intersections
                .OrderBy(n => Vector2.Distance(Util.Vector3To2(node1.pos), n.result.point))
                .ToList();

            // TODO: This does not need to be a for loop, since all it needs is the first element
            foreach(IntersectionInfo info in sorted) {
                Node n = AddNode(new Node(Util.Vector2To3(info.result.point), node1.type));

                // Split the connection to include the new intersection node
                DisconnectNodes(info.from, info.connection.node);
                ConnectNodes(info.from, n, info.connection.type);
                ConnectNodes(info.connection.node, n, info.connection.type);

                // Connect the origin node to the new intersection node
                ConnectNodes(node1, n, type);

                return new ConnectionResult(false, true, false, n);
            }
        }

        // If no intersections or no snapping are found, just connect the desired nodes
        success = ConnectNodes(node1, node2, type);

        return new ConnectionResult(success, false, false, node2);
    }

    void DoAgentWork() {
        if (this.queue.Count == 0) return;

        int iterations = 0;
        while (this.queue.Count > 0 && iterations < MAX_AGENT_QUEUE_ITERATIONS) {
            Agent agent = this.queue.Peek();
            agent.Work();

            Util.DebugDrawCircle(agent.pos, 0.2f, new Color(0, 1, 0));

            print("Do work on agent with priority " + agent.priority);

            if (agent.dead) {
                print("Agent with priority " + agent.priority + " just died");
                this.queue.Dequeue();
            }

            iterations++;
        }
    }

    void Update()
    {
        Vector3 mousePos = Util.GetPlaneMousePos(new Vector3(0, 0, 0));

        bool click = Input.GetButtonDown("Fire1");
        if (click && !prevClick) {

            Node node1 = this.prevNode;
            Node node2 = new Node(mousePos);

            if (node1 == null) {
                 node1 = AddNodeNearby(new Node(Vector3.zero), 0.5f);
            }


            ConnectionResult info = ConnectNodesWithIntersect(node1, node2, 0.5f);
            print(info.success + " , " + info.didIntersect + " , " + info.didSnap);

            if (info.success && !info.didIntersect && !info.didSnap) {
                AddNode(node2);
                print("adding node");
            }

            prevNode = info.prevNode;
        }
        prevClick = click;

        DoAgentWork();


        Envelope searchBounds = new Envelope(mousePos.x - 0.05f, mousePos.z - 0.05f, mousePos.x + 0.05f, mousePos.z + 0.05f);
        IEnumerable<Node> result = tree.Search(searchBounds);

        int count = 0;
        foreach (Node n in result) {
            n.hovering = true;
            count++;
        }
        // print(count);

        Util.DebugDrawEnvelope(searchBounds, new Color(1, 1, 1, 0.1f));

        int idx = 0;
        foreach (Node n in nodes)
        {
            Util.DebugDrawCircle(n.pos, 0.025f + idx / 100.0f, n.hovering ? new Color(0, 1, 0) : new Color(0, 1, 1));

            Util.DebugDrawEnvelope(n.Envelope, new Color(0, 0, 1, 0.1f));

            Vector2 mousePos2 = Util.Vector3To2(mousePos);
            Debug.DrawLine(this.prevNode != null ? this.prevNode.pos : Vector3.zero, mousePos);

            foreach (Node.NodeConnection c in n.connections)
            {
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
            }

            idx++;
            n.hovering = false;
        }
    }
}
