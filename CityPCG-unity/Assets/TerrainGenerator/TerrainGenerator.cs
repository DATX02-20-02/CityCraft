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
    [SerializeField] private MeshFilter terrainMeshFilter = null;
    [SerializeField] private Transform sea = null;

    // Terrain dimensions.
    [SerializeField] private int width = 400;
    [SerializeField] private int depth = 400;

    // Terrain properties.
    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float zOffset = 0f;
    [SerializeField] private float seaLevel = 0f;

    // Noise params.
    [SerializeField] private float maxHeight = 80f;
    [SerializeField] private float frequency = 0.05f;

    // Amount of quads used in the terrain.
    [SerializeField] private int xResolution = 100;
    [SerializeField] private int zResolution = 100;

    [SerializeField] private bool debug = false;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    private Vector2[] uvs;


    public void GenerateTerrain() {
        GenerateTerrain(Random.Range(0f, 10000f), Random.Range(0f, 10000f));
    }

    private void GenerateTerrain(float xOffset, float zOffset) {
        this.xOffset = xOffset;
        this.zOffset = zOffset;

        GenerateVertices();
        GenerateTriangles();
        ColorTerrain();
        TextureTerrain();
        UpdateTerrainMesh();
    }

    private void GenerateVertices() {
        this.vertices = new Vector3[(xResolution+1) * (zResolution+1)];

        int i = 0;
        for (int z = 0; z <= zResolution; z++) {
            for (int x = 0; x <= xResolution; x++) {
                float xPos = ((float)x / xResolution) * width;
                float zPos = ((float)z / zResolution) * depth;
                float yPos = PerlinFunc(xPos + xOffset, zPos + zOffset, maxHeight, frequency);

                this.vertices[i++] = new Vector3(xPos, yPos, zPos);
            }
        }
    }

    private void GenerateTriangles() {
        triangles = new int[xResolution * zResolution * 6];

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < zResolution; z++) {
            for (int x = 0; x < xResolution; x++) {
                this.triangles[tris + 0] = vert + 0;
                this.triangles[tris + 1] = vert + xResolution + 1;
                this.triangles[tris + 2] = vert + 1;
                this.triangles[tris + 3] = vert + 1;
                this.triangles[tris + 4] = vert + xResolution + 1;
                this.triangles[tris + 5] = vert + xResolution + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    // NOTE: Only based on height for now.
    private void ColorTerrain() {
        this.colors = new Color[this.vertices.Length];

        int i = 0;
        for(int z = 0; z <= zResolution; z++) {
            for(int x = 0; x <= xResolution; x++) {
                float height = this.vertices[i].y;
                this.colors[i++] = heightGradient.Evaluate(height / maxHeight);
            }
        }
    }

    private void TextureTerrain() {
        // uvs = new Vector2[vertices.Length];

        // uvs[VertexIndex(0, 0)] = new Vector2(0, 0);
        // uvs[VertexIndex(1, 0)] = new Vector2(1, 0);
        // uvs[VertexIndex(0, 1)] = new Vector2(0, 1);
        // uvs[VertexIndex(1, 1)] = new Vector2(1, 1);
    }

    private void UpdateTerrainMesh() {
        this.mesh.Clear();

        this.mesh.vertices = this.vertices;
        this.mesh.triangles = this.triangles;
        this.mesh.colors = this.colors;
        //mesh.uv = this.uvs;

        this.mesh.RecalculateNormals();
    }

    private int VertexIndex(int x, int z) {
        return z * (zResolution+1) + x;
    }

    // Helper function for generating perlin noise. Takes in x & y coords, constant con to multiply the noise and amp to amplify the coord values.
    private float PerlinFunc(float x, float y, float con, float amp) {
        return con * Mathf.PerlinNoise(amp * x, amp * y);
    }

    private void Awake() {
        this.mesh = new Mesh();
        this.terrainMeshFilter.mesh = mesh;

        if(this.debug)
            GenerateTerrain();
    }

    private void Update() {
        sea.position = new Vector3(width / 2.0f, this.seaLevel, depth / 2.0f);
        sea.localScale = new Vector3(width, 1, depth);

        if (this.debug)
            GenerateTerrain(this.xOffset, this.zOffset);
    }
}
