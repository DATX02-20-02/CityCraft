using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Formats.Fbx.Exporter;
#endif

/*
  What? Handles all user interaction.
  Why? The user needs control over how the city models are generated.
  How? Listens to events from Unity UI and enables/disables such UI elements.
*/
public class App : MonoBehaviour {

    [SerializeField] private GameObject worldMesh = null;
    [SerializeField] private WorldGenerator worldGenerator = null;
    [SerializeField] private GameObject[] menuPanels = null;
    [SerializeField] private bool debug = false;

    private int currentMenuPanel = 0;


    public void Next() {
        NextMenu();
    }

    public void Undo() {
        worldGenerator.Undo();
        PrevMenu();
    }

    public void GenerateTerrain() {
        Log("Generating terrain...");
        worldGenerator.GenerateTerrain();
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

    public void ExportModelToFBX() {
#if UNITY_EDITOR
        string filePath = Path.Combine(Application.persistentDataPath, "city.fbx");
        ModelExporter.ExportObject(filePath, worldMesh);
#endif
    }

    private void NextMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Min(menuPanels.Length, currentMenuPanel + 1);
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
