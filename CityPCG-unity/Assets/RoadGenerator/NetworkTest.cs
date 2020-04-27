using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class NetworkTest : MonoBehaviour {
    [SerializeField] private TerrainGenerator terrainGen = null;
    [SerializeField] private NoiseGenerator noiseGen = null;
    [SerializeField] private RoadGenerator roadGen = null;

    private TerrainModel terrain;
    private Noise population;
    private RoadNetwork network;
    private Agent agent;
    private Node prevNode;

    void Start() {
    }

    void OnEnable() {
        StartCoroutine("Initialize");
    }

    public IEnumerator Initialize() {
        yield return new WaitForEndOfFrame();

        terrain = terrainGen.GenerateTerrain();
        population = noiseGen.Generate();
        network = roadGen.Generate(terrain, population, new List<CityInput>() { }, (RoadNetwork network) => { });

        agent = new Agent(network, new Vector3(10, 0, 10), new Vector3(0, 0, 0), null, 0);

        prevNode = agent.PlaceNode(new Vector3(10, 0, 10), Node.NodeType.Main, ConnectionType.Main);
        prevNode = agent.PlaceNode(new Vector3(20, 0, 10), Node.NodeType.Main, ConnectionType.Main);
    }

    void Update() {
        if (network == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            Vector3 pos = hit.point;
            pos.y = Mathf.Max(terrain.seaLevel, pos.y);

            DrawUtil.DebugDrawCircle(pos, agent.config.snapRadius, Color.yellow, 20);

            if (Input.GetMouseButtonDown(0)) {
                prevNode = agent.PlaceNode(pos, Node.NodeType.Main, ConnectionType.Main);
            }

            if (prevNode != null) {
                Vector3 dir = (pos - prevNode.pos).normalized;
                Vector3 perp = Vector3.Cross(dir, Vector3.up);

                Vector3 p1 = pos + perp * agent.config.snapRadius;
                Vector3 p2 = prevNode.pos + perp * agent.config.snapRadius;
                Vector3 tp1 = terrain.GetMeshIntersection(p1.x, p1.z).point;
                Vector3 tp2 = terrain.GetMeshIntersection(p2.x, p2.z).point;

                Vector3 p3 = pos - perp * agent.config.snapRadius;
                Vector3 p4 = prevNode.pos - perp * agent.config.snapRadius;
                Vector3 tp3 = terrain.GetMeshIntersection(p3.x, p3.z).point;
                Vector3 tp4 = terrain.GetMeshIntersection(p4.x, p4.z).point;

                Debug.DrawLine(tp1, tp2, Color.yellow);
                Debug.DrawLine(tp3, tp4, Color.yellow);
                Debug.DrawLine(tp2, tp4, Color.yellow);
            }
        }

        network.DrawDebug(true);
    }

    void OnGUI() {
        if (network != null) {
            GUI.Label(new Rect(10, 10, 100, 20), "node count: " + network.Nodes.Count);
            GUI.Label(new Rect(10, 40, 100, 20), "tree count: " + network.Tree.Count);
        }
    }

}
