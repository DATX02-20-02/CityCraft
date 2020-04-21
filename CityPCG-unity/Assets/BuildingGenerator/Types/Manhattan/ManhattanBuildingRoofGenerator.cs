using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ManhattanBuildingRoofGenerator {

    public static TemporaryTransformedMesh Generate(List<Vector2> topVertices, Material roofMaterial,
        float buildingHeight) {

        //this can be optimzed via not having to create a object.
        var roofObject = new GameObject("Roof");

        var vertices = topVertices.ConvertAll(v => new Vector3(v.x, 0, v.y)).ToArray();

        var meshRenderer = roofObject.AddComponent<MeshRenderer>();
        meshRenderer.material = roofMaterial;

        var mesh = roofObject.AddComponent<MeshFilter>().mesh;
        var triangulator = new Triangulator(vertices);

        mesh.vertices = vertices;
        mesh.triangles = triangulator.Triangulate();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        var transform = Matrix4x4.Translate(new Vector3(0, buildingHeight, 0));

        Object.Destroy(roofObject);

        return new TemporaryTransformedMesh(transform, roofObject);
    }

}
