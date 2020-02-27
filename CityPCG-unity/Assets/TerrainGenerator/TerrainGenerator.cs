﻿using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int depth = 50;
    public int width = 256;
    public int height = 256;
    public float scale = 5f;
    public float offsetX; //= 0f;
    public float offsetY; //= 0f;
    public float frequency;

    public AnimationCurve terrHeightCurve;

    void Start ()
    {
        depth = 70;
        offsetY = 0;
        offsetX = 0; 
        scale = 20f;
        frequency = 2;
    }
    
    void Update() 
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
        
        offsetX += Time.deltaTime*5;
    }
    
    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        
        terrainData.size = new Vector3(width, depth, height);
        
        terrainData.SetHeights(0,0,GenerateHeights());
        return terrainData;
    }
    
    float[,] GenerateHeights ()
    {
        float[,] heights = new float [width, height];
        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = terrHeightCurve.Evaluate(CalculateHeight(x, y)); //PERLIN NOISE

            }
        }
        return heights;
        
    }
    
    float CalculateHeight(int x, int y)
    {


        float xCoord = (float)x/width*scale+offsetX;
        float yCoord = (float)y/height*scale+offsetY;

        float noise = perlinfunc (xCoord,yCoord,0.9f,0.1f);
        float noise2 = perlinfunc (xCoord,yCoord,0.2f,0.5f);


        if (noise > 0.6f) { //for high ground
            float t = Mathf.InverseLerp(0.6f, 0.9f, noise);
            //float t = (Mathf.Clamp(noise,0.6f,0.8f)-0.6f)/(0.8f-0.6f);
            return noise+(t) * noise2;
        }


        return noise;
    }

    float perlinfunc(float x, float y, float con, float amp)
    {
        return con*Mathf.PerlinNoise(amp*x,amp*y);
        
    }
    

    float power(float x, float y)
    {
    float ans = 1;

    for(int i=0; i<y; i++)
      ans *= x;

    return ans;
    }

    float max (float x, float y)
    {
        if (x > y) return x;
        return y;
    }
}
