using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ManhattanBuildingRoofGenerator {

    public static GameObject Generate(List<Vector2> topVertices, Material roofMaterial, GameObject buildingObject,
        float buildingHeight) {
        var roofObject = new GameObject("Roof");
        roofObject.transform.parent = buildingObject.transform;
        roofObject.transform.position = new Vector3(0, buildingHeight, 0);

        var vertices = topVertices.ConvertAll(v => new Vector3(v.x, 0, v.y)).ToArray();

        var meshRenderer = roofObject.AddComponent<MeshRenderer>();
        meshRenderer.material = roofMaterial;

        var mesh = roofObject.AddComponent<MeshFilter>().mesh;
        var triangulator = new Triangulator(vertices);

        mesh.vertices = vertices;
        mesh.triangles = triangulator.Triangulate();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return roofObject;
    }

}
