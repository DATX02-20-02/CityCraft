using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RBush;

public class Node : ISpatialData {

    public enum NodeType {
        Main,
        Highway,
        Street
    }

    public Vector3 pos;
    public NodeType type;
    public bool added = false;

    public List<NodeConnection> connections = new List<NodeConnection>();
    private Envelope envelope = Envelope.EmptyBounds;

    public Node(Vector3 pos, NodeType type = NodeType.Main) {
        this.pos = pos;
        this.type = type;

        UpdateEnvelope();
    }

    public bool HasConnection(Node other) {
        foreach (NodeConnection connection in connections) {
            if (connection.node.Equals(other)) return true;
        }

        return false;
    }

    public bool ConnectTo(Node node, ConnectionType type = ConnectionType.Street) {
        if (HasConnection(node)) return false;

        this.connections.Add(new NodeConnection(node, type));
        node.connections.Add(new NodeConnection(this, type));

        this.UpdateEnvelope();
        node.UpdateEnvelope();

        return true;
    }

    public bool Disconnect(Node node) {
        if (!HasConnection(node)) return false;

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

        envelope = RoadNetwork.GetEnvelopeFromNodes(nodes);
        return envelope;
    }

    public ref readonly Envelope Envelope {
        get { return ref envelope; }
    }

    public Node Clone() {
        return new Node(pos, type);
    }

    public override string ToString() {
        return "Node (" + pos + ")";
    }
}
