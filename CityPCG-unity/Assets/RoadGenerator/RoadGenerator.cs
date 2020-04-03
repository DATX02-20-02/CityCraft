// #define DEBUG_AGENT_WORK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private GameObject ghostObject = null;

    [SerializeField] private List<Vector3> debugPoints = new List<Vector3>();

    private GameObject ghostObjectInstance = null;

    private RoadNetwork network;

    private PriorityQueue<Agent> queue = new PriorityQueue<Agent>();
    private int prevQueueCount = 0;

    private bool areAgentsWorking = false;
    private Action<RoadNetwork> callback;

    public RoadNetwork Network {
        set { this.network = value; }
        get { return this.network; }
    }

    public enum CityType {
        Paris,
        Manhattan
    }

    public struct CityInput {
        public Vector3 position;
        public CityType type;
        public GameObject ghostObject;
        public float radius;

        public CityInput(Vector3 position, CityType type, GameObject ghostObject, float radius) {
            this.position = position;
            this.type = type;
            this.ghostObject = ghostObject;
            this.radius = radius;
        }
    }

    private List<CityInput> cityInputs = new List<CityInput>();

    public void Reset() {
        this.network = null;

        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        this.cityInputs = new List<CityInput>();
    }

    // Generates a complete road network.
    public void Generate(TerrainModel terrain, Noise population, Action<RoadNetwork> callback = null) {
        this.callback = callback;
        prevQueueCount = 0;
        areAgentsWorking = false;

        network = new RoadNetwork(terrain, population, terrain.width, terrain.depth);
        queue = new PriorityQueue<Agent>();

        ParisAgentFactory factory = new ParisAgentFactory();

        int priority = 0;
        foreach (CityInput cityInput in cityInputs) {
            switch (cityInput.type) {
                case CityType.Paris:
                    priority = factory.Create(this, network, cityInput.position, cityInput.radius, priority++);
                    priority++;
                    break;
            }
        }


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

        StreetsAgentFactory factory = new StreetsAgentFactory();
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

    float radius = 70;
    public void UpdateWhenState(TerrainModel terrain) {
        if (ghostObject != null && ghostObjectInstance == null) {
            ghostObjectInstance = Instantiate(ghostObject, transform);
        }

        if (ghostObjectInstance != null) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                Vector3 pos = hit.point;

                ghostObjectInstance.SetActive(true);

                ghostObjectInstance.transform.position = pos;
                ghostObjectInstance.transform.localScale = Vector3.one * (radius * 2 + 10);

                Vector3 scroll = Input.mouseScrollDelta;
                radius += scroll.y * 2;

                if (Input.GetMouseButtonDown(0)) {
                    if (!EventSystem.current.IsPointerOverGameObject()) {
                        cityInputs.Add(new CityInput(pos, CityType.Paris, ghostObjectInstance, radius));
                        ghostObjectInstance = null;
                    }
                }
            }
            else {
                ghostObjectInstance.SetActive(false);
            }
        }
    }
}
