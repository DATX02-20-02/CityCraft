using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class ParkGenerator : MonoBehaviour {
    [Range(0, 1)]
    [SerializeField] private float objectFrequency = 1;
    [SerializeField] private GameObject[] trees = null;
    [SerializeField] private GameObject rock = null;
    [SerializeField] private GameObject[] bushes = null;
    [SerializeField] private GameObject objectParent = null;
    [SerializeField] private GameObject pathGeneratorPrefab = null;
    [SerializeField] private GameObject pathParent = null;
    private TerrainModel terrain;


    public void Reset() {
        foreach (Transform child in objectParent.transform)
            Destroy(child.gameObject);
        foreach (Transform p in pathParent.transform)
            Destroy(p.gameObject);
    }

    // Coordinates calls the Triangulator function in order to divide polygons into triangles
    public void Generate(TerrainModel terrain, Plot plot) {
        this.terrain = terrain;
        GameObject pathGeneratorObj = Instantiate(pathGeneratorPrefab, pathParent.transform);
        PathGenerator pathGenerator = pathGeneratorObj.GetComponent<PathGenerator>();
        pathGenerator.GeneratePlotPath(terrain, plot, () => {

            Vector3[] area = plot.vertices.ToArray();
            int objectsToPlace = Mathf.RoundToInt(PolygonUtil.PolygonArea(plot.vertices) * objectFrequency);
            Triangulator triangulator = new Triangulator(area);
            int[] triangulated = triangulator.Triangulate();
            Triangle[] triangles = FromTriangulator(area, triangulated);

            float totArea = Mathf.Max(PolygonUtil.PolygonArea(plot.vertices), 0.1f);

            foreach (Triangle triangle in triangles) {
                int amount = (int) (objectsToPlace * triangle.Area() / totArea); // NOTE: avoid division by zero

                for (int i = 0; i < amount; i++) {
                    Vector3 point = triangle.RandomPoint();
                    Vector3 pos = terrain.GetMeshIntersection(point.x, point.z).point;

                    float seed = Random.Range(0, 10000.0f);
                    PlaceObject(pos, seed);
                }
            }
        });
    }
    // NoiseEvaluate creates a pseudorandom value using Perlin Noise and determines what object to spawn based on it
    void PlaceObject(Vector3 pos, float seed) {
        float random = Random.Range(0f, 10f);
        float scale = 1;
        if (random >= 5f) {
            scale = Random.Range(0.03f, 0.04f);
            GameObject tree = trees[(int)Random.Range(0, trees.Length) % trees.Length];
            tree.layer = LayerMask.NameToLayer("Tree");
            InitMesh(tree, pos, scale);
        }
        else {
            if (Random.Range(0, 1.0f) < 0.2f) {
                scale = Random.Range(0.002f, 0.004f);
                rock.layer = LayerMask.NameToLayer("Misc");
                InitMesh(rock, pos, scale, UnityEngine.Random.rotation);
            }
            else {
                scale = Random.Range(0.04f, 0.06f);
                GameObject bush = bushes[(int)Random.Range(0, bushes.Length) % bushes.Length];
                bush.layer = LayerMask.NameToLayer("Misc");
                InitMesh(bush, pos, scale);
            }
        }
    }

    // InitMesh is called for spawning Game objects, assigning them a scale, position, and rotation.
    void InitMesh(GameObject g, Vector3 pos, float scale, Quaternion rotation) {
        GameObject obj = Instantiate(g, objectParent.transform);
        obj.AddComponent<MeshCollider>();
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        obj.transform.position = terrain.GetMeshIntersection(pos.x, pos.z).point;
        obj.transform.localScale = new Vector3(scale, scale, scale);
        obj.transform.rotation = rotation;
        float treeRadius = 0.06f;
        float miscRadius = 0.002f;
        float pathRadius = 0.1f;
        if (obj.layer == 9) {
            Collider[] miscCollisions = Physics.OverlapSphere(obj.transform.position, miscRadius, 1 << obj.layer);
            if (miscCollisions.Length > 1) {
                Destroy(obj);
            }
        }
        if (obj.layer == 8) {
            Collider[] treeCollisions = Physics.OverlapSphere(obj.transform.position, treeRadius, 1 << obj.layer);
            if (treeCollisions.Length > 1) {
                Destroy(obj);
            }
        }
        Collider[] pathCollisions = Physics.OverlapSphere(obj.transform.position, pathRadius, 1 << 10);
        if (pathCollisions.Length > 0)
            Destroy(obj);

    }

    void InitMesh(GameObject g, Vector3 pos, float scale) {
        InitMesh(g, pos, scale, Quaternion.identity);
    }

    public static Triangle[] FromTriangulator(Vector3[] area, int[] indices) {
        Triangle[] result = new Triangle[indices.Length / 3];

        for (int i = 0; i < indices.Length; i += 3) {
            result[i / 3] = new Triangle(
                area[indices[i + 0]],
                area[indices[i + 1]],
                area[indices[i + 2]]
            );
        }

        return result;
    }
}
