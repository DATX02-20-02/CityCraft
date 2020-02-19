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
        Node node3 = new Node(new Vector3(-0.5f, 0, 0));

        AddNode(node1);
        AddNode(node2);
        AddNode(node3);

        ConnectNodes(node1, node2);
        ConnectNodes(node2, node3);

    }

    public void AddAgent(Agent agent) {
        // if (_.find(this.agents, agent)) return;

        // this.queue.add(agent);
        this.queue.Enqueue(agent);
        agent.Start();
        print("Adding agent");
    }


    public Node AddNode(Node node) {
        nodes.Add(node);
        tree.Insert(node);

        node.added = true;

        return node;
    }

    public Node AddNodeNearby(Node node, float radius) {
        Envelope searchBounds = new Envelope(
            node.pos.x - radius,
            node.pos.z - radius,
            node.pos.x + radius,
            node.pos.z + radius
        );
        IEnumerable<Node> result = tree.Search(searchBounds);

        Node closestNode = Util.GetClosestNode(node, result);
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
        print("before: " + this.tree.Count);
        this.tree.Delete(node);
        this.tree.Insert(node);
        print("after: " + this.tree.Count);
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
        Envelope bBox = Util.GetEnvelopeFromNodes(new List<Node>() { node1, node2 });

        IEnumerable<Node> results = tree.Search(bBox);

        bool success = false;
        bool didSnap = false;
        bool didIntersect = false;
        Node prevNode = node1;
        List<IntersectionInfo> intersections = new List<IntersectionInfo>();

        foreach (Node other in results) {
            if (other == node1 || other == node2) continue;

            foreach (Node.NodeConnection connection in other.connections) {
                if (connection.node == node1) continue;

                Util.LineIntersection.Result intersection = Util.LineIntersection.CheckIntersection(
                    Util.Vector3To2(node2.pos),
                    Util.Vector3To2(node1.pos),
                    Util.Vector3To2(other.pos),
                    Util.Vector3To2(connection.node.pos)
                );

                if (intersection.type == Util.LineIntersection.Type.Intersecting) {
                    intersections.Add(new IntersectionInfo(other, connection, intersection));
                }
            }
        }

        List<IntersectionInfo> sorted = intersections
            .OrderBy(n => Vector2.Distance(Util.Vector3To2(prevNode.pos), n.result.point))
            .ToList();

        foreach(IntersectionInfo info in sorted) {
            if (Vector2.Distance(Util.Vector3To2(info.from.pos), info.result.point) < snapRadius) {
                success = this.ConnectNodes(node1, info.from, type);
                this.ConnectNodes(info.from, node2, type);
                didSnap = true;
                break;
            }
            else if (Vector2.Distance(Util.Vector3To2(info.connection.node.pos), info.result.point) < snapRadius) {
                success = this.ConnectNodes(node1, info.connection.node, type);
                this.ConnectNodes(info.connection.node, node2, type);
                didSnap = true;
                break;
            }

            print("adding intersection node");
            Node n = AddNode(new Node(Util.Vector2To3(info.result.point), node1.type));

            ConnectNodes(node1, n, type);
            ConnectNodes(n, node2, type);

            DisconnectNodes(info.from, info.connection.node);
            ConnectNodes(info.from, n, info.connection.type);
            ConnectNodes(info.connection.node, n, info.connection.type);

            prevNode = n;

            didIntersect = true;
        }

        if (intersections.Count == 0) {
            success = ConnectNodes(node1, node2, type);
            return new ConnectionResult(success, didIntersect, didSnap, node2);
        }

        return new ConnectionResult(success, didIntersect, didSnap, prevNode);
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
            print("adding node");

            Node node1 = new Node(Vector3.zero);
            Node node2 = new Node(mousePos);
            node1 = AddNodeNearby(node1, 0.1f);
            AddNode(node2);

            ConnectNodesWithIntersect(node1, node2, 0.1f);
        }
        prevClick = click;

        DoAgentWork();


        Envelope searchBounds = new Envelope(mousePos.x - 0.01f, mousePos.z - 0.01f, mousePos.x + 0.01f, mousePos.z + 0.01f);
        IEnumerable<Node> result = tree.Search(searchBounds);

        int count = 0;
        foreach (Node n in result) {
            n.hovering = true;
            count++;
        }
        // print(count);

        Util.DebugDrawEnvelope(searchBounds, new Color(1, 1, 1, 0.1f));

        foreach (Node n in nodes)
        {
            Util.DebugDrawCircle(n.pos, 0.025f, n.hovering ? new Color(0, 1, 0) : new Color(0, 1, 1));

            Util.DebugDrawEnvelope(n.Envelope, new Color(0, 0, 1, 0.1f));

            Vector2 mousePos2 = Util.Vector3To2(mousePos);
            Debug.DrawLine(Vector3.zero, mousePos);

            foreach (Node.NodeConnection c in n.connections)
            {
                Debug.DrawLine(n.pos, c.node.pos, new Color(1, 0, 0));

                Util.LineIntersection.Result res = Util.LineIntersection.CheckIntersection(
                    Util.Vector3To2(n.pos),
                    Util.Vector3To2(c.node.pos),
                    Vector2.zero, mousePos2
                );


                if (res.type == Util.LineIntersection.Type.Intersecting){
                    Util.DebugDrawCircle(new Vector3(res.point.x, 0, res.point.y), 0.01f, new Color(1, 0, 1));
                }
            }

            n.hovering = false;
        }
    }
}
