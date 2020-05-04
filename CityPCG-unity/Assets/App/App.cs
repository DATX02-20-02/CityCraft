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

    [Header("General")]
    [SerializeField] private WorldGenerator worldGenerator = null;
    [SerializeField] private GameObject[] menuPanels = null;
    [SerializeField] private Slider sliderStep = null;
    [SerializeField] private Button btnBack = null;
    [SerializeField] private Button btnUndo = null;
    [SerializeField] private Button btnNext = null;
    [SerializeField] private bool debug = false;

    [SerializeField] private RoadUIHandler roadUIHandler = null;

    [Header("Terrain Settings")]
    [SerializeField] private Slider sliderSeaLevel = null;
    [SerializeField] private Slider sliderX = null;
    [SerializeField] private Slider sliderZ = null;
    [SerializeField] private Slider sliderWidth = null;
    [SerializeField] private Slider sliderDepth = null;

    private Vector2 terrainOffset;

    private int currentMenuPanel = 0;
    private int reachedMenuPanel = 0;

    public void Next() {
        bool reachedFurther = currentMenuPanel + 1 < reachedMenuPanel;
        btnUndo.interactable = reachedFurther;
        btnNext.interactable = reachedFurther && currentMenuPanel + 1 < menuPanels.Length - 1;
        btnBack.interactable = true;

        WorldGenerator.State prevState = worldGenerator.CurrentState;
        WorldGenerator.State nextState = worldGenerator.NextState();
        NextMenu();

        if (nextState != prevState) OnStateChanged(nextState, prevState);
    }

    public void Undo() {
        btnUndo.interactable = false;
        btnNext.interactable = false;

        worldGenerator.Undo();
        reachedMenuPanel = currentMenuPanel;
    }

    public void Prev() {
        WorldGenerator.State prevState = worldGenerator.CurrentState;
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
        worldGenerator.GenerateTerrain(terrainOffset, (int)sliderWidth.value, (int)sliderDepth.value, true);
        reachedMenuPanel = 1;
        SetBusy(false);
        Log("Terrain generated.");
    }

    public void GenerateRoads() {
        Log("Generating roads...");
        SetBusy(true);
        worldGenerator.GenerateRoads(this.roadUIHandler.CityInputs, (RoadNetwork network) => {
            reachedMenuPanel = 2;
            SetBusy(false);
        });
        Log("Roads generated.");
    }

    public void GenerateStreets() {
        Log("Generating streets...");
        SetBusy(true);
        worldGenerator.GenerateStreets((RoadNetwork network) => {
            reachedMenuPanel = 3;
            SetBusy(false);
        });
        Log("Streets generated.");
    }

    public void GenerateCities() {
        Log("Generating buildings...");
        SetBusy(true);
        worldGenerator.GeneratePlotContent(() => {
            reachedMenuPanel = 4;
            SetBusy(false);
        });
        Log("Buildings generated.");
    }

    public void ExportModelToGLTF() {
        try {
            // Choose folder dialog
            var path = StandaloneFileBrowser.OpenFolderPanel("Choose Export Destination Folder", "", false)[0];

            // Export
            var exporter = new GLTFSceneExporter(new[] { worldGenerator.transform }, (t) => t.name);
            exporter.SaveGLTFandBin(path, "World");
            Log("Model exported to: " + path);
        }
        catch {
            Debug.LogError("Export failed for some reason.");
            return;
        }
    }

    public void ExportModelToGLB() {
        try {
            // Choose folder dialog
            var path = StandaloneFileBrowser.OpenFolderPanel("Choose Export Destination Folder", "", false)[0];

            // Export
            var exporter = new GLTFSceneExporter(new[] { worldGenerator.transform }, (t) => t.name);
            exporter.SaveGLB(path, "World");
            Log("Model exported to: " + path);
        }
        catch {
            Debug.LogError("Export failed for some reason.");
            return;
        }
    }

    public void EndDragOffset() {
        this.terrainOffset = new Vector2(sliderX.value, sliderZ.value);
        if (reachedMenuPanel > 0)
            worldGenerator.GenerateTerrain(terrainOffset, (int)sliderWidth.value, (int)sliderDepth.value);
    }

    public void ModifyTerrainSea(float a) {
        worldGenerator.ModifyTerrainSea(a);
    }

    public void ModifyTerrainSize() {
        if (reachedMenuPanel > 0)
            worldGenerator.GenerateTerrain(terrainOffset, (int)sliderWidth.value, (int)sliderDepth.value);
    }

    private void SetBusy(bool isBusy) {
        var selectables = GetComponentsInChildren<Selectable>();
        foreach (var s in selectables)
            s.interactable = !isBusy;

        // There is no "back" at the first panel.
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

        ModifyTerrainSea(sliderSeaLevel.value);

        roadUIHandler.enabled = false;
    }
}
