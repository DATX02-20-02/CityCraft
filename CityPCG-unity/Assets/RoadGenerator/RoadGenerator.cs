// #define DEBUG_AGENT_WORK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RBush;
using VisualDebugging;

/*
  What? Generates a road network based on terrain and population.
  Why? Roads are needed for cities.
  How? Uses agent-based generation, where each agent places down nodes and edges between them.
*/
public class RoadGenerator : MonoBehaviour {
    [Range(0, 1000)]
    [SerializeField] private int maxAgentQueueIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float generationTickInterval = 0.2f;

    [Range(0, 2.0f)]
    [SerializeField] private float roadMeshTickInterval = 0.2f;

    [SerializeField] private bool debugRoadMeshGeneration = true;

    [SerializeField] private List<Vector3> debugPoints = new List<Vector3>();

    private RoadNetwork network;

    private PriorityQueue<Agent> queue = new PriorityQueue<Agent>();
    private int prevQueueCount = 0;

    private bool areAgentsWorking = false;
    private Action<RoadNetwork> callback;

    public RoadNetwork Network {
        set { this.network = value; }
        get { return this.network; }
    }

    // Generates a complete road network.
    public void Generate(TerrainModel terrain, Noise population, Action<RoadNetwork> callback = null) {
        this.callback = callback;
        prevQueueCount = 0;
        areAgentsWorking = false;

        network = new RoadNetwork(terrain, population, terrain.width, terrain.depth);
        queue = new PriorityQueue<Agent>();

        IAgentFactory factory = new ParisAgentFactory();

        factory.Create(this, network, new Vector3(300 - 150, 0, 300));
        factory.Create(this, network, new Vector3(300 + 0, 0, 300));


        // int max = 4;
        // for (int i = 0; i < max; i++) {
        //     float rad = (Mathf.PI * 2) / max;

        //     Vector3 dir = new Vector3(Mathf.Cos(rad * i), 0, Mathf.Sin(rad * i));
        // Vector3 dir = new Vector3(1, 0, 0);
        // Agent agent = new Agent(
        //     network,
        //     new Vector3(128, 0, 128),
        //     dir,
        //     new HighwayAgentStrategy(),
        //     1
        // );
        // agent.config.maxBranchCount = 5;
        // this.AddAgent(agent);
        // }
        factory.Create(this, network, new Vector3(0, 0, 0));
        // factory.Create(this, network, new Vector3(20, 0, 0));
    }

    // Generates road meshes
    public void GenerateMesh() {
        RoadMeshGenerator meshGenerator = GetComponent<RoadMeshGenerator>();
        if (meshGenerator == null) return;

        meshGenerator.Generate(network);
    }

    public void GenerateStreets(TerrainModel terrain, Noise population, Action<RoadNetwork> callback) {
        this.callback = callback;

        IAgentFactory factory = new StreetsAgentFactory();
        factory.Create(this, network, Vector3.zero);
    }

    // Adds an agent to the pool of active agents.
    public void AddAgent(Agent agent) {
        this.queue.Enqueue(agent);
    }

    // Iterate through queue and let the agents work.
    IEnumerator DoAgentWork() {
        if (prevQueueCount != 0 && this.queue.Count == 0) {
            if (callback != null) {
                this.callback(network);
            }
            prevQueueCount = 0;
            yield break;
        }

#if DEBUG_AGENT_WORK
        if (prevQueueCount == 0) {
            VisualDebug.Initialize();
            VisualDebug.BeginFrame("Start", true);
        }
#endif

        areAgentsWorking = true;
        prevQueueCount = this.queue.Count;

        int iterations = 0;
        while (this.queue.Count > 0 && iterations < maxAgentQueueIterations) {
            Agent agent = this.queue.Peek();
            AgentConfiguration config = agent.config;

            if (config.requeue) this.queue.Dequeue();

            Vector3 oldPos = agent.Position;

            if (!agent.IsStarted) {
                agent.Start();
            }

            List<Agent> newAgents = agent.Work();
            foreach (Agent newAgent in newAgents) {
                this.AddAgent(newAgent);
            }

            if (agent.IsTerminated) {
                if (!config.requeue)
                    this.queue.Dequeue();

            }
            else {
                if (config.requeue)
                    this.queue.Enqueue(agent);
            }

#if DEBUG_AGENT_WORK
            if (agent.IsTerminated) {
                VisualDebug.BeginFrame("Agent terminated", true);
                VisualDebug.SetColour(Colours.lightRed);
            }
            else {
                VisualDebug.BeginFrame("Agent work", true);
                VisualDebug.SetColour(Colours.lightGreen, Colours.veryDarkGrey);
            }
            VisualDebug.DrawPoint(agent.Position, .05f);

            VisualDebug.SetColour(Colours.lightGreen, Colours.veryDarkGrey);
            VisualDebug.DrawLineSegment(agent.Position, oldPos);
#endif

            iterations++;
        }

#if DEBUG_AGENT_WORK
        if (this.queue.Count == 0) {
            VisualDebug.Save();
        }
#endif

        yield return new WaitForSeconds(generationTickInterval);
        areAgentsWorking = false;
    }

