using System.Collections.Generic;
using UnityEngine;

public class StreetAgentStrategy : AgentStrategy {
    public ConnectionType connectionType = ConnectionType.Street;
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
        this.stepVariance = Random.Range(-1.0f, 1.0f) * 0.05f;
    }

    public override int CompareTo(Agent agentA, Agent agentB) {
        return agentA.Priority.CompareTo(agentB.Priority);
    }

    public override void Start(Agent agent) {
        // Initialize agent data
        if(agent.data == null) {
            AgentData data;
            agent.data = data;
        }

        if(agent.PreviousNode == null) {
            Node node = agent.network.AddNodeNearby(new Node(agent.Position), agent.config.snapRadius);
            agent.PreviousNode = node;
        };
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData)agent.data;
        AgentConfiguration config = agent.config;

        agent.Angle += Random.Range(-1.0f, 1.0f) * 0;
        agent.Position += agent.Direction * (config.stepSize + this.stepVariance);

        agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);

        if(!info.success) agent.Terminate();
    }

    public override List<Agent> Branch(Agent agent, Node node) {
        List<Agent> newAgents = new List<Agent>();

        if(Random.Range(0.0f, 1.0f) <= 0.8f
            && agent.BranchCount <= agent.config.maxBranchCount
            && agent.StepCount < agent.config.maxStepCount - 1
        ) {
            float revert = Mathf.Sign(Random.Range(-1.0f, 1.0f));

            Agent ag = Agent.Clone(agent);
            ag.Direction = Vector3.Lerp(
                new Vector3(-agent.Direction.z * revert, 0, agent.Direction.x * revert),
                agent.Direction,
                Random.Range(-0.4f, 0.4f) * 0
            );

            // ag.stepSize = 0.5f;
            // ag.snapRadius = 0.1f;

            AgentData data = AgentData.Copy(agent.data);
            ag.data = data;

            newAgents.Add(ag);
        }

        return newAgents;
    }

    public override bool ShouldDie(Agent agent, Node node) {
        return agent.config.maxStepCount > 0 && agent.StepCount > agent.config.maxStepCount;
    }
}
