using System.Collections.Generic;
using UnityEngine;

public class ManhattanAgentStrategy : AgentStrategy {
    private ConnectionType connectionType = ConnectionType.Main;
    private Node.NodeType nodeType = Node.NodeType.Main;

    private Vector3 center;

    struct AgentData : IAgentData {
        public bool stopAtRoad;

        public static AgentData Copy(IAgentData idata) {
            AgentData data = (AgentData)idata;

            AgentData newData;
            newData.stopAtRoad = data.stopAtRoad;

            return newData;
        }
    }

    public ManhattanAgentStrategy(Vector3 center) {
        this.center = center;
    }

    public override void Start(Agent agent) {
        // Initialize agent data
        if (agent.Data == null) {
            AgentData data;
            data.stopAtRoad = false;
            agent.Data = data;
        }

        agent.config.maxFailedNodes = 10;
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData)agent.Data;
        AgentConfiguration config = agent.config;

        Node prevNode = agent.PreviousNode;

        float distance = Vector3.Distance(agent.Position, center);

        Node n = agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);
        if (n != null && info != null) {
        }
        else if (n == null && prevNode != null)
            agent.Terminate();

        if (agent.StepCount >= agent.config.maxStepCount && Random.value < 0.25f) {
            agent.SetStrategy(new HighwayAgentStrategy());
            agent.Priority = 100;
        }
        else {
            agent.Angle += Random.Range(-1.0f, 1.0f) * 0.2f * Mathf.Deg2Rad;
        }
        agent.Position += agent.Direction * config.stepSize;
    }

    public override List<Agent> Branch(Agent agent, Node node) {
        List<Agent> newAgents = new List<Agent>();

        return newAgents;
    }

    public override bool ShouldDie(Agent agent, Node node) {
        return agent.config.maxStepCount > 0 && agent.StepCount > agent.config.maxStepCount;
    }
}
