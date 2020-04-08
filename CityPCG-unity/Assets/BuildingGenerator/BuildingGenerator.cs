// #define DEBUG_BUILDING_WORK

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour {

    [SerializeField] private GameObject building = null;
    [SerializeField] private GameObject skyscraper = null;

    [Range(0, 1000)]
    [SerializeField] private int maxQueueIterations = 1;

    [Range(0, 0.4f)]
    [SerializeField] private float generationTickInterval = 0.2f;

    private Stack<Plot> queue = new Stack<Plot>();
    private List<GameObject> buildings;
    private Action<List<GameObject>> callback;

    private int prevQueueCount;
    private bool isProcessing;

    public void Reset() {
        if (buildings != null) {
            foreach (GameObject building in buildings) {
                Destroy(building);
            }
        }

        queue = new Stack<Plot>();
        buildings = new List<GameObject>();
    }

    public void StartGeneration(Action<List<GameObject>> callback) {
        Reset();
        this.callback = callback;
    }

    public void Queue(Plot plot) {
        this.queue.Push(plot);
    }

    IEnumerator ProcessQueue() {
        if (prevQueueCount != 0 && this.queue.Count == 0) {
            if (callback != null) {
                this.callback(buildings);
            }
            prevQueueCount = 0;
            yield break;
        }

        isProcessing = true;
        prevQueueCount = this.queue.Count;

        int iterations = 0;
        while (this.queue.Count > 0 && iterations < maxQueueIterations) {
            Plot plot = this.queue.Pop();

            GameObject obj = Generate(plot);
            buildings.Add(obj);

            iterations++;
        }

        yield return new WaitForSeconds(generationTickInterval);
        isProcessing = false;
    }

    public GameObject Generate(Plot plot) {
        /* SkyscraperGenerator */
        if (plot.type == PlotType.Skyscraper) {
            var s = Instantiate(skyscraper, transform);
            s.GetComponent<SkyscraperGenerator>().Generate(plot);
            return s;
        }

        if (plot.type == PlotType.Apartments) {

            /* Normal building generator */
            var b = Instantiate(building, transform);
            var mesh = b.GetComponent<MeshFilter>().mesh;

            mesh.Clear();

            int plotLength = plot.vertices.Count;
            Vector3 up = Vector3.up;
            float buildingHeight = UnityEngine.Random.Range(0.5f, 1.5f);

            Vector3[] meshVertices = new Vector3[plotLength * 4 + 4 + plotLength];
            int[] wallIndices = new int[(plotLength * 6 + 6)];

            Vector3[] topVertices = new Vector3[plotLength];

            float highestPoint = float.MinValue;
            foreach (Vector3 vert in plot.vertices) {
                if (highestPoint < vert.y) {
                    highestPoint = vert.y;
                }
            }

            int meshIdx = 0;
            int triIdx = 0;
            int topIdx = 0;
            for (int i = 0; i < plotLength; i++) {
                Vector3 vert0 = plot.vertices[i];
                Vector3 vert1 = plot.vertices[(i + 1) % plotLength];

                Vector3 eVert0 = new Vector3(vert0.x, highestPoint, vert0.z);
                Vector3 eVert1 = new Vector3(vert1.x, highestPoint, vert1.z);

                meshVertices[meshIdx + 0] = vert0;
                meshVertices[meshIdx + 1] = vert1;

                meshVertices[meshIdx + 2] = eVert1 + up * buildingHeight;
                meshVertices[meshIdx + 3] = eVert0 + up * buildingHeight;

                wallIndices[triIdx + 0] = meshIdx + 2;
                wallIndices[triIdx + 1] = meshIdx + 1;
                wallIndices[triIdx + 2] = meshIdx + 0;

                wallIndices[triIdx + 3] = meshIdx + 3;
                wallIndices[triIdx + 4] = meshIdx + 2;
                wallIndices[triIdx + 5] = meshIdx + 0;

                topVertices[topIdx] = meshVertices[meshIdx + 3];

                meshIdx += 4;
                triIdx += 6;
                topIdx++;
            }

            var triangulator = new Triangulator(topVertices);
            int[] roofIndices = triangulator.Triangulate();

            int[] meshIndices = new int[wallIndices.Length + roofIndices.Length];


            for (int i = 0; i < topVertices.Length; i++) {
                meshVertices[plotLength * 4 + 4 + i] = topVertices[i];
            }

            for (int i = 0; i < wallIndices.Length; i++) {
                meshIndices[i] = wallIndices[i];
            }

            for (int i = 0; i < roofIndices.Length; i++) {
                meshIndices[wallIndices.Length + i] = plotLength * 4 + 4 + roofIndices[i];
            }

            mesh.vertices = meshVertices;
            mesh.triangles = meshIndices;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return b;
        }

        return null;
    }

    private void Update() {
        if (this.queue != null && this.queue.Count > 0 && !isProcessing) {
            StartCoroutine("ProcessQueue");
        }
    }
}
