using UnityEngine;

/*
  What? The generator that creates and designs the landscape on which the city is built upon.
  Why? For realistic reasons, mountains and oceans are also part of nature, not only flat ground.
       The city roads needs to adjust to the landscape not the other way around
  How? Perlin Noise is the main tool to generate the landscape.
*/
public class TerrainGenerator : MonoBehaviour {
    [SerializeField] private Terrain terrain = null;
    private int depth = 50;
    private int width = 256;
    private int height = 256;
    private float scale = 20f;
    private float offsetX = 0f;
    private float offsetY = 0f;


    public void GenerateTerrain() {
        terrain.gameObject.SetActive(true);
        offsetX = Random.Range(0f, 10000f);
        offsetY = Random.Range(0f, 10000f);
        terrain.terrainData = BuildTerrain(terrain.terrainData);
    }

    private TerrainData BuildTerrain(TerrainData terrainData) {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    // For each point in the terrain plane, calculate the height for that point.
    private float[,] GenerateHeights() {
        float[,] heights = new float[width, height];
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                heights[x, y] = /*terrHeightCurve.Evaluate*/(CalculateHeight(x, y)); //PERLIN NOISE
            }
        }
        return heights;
    }

    private float CalculateHeight(int x, int y) {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;

        float noiseBase = PerlinFunc(xCoord, yCoord, 0.9f, 0.1f);
        float noiseMountain = PerlinFunc(xCoord, yCoord, 0.3f, 0.5f);

        if(noiseBase > 0.6f) { // High ground Perlin Noise
            float t = Mathf.InverseLerp(0.6f, 0.9f, noiseBase);
            //float t = (Mathf.Clamp(noise,0.6f,0.8f)-0.6f)/(0.8f-0.6f);
            return noiseBase + (t) * noiseMountain;
        }
        return noiseBase;
    }

    // Helper function for generating perlin noise. Takes in x & y coords, constant con to multiply the noise and amp to amplify the coord values.
    private float PerlinFunc(float x, float y, float con, float amp) {
        return con * Mathf.PerlinNoise(amp * x, amp * y);
    }

    private void Awake() {
        terrain.gameObject.SetActive(false);
    }
}
