using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAgentData {
}

public struct AgentConfiguration {
    public float stepSize;
    public float snapRadius;

    public int maxStepCount;
    public int maxBranchCount;

    public bool requeue;
}

public class Agent : IComparable {
    public AgentConfiguration config;

    private RoadNetwork network;
    private IAgentData data;
    private AgentStrategy strategy;

    private Vector3 pos;
    private Vector3 dir;

    private float priority = 0;
    private int stepCount = 0;
    private int branchCount = 0;

    private bool terminated = false;
    private bool started = false;

    private Node prevNode;

    public RoadNetwork Network {
        get { return this.network; }
    }

    public IAgentData Data {
        get { return this.data; }
        set { this.data = value; }
    }

    public Vector3 Position {
        get { return pos; }
        set { this.pos = value; }
    }

    public Vector3 Direction {
        get { return dir; }
        set { this.dir = value.normalized; }
    }

    public float Angle {
        get {
            return Mathf.Atan2(this.dir.z, this.dir.x);
        }
        set {
            this.dir = new Vector3(Mathf.Cos(value), 0, Mathf.Sin(value)).normalized;
        }
    }

    public float Priority {
        get { return this.priority; }
    }

    public float StepCount {
        get { return this.stepCount; }
    }

    public float BranchCount {
        get { return this.branchCount; }
    }

    public Node PreviousNode {
        get { return this.prevNode; }
        set { this.prevNode = value; }
    }

    public bool IsTerminated {
        get { return this.terminated; }
    }

    public bool IsStarted {
        get { return this.started; }
    }

    public Agent(RoadNetwork network, Vector3 pos, Vector3 dir, AgentStrategy strategy, float priority = 0) {
        this.network = network;
        this.pos = pos;
        this.dir = dir;
        this.strategy = strategy;
        this.priority = priority;

        SetDefaultConfig();
    }

    public static Agent Clone(Agent other) {
        Agent ag = new Agent(
            other.network,
            other.pos,
            other.dir,
            other.strategy,
            other.priority
        );

        ag.config = other.config;

        ag.stepCount = other.stepCount;
        ag.branchCount = other.branchCount;

        ag.prevNode = other.prevNode;

        ag.data = other.data;

        return ag;
    }

    public void SetDefaultConfig() {
        this.config = new AgentConfiguration();

        this.config.stepSize = 5f;
        this.config.snapRadius = 1f;

        this.config.maxStepCount = 40;
        this.config.maxBranchCount = 4;

        this.config.requeue = true;
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

    public Node PlaceNode(Vector3 pos, Node.NodeType nodeType, ConnectionType connectionType, out ConnectionResult info) {
        info = null;

        if (!VectorUtil.IsInBounds(VectorUtil.Vector3To2(pos), this.network.Width, this.network.Height)) {
            return null;
        }

        Node node = this.network.CreateNode(VectorUtil.Vector3To2(pos), nodeType);
        float yLevel = node.pos.y;


        Vector3 normal = network.Terrain.GetNormal(pos.x, pos.z);
        float steepness = Vector3.Dot(normal, Vector3.up);
        float angle = 90 - Vector3.Angle(Vector3.up, normal);

        // TODO: Do not hardcode this.
        if (Mathf.Abs(angle) <= 45) {
            return null;
        }

        // This might be used in the future to decide on max height difference
        // This could occur if we have ravines and stuff in the terrain
        
        // if (this.prevNode != null) {
        //     float prevYLevel = this.prevNode.pos.y;

        //     float yDiff = Mathf.Abs(yLevel - prevYLevel);
        //     if (yDiff >= 5) return null;
        // }


        if (this.prevNode == null) {
            this.network.AddNode(node);
            this.prevNode = node;

            return node;
        }
        else if (connectionType != ConnectionType.None) {
            info = this.network.ConnectNodesWithIntersect(this.prevNode, node, config.snapRadius, connectionType);

            if (info.success && !info.didIntersect && !info.didSnap) {
                this.network.AddNode(node);
            }
            this.prevNode = info.prevNode;
            return info.prevNode;
        }

        return null;
    }

    public Node PlaceNode(Vector3 pos, Node.NodeType nodeType, ConnectionType connectionType) {
        return PlaceNode(pos, nodeType, connectionType, out ConnectionResult info);
    }

    public void Start() {
    }

    public void SetStrategy(AgentStrategy strat) {
        this.strategy = strat;
        this.data = null;

        if (!this.strategy.started) {
            this.strategy.started = true;
            this.strategy.Start(this);
        }
    }

    public List<Agent> Work() {
        List<Agent> newAgents = new List<Agent>();

        if (this.strategy == null) return newAgents;

        if (!this.strategy.started) {
            this.strategy.started = true;
            this.strategy.Start(this);
        }

        this.strategy.Work(this);

        if (!VectorUtil.IsInBounds(VectorUtil.Vector3To2(this.pos), this.network.Width, this.network.Height)) {
            this.Terminate();
        }

        this.stepCount++;
        if (!this.terminated && this.strategy.ShouldDie(this, this.prevNode))
            this.Terminate();

        if (!this.terminated) {
            newAgents = this.strategy.Branch(this, this.prevNode);
            foreach (Agent newAgent in newAgents) {
                newAgent.branchCount = this.branchCount + 1;
            }
        }

        return newAgents;
    }

    public void Terminate() {
        this.terminated = true;
    }
}
