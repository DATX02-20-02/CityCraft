using System.Collections.Generic;

public abstract class AgentStrategy {
    public virtual void Start(Agent agent) { }
    public virtual List<Agent> Branch(Agent agent, Node node) {
        return new List<Agent>();
    }

    public abstract void Work(Agent agent);

    public virtual bool ShouldDie(Agent agent, Node node) {
        return agent.config.maxStepCount > 0 && agent.StepCount > agent.config.maxStepCount;
    }

    public virtual int CompareTo(Agent agentA, Agent agentB) {
        var i = agentA.Priority.CompareTo(agentB.Priority);

        if(i == 0) {
            return agentA.StepCount.CompareTo(agentB.StepCount);
        }
        return i;
    }
}