    private void OnGUI() {
        if (network == null) return;
        GUI.Label(new Rect(10, 10, 100, 20), "node count: " + network.Nodes.Count);
        GUI.Label(new Rect(10, 40, 100, 20), "tree count: " + network.Tree.Count);
    }

    struct RoadStep {
        public Node n;
        public Node prev;
        public BezierSpline spline;
        public RoadMesh roadMesh;

        public RoadStep(Node n, Node prev = null, BezierSpline spline = null, RoadMesh roadMesh = null) {
            this.n = n;
            this.prev = prev;
            this.spline = spline;
            this.roadMesh = roadMesh;
        }
    }

    public void GenerateRoadMesh()
    {
        StartCoroutine("GenerateRoadMeshCoroutine");
    }

    IEnumerator GenerateRoadMeshCoroutine()
    {
        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
        Queue<RoadStep> nodesToVisit = new Queue<RoadStep>();

        nodesToVisit.Enqueue(new RoadStep(network.Nodes[0]));
        Color color = new Color(0, 0, 1);

        // Find suitable starting node
        while (nodesToVisit.Count != 0)
        {
            RoadStep segment = nodesToVisit.Dequeue();
            Node n = segment.n;
            Node prev = segment.prev;

            visited[n] = true;

            if (n.connections.Count > 2)
            {
                nodesToVisit.Clear();
                visited.Clear();
                nodesToVisit.Enqueue(new RoadStep(n));
                break;
            }

            foreach (NodeConnection c in n.connections)
            {
                if (visited.ContainsKey(c.node)) continue;
                if (c.type == ConnectionType.Street) continue;

                nodesToVisit.Enqueue(new RoadStep(c.node, n));
            }
        }

        while (nodesToVisit.Count != 0)
        {
            RoadStep segment = nodesToVisit.Dequeue();
            visited[segment.n] = true;

            if (segment.spline == null) {
                GameObject roadGameObject = new GameObject("Road Mesh");
                roadGameObject.transform.parent = transform;
                segment.spline = roadGameObject.AddComponent<BezierSpline>();
                segment.roadMesh = roadGameObject.AddComponent<RoadMesh>();
            }

            if (segment.prev != null) {
                if (segment.spline.ControlPointCount == 0)
                {
                    segment.spline.AddPoint(segment.prev.pos);
                }
                segment.spline.AddPoint(segment.n.pos);
                segment.roadMesh.GenerateRoadMesh();
            }
            else {
                // First node
            }

            foreach (NodeConnection c in segment.n.connections)
            {
                if (visited.ContainsKey(c.node))
                {
                    if (c.node != segment.prev)
                    {
                        if (segment.n.connections.Count == 1) {
                        }
                        else if (segment.n.connections.Count == 2) {
                            // segment.spline.AddPoint(c.node.pos);
                            // segment.roadMesh.GenerateRoadMesh();
                        }
                        else {
                            // GameObject newRoadGameObject = new GameObject("Road Mesh");
                            // BezierSpline newSpline = newRoadGameObject.AddComponent<BezierSpline>();
                            // RoadMesh newRoadMesh = newRoadGameObject.AddComponent<RoadMesh>();

                            // newRoadGameObject.transform.parent = transform;

                            // newSpline.AddPoint(segment.n.pos);
                            // newSpline.AddPoint(c.node.pos);
                            // newRoadMesh.GenerateRoadMesh();
                        }
                    }

                    continue;
                }

                bool shouldCreateNew = segment.n.connections.Count > 2;
                RoadStep newRoadstep = new RoadStep(c.node, segment.n, shouldCreateNew ? null : segment.spline, shouldCreateNew ? null : segment.roadMesh);
                nodesToVisit.Enqueue(newRoadstep);
            }

            if (debugRoadMeshGeneration) {
                yield return new WaitForSeconds(roadMeshTickInterval);
            }
        }

        yield return true;
    }

    private void Start() {
    }

    private void Update() {
        if (this.queue != null && !areAgentsWorking) {
            StartCoroutine("DoAgentWork");
        }

        if (this.network != null)
            this.network.DrawDebug();

        foreach (Vector3 p in debugPoints) {
            DrawUtil.DebugDrawCircle(p, 0.03f, new Color(1, 0.5f, 0));
        }
    }
}
