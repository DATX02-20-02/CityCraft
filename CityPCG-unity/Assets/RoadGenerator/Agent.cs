using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : IComparable
{
    public Vector3 pos;
    public Vector3 dir;
    public float stepSize = 40;
    public int stepCount = 0;
    public int maxStepCount = 40;
    public int branchCount = 0;
    public int maxBranchCount = 4;
    public float snapRadius = 30;
    public float rad = 0;
    public float radIncrement = 0;
    public float priority = 0;
    public bool dead = false;

    public RoadGenerator generator;
    public IAgentStrategy strategy;

    private Node prevNode;

    public Agent(RoadGenerator generator, Vector3 pos, Vector3 dir, bool noNode) {
        this.generator = generator;
        this.pos = pos;
        this.dir = dir;
    }

    // Required for priority queue
    public int CompareTo(object obj) {
        if (obj == null) return 1;

        Agent agent = obj as Agent;
        if (agent != null) {
            return priority.CompareTo(agent.priority);
        }
        else {
            throw new ArgumentException("Object is not an Agent");
        }
    }

    public void PlaceNode(Vector3 pos, Node.NodeType nodeType, Node.ConnectionType connectionType = Node.ConnectionType.None) {
        Node node = new Node(pos, nodeType);
        this.generator.AddNode(node);

        UnityEngine.Debug.Log("Placing node at " + pos);

        if (this.prevNode != null && connectionType != Node.ConnectionType.None) {
            this.generator.ConnectNodes(node, this.prevNode, connectionType);
        }

        this.prevNode = node;
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
            this.dead = true;

        if (!this.dead) this.strategy.Branch(this, this.prevNode);
    }
}
