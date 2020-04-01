using UnityEngine;
using System.Collections.Generic;

public class SkyscraperGenerator : MonoBehaviour {

    [System.Serializable]
    private struct MaterialConfig {
        public Material material;
        public Vector2 patternSize;
        public Vector2 stepSize;

        public MaterialConfig(Material material, Vector2 patternSize, Vector2 stepSize) {
            this.material = material;
            this.patternSize = patternSize;
            this.stepSize = stepSize;
        }
    }

    [SerializeField] private GameObject roof = null;
    [SerializeField] private GameObject northWall = null;
    [SerializeField] private GameObject southWall = null;
    [SerializeField] private GameObject eastWall = null;
    [SerializeField] private GameObject westWall = null;

    [SerializeField] private MaterialConfig[] matConfigs = null;
    [SerializeField] private Vector3 worldSize = Vector3.zero;
    [SerializeField] private int sizeX = 0;
    [SerializeField] private int sizeY = 0;
    [SerializeField] private int sizeZ = 0;
    [SerializeField] private bool debug = false;

    private MaterialConfig wallMatConfig;


    public void Generate(ElevatedPlot plot) {

        List<Vector2> polygon = new List<Vector2>();
        Vector3 center = Vector3.zero;
        foreach (var v in plot.vertices) {
            center += v;
            polygon.Add(VectorUtil.Vector3To2(v));
        }
        center /= plot.vertices.Count;

        var rect = ApproximateLargestRectangle(polygon);

        transform.position = center;
        this.worldSize.x = 2 * rect.width;
        this.worldSize.z = 2 * rect.height;
        this.worldSize.y = 5f;
        Build();

        // NOTE: angle is negated because Unity rotates clockwise.
        transform.localRotation = Quaternion.Euler(0, -rect.angle * Mathf.Rad2Deg, 0);
    }

    private Utils.PolygonUtil.Rectangle ApproximateLargestRectangle(List<Vector2> polygon) {
        return Utils.PolygonUtil.ApproximateLargestRectangle(polygon, Random.Range(1.0f, 3.0f), 0.1f, 6, 10);
    }

    private void Build() {
        // Roof
        roof.transform.position = transform.position + new Vector3(0, worldSize.y, 0);
        roof.transform.localScale = worldSize / 10.0f; // Planes are 10x10 units

        // North and south walls.
        BuildWall(northWall, sizeX, worldSize.x);
        BuildWall(southWall, sizeX, worldSize.x);

        northWall.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        northWall.transform.position = transform.position + new Vector3(0, 0, worldSize.z / 2.0f);
        southWall.transform.position = transform.position - new Vector3(0, 0, worldSize.z / 2.0f);

        // East and west walls.
        BuildWall(eastWall, sizeZ, worldSize.z);
        BuildWall(westWall, sizeZ, worldSize.z);

        eastWall.transform.localRotation = Quaternion.Euler(0, -90f, 0);
        westWall.transform.localRotation = Quaternion.Euler(0, 90f, 0);

        eastWall.transform.position = transform.position + new Vector3(worldSize.x / 2.0f, 0, 0);
        westWall.transform.position = transform.position - new Vector3(worldSize.x / 2.0f, 0, 0);
    }

    private void BuildWall(GameObject wall, int width, float worldWidth) {
        var renderer = wall.GetComponent<MeshRenderer>();
        renderer.material = wallMatConfig.material;

        var mesh = wall.GetComponent<MeshFilter>().mesh;
        BuildWall(mesh, width, worldWidth);
    }

    private void BuildWall(Mesh mesh, int width, float worldWidth) {

        // Builds vertices.
        var vertices = new Vector3[4 * width * sizeY];
        int vert = 0;
        // Iterate through all quads.
        for (int f = 0; f < sizeY; f++) {
            for (int c = 0; c < width; c++) {

                // Iterate through quad's 4 unique vertices.
                for (int a = 0; a < 2; a++) {
                    for (int b = 0; b < 2; b++) {
                        float x = ((float)(c + b) / width) * worldWidth;
                        float y = ((float)(f + a) / sizeY) * worldSize.y;
                        vertices[vert++] = new Vector3(x - worldWidth / 2.0f, y, 0);
                    }
                }
            }
        }

        // Builds triangles.
        var triangles = new int[6 * width * sizeY];
        int tris = 0;
        vert = 0;
        for (int f = 0; f < sizeY; f++) {
            for (int c = 0; c < width; c++) {
                // Right-angle in bottom-left corner.
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + 2;
                triangles[tris + 2] = vert + 1;

                // Right-angle in top-right corner.
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + 2;
                triangles[tris + 5] = vert + 3;

                tris += 6;
                vert += 4;
            }
        }

        // Builds UV coords.
        var uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i += 4) {
            float w = wallMatConfig.patternSize.x;
            float h = wallMatConfig.patternSize.y;

            int xMax = Mathf.CeilToInt(1.0f / wallMatConfig.patternSize.x);
            int yMax = Mathf.CeilToInt(1.0f / wallMatConfig.patternSize.y);

            float x0 = wallMatConfig.stepSize.x * (int)Random.Range(0, xMax);
            float y0 = wallMatConfig.stepSize.y * (int)Random.Range(0, yMax);

            uvs[i + 0] = new Vector2(x0 + 0, y0 + 0);
            uvs[i + 1] = new Vector2(x0 + w, y0 + 0);
            uvs[i + 2] = new Vector2(x0 + 0, y0 + h);
            uvs[i + 3] = new Vector2(x0 + w, y0 + h);
        }

        // Update mesh.
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    private void Awake() {
        this.wallMatConfig = matConfigs[(int)Random.Range(0, matConfigs.Length)];
    }

    private void Update() {
        if (debug)
            Build();
    }

}
