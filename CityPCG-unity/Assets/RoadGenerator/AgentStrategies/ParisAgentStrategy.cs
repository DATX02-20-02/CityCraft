using System.Collections.Generic;
using UnityEngine;

public class ParisAgentStrategy : AgentStrategy {
    private ConnectionType connectionType = ConnectionType.Main;
    private Node.NodeType nodeType = Node.NodeType.Main;

    private Vector3 center;
    private float radius;
    private bool straight;
    private float angleIncrement;

    struct AgentData : IAgentData {
        public bool stopAtRoad;

        public static AgentData Copy(IAgentData idata) {
            AgentData data = (AgentData)idata;

            AgentData newData;
            newData.stopAtRoad = data.stopAtRoad;

            return newData;
        }
    }

    public ParisAgentStrategy(Vector3 center, float radius, bool straight, float angleIncrement = 0) {
        this.center = center;
        this.radius = radius;
        this.straight = straight;
        this.angleIncrement = angleIncrement;
    }

    public override void Start(Agent agent) {
        // Initialize agent data
        if (agent.Data == null) {
            AgentData data;
            data.stopAtRoad = false;
            agent.Data = data;
        }

        agent.config.stepSize = 10;

        if (this.straight) {
            agent.config.maxFailedNodes = (int) (radius / agent.config.stepSize);
        }
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData)agent.Data;
        AgentConfiguration config = agent.config;

        Node prevNode = agent.PreviousNode;

        if (this.straight) {
            float distance = Vector3.Distance(agent.Position, center);

            Node n = agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);
            if (n != null && info != null) {
                if (distance > radius + 5) {
                    agent.SetStrategy(new HighwayAgentStrategy());
                    agent.Priority = 100;
                }
            }
            else if (n == null && prevNode != null)
                agent.Terminate();

            agent.Angle += Random.Range(-1.0f, 1.0f) * 10.0f * Mathf.Deg2Rad;
            agent.Position += agent.Direction * config.stepSize;
        }
        else {
            agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);

            if (info != null && prevNode != null) {
                if (info.didIntersect) {
                    agent.Terminate();
                }

                if (info.didSnap && prevNode != info.prevNode) {
                    agent.Terminate();
                }
            }

            agent.Angle = agent.Angle + this.angleIncrement;

            float randRadius = agent.StepCount == 0 || agent.StepCount == agent.config.maxStepCount - 1 ?
                0 : Random.Range(-4.0f, 4.0f) * 0.1f;
            agent.Position = this.center + agent.Direction * (this.radius + randRadius);
        }
    }

    public override List<Agent> Branch(Agent agent, Node node) {
        List<Agent> newAgents = new List<Agent>();

        return newAgents;
    }

    public override bool ShouldDie(Agent agent, Node node) {
        return agent.config.maxStepCount > 0 && agent.StepCount > agent.config.maxStepCount;
    }
}
