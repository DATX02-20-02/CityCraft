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


                for (int invert = -1; invert <= 1; invert += 2) {
                    if (Random.Range(0.0f, 1.0f) <= 2f) {
                        Agent ag = new Agent(
                            network,
                            node.pos,
                            perp * invert,
                            new StreetAgentStrategy(),
                            10
                        );

                        ag.PreviousNode = node;

                        generator.AddAgent(ag);

                        break;
                    }
                }
            }
        }
    }
}
