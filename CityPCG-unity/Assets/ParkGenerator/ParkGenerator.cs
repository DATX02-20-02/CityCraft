using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParkGenerator : MonoBehaviour {
    [Range(5, 100)]
    [SerializeField] private int count = 20;
    [SerializeField] private GameObject[] trees = null;
    [SerializeField] private GameObject rock = null;
    [SerializeField] private GameObject[] bushes = null;
    [SerializeField] private int layerMask;
    [SerializeField] private int layerMasked;
    [SerializeField] private GameObject step = null;
    private TerrainModel terrain;


    // Coordinates calls the Triangulator function in order to divide polygons into triangles
    public void Generate(TerrainModel terrain, Block block, Plot plot) {
        //	AddPath(terrain,plot);
        this.terrain = terrain;
        Vector3[] area = plot.vertices.ToArray();
        Triangulator triangulator = new Triangulator(area);
        int[] triangulated = triangulator.Triangulate();
        Triangle[] triangles = FromTriangulator(area, triangulated);
        int accumulator = count;
        foreach (Triangle triangle in triangles) {
            int amount = (int)Mathf.Ceil((float)count / (float)triangles.Length);
            for (int i = 0; i < Mathf.Min(accumulator, amount); i++) {
                Vector3 point = triangle.RandomPoint();
                Vector3 pos = terrain.GetPosition(point.x, point.z);
                float seed = Random.Range(0, 10000.0f);
                NoiseEvaluate(pos, seed);
            }
            accumulator -= amount;
        }
    }
    // NoiseEvaluate creates a pseudorandom value using Perlin Noise and determines what object to spawn based on it
    void NoiseEvaluate(Vector3 pos, float seed) {
        float random = Random.Range(0f, 10f);
        float scale = 1;
        if (random >= 5f) {
            scale = Random.Range(0.02f, 0.04f);
            GameObject tree = trees[(int)Random.Range(0, trees.Length) % trees.Length];
            tree.layer = 2;
            layerMask = 1 << 2;
            InitMesh(tree, pos, scale);
        }
        else {
            if (Random.Range(0, 1.0f) < 0.2f) {
                scale = Random.Range(0.0004f, 0.0008f);
                rock.layer = 1;
                layerMasked = 1 << 1;
                InitMesh(rock, pos, scale, UnityEngine.Random.rotation);
            }
            else {
                scale = Random.Range(0.01f, 0.02f);
                GameObject bush = bushes[(int)Random.Range(0, bushes.Length) % bushes.Length];
                bush.layer = 1;
                layerMasked = 1 << 1;
                InitMesh(bush, pos, scale);
            }
        }
    }

    // InitMesh is called for spawning Game objects, assigning them a scale, position, and rotation.
    void InitMesh(GameObject g, Vector3 pos, float scale, Quaternion rotation) {
        GameObject obj = Instantiate(g, transform);
        obj.AddComponent<MeshCollider>();
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        obj.transform.position = terrain.GetMeshIntersection(pos.x, pos.z).point;
        obj.transform.localScale = new Vector3(scale, scale, scale);
        obj.transform.rotation = rotation;
        Rigidbody body = obj.AddComponent<Rigidbody>();
        body.isKinematic = true;
        float treeRadius = 0.06f;
        float miscRadius = 0.001f;
        float pathRadius = 0.02f;
        if (obj.layer == 1) {
            Collider[] miscCollisions = Physics.OverlapSphere(obj.transform.position, miscRadius, layerMasked);
            if (miscCollisions.Length > 1) {
                Destroy(obj);
            }
        }
        if (obj.layer == 2) {
            Collider[] treeCollisions = Physics.OverlapSphere(obj.transform.position, treeRadius, layerMask);
            if (treeCollisions.Length > 1) {
                Destroy(obj);
            }
        }
        Collider[] pathCollisions = Physics.OverlapSphere(obj.transform.position, pathRadius, 1 << 4);
        if (pathCollisions.Length > 0)
            Destroy(obj);


    }

    void InitMesh(GameObject g, Vector3 pos, float scale) {
        InitMesh(g, pos, scale, Quaternion.identity);
    }

    public static Vector2[] ToVector2Array(Vector3[] v3) {
        return System.Array.ConvertAll<Vector3, Vector2>(v3, GetV2FromV3);
    }
    public static Vector2 GetV2FromV3(Vector3 v3) {
        return new Vector2(v3.x, v3.z);
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

    public void GeneratePaths(TerrainModel terrain, Block block, Plot plot) {
        GameObject path = new GameObject("Path Generator");
        path.transform.parent = this.transform;
        var pg = path.AddComponent<PathGenerator>();
        pg.step = step;
        pg.GeneratePlotPath(terrain, block, plot);
    }
}
