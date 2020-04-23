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
    [SerializeField] private RoadMeshGenerator meshGenerator = null;

    [Range(0, 1000)]
    [SerializeField] private int maxAgentQueueIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float generationTickInterval = 0.2f;
    [SerializeField] private GameObject ghostObject = null;

    [Header("Instantiate Parents")]
    [SerializeField] private GameObject ghostParent = null;

    [Header("Debug")]
    [SerializeField] private List<Vector3> debugPoints = new List<Vector3>();

    [SerializeField] private bool debug = false;

    private GameObject ghostObjectInstance = null;
    private CityInput selected;
    private CityInput dragging;
    private Vector3 dragOffset;

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

    public enum CityType {
        Paris,
        Manhattan
    }

    public class CityInput {
        public Vector3 position;
        public CityType type;
        public GameObject ghostObject;
        public float radius;

        private bool hovering;
        private bool selected;

        public CityInput(Vector3 position, CityType type, GameObject ghostObject, float radius) {
            this.position = position;
            this.type = type;
            this.ghostObject = ghostObject;
            this.radius = radius;
        }

        public void SetHovering(bool hovering) {
            if (!this.selected && this.hovering != hovering) {
                if (hovering) {
                    ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", Color.red);
                }
                else {
                    ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", new Color(93 / 255f, 151 / 255f, 1));
                }
            }

            this.hovering = hovering;
        }

        public void SetSelected(bool selected) {
            if (this.selected != selected) {
                if (selected) {
                    ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", new Color(1, 0.5f, 0));
                }
                else {
                    ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", new Color(93 / 255f, 151 / 255f, 1));
                }
            }

            this.selected = selected;
        }
    }

    private List<CityInput> cityInputs = new List<CityInput>();

    public void Reset() {
        this.network = null;

        if (ghostParent != null)
            foreach (Transform child in ghostParent.transform) {
                Destroy(child.gameObject);
            }

        this.cityInputs = new List<CityInput>();
    }

    // Generates a complete road network.
    public void Generate(TerrainModel terrain, Noise population, Action<RoadNetwork> callback = null) {
        this.terrainModel = terrain;
        this.callback = callback;
        prevQueueCount = 0;
        areAgentsWorking = false;

        network = new RoadNetwork(terrain, population, terrain.width, terrain.depth);
        queue = new PriorityQueue<Agent>();

        ParisAgentFactory parisFactory = new ParisAgentFactory();
        ManhattanAgentFactory manhattanFactory = new ManhattanAgentFactory();

        int priority = 0;
        foreach (CityInput cityInput in cityInputs) {
            switch (cityInput.type) {
                case CityType.Paris:
                    priority = parisFactory.Create(this, network, cityInput.position, cityInput.radius, priority++);
                    population.AddAmplifier(
                        new CircularAmplifier(
                            new Vector2(cityInput.position.x / terrain.width, cityInput.position.z / terrain.depth),
                            0, 0.3f, 1f
                        )
                    );
                    priority++;
                    break;

                case CityType.Manhattan:
                    priority = manhattanFactory.Create(this, network, cityInput.position, cityInput.radius, priority++);
                    priority++;
                    break;
            }
        }
    }

    // Generates road meshes
    public void GenerateMesh() {
        meshGenerator.Generate(network, this.terrainModel);
    }

    public void GenerateStreets(TerrainModel terrain, Noise population, Action<RoadNetwork> callback) {
        this.terrainModel = terrain;
        this.callback = callback;

        StreetsAgentFactory factory = new StreetsAgentFactory();
        factory.Create(this, network, Vector3.zero);
    }

    // Adds an agent to the pool of active agents.
    public void AddAgent(Agent agent) {
        this.queue.Enqueue(agent);
    }

    public void AddCityInput(CityInput input) {
        if (input.ghostObject == null)
            input.ghostObject = Instantiate(ghostObject, ghostParent.transform);

        input.ghostObject.transform.position = input.position;
        input.ghostObject.transform.localScale = Vector3.one * (input.radius * 2 + 10);

        cityInputs.Add(input);
    }

    // Iterate through queue and let the agents work.
    IEnumerator DoAgentWork() {
        if (prevQueueCount != 0 && this.queue.Count == 0) {
            if (callback != null) {
                meshGenerator.Generate(network, this.terrainModel);
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

        if (debug && this.network != null) {
            this.network.DrawDebug();
        }
    }

    float radius = 70;
    public void UpdateWhenState(TerrainModel terrain) {
        if (ghostObject != null && ghostObjectInstance == null) {
            ghostObjectInstance = Instantiate(ghostObject, ghostParent.transform);
        }

        if (ghostObjectInstance != null) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                Vector3 pos = hit.point;
                pos.y = Mathf.Max(terrain.seaLevel, pos.y);

                CityInput hover = null;
                foreach (CityInput input in cityInputs) {
                    input.SetHovering(false);
                    if (hover == null && Vector3.Distance(pos, input.position) < 20) {
                        input.SetHovering(true);
                        hover = input;
                    }
                }

                if (dragging != null) {
                    dragging.position = pos - dragOffset;
                    dragging.ghostObject.transform.position = dragging.position;

                    if (Input.GetMouseButtonUp(0)) {
                        dragging = null;
                    }
                }

                if (hover != null && hover != selected) {
                    ghostObjectInstance.SetActive(false);

                    if (Input.GetMouseButtonDown(0)) {
                        if (!EventSystem.current.IsPointerOverGameObject()) {
                            if (selected != null)
                                selected.SetSelected(false);

                            selected = hover;
                            selected.SetSelected(true);

                            dragging = hover;
                            dragOffset = pos - dragging.position;
                        }
                    }
                }
                else {
                    if (selected == null) {
                        ghostObjectInstance.SetActive(true);

                        Vector3 scroll = Input.mouseScrollDelta;
                        radius = Mathf.Clamp(radius + scroll.y * 2, 0.1f, 500f);

                        ghostObjectInstance.transform.position = pos;
                        ghostObjectInstance.transform.localScale = Vector3.one * (radius * 2 + 10);

                        if (Input.GetMouseButtonDown(0)) {
                            if (!EventSystem.current.IsPointerOverGameObject()) {
                                cityInputs.Add(new CityInput(pos, CityType.Manhattan, ghostObjectInstance, radius));
                                ghostObjectInstance = null;
                            }
                        }
                    }
                    else {
                        if (Input.GetMouseButtonDown(0)) {
                            if (hover != selected) {
                                if (!EventSystem.current.IsPointerOverGameObject()) {
                                    selected.SetSelected(false);
                                    selected = null;
                                }
                            }
                            else {
                                dragging = hover;
                                dragOffset = pos - dragging.position;
                            }
                        }
                    }
                }

                if (selected != null) {
                    Vector3 scroll = Input.mouseScrollDelta;
                    selected.radius = Mathf.Clamp(selected.radius + scroll.y * 2, 0.1f, 500f);
                    selected.ghostObject.transform.localScale = Vector3.one * (selected.radius * 2 + 10);

                    if (Input.GetKeyDown(KeyCode.Delete)) {
                        Destroy(selected.ghostObject);
                        cityInputs.Remove(selected);
                        selected = null;
                    }
                }
            }
            else {
                ghostObjectInstance.SetActive(false);
            }
        }
    }
}
