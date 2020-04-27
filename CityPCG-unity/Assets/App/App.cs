using System;
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

    [SerializeField] private Slider sliderX = null;
    [SerializeField] private Slider sliderZ = null;
    [SerializeField] private Slider sliderStep = null;
    [SerializeField] private WorldGenerator worldGenerator = null;
    [SerializeField] private GameObject[] menuPanels = null;
    [SerializeField] private Button btnBack = null;
    [SerializeField] private Button btnUndo = null;
    [SerializeField] private Button btnNext = null;
    [SerializeField] private bool debug = false;

    [SerializeField] private RoadUIHandler roadUIHandler = null;

    private int currentMenuPanel = 0;

    public void Next() {
        btnNext.interactable = false;
        btnBack.interactable = true;

        WorldGenerator.State prevState = worldGenerator.CurrentState;
        WorldGenerator.State nextState = worldGenerator.NextState();

        NextMenu();

        if (nextState != prevState) OnStateChanged(nextState, prevState);
    }

    public void Undo() {
        btnUndo.interactable = false;
        if (currentMenuPanel == 0)
            btnNext.interactable = false;

        worldGenerator.Undo();
    }

    public void Prev() {
        WorldGenerator.State prevState = worldGenerator.CurrentState;

        worldGenerator.Undo();
        WorldGenerator.State nextState = worldGenerator.PreviousState();
        PrevMenu();

        if (currentMenuPanel == 0)
            btnBack.interactable = false;

        btnUndo.interactable = true;
        btnNext.interactable = true;

        if (nextState != prevState) OnStateChanged(nextState, prevState, true);
    }



    public void GenerateTerrain() {
        Log("Generating terrain...");
        SetBusy(true);
        worldGenerator.GenerateTerrain();
        SetBusy(false);
        Log("Terrain generated.");
    }

    public void GenerateRoads() {
        Log("Generating roads...");
        SetBusy(true);
        worldGenerator.GenerateRoads(this.roadUIHandler.CityInputs, (RoadNetwork network) => {
            SetBusy(false);
        });
        Log("Roads generated.");
    }

    public void GenerateStreets() {
        Log("Generating streets...");
        SetBusy(true);
        worldGenerator.GenerateStreets((RoadNetwork network) => {
            SetBusy(false);
        });
        Log("Streets generated.");
    }

    public void GenerateBuildings() {
        Log("Generating buildings...");
        SetBusy(true);
        worldGenerator.GenerateBuildings(() => {
            SetBusy(false);
        });
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
        worldGenerator.SetOffsetSpeedX(v);
    }

    public void ModifyTerrainOffsetZ(float v) {
        worldGenerator.SetOffsetSpeedZ(v);
    }

    public void ModifyTerrainSea(float a) {
        worldGenerator.ModifyTerrainSea(a);
    }

    private void SetBusy(bool isBusy) {
        var selectables = GetComponentsInChildren<Selectable>();
        foreach (var s in selectables)
            s.interactable = !isBusy;

        if (currentMenuPanel == 0)
            btnBack.interactable = false;

        // There is no "next" at the last panel.
        if (currentMenuPanel == menuPanels.Length - 1)
            btnNext.interactable = false;
    }

    private void OnStateChanged(WorldGenerator.State currentState, WorldGenerator.State prevState, bool previous = false) {
        this.roadUIHandler.enabled = currentState == WorldGenerator.State.Roads;
        this.roadUIHandler.SetTerrain(worldGenerator.Terrain);

        if (previous && prevState == WorldGenerator.State.Roads) {
            this.roadUIHandler.Reset();
        }
    }

    private void NextMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Min(menuPanels.Length - 1, currentMenuPanel + 1);
        menuPanels[currentMenuPanel].SetActive(true);

        sliderStep.value = currentMenuPanel;
    }

    private void PrevMenu() {
        menuPanels[currentMenuPanel].SetActive(false);
        currentMenuPanel = Mathf.Max(0, currentMenuPanel - 1);
        menuPanels[currentMenuPanel].SetActive(true);

        sliderStep.value = currentMenuPanel;
    }

    private void Log(object msg) {
        if (debug)
            Debug.Log(msg);
    }

    private void Start() {
        if (roadUIHandler == null)
            throw new Exception("No road UI handler is connected!");

        roadUIHandler.enabled = false;
    }
}
