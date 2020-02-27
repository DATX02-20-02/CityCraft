using UnityEngine;

public class ParisAgentStrategy : IAgentStrategy {
    public Node.ConnectionType connectionType = Node.ConnectionType.Main;
    public Node.NodeType nodeType = Node.NodeType.Main;

    private Vector3 center;
    private float radius;
    private bool straight;

    struct AgentData : IAgentData {
        public float angleIncrement;
        public bool stopAtRoad;

        public static AgentData Copy(IAgentData idata) {
            AgentData data = (AgentData) idata;

            AgentData newData;
            newData.angleIncrement = data.angleIncrement;
            newData.stopAtRoad = data.stopAtRoad;

            return newData;
        }
    }

    public ParisAgentStrategy(Vector3 center, float radius, bool straight) {
        this.center = center;
        this.radius = radius;
        this.straight = straight;
    }

    public override void Start(Agent agent) {
        // Initialize agent data
        if (agent.data == null) {
            AgentData data;
            data.angleIncrement = ((10 * Mathf.PI) / 180);
            data.stopAtRoad = false;
            agent.data = data;
        }

        if (this.straight && agent.prevNode == null) {
            Node node = agent.generator.AddNodeNearby(new Node(agent.pos), 0.2f);
            agent.prevNode = node;
        };
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData) agent.data;

        if (this.straight){
            agent.SetAngle(agent.angle + Random.Range(-1.0f, 1.0f) * ((10.0f * Mathf.PI) / 180));

            Vector3 oldPos = agent.pos;
            agent.pos += agent.dir * agent.stepSize;

            Node n = agent.PlaceNode(agent.pos, this.nodeType, this.connectionType, out RoadGenerator.ConnectionResult info);
            Vector3 dir = n.pos - oldPos;
            Vector3 newDir = Vector3.Lerp(dir, agent.dir, 0.2f);
            agent.angle = Mathf.Atan2(newDir.z, newDir.x);

            if (agentData.stopAtRoad && (!info.success)) {
                agent.Terminate();
            }
        }
        else {
            agent.SetAngle(agent.angle + agentData.angleIncrement);

            float randRadius = Random.Range(-1.0f, 1.0f) * 0.1f;
            agent.pos = this.center + new Vector3(Mathf.Cos(agent.angle), 0, Mathf.Sin(agent.angle)) * (this.radius + randRadius);

            agent.PlaceNode(agent.pos, this.nodeType, this.connectionType, out RoadGenerator.ConnectionResult info);
            if (info != null)   {
                if (info.didSnap || info.didIntersect) {
                    agent.Terminate();
                }
            }
        }
    }

    public override void Branch(Agent agent, Node node) {
        if ((this.straight && Vector3.Distance(agent.pos, center) >= radius)) {
            if (Random.Range(0.0f, 1.0f) < 0.5f
                && agent.branchCount < agent.maxBranchCount
                && agent.stepCount < agent.maxStepCount - 3
            ) {
                float revert = Mathf.Sign(Random.Range(-1.0f, 1.0f));

                Agent ag = Agent.Clone(agent);
                ag.SetDirection(
                    Vector3.Lerp(
                        new Vector3(-agent.dir.z * revert, 0, agent.dir.x * revert),
                        agent.dir,
                        Random.Range(-0.4f, 0.4f)
                    )
                );

                // ag.stepSize = 0.5f;
                // ag.snapRadius = 0.1f;

                AgentData data = AgentData.Copy(agent.data);
                data.stopAtRoad = true;
                ag.data = data;

                agent.generator.AddAgent(ag);
            }
        }
    }

    public override bool ShouldDie(Agent agent, Node node) {
        return agent.maxStepCount > 0 && agent.stepCount > agent.maxStepCount;
    }
}
