using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTree;

public class RoadGenerator : MonoBehaviour
{
    private List<Node> nodes;
    private RTree<Node> tree;

    void Start()
    {
        tree = new RTree<Node>();
        nodes = new List<Node>();

        float step = 2 * Mathf.PI / 10.0f;
        for (int i = 1; i < 2; i++) {
            float x = Mathf.Sin(step * i) * 2;
            float y = Mathf.Cos(step * i) * 2;

            Node node1 = new Node(new Vector3(x, 0, y));
            Node node2 = new Node(new Vector3(x * 2, 0, y * 2));

            node1.ConnectTo(node2);

            AddNode(node1);
            AddNode(node2);
        }
    }

    public void AddAgent(Agent agent) {
        // if (_.find(this.agents, agent)) return;

        // this.queue.add(agent);
    }


    public Node AddNode(Node node) {
        node.added = true;

        nodes.Add(node);
        tree.Insert(node);

        return node;
    }

    public void RemoveNode(Node node) {
        this.nodes.Remove(node);
        this.tree.Delete(node);
    }

    public void updateNodeInTree(Node node) {
        this.tree.Delete(node);
        this.tree.Insert(node);
    }


    void Update()
    {
        Vector3 mousePos = Util.GetPlaneMousePos(new Vector3(0, 0, 0));

        Envelope searchBounds = new Envelope(mousePos.x - 0.2f, mousePos.z - 0.2f, mousePos.x + 0.2f, mousePos.z + 0.2f);
        IEnumerable<Node> result = tree.Search(searchBounds);

        Util.DebugDrawEnvelope(searchBounds);

        foreach (Node n in nodes)
        {
            Util.DebugDrawCircle(n.pos, 0.2f, new Color(0, 1, 0));

            Util.DebugDrawEnvelope(n.Envelope);

            foreach (Node.NodeConnection c in n.connections)
            {
              Debug.DrawLine(n.pos, c.node.pos, new Color(1, 0, 0));
            }
        }
    }
}
