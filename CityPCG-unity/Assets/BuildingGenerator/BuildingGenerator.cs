using UnityEngine;

public class BuildingGenerator : MonoBehaviour {
    public GameObject building;

    public GameObject Generate(Plot plot) {
        var b = Instantiate(building, transform);
        var mesh = b.GetComponent<MeshFilter>().mesh;

        mesh.Clear();

        int plotLength = plot.vertices.Count;
        Vector3 up = Vector3.up;
        float buildingHeight = 5; //Random.Range(3, 8);

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
}
