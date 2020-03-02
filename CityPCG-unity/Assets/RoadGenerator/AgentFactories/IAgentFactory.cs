using UnityEngine;

public interface IAgentFactory {
    void Create(RoadGenerator generator, RoadNetwork network, Vector3 origin);
}
