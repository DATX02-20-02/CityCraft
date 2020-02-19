using System;
using System.Collections;
using UnityEngine;

public class ParisCityGenerator : ICityGenerator {
    public ParisCityGenerator() {}

    public void Generate(RoadGenerator generator, Vector3 origin) {

        Agent agent1 = new Agent(generator, new Vector3(0, 0, 0), new Vector3(1, 0, 0), true);
        agent1.strategy = new ParisAgentStrategy(new Vector3(0, 0, 0), 10, false);
        agent1.priority = 0;
        agent1.radIncrement = ((10 * Mathf.PI) / 180);
        generator.AddAgent(agent1);
    }
}
