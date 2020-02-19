
public interface IAgentStrategy {
    void Start(Agent agent);
    void Work(Agent agent);
    void Branch(Agent agent, Node node);
    bool ShouldDie(Agent agent, Node node);
}
