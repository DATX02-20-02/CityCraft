using System.Collections;
using UnityEngine;

public class StreetsAgentFactory : IAgentFactory {

    public void Create(RoadGenerator generator, RoadNetwork network, Vector3 origin) {
        foreach (Node node in network.Nodes) {
            if (node.type != Node.NodeType.Main) {
                continue;
            }

            foreach (NodeConnection c in node.connections) {
                Vector3 dir = c.node.pos - node.pos;
                Vector3 perp = Vector3.Cross(dir, Vector3.up);

                for (int i = 0; i < 1; i++) {
                    int revert = i * 2 - 1;
                    if (Random.Range(0.0f, 1.0f) <= 2f) {
                        Agent ag = new Agent(
                            network,
                            node.pos,
                            perp * revert,
                            new StreetAgentStrategy(),
                            10
                        );

                        ag.config.stepSize = 5 * 0.3f;
                        ag.config.snapRadius = 5 * 0.2f;
                        ag.config.maxBranchCount = 5;
                        ag.config.maxStepCount = 20;

                        ag.PreviousNode = node;

                        generator.AddAgent(ag);

                        break;
                    }
                }
            }
        }
    }
}
