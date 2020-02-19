using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent
{
    public Vector2 pos;
    public Vector2 dir;
    public float stepSize = 40;
    public int stepCount = 0;
    public int maxStepCount = 50;
    public int branchCount = 0;
    public int maxBranchCount = 4;
    public float snapRadius = 30;
    public float rad = 0;
    public float priority = 0;

    public RoadGenerator generator;

    public Agent(RoadGenerator generator, Vector2 pos, Vector2 dir, bool noNode) {
        this.generator = generator;
        this.pos = pos;
        this.dir = dir;
    }
}
