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

    IEnumerator GenerateRoadMesh()
    {
        Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
        Dictionary<Node, Node> backtrack = new Dictionary<Node, Node>();
        Dictionary<Node, Dictionary<Node, bool>> placed = new Dictionary<Node, Dictionary<Node, bool>>();
        Dictionary<Node, BezierSpline> splines = new Dictionary<Node, BezierSpline>();
        Stack<Node> nodesToVisit = new Stack<Node>();

        nodesToVisit.Push(network.Nodes[0]);
        Node splineStart = network.Nodes[0];
        Color color = new Color(0, 0, 1);

        GameObject roadGameObject = null;
        BezierSpline spline = null;
        RoadMesh roadMesh = null;

        Node prev = null;

        while (nodesToVisit.Count != 0)
        {
            Node n = nodesToVisit.Pop();
            visited[n] = true;

            if (n.connections.Count > 2 || n.connections.Count == 1)
            {
                nodesToVisit.Clear();
                visited.Clear();
                nodesToVisit.Push(n);
                break;
            }

            foreach (NodeConnection c in n.connections)
            {
                if (visited.ContainsKey(c.node)) continue;
                if (c.type == ConnectionType.Street) continue;

                nodesToVisit.Push(c.node);
            }
        }

        while (nodesToVisit.Count != 0)
        {
            Node n = nodesToVisit.Pop();
            if (!placed.ContainsKey(n)) {
                placed[n] = new Dictionary<Node, bool>();
            }

            visited[n] = true;

            if (spline == null)
            {
                roadGameObject = new GameObject("Road Mesh");
                spline = roadGameObject.AddComponent<BezierSpline>();
                roadMesh = roadGameObject.AddComponent<RoadMesh>();
            }

            spline.AddPoint(n.pos);
            roadMesh.GenerateRoadMesh();

            if (prev != null)
                placed[prev][n] = true;

            bool didAdd = false;
            foreach (NodeConnection c in n.connections)
            {
                if (visited.ContainsKey(c.node))
                {
                    // if (spline != null)
                    // {
                    //     spline.AddPoint(n.pos);
                    //     roadMesh.GenerateRoadMesh();
                    //     spline = null;
                    // }
                    // if (spline == null)
                    // {
                    //     roadGameObject = new GameObject("Road Mesh");
                    //     spline = roadGameObject.AddComponent<BezierSpline>();
                    //     roadMesh = roadGameObject.AddComponent<RoadMesh>();

                    //     roadGameObject.transform.parent = transform;

                    //     spline.AddPoint(n.pos);
                    //     spline.AddPoint(c.node.PPS);
                    // }

                    Dictionary<Node, bool> b = placed.ContainsKey(c.node) ?
                        placed[c.node] : new Dictionary<Node, bool>();

                    if (!b.ContainsKey(n)) {
                        if (n.connections.Count == 2) {
                            if (spline) {
                                Debug.Log("hej");
                                spline.AddPoint(c.node.pos);
                                roadMesh.GenerateRoadMesh();
                                spline = null;
                                prev = null;
                            }
                        }
                        else {
                            GameObject newRoadGameObject = new GameObject("Road Mesh");
                            BezierSpline newSpline = newRoadGameObject.AddComponent<BezierSpline>();
                            RoadMesh newRoadMesh = newRoadGameObject.AddComponent<RoadMesh>();

                            newRoadGameObject.transform.parent = transform;

                            newSpline.AddPoint(n.pos);
                            newSpline.AddPoint(c.node.pos);
                            newRoadMesh.GenerateRoadMesh();
                        }
                    }

                    continue;
                }
                // if (c.type == Node.ConnectionType.Street) continue;

                nodesToVisit.Push(c.node);

                didAdd = true;
            }

            prev = n;
            if (n.connections.Count == 1 || !didAdd)
            {
                spline = null;
                prev = null;
            }
            if (n.connections.Count == 2)
            {
            }
            if (n.connections.Count > 2)
            {
                if (didAdd) {
                    roadGameObject = new GameObject("Road Mesh");
                    spline = roadGameObject.AddComponent<BezierSpline>();
                    roadMesh = roadGameObject.AddComponent<RoadMesh>();

                    roadGameObject.transform.parent = transform;

                    spline.AddPoint(n.pos);
                }
            }

            // foreach (Node.NodeConnection c in n.connections)
            // {
            //     if (visited.ContainsKey(c.node)) {
            //         // roadGameObject = new GameObject("Road Mesh");
            //         // spline = roadGameObject.AddComponent<BezierSpline>();
            //         // roadMesh = roadGameObject.AddComponent<RoadMesh>();

            //         // roadGameObject.transform.parent = transform;

            //         continue;
            //     };
            //     if (c.type == Node.ConnectionType.Street) continue;

            //     nodesToVisit.Push(c.node);
            // }

            // if (n.connections.Count > 2) {
            //     foreach (Node.NodeConnection c in n.connections) {
            //         roadGameObject = new GameObject("Road Mesh Junction");
            //         spline = roadGameObject.AddComponent<BezierSpline>();
            //         roadMesh = roadGameObject.AddComponent<RoadMesh>();

            //         roadGameObject.transform.parent = transform;

            //         spline.AddPoint(n.pos);
            //         spline.AddPoint(c.node.pos);
            //         splines[c.node] = spline;

            //         if (visited.ContainsKey(c.node)) continue;
            //         if (c.type == Node.ConnectionType.Street) continue;

            //         nodesToVisit.Push(c.node);
            //     }
            // }
            // if (n.connections.Count == 2) {
            //     foreach (Node.NodeConnection c in n.connections) {


            //         if (visited.ContainsKey(c.node)) continue;
            //         if (c.type == Node.ConnectionType.Street) continue;
            //         nodesToVisit.Push(c.node);
            //     }
            //     spline.AddPoint(n.pos);
            //     spline.AddPoint(c.node.pos);
            // }
            // if (n.connections.Count == 1) {
            //     spline.AddPoint(n.pos);
            //     spline.AddPoint(c.node.pos);
            // }


            yield return new WaitForSeconds(roadMeshTickInterval);
        }
        // yield return true;
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

        bool doMeshGeneration = Input.GetButtonDown("Jump");
        if (doMeshGeneration) {
            StartCoroutine("GenerateRoadMesh");
        }
    }
}
