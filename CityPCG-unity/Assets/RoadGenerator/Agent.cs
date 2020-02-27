using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAgentData {
}

public class Agent : IComparable
{
    public Vector3 pos;
    public Vector3 dir;
    public float stepSize = 0.5f;
    public int stepCount = 0;
    public int maxStepCount = 40;
    public int branchCount = 0;
    public int maxBranchCount = 4;
    public float snapRadius = 0.2f;
    public float angle = 0;
    public float priority = 0;
    public bool terminated = false;
    public bool started = false;

    public RoadGenerator generator;
    public IAgentStrategy strategy;
    public IAgentData data;

    public Node prevNode;

    public Agent(RoadGenerator generator, Vector3 pos, Vector3 dir, IAgentStrategy strategy, float priority = 0) {
        this.generator = generator;
        this.pos = pos;
        this.dir = dir;
        this.strategy = strategy;
        this.priority = priority;

        this.SetDirection(dir);
    }

    public static Agent Clone(Agent other) {
        Agent ag = new Agent(
            other.generator,
            other.pos,
            other.dir,
            other.strategy,
            other.priority
        );

        ag.angle = other.angle;
        ag.stepCount = other.stepCount;
        ag.maxStepCount = other.maxStepCount;
        ag.stepSize = other.stepSize;

        ag.branchCount = other.branchCount;
        ag.maxBranchCount = other.maxBranchCount;

        ag.snapRadius = other.snapRadius;

        ag.prevNode = other.prevNode;
        ag.data = other.data;

        return ag;
    }

    // Required for priority queue
    public int CompareTo(object obj) {
        if (obj == null) return 1;

        Agent agent = obj as Agent;
        if (agent != null) {
            return this.strategy.CompareTo(this, agent);
        }
        else {
            throw new ArgumentException("Object is not an Agent");
        }
    }

    public void SetDirection(Vector3 dir) {
        this.dir = dir.normalized;

        this.angle = Mathf.Atan2(this.dir.z, this.dir.x);
    }

    public void SetAngle(float angle){
        this.angle = angle;
        this.dir = new Vector3(Mathf.Cos(this.angle), 0, Mathf.Sin(this.angle)).normalized;
    }

    public Node PlaceNode(Vector3 pos, Node.NodeType nodeType, Node.ConnectionType connectionType, out RoadGenerator.ConnectionResult info) {
        Node node = new Node(pos, nodeType);

        info = null;
        if (this.prevNode == null) {
            this.generator.AddNode(node);
            this.prevNode = node;

            return node;
        }
        else if (connectionType != Node.ConnectionType.None) {
            info = this.generator.ConnectNodesWithIntersect(this.prevNode, node, this.snapRadius);

            if (info.success && !info.didIntersect && !info.didSnap) {
                this.generator.AddNode(node);
            }
            this.prevNode = info.prevNode;
            return info.prevNode;
        }

        return null;
    }

    public Node PlaceNode(Vector3 pos, Node.NodeType nodeType, Node.ConnectionType connectionType) {
        return PlaceNode(pos, nodeType, connectionType, out RoadGenerator.ConnectionResult info);
    }

    public void Start() {
        if (this.strategy != null) {
            this.strategy.Start(this);
        }
    }

    public void Work() {
        if (this.strategy == null) return;

        this.strategy.Work(this);

        this.stepCount++;
        if (this.strategy.ShouldDie(this, this.prevNode))
            this.Terminate();

        if (!this.terminated) this.strategy.Branch(this, this.prevNode);
    }

    public void Terminate() {
        this.terminated = true;
    }
}
