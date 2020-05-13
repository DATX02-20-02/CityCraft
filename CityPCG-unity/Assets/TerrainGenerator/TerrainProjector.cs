using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.PolygonSplitter;

public class ProjectedMesh {
    public Vector3[] vertices;
    public int[] indices;
    public Vector2[] uv;

    public ProjectedMesh(Vector3[] vertices, int[] indices, Vector2[] uv) {
        this.vertices = vertices;
        this.indices = indices;
        this.uv = uv;
    }
}

public class TerrainProjector {
    public static ProjectedMesh ProjectPolygon(List<Vector3> vertices, TerrainModel terrain) {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        List<Vector2> vertices2D = new List<Vector2>();

        foreach (Vector3 vert in vertices) {
            minX = Mathf.Min(minX, vert.x);
            minY = Mathf.Min(minY, vert.z);
            maxX = Mathf.Max(maxX, vert.x);
            maxY = Mathf.Max(maxY, vert.z);

            vertices2D.Add(VectorUtil.Vector3To2(vert));
        }

        vertices2D.Add(vertices2D[0]);

        Polygon poly = new Polygon(vertices2D);

        Vector2Int res = terrain.Resolution;
        int minXStep = (int)((minX / terrain.width) * res.x);
        int minYStep = (int)((minY / terrain.depth) * res.y);

        int maxXStep = (int)((maxX / terrain.width) * res.x);
        int maxYStep = (int)((maxY / terrain.depth) * res.y);

        List<Triangle> candidates = new List<Triangle>();
        for (int x = minXStep; x <= maxXStep; x++) {
            for (int y = minYStep; y <= maxYStep; y++) {
                Vector3 p0 = terrain.GetTriangleCornerPos(x, y);
                Vector3 p1 = terrain.GetTriangleCornerPos(x + 1, y);
                Vector3 p2 = terrain.GetTriangleCornerPos(x + 1, y + 1);
                Vector3 p3 = terrain.GetTriangleCornerPos(x, y + 1);

                candidates.Add(new Triangle(p0, p1, p3));
                candidates.Add(new Triangle(p2, p1, p3));
            }
        }

        List<Vector3> meshVertices = new List<Vector3>();
        Dictionary<Vector3, int> meshVertexMap = new Dictionary<Vector3, int>();
        List<int> meshIndices = new List<int>();
        List<Vector2> meshUVs = new List<Vector2>();

        int TryAddVertex(Vector3 vertex) {
            if (!meshVertexMap.ContainsKey(vertex)) {
                int idx = meshVertices.Count;
                meshVertexMap[vertex] = idx;
                meshVertices.Add(vertex);
                meshUVs.Add(
                    new Vector2(
                        Mathf.InverseLerp(minX, maxX, vertex.x) * (maxX - minX),
                        Mathf.InverseLerp(minY, maxY, vertex.z) * (maxY - minY)
                    )
                );

                return idx;
            }

            return meshVertexMap[vertex];
        };

        void TryAddTriangle(Triangle tri) {
            int i1 = TryAddVertex(tri.point1);
            int i2 = TryAddVertex(tri.point2);
            int i3 = TryAddVertex(tri.point3);

            meshIndices.Add(i1);
            meshIndices.Add(i2);
            meshIndices.Add(i3);
        }

        foreach (Triangle tri in candidates) {
            Vector2 p1 = VectorUtil.Vector3To2(tri.point1);
            Vector2 p2 = VectorUtil.Vector3To2(tri.point2);
            Vector2 p3 = VectorUtil.Vector3To2(tri.point3);

            List<Vector2> triVertices = new List<Vector2>() { p1, p2, p3, p1 };
            Polygon triPoly = new Polygon(triVertices);

            List<IEnumerable<Vector2>> results2D = new List<IEnumerable<Vector2>>();
            if (poly.Intersects(triPoly)) {
                List<List<Vector2>> diffVerts = Habrador.GreinerHormann.ClipPolygons(
                    poly.points,
                    triPoly.points,
                    Habrador.BooleanOperation.Intersection
                );

                foreach (List<Vector2> result in diffVerts) {
                    results2D.Add(result);
                }
            }
            else if (triPoly.Contains(poly)) {
                results2D.Add(poly.points);
            }
            else if (poly.Contains(triPoly)) {
                results2D.Add(triPoly.points);
            }
            else continue;

            foreach (IEnumerable<Vector2> result2D in results2D) {
                Vector3[] result3D = result2D
                    .Distinct()
                    .Select(
                        v => {
                            var p = terrain.GetMeshIntersection(v.x, v.y);
                            return p.point + p.normal * 0.01f;
                        }
                    )
                    .ToArray();

                Triangulator triangulator = new Triangulator(result3D);
                int[] triangulated = triangulator.Triangulate();

                for (int i = 0; i < triangulated.Length; i += 3) {
                    TryAddTriangle(
                        new Triangle(
                            result3D[triangulated[i + 0]],
                            result3D[triangulated[i + 1]],
                            result3D[triangulated[i + 2]]
                        )
                    );
                }
            }
        }

        return new ProjectedMesh(
            meshVertices.ToArray(),
            meshIndices.ToArray(),
            meshUVs.ToArray()
        );
    }
}
