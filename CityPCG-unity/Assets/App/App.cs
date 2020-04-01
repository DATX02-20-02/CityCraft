using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityGLTF;
using SFB;
using UnityEngine.UI;

/*
  What? Handles all user interaction.
  Why? The user needs control over how the city models are generated.
  How? Listens to events from Unity UI and enables/disables such UI elements.
*/
public class App : MonoBehaviour {

    public Slider sliderSea;
    public Slider sliderX;
    public Slider sliderZ;
    [SerializeField] private WorldGenerator worldGenerator = null;
    [SerializeField] private GameObject[] menuPanels = null;
    [SerializeField] private bool debug = false;

    private int currentMenuPanel = 0;
    private bool terrainGenerated = false;


    public void Next() {
        worldGenerator.NextState();
        NextMenu();
    }

    public void Undo() {
        worldGenerator.Undo();
        worldGenerator.PreviousState();
        PrevMenu();
    }

    public void GenerateTerrain() {
        Log("Generating terrain...");
        worldGenerator.GenerateTerrain();
        terrainGenerated = true;
        Log("Terrain generated.");
    }

    public void GenerateRoads() {
        Log("Generating roads...");
        worldGenerator.GenerateRoads();
        Log("Roads generated.");
    }

    public void GenerateStreets() {
        Log("Generating streets...");
        worldGenerator.GenerateStreets();
        Log("Streets generated.");
    }

    public void GenerateBuildings() {
        Log("Generating buildings...");
        worldGenerator.GenerateBuildings();
        Log("Buildings generated.");
    }

    public void ExportModelToGLTF() {
        // Choose folder dialog
        var path = StandaloneFileBrowser.OpenFolderPanel("Choose Export Destination Folder", "", false)[0];

        // Export
        var exporter = new GLTFSceneExporter(new[] { worldGenerator.transform }, (t) => t.name);
        exporter.SaveGLTFandBin(path, "World");
        Log("Model exported to: " + path);
    }

    public void ExportModelToGLB() {
        // Choose folder dialog
        var path = StandaloneFileBrowser.OpenFolderPanel("Choose Export Destination Folder", "", false)[0];

        // Export
        var exporter = new GLTFSceneExporter(new[] { worldGenerator.transform }, (t) => t.name);
        exporter.SaveGLB(path, "World");
        Log("Model exported to: " + path);
    }
    
    public void EndDragOffset() {
        sliderX.value = 0;
        sliderZ.value = 0;
    }
    
    public void ModifyTerrainOffsetX(float v) {
        if(terrainGenerated){
            worldGenerator.GenerateTerrain(v,0);
        }
    }
    public void ModifyTerrainOffsetZ(float v) {
        if(terrainGenerated){
        worldGenerator.GenerateTerrain(0, v);
        }
    }
    public void ModifyTerrainSea(float a) {
        if(terrainGenerated){
        worldGenerator.ModifyTerrainSea(a); 
        }
    }
    
    

    private void NextMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Min(menuPanels.Length-1, currentMenuPanel + 1);
        menuPanels[currentMenuPanel].SetActive(true);
    }

    private void PrevMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Max(0, currentMenuPanel - 1);
        menuPanels[currentMenuPanel].SetActive(true);
    }

    private void Log(object msg) {
        if (debug)
            Debug.Log(msg);
    }

}
