using UnityEngine;

public class StreetAgentStrategy : IAgentStrategy {
    public Node.ConnectionType connectionType = Node.ConnectionType.Street;
    public Node.NodeType nodeType = Node.NodeType.Street;

    private float stepVariance;

    struct AgentData : IAgentData {

        public static AgentData Copy(IAgentData idata) {
            AgentData data = (AgentData)idata;

            AgentData newData;

            return newData;
        }
    }

    public StreetAgentStrategy() {
        this.stepVariance = Random.Range( -1.0f, 1.0f ) * 0.05f;
    }

    public override int CompareTo(Agent agentA, Agent agentB) {
        return agentA.priority.CompareTo( agentB.priority );
    }

    public override void Start(Agent agent) {
        // Initialize agent data
        if (agent.data == null) {
            AgentData data;
            agent.data = data;
        }

        if (agent.prevNode == null) {
            Node node = agent.generator.AddNodeNearby( new Node( agent.pos ), agent.snapRadius );
            agent.prevNode = node;
        };
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData)agent.data;

        agent.SetAngle( agent.angle + Random.Range( -1.0f, 1.0f ) * 0 );
        agent.pos += agent.dir * (agent.stepSize + this.stepVariance);

        agent.PlaceNode( agent.pos, this.nodeType, this.connectionType, out RoadGenerator.ConnectionResult info );

        if (!info.success) agent.Terminate();
    }

    public override void Branch(Agent agent, Node node) {
        if (Random.Range( 0.0f, 1.0f ) <= 0.8f
            && agent.branchCount <= agent.maxBranchCount
            && agent.stepCount < agent.maxStepCount - 1
        ) {
            float revert = Mathf.Sign( Random.Range( -1.0f, 1.0f ) );

            Agent ag = Agent.Clone( agent );
            ag.SetDirection(
                Vector3.Lerp(
                    new Vector3( -agent.dir.z * revert, 0, agent.dir.x * revert ),
                    agent.dir,
                    Random.Range( -0.4f, 0.4f ) * 0
                )
            );

            // ag.stepSize = 0.5f;
            // ag.snapRadius = 0.1f;

            AgentData data = AgentData.Copy( agent.data );
            ag.data = data;

            agent.generator.AddAgent( ag );
        }
    }

    public override bool ShouldDie(Agent agent, Node node) {
        return agent.maxStepCount > 0 && agent.stepCount > agent.maxStepCount;
    }
}
