using UnityEngine;

/*
  What: The generator that creates and designs the landscape on which the city is built upon.
  Why : For realistic reasons, mountains and oceans are also part of nature, not only flat ground.
        The city roads needs to adjust to the landscape not the other way around.
  How : Perlin Noise is the main tool to generate the landscape.
*/
public class TerrainGenerator : MonoBehaviour {

    // Color gradient for different heights.
    [SerializeField] private Gradient heightGradient = null;
    [SerializeField] private NoiseGenerator noiseGenerator = null;
    [SerializeField] private MeshFilter terrainMeshFilter = null;
    [SerializeField] private Transform sea = null;

    // Terrain dimensions.
    [SerializeField] private int width = 400;
    [SerializeField] private int depth = 400;

    // Terrain properties.
    [SerializeField] private float seaLevel = 0f;

    // Noise params.
    [SerializeField] private float maxHeight = 80f;

    // Amount of quads used in the terrain.
    [SerializeField] private int xResolution = 100;
    [SerializeField] private int zResolution = 100;

    [SerializeField] private bool debug = false;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    private Vector2[] uvs;

    public TerrainModel GenerateTerrain() {
        // Store current RNG state.
        var prevRandomState = Random.state;
        this.noiseGenerator.Offset = offset;
        var noise = this.noiseGenerator.Generate();

        var terrainModel = new TerrainModel(width, depth, seaLevel, maxHeight, noise);

        GenerateVertices(terrainModel);
        GenerateTriangles();
        ColorTerrain();
        TextureTerrain();
        UpdateTerrainMesh();

        // Restore RNG state.
        Random.state = prevRandomState;

        return terrainModel;
    }

    public Vector2 NoiseOffset {
        get { return this.noiseGenerator.Offset; }
    }

    private void GenerateVertices(TerrainModel terrainModel) {
        this.vertices = new Vector3[4 * (xResolution) * (zResolution)];

        int i = 0;
        for (int z = 0; z < zResolution; z++) {
            for (int x = 0; x < xResolution; x++) {

                // Unique vertices for each quad
                // Each quad's vertices form an "N" shape
                for (int a = 0; a < 2; a++) {
                    for (int b = 0; b < 2; b++) {
                        float xPos = (float)((x + a) / (float)xResolution) * width;
                        float zPos = (float)((z + b) / (float)zResolution) * depth;
                        float yPos = terrainModel.GetHeight(xPos, zPos);

                        this.vertices[i++] = new Vector3(xPos, yPos, zPos);
                    }
                }

            }
        }
    }

    private void GenerateTriangles() {
        triangles = new int[xResolution * zResolution * 6];

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < zResolution; z++) {
            for (int x = 0; x < xResolution; x++) {
                // Right-angle in bottom-left corner.
                this.triangles[tris + 0] = vert + 0;
                this.triangles[tris + 1] = vert + 1;
                this.triangles[tris + 2] = vert + 2;

                // Right-angle in top-right corner.
                this.triangles[tris + 3] = vert + 2;
                this.triangles[tris + 4] = vert + 1;
                this.triangles[tris + 5] = vert + 3;

                vert += 4;
                tris += 6;
            }
        }
    }

    // NOTE: Only based on height for now.
    private void ColorTerrain() {
        this.colors = new Color[this.vertices.Length];

        for (int i = 0; i < this.vertices.Length; i++) {
            float height = this.vertices[i].y;
            this.colors[i] = heightGradient.Evaluate(height / maxHeight);
        }
    }

    private void TextureTerrain() {
        this.uvs = new Vector2[vertices.Length];

        for (int i = 0; i < this.vertices.Length; i += 4) {
            float size = Random.Range(0.22f, 0.28f);
            float x0 = Random.Range(0, 1 - size);
            float z0 = Random.Range(0, 1 - size);

            this.uvs[i + 0] = new Vector2(x0 + 0.0f, z0 + 0.0f);
            this.uvs[i + 1] = new Vector2(x0 + 0.0f, z0 + size);
            this.uvs[i + 2] = new Vector2(x0 + size, z0 + 0.0f);
            this.uvs[i + 3] = new Vector2(x0 + size, z0 + size);
        }
    }

    private void UpdateTerrainMesh() {
        this.mesh.Clear();

        this.mesh.vertices = this.vertices;
        this.mesh.triangles = this.triangles;
        this.mesh.colors = this.colors;
        this.mesh.uv = this.uvs;

        this.mesh.RecalculateNormals();
    }

    // Helper function for generating perlin noise. Takes in x & y coords, constant con to multiply the noise and amp to amplify the coord values.
    private float PerlinFunc(float x, float y, float con, float amp) {
        return con * Mathf.PerlinNoise(amp * x, amp * y);
    }

    private void Awake() {
        this.mesh = new Mesh();
        this.mesh.MarkDynamic(); // Optimize mesh for frequent updates.
        this.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support lots of triangles.

        this.terrainMeshFilter.mesh = mesh;

        if (this.debug) {
            GenerateTerrain();
        }
    }

    private void Update() {
        sea.position = new Vector3(width / 2.0f, this.seaLevel, depth / 2.0f);
        sea.localScale = new Vector3(width, 1, depth);

        if (this.debug)
            GenerateTerrain();
    }
}
