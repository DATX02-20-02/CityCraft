using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RTree;

public class Node : ISpatialData
{
    public class NodeConnection
    {
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
        Main,
        Highway,
        Street
    };

    public Vector3 pos;
    public float width;
    public float height;
    public NodeType type;
    public bool added = false;

    public List<NodeConnection> connections = new List<NodeConnection>();

    public Node(Vector3 pos, NodeType type = NodeType.Main) {
        this.pos = pos;
        this.type = type;
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

        return true;
    }

    public void Disconnect(Node node) {
        if (!HasConnection(node)) return false;

        this.connections.RemoveAll(c => c.node.Equals(node));
        node.connections.RemoveAll(c => c.node.Equals(this));

        return true;
    }


    public Envelope Envelope {
        get {
            List<Node> nodes = new List<Node>();
            nodes.AddRange(this.connections.Select(c => c.node));
            nodes.Add(this);

            return Util.getEnvelopeFromNodes(nodes);
        }
    }
}

/*
class Node {


  constructor(x, y, type = Node.NodeTypes.MAIN) {
  }

  connect(node, type = Node.ConnectionTypes.STREET) {
    if (_.find(this.connections, n => n.node == node)) return false;

    this.connections.push({ node, type });
    node.connections.push({ node: this, type });

    return true;
  }

  disconnect(node) {
    let idx = _.findIndex(this.connections, n => n.node == node);
    if (idx >= 0) this.connections.splice(idx, 1);

    idx = _.findIndex(node.connections, n => n.node == this);
    if (idx >= 0) node.connections.splice(idx, 1);
  }

  getBoundingBox() {
    return getBoundingBoxFromNodes([
      this,
      ..._.map(this.connections, n => n.node)
    ]);
  }
  // jsQuad methods
  QTsetParent(parent) {
    this.QTparent = parent;
  }
  QTgetParent() {
    return this.QTparent;
  }
  QTenclosed(xMin, yMin, xMax, yMax) {
    const box = this.getBoundingBox();
    var x0 = box.x,
      x1 = box.x + box.w;
    var y0 = box.y,
      y1 = box.y + box.h;
    return x0 >= xMin && x1 <= xMax && y0 >= yMin && y1 <= yMax;
  }
}

export default Node;

*/
