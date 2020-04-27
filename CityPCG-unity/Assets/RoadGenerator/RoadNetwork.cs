using System;
using System.Collections.Generic;
using System.Linq;
using RBush;
using UnityEngine;

public class RoadNetwork {
    private TerrainModel terrain;
    private Noise population;
    private float width;
    private float height;

    private List<Node> nodes;
    private RBush<Node> tree; // See: https://www.wikiwand.com/en/R-tree

    public TerrainModel Terrain {
        get {
            return terrain;
        }
    }

    public Noise Population {
        get {
            return population;
        }
    }


    public List<Node> Nodes {
        get {
            return nodes;
        }
    }

    public RBush<Node> Tree {
        get {
            return tree;
        }
    }

    public float Width { get { return width; } }
    public float Height { get { return height; } }

    public RoadNetwork(TerrainModel terrain, Noise population, float width, float height) {
        this.terrain = terrain;
        this.population = population;
        this.width = width;
        this.height = height;

        tree = new RBush<Node>();
        nodes = new List<Node>();
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

        return node;
    }

    public Node AddNodeNearby(Node node, float radius) {
        IEnumerable<Node> result = FindNodesInRadius(node.pos, radius);

        Node closestNode = GetClosestNode(node, result);
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

    // Update value of a node.
    public void UpdateNodeInTree(Node node) {
        this.tree.Delete(node);
        this.tree.Insert(node);
    }

    public bool ConnectNodes(Node node1, Node node2, ConnectionType type = ConnectionType.Street) {
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

    public ConnectionResult ConnectNodesWithIntersect(Node node1, Node node2, float snapRadius, ConnectionType type = ConnectionType.Street) {
        if (Vector3.Distance(node1.pos, node2.pos) <= snapRadius) {
            return new ConnectionResult(false, false, true, node1);
        }

        // Create bounding envelope containing both nodes, including some snapRadius margin
        Envelope bBox = GetEnvelopeFromNodes(new List<Node>() { node1, node2 }, snapRadius);

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

            foreach (NodeConnection connection in other.connections) {
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
                LineIntersection.Result intersection = LineIntersection.RayTest(
                    VectorUtil.Vector3To2(other.pos),
                    VectorUtil.Vector3To2(connection.node.pos),
                    VectorUtil.Vector3To2(node1.pos),
                    VectorUtil.Vector3To2(node2.pos - node1.pos)
                );

                bool didIntersect = false;
                if (intersection.type == LineIntersection.Type.Intersecting) {
                    // Scenario #2
                    if (intersection.factorB <= 1) {
                        intersections.Add(new IntersectionInfo(other, connection, intersection.point, false));
                        didIntersect = true;
                    }
                    // Scenario #3
                    else {
                        float distLine = Vector2.Distance(VectorUtil.Vector3To2(node2.pos), intersection.point);
                        if (distLine <= snapRadius) {
                            // The ray intersection handles the extension for us, so simply add this result
                            intersections.Add(new IntersectionInfo(other, connection, intersection.point, false));
                            shouldExtend = true;
                        }
                    }
                }

                // Try projecting the end node onto the edge and check if it is within snap radius
                if (!didIntersect) {
                    Vector2 proj = VectorUtil.GetProjectedPointOnLine(
                        VectorUtil.Vector3To2(node2.pos),
                        VectorUtil.Vector3To2(other.pos),
                        VectorUtil.Vector3To2(connection.node.pos)
                    );

                    float distProj = Vector2.Distance(proj, VectorUtil.Vector3To2(node2.pos));
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
            .OrderBy(n => Vector2.Distance(VectorUtil.Vector3To2(node1.pos), n.point))
            .ToList();

        Node finalNode = node2;

        // If there is an intersection, create a ghost node at that point
        if (sortedIntersections.Count > 0) {
            IntersectionInfo info = sortedIntersections.First();
            finalNode = this.CreateNode(info.point, node1.type);
        }

        Vector3 dir = (finalNode.pos - node1.pos).normalized;
        Dictionary<Node, bool> neighbors = new Dictionary<Node, bool>();
        foreach (NodeConnection con in node1.connections) {
            neighbors[con.node] = true;

            // TODO: Do not hardcode this
            float angle = Vector3.Angle(con.node.pos - node1.pos, dir);
            if (angle < 20) {
                finalNode = con.node;
            }
        }

        // TODO: These two statements can be merged into one for loop
        bool didSnap = false;
        foreach (Node other in nearestNodes) {
            if (other == node1) continue;

            float dist = VectorUtil.GetMinimumDistanceToLine(
                VectorUtil.Vector3To2(other.pos),
                VectorUtil.Vector3To2(node1.pos),
                VectorUtil.Vector3To2(node2.pos)
            );
            if (dist <= snapRadius) {
                if (neighbors.ContainsKey(other) || Vector3.Distance(node1.pos, finalNode.pos) > Vector3.Distance(node1.pos, other.pos)) {
                    finalNode = other;
                    didSnap = true;
                    break;
                }
            }
        }

        // If no nodes were found along the line, try finding one near the end node
        if (!didSnap) {
            Node closestNode = GetClosestNode(finalNode, nearestNodes);
            if (closestNode != null && Vector3.Distance(closestNode.pos, finalNode.pos) <= snapRadius) {
                finalNode = closestNode;
                didSnap = true;
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
        foreach (NodeConnection con in node1.connections) {
            // If we are close to a node on the other side of the connection, snap to it
            if (Vector3.Distance(node2.pos, con.node.pos) <= snapRadius) {
                return new ConnectionResult(false, false, true, con.node);
            }

            Vector2 proj = VectorUtil.GetProjectedPointOnLine(
                VectorUtil.Vector3To2(node2.pos),
                VectorUtil.Vector3To2(node1.pos),
                VectorUtil.Vector3To2(con.node.pos)
            );

            // Check if we should cut the connection at the projection point
            float distProj = Vector2.Distance(proj, VectorUtil.Vector3To2(node2.pos));
            if (distProj <= snapRadius) {
                Node n = this.CreateNode(proj, node1.type);

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

    public void DrawDebug(bool drawNodes = false) {
        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();

        int idx = 0;
        foreach (Node n in nodes) {
            visited[n] = true;

            bool hasCloseNodes = false;
            foreach (NodeConnection c in n.connections) {
                if (visited.ContainsKey(c.node)) continue;

                var color = new Color(1, 0, 0);
                if (c.type == ConnectionType.Street)
                    color = new Color(0, 1, 0);
                else if (c.type == ConnectionType.Highway)
                    color = new Color(0, 0, 1);

                Debug.DrawLine(n.pos, c.node.pos, color);

                if (Vector3.Distance(c.node.pos, n.pos) < 0.1f)
                    hasCloseNodes = true;
            }

            if (hasCloseNodes)
                DrawUtil.DebugDrawCircle(n.pos, 0.1f, Color.blue);

            if (drawNodes)
                DrawUtil.DebugDrawCircle(n.pos, 0.05f, Color.blue, 3);

            idx++;
        }
    }

    public static Envelope GetEnvelopeFromNodes(IEnumerable<Node> nodes, float padding = 0) {
        float minX = float.MaxValue;
        float minZ = float.MaxValue;

        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach (Node node in nodes) {
            minX = Mathf.Min(minX, node.pos.x);
            minZ = Mathf.Min(minZ, node.pos.z);

            maxX = Mathf.Max(maxX, node.pos.x);
            maxZ = Mathf.Max(maxZ, node.pos.z);
        }

        return new Envelope(minX - padding, minZ - padding, maxX + padding, maxZ + padding);
    }

    public static Node GetClosestNode(Node node, IEnumerable<Node> nodes) {
        float leastDistance = float.MaxValue;
        Node leastNode = null;

        foreach (Node n in nodes) {
            if (n == node) continue;

            float dist = Vector3.Distance(n.pos, node.pos);
            if (dist < leastDistance) {
                leastNode = n;
                leastDistance = dist;
            }
        }

        return leastNode;
    }

    public Node CreateNode(Vector2 pos, Node.NodeType nodeType) {
        Vector3 terrainPos = new Vector3(pos.x, terrain.GetHeight(pos.x, pos.y), pos.y);
        return new Node(terrainPos, nodeType);
    }

    public RoadNetwork Snapshot() {
        RoadNetwork snapshot = new RoadNetwork(
            this.terrain, this.population, this.width, this.height
        );

        Dictionary<Node, Node> cloneMap = new Dictionary<Node, Node>();

        snapshot.nodes = new List<Node>();
        foreach (Node node in nodes) {
            Node clonedNode = snapshot.AddNode(node.Clone());
            cloneMap[node] = clonedNode;

            // This will clone connections only one way since the
            // other node might not have been cloned yet.
            // Luckily, Node class handles the bi-directional
            // connection anyway so it is not a problem here!
            foreach (NodeConnection c in node.connections) {
                if (cloneMap.ContainsKey(c.node)) {
                    clonedNode.ConnectTo(cloneMap[c.node], c.type);
                }
            }
        }


        return snapshot;
    }
}
