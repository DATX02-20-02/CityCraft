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
    [SerializeField] private RoadMeshGenerator meshGenerator = null;

    [Range(0, 1000)]
    [SerializeField] private int maxAgentQueueIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float generationTickInterval = 0.2f;

    [SerializeField] private List<Vector3> debugPoints = new List<Vector3>();

    [SerializeField] private bool debug = false;

    private RoadNetwork network;

    private PriorityQueue<Agent> queue = new PriorityQueue<Agent>();
    private int prevQueueCount = 0;

    private bool areAgentsWorking = false;
    private Action<RoadNetwork> callback;

    private TerrainModel terrainModel;

    public RoadNetwork Network {
        set { this.network = value; }
        get { return this.network; }
    }

    // Generates a complete road network.
    public void Generate(TerrainModel terrain, Noise population, Action<RoadNetwork> callback = null) {
        this.terrainModel = terrain;
        this.callback = callback;
        prevQueueCount = 0;
        areAgentsWorking = false;

        network = new RoadNetwork(terrain, population, terrain.width, terrain.depth);
        queue = new PriorityQueue<Agent>();

        IAgentFactory factory = new ParisAgentFactory();

        factory.Create(this, network, new Vector3(300 - 150, 0, 300));
        factory.Create(this, network, new Vector3(300 + 0, 0, 300));
    }

    // Generates road meshes
    public void GenerateMesh() {
        meshGenerator.Generate(network, this.terrainModel);
    }

    public void GenerateStreets(TerrainModel terrain, Noise population, Action<RoadNetwork> callback) {
        this.terrainModel = terrain;
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
                GetComponent<RoadMeshGenerator>().Generate(network, this.terrainModel);
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
        if (debug && network != null) {
            GUI.Label(new Rect(10, 10, 100, 20), "node count: " + network.Nodes.Count);
            GUI.Label(new Rect(10, 40, 100, 20), "tree count: " + network.Tree.Count);
        }
    }

    private void Update() {
        if (this.queue != null && !areAgentsWorking) {
            StartCoroutine("DoAgentWork");
        }

        if (debug) {
            if (this.network != null)
                this.network.DrawDebug();

            foreach (Vector3 p in debugPoints) {
                DrawUtil.DebugDrawCircle(p, 0.03f, new Color(1, 0.5f, 0));
            }
        }
    }
}
