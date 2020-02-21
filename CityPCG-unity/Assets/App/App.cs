using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.Formats.Fbx.Exporter;

// What? Handles all user interaction.
// Why? The user needs control over how the city models are generated.
// How? Listens to events from Unity UI and enables/disables such UI elements.
public class App : MonoBehaviour {

    [SerializeField] private GameObject worldMesh = null;
    [SerializeField] private CityGenerator cityGenerator = null;
    [SerializeField] private GameObject[] menuPanels = null;

    private int currentMenuPanel = 0;


    public void Undo() {
        cityGenerator.Undo();
        PrevMenu();
    }

    public void GenerateTerrain() {
        Debug.Log("Generating terrain...");
        cityGenerator.GenerateTerrain();
        Debug.Log("Terrain generated.");
        NextMenu();
    }

    public void GenerateRoads() {
        Debug.Log("Generating roads...");
        cityGenerator.GenerateRoads();
        Debug.Log("Roads generated.");
        NextMenu();
    }

    public void GenerateStreets() {
        Debug.Log("Generating streets...");
        cityGenerator.GenerateStreets();
        Debug.Log("Streets generated.");
        NextMenu();
    }

    public void GenerateBuildings() {
        Debug.Log("Generating buildings...");
        cityGenerator.GenerateBuildings();
        Debug.Log("Buildings generated.");
    }

    public void ExportModelToFBX() {
        string filePath = Path.Combine(Application.persistentDataPath, "city.fbx");
        ModelExporter.ExportObject(filePath, worldMesh);
    }

    private void NextMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Min(menuPanels.Length, currentMenuPanel+1);
        menuPanels[currentMenuPanel].SetActive(true);
    }

    private void PrevMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Max(0, currentMenuPanel-1);
        menuPanels[currentMenuPanel].SetActive(true);
    }
}
