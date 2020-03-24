using System.Collections.Generic;
using UnityEngine;

public class HighwayAgentStrategy : AgentStrategy {
    private ConnectionType connectionType = ConnectionType.Highway;
    private Node.NodeType nodeType = Node.NodeType.Highway;

    struct AgentData : IAgentData {
        public Vector3 startDirection;
    }

    public HighwayAgentStrategy() { }

    public override void Start(Agent agent) {
        // Initialize agent data
        if (agent.Data == null) {
            AgentData data;
            data.startDirection = agent.Direction;
            agent.Data = data;
        }

        if (agent.PreviousNode == null) {
            Vector3 pos = agent.Position;

            Node node = agent.Network.AddNodeNearby(new Node(pos), agent.config.snapRadius);
            agent.PreviousNode = node;
        };
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData)agent.Data;
        AgentConfiguration config = agent.config;

        Noise popMap = agent.Network.Population;

        Vector2 slope = popMap.GetSlope(
            agent.Position.x / agent.Network.Width,
            agent.Position.z / agent.Network.Height
        );

        if (slope != Vector2.zero) {
            Vector3 newDir = Vector3.RotateTowards(
                agent.Direction,
                VectorUtil.Vector2To3(slope),
                5 * Mathf.Deg2Rad, 1
            );

            if (Vector3.Dot(newDir, agentData.startDirection) > 0)
                agent.Direction = newDir;
        }

        agent.Angle += Random.Range(-1.0f, 1.0f) * 10.0f * Mathf.Deg2Rad;

        Vector3 oldPos = agent.Position;
        agent.Position += agent.Direction * config.stepSize;

        Node n = agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);
        if (n != null && info != null) {
            Vector3 dir = n.pos - oldPos;
            Vector3 newDir = Vector3.Lerp(dir, agent.Direction, 0.2f);
            agent.Angle = Mathf.Atan2(newDir.z, newDir.x);

            if (!info.success) {
                agent.Terminate();
            }
        }
        else {
            agent.Terminate();
        }
    }

    public override List<Agent> Branch(Agent agent, Node node) {
        List<Agent> newAgents = new List<Agent>();

        if (Random.Range(0.0f, 1.0f) < 0.05f
            && agent.BranchCount < agent.config.maxBranchCount
            && agent.StepCount > 2
            && agent.StepCount < agent.config.maxStepCount - 3
        ) {
            float revert = Mathf.Sign(Random.Range(-1.0f, 1.0f));

            Agent ag = Agent.Clone(agent);
            ag.Direction = Vector3.Lerp(
                new Vector3(-agent.Direction.z * revert, 0, agent.Direction.x * revert),
                agent.Direction,
                Random.Range(0.2f, 0.5f)
            );

            ag.config = agent.config;

            newAgents.Add(ag);
        }

        return newAgents;
    }

    public override bool ShouldDie(Agent agent, Node node) {
        return agent.config.maxStepCount > 0 && agent.StepCount > agent.config.maxStepCount;
    }
}
