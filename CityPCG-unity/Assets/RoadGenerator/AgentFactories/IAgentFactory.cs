using UnityEngine;

public interface IAgentFactory {
    void Create(RoadGenerator generator, Vector3 origin);
}
