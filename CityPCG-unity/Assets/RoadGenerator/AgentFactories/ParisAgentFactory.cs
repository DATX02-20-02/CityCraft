using System.Collections;
using UnityEngine;

public class ParisAgentFactory : IAgentFactory {
    public ParisAgentFactory() {}

    public void Create(RoadGenerator generator, Vector3 origin) {
        // for (int i = 0; i < 20; i++) {
        //     Agent agent1 = new Agent(generator, new Vector3(-12, 0, i - 10), new Vector3(1, 0, 0), true);
        //     agent1.strategy = new ParisAgentStrategy(new Vector3(0, 0, 0), 10, true);
        //     agent1.priority = 0;
        //     agent1.radIncrement = ((10 * Mathf.PI) / 180);
        //     generator.AddAgent(agent1);

        //     Agent agent2 = new Agent(generator, new Vector3(i - 10, 0, 11), new Vector3(0, 0, -1), true);
        //     agent2.strategy = new ParisAgentStrategy(new Vector3(0, 0, 0), 10, true);
        //     agent2.priority = 0;
        //     agent2.radIncrement = ((10 * Mathf.PI) / 180);
        //     generator.AddAgent(agent2);
        // }
        //
        float[] rings = new float[] { 2, 3.5f, 5 };
        Vector2[] directions = new Vector2[] {
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(-1, 0),
            new Vector2(0, -1)
        };

        int priority = 0;
        foreach (float radius in rings) {
            foreach (Vector2 dir in directions) {
                for (int reverse = -1; reverse <= 1; reverse += 2) {
                    float angleIncrement = (10 * Mathf.PI) / 180 * reverse;
                    Agent agent = new Agent(
                        generator,
                        origin,
                        new Vector3(0, 0, 0),
                        new ParisAgentStrategy(origin, radius, false, angleIncrement),
                        priority
                    );
                    agent.SetAngle(Mathf.Atan2(dir.y, dir.x));
                    generator.AddAgent(agent);
                }
            }
        }
        priority++;

        int max = (int) Mathf.Floor(Random.Range(3, 7));
        for (int i = 0; i < max; i++) {
            float rad = (Mathf.PI * 2) / max;

            Vector3 dir = new Vector3(Mathf.Cos(rad * i), 0, Mathf.Sin(rad * i));

            Agent ag = new Agent(
                generator,
                origin,
                dir,
                new ParisAgentStrategy(origin, 5, true),
                priority
            );
            ag.stepSize = 1f;
            ag.snapRadius = 0.2f;
            ag.maxStepCount = 20;
            generator.AddAgent(ag);
        }
    }
}