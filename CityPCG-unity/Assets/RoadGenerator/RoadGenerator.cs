// #define DEBUG_AGENT_WORK

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RBush;
using VisualDebugging;

/*
  What? Generates a road network based on terrain and population.
  Why? Roads are needed for cities.
  How? Uses agent-based generation, where each agent places down nodes and edges between them.
*/
public class RoadGenerator : MonoBehaviour {
    [Range(0, 1000)]
    [SerializeField] private int maxAgentQueueIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float generationTickInterval = 0.2f;

    [SerializeField] private List<Vector3> debugPoints = new List<Vector3>();

    private List<Node> nodes;
    private RBush<Node> tree; // See: https://www.wikiwand.com/en/R-tree
    private PriorityQueue<Agent> queue;
    private int prevQueueCount = 0;

    private bool prevClick;
    private Node prevNode;
    private int increment;

    private bool areAgentsWorking = false;


    // Generates a complete road network.
    public void Generate() {
        tree = new RBush<Node>();
        nodes = new List<Node>();
        queue = new PriorityQueue<Agent>();

        IAgentFactory factory = new ParisAgentFactory();
        factory.Create(this, new Vector3(0, 0, 0));
        factory.Create(this, new Vector3(20, 0, 0));
    }

    // Adds an agent to the pool of active agents.
    public void AddAgent(Agent agent) {
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

    // Add road node
    public Node AddNode(Node node) {
        nodes.Add(node);
        tree.Insert(node);

        node.added = true;
        node.id = this.increment++;

        return node;
    }

    public Node AddNodeNearby(Node node, float radius) {
        IEnumerable<Node> result = FindNodesInRadius(node.pos, radius);

        Node closestNode = Node.GetClosestNode(node, result);
        if(closestNode != null && Vector3.Distance(closestNode.pos, node.pos) <= radius) {
            return closestNode;
        }

        this.AddNode(node);
        return node;
    }

    public void RemoveNode(Node node) {
        this.nodes.Remove(node);
        this.tree.Delete(node);
    }

    // Update value of a node.
    public void UpdateNodeInTree(Node node) {
        this.tree.Delete(node);
        this.tree.Insert(node);
    }

    public bool ConnectNodes(Node node1, Node node2, Node.ConnectionType type = Node.ConnectionType.Street) {
        bool success = node1.ConnectTo(node2, type);

        if(success) {
            if(node1.added) this.UpdateNodeInTree(node1);
            if(node2.added) this.UpdateNodeInTree(node2);
        }

        return success;
    }

    public void DisconnectNodes(Node node1, Node node2) {
        node1.Disconnect(node2);

        if(node1.added) this.UpdateNodeInTree(node1);
        if(node2.added) this.UpdateNodeInTree(node2);
    }

    public ConnectionResult ConnectNodesWithIntersect(Node node1, Node node2, float snapRadius, Node.ConnectionType type = Node.ConnectionType.Street) {
        if(Vector3.Distance(node1.pos, node2.pos) <= snapRadius) {
            return new ConnectionResult(false, false, true, node1);
        }

        // Create bounding envelope containing both nodes, including some snapRadius margin
        Envelope bBox = Node.GetEnvelopeFromNodes(new List<Node>() { node1, node2 }, snapRadius);

        List<Node> nearestNodes = tree.Search(bBox).ToList();
        nearestNodes.Remove(node1);
        nearestNodes.Remove(node2);

        List<IntersectionInfo> intersections = new List<IntersectionInfo>();

        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
        bool success = false;
        bool shouldExtend = false;
        foreach(Node other in nearestNodes) {
            if(other == node1 || other == node2) continue;

            if(visited.ContainsKey(other)) continue;
            visited[other] = true;

            foreach(Node.NodeConnection connection in other.connections) {
                if(connection.node == node1 || connection.node == node2) continue;

                // This is to ensure intersection test is not performed on the same connection twice
                // This is due to nodes having bi-directional connections
                if(visited.ContainsKey(connection.node)) continue;


                // Perform a ray-line intersection test
                // This results in three scenarios:
                // 1. No intersection and no snapping should be done.
                // 2. The new connection line is intersecting with another connection, create intersection
                // 3. The new connection line is almost intersecting with another connection (it is within snapRadius),
                //    "extend" the new connection line so that it intersects with the existing connection
                LineIntersection.Result intersection = LineIntersection.RayTest(
                    VectorUtil.Vector3To2(other.pos),
                    VectorUtil.Vector3To2(connection.node.pos),
                    VectorUtil.Vector3To2(node1.pos),
                    VectorUtil.Vector3To2(node2.pos - node1.pos)
                );

                bool didIntersect = false;
                if(intersection.type == LineIntersection.Type.Intersecting) {
                    // Scenario #2
                    if(intersection.factorB <= 1) {
                        intersections.Add(new IntersectionInfo(other, connection, intersection.point, false));
                        didIntersect = true;
                    }
                    // Scenario #3
                    else {
                        float distLine = Vector2.Distance(VectorUtil.Vector3To2(node2.pos), intersection.point);
                        if(distLine <= snapRadius) {
                            // The ray intersection handles the extension for us, so simply add this result
                            intersections.Add(new IntersectionInfo(other, connection, intersection.point, false));
                            shouldExtend = true;
                        }
                    }
                }

                // Try projecting the end node onto the edge and check if it is within snap radius
                if(!didIntersect) {
                    Vector2 proj = VectorUtil.GetProjectedPointOnLine(
                        VectorUtil.Vector3To2(node2.pos),
                        VectorUtil.Vector3To2(other.pos),
                        VectorUtil.Vector3To2(connection.node.pos)
                    );

                    float distProj = Vector2.Distance(proj, VectorUtil.Vector3To2(node2.pos));
                    if(distProj <= snapRadius) {
                        intersections.Add(new IntersectionInfo(other, connection, proj, true));
                        didIntersect = true;
                    }
                }
            }
        }

        // Sort intersections by closest, ignore projection intersections if an extension should happen instead
        List<IntersectionInfo> sortedIntersections = intersections
            .Where(n => shouldExtend ? !n.isProjection : true)
            .OrderBy(n => Vector2.Distance(VectorUtil.Vector3To2(node1.pos), n.point))
            .ToList();

        Node finalNode = node2;

        // If there is an intersection, create a ghost node at that point
        if(sortedIntersections.Count > 0) {
            IntersectionInfo info = sortedIntersections.First();
            finalNode = new Node(VectorUtil.Vector2To3(info.point), node1.type);
        }

        // TODO: These two statements can be merged into one for loop
        bool didSnap = false;
        foreach(Node other in nearestNodes) {
            if(other == node1) continue;

            float dist = VectorUtil.GetMinimumDistanceToLine(
                VectorUtil.Vector3To2(other.pos),
                VectorUtil.Vector3To2(node1.pos),
                VectorUtil.Vector3To2(node2.pos)
            );
            if(dist <= snapRadius) {
                if(Vector3.Distance(node1.pos, finalNode.pos) > Vector3.Distance(node1.pos, other.pos)) {
                    finalNode = other;
                    didSnap = true;
                    break;
                }
            }
        }
        // If no nodes were found along the line, try finding one near the end node
        if(!didSnap) {
            Node closestNode = Node.GetClosestNode(finalNode, nearestNodes);
            if(closestNode != null && Vector3.Distance(closestNode.pos, finalNode.pos) <= snapRadius) {
                if(Vector3.Distance(node1.pos, finalNode.pos) > Vector3.Distance(node1.pos, closestNode.pos)) {
                    finalNode = closestNode;
                    didSnap = true;
                }
            }
        }

        // If we haven't snapped yet (no pun intended), and there are intersections, do some more checks
        if(sortedIntersections.Count > 0 && !didSnap) {
            IntersectionInfo info = sortedIntersections.First();

            // We might want to snap to one of the edge nodes
            if(Vector3.Distance(info.from.pos, finalNode.pos) < snapRadius) {
                finalNode = info.from;
                didSnap = true;
            }
            else if(Vector3.Distance(info.connection.node.pos, finalNode.pos) < snapRadius) {
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
        if(finalNode != node2) {
            ConnectNodes(node1, finalNode, type);

            return new ConnectionResult(false, false, true, finalNode);
        }

        // Looks like we got nowhere to snap or intersect to, but there are still connections
        // left to check from the origin node since these are ignored in the checks above
        foreach(Node.NodeConnection con in node1.connections) {
            // If we are close to a node on the other side of the connection, snap to it
            if(Vector3.Distance(node2.pos, con.node.pos) <= snapRadius) {
                return new ConnectionResult(false, false, true, con.node);
            }

            Vector2 proj = VectorUtil.GetProjectedPointOnLine(
                VectorUtil.Vector3To2(node2.pos),
                VectorUtil.Vector3To2(node1.pos),
                VectorUtil.Vector3To2(con.node.pos)
            );

            // Check if we should cut the connection at the projection point
            float distProj = Vector2.Distance(proj, VectorUtil.Vector3To2(node2.pos));
            if(distProj <= snapRadius) {
                Node n = new Node(VectorUtil.Vector2To3(proj), node1.type);

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
        if(this.queue.Count == 0) yield break;

#if DEBUG_AGENT_WORK
        if (prevQueueCount == 0) {
            VisualDebug.Initialize();
            VisualDebug.BeginFrame("Start", true);
        }
#endif

        areAgentsWorking = true;

        int iterations = 0;
        while(this.queue.Count > 0 && iterations < maxAgentQueueIterations) {
            Agent agent = this.queue.Peek();
            if(agent.requeue) this.queue.Dequeue();

            Vector3 oldPos = agent.pos;

            if(!agent.started) {
                agent.Start();
                agent.started = true;
            }


            agent.Work();

            if(agent.terminated) {
                if(!agent.requeue)
                    this.queue.Dequeue();

            }
            else {
                if(agent.requeue)
                    this.queue.Enqueue(agent);
            }

#if DEBUG_AGENT_WORK
            if (agent.terminated) {
                VisualDebug.BeginFrame("Agent terminated", true);
                VisualDebug.SetColour(Colours.lightRed);
            }
            else {
                VisualDebug.BeginFrame("Agent work", true);
                VisualDebug.SetColour(Colours.lightGreen, Colours.veryDarkGrey);
            }
            VisualDebug.DrawPoint(agent.pos, .05f);

            VisualDebug.SetColour(Colours.lightGreen, Colours.veryDarkGrey);
            VisualDebug.DrawLineSegment(agent.pos, oldPos);
#endif

            iterations++;
        }

#if DEBUG_AGENT_WORK
        if (this.queue.Count == 0) {
            VisualDebug.Save();
        }
#endif

        prevQueueCount = this.queue.Count;

        yield return new WaitForSeconds(generationTickInterval);
        areAgentsWorking = false;
    }

    private void OnGUI() {
        if(tree == null)
            return;

        GUI.Label(new Rect(10, 10, 100, 20), "node count: " + nodes.Count);
        GUI.Label(new Rect(10, 40, 100, 20), "tree count: " + tree.Count);
    }

    private void Start() {
        // Generate();
    }

    private void Update() {
        if(tree == null)
            return;

        Vector3 mousePos = VectorUtil.GetPlaneMousePos(new Vector3(0, 0, 0));

        bool click = Input.GetButtonDown("Fire1");
        if(click && !prevClick) {

            Node node1 = this.prevNode;
            Node node2 = new Node(mousePos);

            if(node1 == null) {
                node1 = AddNodeNearby(new Node(Vector3.zero), 0.2f);
            }


            ConnectionResult info = ConnectNodesWithIntersect(node1, node2, 0.2f);

            if(info.success && !info.didIntersect && !info.didSnap) {
                AddNode(node2);
            }

            prevNode = info.prevNode;
        }
        prevClick = click;

        if(!areAgentsWorking) {
            StartCoroutine("DoAgentWork");
        }


        float padding = 1f;
        Envelope searchBounds = new Envelope(mousePos.x - padding, mousePos.z - padding, mousePos.x + padding, mousePos.z + padding);
        IEnumerable<Node> result = tree.Search(searchBounds);

        int count = 0;
        foreach(Node n in result) {
            n.hovering = true;
            count++;


            foreach(Node.NodeConnection c in n.connections) {
                Debug.DrawLine(n.pos, c.node.pos, new Color(1, 0, 0));
            }
        }

        DrawUtil.DebugDrawEnvelope(searchBounds, new Color(1, 1, 1, 0.1f));

        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();

        int idx = 0;
        foreach(Node n in nodes) {
            visited[n] = true;

            foreach(Node.NodeConnection c in n.connections) {
                if(visited.ContainsKey(c.node)) continue;

                var color = new Color(1, 0, 0);
                if(c.type == Node.ConnectionType.Street)
                    color = new Color(0, 1, 0);

                Debug.DrawLine(n.pos, c.node.pos, color);
            }

            idx++;
            n.hovering = false;
        }

        foreach(Vector3 p in debugPoints) {
            DrawUtil.DebugDrawCircle(p, 0.03f, new Color(1, 0.5f, 0));
        }
    }
}
