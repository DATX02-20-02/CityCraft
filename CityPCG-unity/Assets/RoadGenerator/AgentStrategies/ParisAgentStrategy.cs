using UnityEngine;

public class ParisAgentStrategy : IAgentStrategy {
    public Node.ConnectionType connectionType = Node.ConnectionType.Main;
    public Node.NodeType nodeType = Node.NodeType.Main;

    private Vector3 center;
    private float radius;
    private bool straight;

    public ParisAgentStrategy(Vector3 center, float radius, bool straight) {
        this.center = center;
        this.radius = radius;
        this.straight = straight;
    }

    public void Start(Agent agent) {}

    public void Work(Agent agent) {
        if (this.straight){
            agent.rad += Random.Range(-1.0f, 1.0f) * ((10 * Mathf.PI) / 180);

            agent.dir = new Vector3(Mathf.Cos(agent.rad), 0, Mathf.Sin(agent.rad));
            agent.pos += agent.dir;

            agent.PlaceNode(agent.pos, this.nodeType, this.connectionType);
        }
        else {
            agent.rad += agent.radIncrement;

            float randRadius = Random.Range(-1.0f, 1.0f) * 2.0f;
            agent.pos = this.center + new Vector3(Mathf.Cos(agent.rad), 0, Mathf.Sin(agent.rad)) * (this.radius + randRadius);

            agent.PlaceNode(agent.pos, this.nodeType, this.connectionType);
        }
    }

    public void Branch(Agent agent, Node node) {
        // foreach (NodeConnection c in node.connections) {
        //     Node n = c.node;

        //     if (Random.Range(0.0f, 1.0f) < 0.5) {
        //         const dir = n.pos - node.pos;
        //         const revert = Mathf.Sign(Random.Range(-1.0f, 1.0f));

        //         const ag = new Agent(agent.generator, node.pos, new Vector3(-dir.z * revert, 0, dir.x * revert));
        //     }
        // }

    }

    public bool ShouldDie(Agent agent, Node node) {
        return agent.maxStepCount > 0 && agent.stepCount > agent.maxStepCount;
    }
}
