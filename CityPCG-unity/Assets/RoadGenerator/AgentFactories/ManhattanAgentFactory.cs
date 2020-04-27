using System.Collections;
using UnityEngine;

public class ManhattanAgentFactory : IAgentFactory {

    public int Create(RoadGenerator generator, RoadNetwork network, Vector3 origin, float radius, int priority = 0) {
        float width = radius * 2;
        float height = radius * 2 * Random.Range(0.4f, 0.6f);
        float angle = Random.Range(0, Mathf.PI * 2);

        Rectangle rect = Rectangle.Create(
            origin.x / network.Terrain.width, origin.z / network.Terrain.depth,
            angle, width / network.Terrain.width, height / network.Terrain.depth);
        network.Population.AddAmplifier(new RectangularAmplifier(rect, 1));

        int max = (int)Mathf.Max(3, Random.Range(height / 20, height / 16));
        for (int i = 0; i < max; i++) {
            for (int invert = -1; invert <= 1; invert += 2) {
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * invert;
                Vector3 perp = Vector3.Cross(dir, Vector3.up);

                Vector3 pos = origin + perp * (-height / 2 + height / (max - 1) * i);

                Agent ag = new Agent(
                    network,
                    pos,
                    dir,
                    new ManhattanAgentStrategy(origin),
                    priority
                );

                ag.config.stepSize = 5f;
                ag.config.snapRadius = 1f;

                float stepCount = width / ag.config.stepSize / 2f;
                ag.config.maxStepCount = (int)Random.Range(stepCount, stepCount * 1.2f) + 1;

                generator.AddAgent(ag);
            }
        }

        max = (int)Mathf.Max(3, Random.Range(width / 16, width / 12));
        for (int i = 0; i < max; i++) {
            for (int invert = -1; invert <= 1; invert += 2) {
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * invert;
                Vector3 perp = Vector3.Cross(dir, Vector3.up);

                Vector3 pos = origin + dir * (-width / 2 + width / (max - 1) * i);

                Agent ag = new Agent(
                    network,
                    pos,
                    perp,
                    new ManhattanAgentStrategy(origin),
                    priority
                );

                ag.config.stepSize = 5f;
                ag.config.snapRadius = 1f;

                float stepCount = height / ag.config.stepSize;
                ag.config.maxStepCount = (int)Random.Range(stepCount, stepCount * 1.2f);

                generator.AddAgent(ag);
            }
        }

        return priority;
    }
}
