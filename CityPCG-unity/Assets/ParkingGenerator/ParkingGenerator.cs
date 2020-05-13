using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class ParkingGenerator : MonoBehaviour {
    [SerializeField] private GameObject groupPrefab = null;
    [SerializeField] private GameObject lotPrefab = null;
    [SerializeField] private GameObject groundPrefab = null;

    [SerializeField] private float lotWidth = 0.1f;
    [SerializeField] private float lotHeight = 0.2f;

    [SerializeField] private float lodTransitionWidth = 0.02f;

    Rectangle rect;
    TerrainModel model;

    private Transform groupTransform;

    public void Reset() {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
    }

    public Rectangle Generate(TerrainModel terrain, Plot plot) {
        List<Vector2> polygon = plot.vertices.Select(VectorUtil.Vector3To2).ToList();

        rect = ApproximateLargestRectangle(polygon);
        if (rect.height < 0.4f || rect.width < 0.4f)
            return new Rectangle();

        var c = terrain.GetMeshIntersection(rect.Center.x, rect.Center.y);

        var groupObj = Instantiate(groupPrefab, c.point, Quaternion.identity, transform);
        groupObj.transform.name = "Parking Group";
        this.groupTransform = groupObj.transform;

        GameObject groundObj = GenerateGround(terrain, plot);

        int xResolution = 16;
        int yResolution = 2;
        float padding = 0.1f;

        GameObject obj = Instantiate(lotPrefab, c.point, Quaternion.identity, groupTransform);
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        Vector2 rectRightDir = new Vector2(Mathf.Cos(rect.angle), Mathf.Sin(rect.angle)).normalized;
        Vector2 rectUpDir = new Vector2(-Mathf.Sin(rect.angle), Mathf.Cos(rect.angle)).normalized;

        float quadWidth = Mathf.Floor(rect.width / lotWidth) * lotWidth;
        float quadHeight = lotHeight * 2;

        float paddedQuadWidth = quadWidth - padding * 2;

        int amountOfLines = (int)Mathf.Max(1, Mathf.Floor(rect.height / (quadHeight * 1.5f) - 1f));

        Vector3[] meshVertices = new Vector3[(xResolution + 1) * (yResolution + 1) * amountOfLines];
        Vector2[] meshUVs = new Vector2[(xResolution + 1) * (yResolution + 1) * amountOfLines];
        int[] meshIndices = new int[xResolution * yResolution * 6 * amountOfLines];

        for (int i = 0; i < amountOfLines; i++) {
            int index = i * meshIndices.Length / amountOfLines;
            int meshIndexStart = i * meshVertices.Length / amountOfLines;

            Vector2 origin = amountOfLines == 1 ?
                Vector2.Lerp(rect.botLeft, rect.topLeft, 0.5f) :
                Vector2.Lerp(
                    rect.botLeft + (rect.topLeft - rect.botLeft).normalized * (quadHeight / 2f + padding),
                    rect.topLeft + (rect.botLeft - rect.topLeft).normalized * (quadHeight / 2f + padding),
                    i / (float)(amountOfLines - 1)
                );

            for (int y = 0; y < yResolution + 1; y++) {
                for (int x = 0; x < xResolution + 1; x++) {
                    Vector2 localPos = origin
                        + rectRightDir * (x * (paddedQuadWidth / xResolution) + rect.width / 2 - paddedQuadWidth / 2)
                        + rectUpDir * (y * (quadHeight / yResolution) - quadHeight / 2);

                    var intersection = terrain.GetMeshIntersection(localPos.x, localPos.y);
                    Vector3 pos = intersection.point + intersection.normal * 0.02f;

                    meshVertices[meshIndexStart + x + y * (xResolution + 1)] = obj.transform.InverseTransformPoint(pos);
                    meshUVs[meshIndexStart + x + y * (xResolution + 1)] = new Vector2(x / (float)xResolution * (paddedQuadWidth / lotWidth), y / (float)yResolution);

                    if (x < xResolution && y < yResolution) {
                        meshIndices[index + 2] = meshIndexStart + x + y * (xResolution + 1);
                        meshIndices[index + 1] = meshIndexStart + (x + 1) + y * (xResolution + 1);
                        meshIndices[index + 0] = meshIndexStart + x + (y + 1) * (xResolution + 1);

                        meshIndices[index + 5] = meshIndexStart + (x + 1) + (y + 1) * (xResolution + 1);
                        meshIndices[index + 4] = meshIndexStart + x + (y + 1) * (xResolution + 1);
                        meshIndices[index + 3] = meshIndexStart + (x + 1) + y * (xResolution + 1);
                        index += 6;
                    }
                }
            }
        }

        mesh.vertices = meshVertices;
        mesh.triangles = meshIndices;
        mesh.uv = meshUVs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        this.model = terrain;

        LODGroup lodGroup = groupObj.GetComponent<LODGroup>();

        lodGroup.SetLODs(
            new LOD[] {
                new LOD(lodTransitionWidth, groupObj.GetComponentsInChildren<Renderer>())
            }
        );
        lodGroup.RecalculateBounds();

        return rect;
    }

    private GameObject GenerateGround(TerrainModel terrain, Plot plot) {
        Vector3 center = PolygonUtil.PolygonCenter(plot.vertices);
        var c = terrain.GetMeshIntersection(rect.Center.x, rect.Center.y);

        ProjectedMesh pMesh = TerrainProjector.ProjectPolygon(plot.vertices, terrain);

        GameObject obj = Instantiate(groundPrefab, c.point, Quaternion.identity, groupTransform);
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        Vector3[] localVertices = new Vector3[pMesh.vertices.Length];
        for (int i = 0; i < localVertices.Length; i++) {
            localVertices[i] = obj.transform.InverseTransformPoint(pMesh.vertices[i]);
        }

        mesh.vertices = localVertices;
        mesh.triangles = pMesh.indices;
        mesh.uv = pMesh.uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return obj;
    }

    public Rectangle ApproximateLargestRectangle(List<Vector2> polygon) {
        return Utils.PolygonUtil.ApproximateLargestRectangle(polygon, Random.Range(1.0f, 3.0f), 0.1f, 6, 10, 10);
    }
}
