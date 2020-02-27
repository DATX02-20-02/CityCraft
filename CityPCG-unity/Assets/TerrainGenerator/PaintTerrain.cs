using UnityEngine;

// What: The generator that creates and designs the landscape on which the city is built upon. 
/* Why : For realistic reasons, mountains and oceans are also part of nature, not only flat ground.
           The city roads needs to adjust to the landscape not the other way around*/
// How : Perlin Noise is the main tool to generate the landscape. 


public class PaintTerrain : MonoBehaviour{

    //Apply textures and at certain heights, apply texture splatting such that the textures transition nicely
    public void paint(Terrain terrain){
        //Terrain terrain = GetComponent <Terrain>();
        TerrainData terrainData = terrain.terrainData;

        float[, ,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++){
            for (int x = 0; x < terrainData.alphamapWidth; x++){
                float y_01 = (float)y/(float)terrainData.alphamapHeight;
                float x_01 = (float)x/(float)terrainData.alphamapWidth;
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapResolution),Mathf.RoundToInt(x_01 * terrainData.heightmapResolution) );

                //Vector3 normal = terrainData.GetInterpolatedNormal(y_01,x_01);
                //float steepness = terrainData.GetSteepness(y_01, x_01);

                float[] splatWeights = new float[terrainData.alphamapLayers];

                float h = 2.0f * height / terrainData.size.y;
                if (h <= 1.0f){
                    splatWeights[1] = h;   
                    splatWeights[0] = 1f-h;
                } else{ 
                    h -= 1.0f;
                    splatWeights[2] = h;
                    splatWeights[1] = 1-h;
                }



                float z = sum(splatWeights);

                for(int i = 0; i<terrainData.alphamapLayers; i++){
                    splatWeights[i] /= z;
                    splatmapData[x,y,i] = splatWeights[i];
                }
            }
        }

        terrainData.SetAlphamaps(0,0,splatmapData);
    }

    float sum(float[] list){
        float a = 0;
        for(int i = 0; i < list.Length; i++){
            a += list[i];
        }
        return a;
    }
}