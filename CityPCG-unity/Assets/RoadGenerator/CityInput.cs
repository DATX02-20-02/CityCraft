using UnityEngine;

public enum CityType {
    Paris,
    Manhattan
}

public class CityInput {
    public Vector3 position;
    public CityType type;
    public GameObject ghostObject;
    public float radius;

    private bool hovering;
    private bool selected;

    public CityInput(Vector3 position, CityType type, GameObject ghostObject, float radius) {
        this.position = position;
        this.type = type;
        this.ghostObject = ghostObject;
        this.radius = radius;
    }

    public void SetHovering(bool hovering) {
        if (!this.selected && this.hovering != hovering) {
            if (hovering) {
                ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", Color.red);
            }
            else {
                ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", new Color(93 / 255f, 151 / 255f, 1));
            }
        }

        this.hovering = hovering;
    }

    public void SetSelected(bool selected) {
        if (this.selected != selected) {
            if (selected) {
                ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", new Color(1, 0.5f, 0));
            }
            else {
                ghostObject.GetComponent<Renderer>().material.SetColor("_MainColor", new Color(93 / 255f, 151 / 255f, 1));
            }
        }

        this.selected = selected;
    }
}
