using System.Collections.Generic;
using UnityEngine;

public static class ManhattanBuildingBasementGenerator {

    public static GameObject Generate(List<Vector2> vertices, Material basementMaterial, GameObject buildingObject,
        float depth) {
        Debug.Log(depth);

        var basementObject = new GameObject("Basement");
        basementObject.transform.parent = buildingObject.transform;
        basementObject.transform.position = new Vector3(0, 0, 0);

        var meshRenderer = basementObject.AddComponent<MeshRenderer>();
        meshRenderer.material = basementMaterial;

        var mesh = basementObject.AddComponent<MeshFilter>().mesh;

        var basementVertices = new List<Vector3>();
        var basementTriangles = new List<int>();

        for (var i = 0; i < vertices.Count; i++) {
            var cur = VectorUtil.Vector2To3(vertices[i]);
            var next = VectorUtil.Vector2To3(vertices[(i + 1) % vertices.Count]);

            var j = basementVertices.Count;

            basementVertices.Add(cur);
            basementVertices.Add(next);
            basementVertices.Add(next + (Vector3.down * depth));
            basementVertices.Add(cur + (Vector3.down * depth));

            basementTriangles.Add(j);
            basementTriangles.Add(j + 1);
            basementTriangles.Add(j + 2);

            basementTriangles.Add(j);
            basementTriangles.Add(j + 2);
            basementTriangles.Add(j + 3);
        }

        mesh.vertices = basementVertices.ToArray();
        mesh.triangles = basementTriangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return basementObject;
    }

}
