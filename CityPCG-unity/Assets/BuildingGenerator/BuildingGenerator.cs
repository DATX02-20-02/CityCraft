using UnityEngine;

public class BuildingGenerator : MonoBehaviour {
    public GameObject building;

    public void Generate(Plot plot) {
        var b = Instantiate(building, transform);
        var mesh = b.GetComponent<MeshFilter>().mesh;

        mesh.Clear();

        var plotLength = plot.vertices.Count;

        var meshVertices = new Vector3[plotLength * 2];
        for (var i = 0; i < plotLength; i++) {
            meshVertices[i] = plot.vertices[i];
        }

        var up = new Vector3(0, 5, 0);
        var topVertices = new Vector3[plotLength];

        for (var i = plotLength; i < meshVertices.Length; i++) {
            var newVert = meshVertices[i - plotLength] + up;
            topVertices[i - plotLength] = newVert;
            meshVertices[i] = newVert;
        }

        var wallIndices = new int[plotLength * 6];
        var numVert = meshVertices.Length;

        //Walls
        for (var i = 0; i < plotLength; i++) {
            var j = i * 6;

            wallIndices[j] = i;
            wallIndices[j + 1] = (i + plotLength) % numVert;
            wallIndices[j + 2] = (i + 1) % plotLength;

            wallIndices[j + 3] = (i + plotLength) % numVert;
            wallIndices[j + 4] = (i + plotLength + 1) % numVert;
            wallIndices[j + 5] = (i + 1) % plotLength;
        }

        var k = (plotLength - 1) * 6 + 4;
        if (wallIndices[k] == 0) {
            wallIndices[k] = plotLength;
        }

        var triangulator = new Triangulator(topVertices);
        var roofIndices = triangulator.Triangulate();

        var indices = new int[wallIndices.Length + roofIndices.Length];

        for (var i = 0; i < wallIndices.Length; i++) {
            indices[i] = wallIndices[i];
        }

        for (var (i, j) = (0, wallIndices.Length); i < roofIndices.Length; i++, j++) {
            indices[j] = roofIndices[i] + plotLength;
        }


        mesh.vertices = meshVertices;
        mesh.triangles = indices;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
