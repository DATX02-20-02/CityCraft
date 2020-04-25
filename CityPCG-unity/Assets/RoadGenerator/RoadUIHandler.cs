using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

class RoadUIHandler : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private Dropdown cityTypeDropdown = null;

    [Header("Instantiate prefabs")]
    [SerializeField] private GameObject ghostObject = null;

    [Header("Instantiate parents")]
    [SerializeField] private GameObject ghostParent = null;

    private float radius = 75;
    private CityType cityType = CityType.Paris;
    private TerrainModel terrain;

    private List<CityInput> cityInputs = new List<CityInput>();

    private GameObject ghostObjectInstance = null;
    private CityInput selected;
    private CityInput dragging;
    private Vector3 dragOffset;

    public List<CityInput> CityInputs {
        get { return cityInputs; }
    }

    public void SetTerrain(TerrainModel terrain) {
        this.terrain = terrain;
    }

    public void AddCityInput(CityInput input) {
        if (input.ghostObject == null)
            input.ghostObject = Instantiate(ghostObject, ghostParent.transform);

        input.ghostObject.transform.position = input.position;
        input.ghostObject.transform.localScale = Vector3.one * (input.radius * 2 + 10);

        cityInputs.Add(input);
    }

    public void OnEnable() {
        foreach (CityInput cityInput in cityInputs) {
            cityInput.ghostObject = Instantiate(ghostObject, ghostParent.transform);

            cityInput.ghostObject.transform.position = cityInput.position;
            cityInput.ghostObject.transform.localScale = Vector3.one * (cityInput.radius * 2 + 10);
        }
    }

    public void OnDisable() {
        if (selected != null) {
            selected.SetSelected(false);
            selected = null;
        }
        DestroyGhosts();
    }

    public void OnCityTypeChanged(Dropdown change) {
        this.cityType = (CityType)change.value;
        if (selected != null)
            selected.type = this.cityType;
    }

    public void Reset() {
        DestroyGhosts();

        this.cityInputs = new List<CityInput>();
    }

    private void DestroyGhosts() {
        if (ghostParent != null)
            foreach (Transform child in ghostParent.transform) {
                Destroy(child.gameObject);
            }
    }

    public void Update() {
        if (ghostObject != null && ghostObjectInstance == null) {
            ghostObjectInstance = Instantiate(ghostObject, ghostParent.transform);
        }

        if (ghostObjectInstance != null) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                Vector3 pos = hit.point;
                pos.y = Mathf.Max(terrain.seaLevel, pos.y);

                CityInput hover = null;
                foreach (CityInput input in cityInputs) {
                    input.SetHovering(false);
                    if (hover == null && Vector3.Distance(pos, input.position) < 20) {
                        input.SetHovering(true);
                        hover = input;
                    }
                }

                if (dragging != null) {
                    dragging.position = pos - dragOffset;
                    dragging.ghostObject.transform.position = dragging.position;

                    if (Input.GetMouseButtonUp(0)) {
                        dragging = null;
                    }
                }

                if (hover != null && hover != selected) {
                    ghostObjectInstance.SetActive(false);

                    if (Input.GetMouseButtonDown(0)) {
                        if (!EventSystem.current.IsPointerOverGameObject()) {
                            if (selected != null)
                                selected.SetSelected(false);

                            selected = hover;
                            selected.SetSelected(true);

                            dragging = hover;
                            dragOffset = pos - dragging.position;

                            if (cityTypeDropdown) {
                                cityTypeDropdown.value = (int)selected.type;
                            }
                        }
                    }
                }
                else {
                    if (selected == null) {
                        ghostObjectInstance.SetActive(true);

                        Vector3 scroll = Input.mouseScrollDelta;
                        radius = Mathf.Clamp(radius + scroll.y * 2, 0.1f, 500f);

                        ghostObjectInstance.transform.position = pos;
                        ghostObjectInstance.transform.localScale = Vector3.one * (radius * 2 + 10);

                        if (Input.GetMouseButtonDown(0)) {
                            if (!EventSystem.current.IsPointerOverGameObject()) {
                                cityInputs.Add(new CityInput(pos, cityType, ghostObjectInstance, radius));
                                ghostObjectInstance = null;
                            }
                        }
                    }
                    else {
                        if (Input.GetMouseButtonDown(0)) {
                            if (hover != selected) {
                                if (!EventSystem.current.IsPointerOverGameObject()) {
                                    selected.SetSelected(false);
                                    selected = null;
                                }
                            }
                            else {
                                dragging = hover;
                                dragOffset = pos - dragging.position;
                            }
                        }
                    }
                }

                if (selected != null) {
                    Vector3 scroll = Input.mouseScrollDelta;
                    selected.radius = Mathf.Clamp(selected.radius + scroll.y * 2, 0.1f, 500f);
                    selected.ghostObject.transform.localScale = Vector3.one * (selected.radius * 2 + 10);

                    if (Input.GetKeyDown(KeyCode.Delete)) {
                        Destroy(selected.ghostObject);
                        cityInputs.Remove(selected);
                        selected = null;
                    }
                }
            }
            else {
                ghostObjectInstance.SetActive(false);
            }
        }
    }
}
