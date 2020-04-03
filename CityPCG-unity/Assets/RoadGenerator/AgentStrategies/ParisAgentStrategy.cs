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

        if (agent.PreviousNode == null) {
            Vector3 pos = agent.Position;

            if (!this.straight) pos = center + agent.Direction * radius;

            Node node = agent.Network.AddNodeNearby(
                agent.Network.CreateNode(
                    VectorUtil.Vector3To2(pos), nodeType
                ),
                agent.config.snapRadius
            );
            agent.PreviousNode = node;
        };
    }

    public override void Work(Agent agent) {
        AgentData agentData = (AgentData)agent.Data;
        AgentConfiguration config = agent.config;

        if (this.straight) {
            agent.Angle += Random.Range(-1.0f, 1.0f) * 10.0f * Mathf.Deg2Rad;

            float distance = Vector3.Distance(agent.Position, center);

            Vector3 oldPos = agent.Position;
            agent.Position += agent.Direction * config.stepSize;

            Node n = agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);
            if (n != null && info != null) {
                Vector3 dir = n.pos - oldPos;

                if (distance > radius) {
                    agent.SetStrategy(new HighwayAgentStrategy());
                    agent.Priority = 100;
                }
            }
            else {
                agent.Terminate();
            }
        }
        else {
            agent.Angle += this.angleIncrement;

            float randRadius = agent.StepCount == 0 ? 0 : Random.Range(-1.0f, 1.0f) * 0.1f;
            agent.Position = this.center + new Vector3(
                Mathf.Cos(agent.Angle),
                0,
                Mathf.Sin(agent.Angle)
            ) * (this.radius + randRadius);

            agent.PlaceNode(agent.Position, this.nodeType, this.connectionType, out ConnectionResult info);
            if (info != null) {
                if (info.didSnap || info.didIntersect) {
                    agent.Terminate();
                }
            }
            else {
                agent.Terminate();
            }
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
