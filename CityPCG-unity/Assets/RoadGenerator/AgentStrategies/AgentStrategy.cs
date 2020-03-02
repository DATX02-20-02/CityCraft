using System.Collections.Generic;

public abstract class AgentStrategy {
    public virtual void Start(Agent agent) { }
    public virtual List<Agent> Branch(Agent agent, Node node) {
        return new List<Agent>();
    }

    public abstract void Work(Agent agent);

    public virtual bool ShouldDie(Agent agent, Node node) {
        return agent.maxStepCount > 0 && agent.stepCount > agent.maxStepCount;
    }

    public virtual int CompareTo(Agent agentA, Agent agentB) {
        var i = agentA.priority.CompareTo(agentB.priority);

        if(i == 0) {
            return agentA.stepCount.CompareTo(agentB.stepCount);
        }
        return i;
    }
}
