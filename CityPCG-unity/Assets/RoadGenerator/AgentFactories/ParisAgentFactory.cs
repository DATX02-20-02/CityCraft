using System.Collections;
using UnityEngine;

public class ParisAgentFactory : IAgentFactory {

    public int Create(RoadGenerator generator, RoadNetwork network, Vector3 origin, float radius, int priority = 0) {
        int amountOfRings = (int) Mathf.Max(1, Random.Range(Mathf.Min(2, radius / 20), radius / 20));

        float[] rings = new float[amountOfRings];
        for (int i = 0; i < amountOfRings; i++) {
            float spacing = radius / (float) amountOfRings;
            rings[i] = (i + 1) * spacing + Random.Range(-1f, 1f) * 0.1f * spacing;
        }

        Vector2[] directions = new Vector2[] {
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(-1, 0),
            new Vector2(0, -1)
        };

        foreach (float ringRadius in rings) {
            foreach (Vector2 dir in directions) {
                for (int reverse = -1; reverse <= 1; reverse += 2) {
                    float angleIncrement = (10 * Mathf.PI) / 180 * reverse;
                    Agent agent = new Agent(
                        network,
                        origin,
                        new Vector3(0, 0, 0),
                        new ParisAgentStrategy(origin, ringRadius, false, angleIncrement),
                        priority
                    );
                    agent.Angle = Mathf.Atan2(dir.y, dir.x);
                    generator.AddAgent(agent);
                }
            }
        }
        priority++;

        int max = (int)Mathf.Floor(Random.Range(3, 9));
        for (int i = 0; i < max; i++) {
            float rad = (Mathf.PI * 2) / max;

            Vector3 dir = new Vector3(Mathf.Cos(rad * i), 0, Mathf.Sin(rad * i));

            Agent ag = new Agent(
                network,
                origin,
                dir,
                new ParisAgentStrategy(origin, radius, true),
                priority
            );

            ag.config.stepSize = 5f;
            ag.config.snapRadius = 1f;
            ag.config.maxStepCount = 1000;//(int) (radius / ag.config.stepSize) * 2;

            generator.AddAgent(ag);
        }

        return priority;
    }
}
