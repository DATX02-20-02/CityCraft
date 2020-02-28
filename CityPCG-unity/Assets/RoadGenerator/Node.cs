using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RBush;

public class Node : ISpatialData {
    public class NodeConnection {
        public Node node;
        public ConnectionType type;

        public NodeConnection(Node node, ConnectionType type) {
            this.node = node;
            this.type = type;
        }
    }

    public static int MAX_NODE_CONNECTIONS = 8;

    public enum NodeType {
        Main,
        Highway,
        Street
    }

    public enum ConnectionType {
        None,
        Main,
        Highway,
        Street
    };

    public override string ToString() {
        return "Node " + this.id;
    }

    public int id = 0;
    public Vector3 pos;
    public float width;
    public float height;
    public NodeType type;
    public bool added = false;
    public bool hovering = false;

    public List<NodeConnection> connections = new List<NodeConnection>();
    private Envelope envelope = Envelope.EmptyBounds;

    public Node(Vector3 pos, NodeType type = NodeType.Main) {
        this.pos = pos;
        this.type = type;

        UpdateEnvelope();
    }

    public bool HasConnection(Node other) {
        foreach(NodeConnection connection in connections) {
            if(connection.node.Equals(other)) return true;
        }

        return false;
    }

    public bool ConnectTo(Node node, ConnectionType type = ConnectionType.Street) {
        if(HasConnection(node)) return false;

        this.connections.Add(new NodeConnection(node, type));
        node.connections.Add(new NodeConnection(this, type));

        this.UpdateEnvelope();
        node.UpdateEnvelope();

        return true;
    }

    public bool Disconnect(Node node) {
        if(!HasConnection(node)) return false;

        this.connections.RemoveAll(c => c.node.Equals(node));
        node.connections.RemoveAll(c => c.node.Equals(this));

        this.UpdateEnvelope();
        node.UpdateEnvelope();

        return true;
    }

    public Envelope UpdateEnvelope() {
        List<Node> nodes = new List<Node>();
        nodes.AddRange(this.connections.Select(c => c.node));
        nodes.Add(this);

        envelope = GetEnvelopeFromNodes(nodes);
        return envelope;
    }

    public ref readonly Envelope Envelope {
        get { return ref envelope; }
    }

    public static Envelope GetEnvelopeFromNodes(IEnumerable<Node> nodes, float padding = 0) {
        float minX = float.MaxValue;
        float minZ = float.MaxValue;

        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach(Node node in nodes) {
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

        foreach(Node n in nodes) {
            if(n == node) continue;

            float dist = Vector3.Distance(n.pos, node.pos);
            if(dist < leastDistance) {
                leastNode = n;
                leastDistance = dist;
            }
        }

        return leastNode;
    }
}
