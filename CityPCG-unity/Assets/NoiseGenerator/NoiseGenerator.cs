using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour {
    [Header("Generator settings")]
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private float seed = 0;
    [SerializeField] private Layer[] layers = null;

    [Header("Debug settings")]
    [SerializeField] private float width = 10;
    [SerializeField] private float height = 10;
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private GameObject debugPlane = null;

    [SerializeField] private bool debug = false;
    private Noise noise;

    private Texture2D texture = null;

    public float Seed {
        get {
            return randomSeed ? Random.Range(-100000, 100000) : this.seed;
        }
        set {
            this.seed = value;
        }
    }

    public Noise Generate() {
        if (this.debugPlane != null) {
            if (this.texture == null)
                this.texture = new Texture2D(this.textureWidth, this.textureHeight);

            this.texture.Resize(this.textureWidth, this.textureHeight);
        }

        Color[] colors = new Color[this.textureWidth * this.textureHeight];

        this.noise = new Noise(this.layers, this.Seed);

        for (int x = 0; x < this.textureWidth; x++) {
            for (int y = 0; y < this.textureHeight; y++) {
                float value = this.noise.GetValue(x / (float)this.textureWidth, y / (float)this.textureHeight);

                colors[x + y * this.textureWidth] = new Color(value, value, value);
            }
        }

        if (this.debugPlane != null) {
            this.texture.SetPixels(colors);
            this.texture.Apply();

            this.debugPlane.GetComponent<Renderer>().sharedMaterial.mainTexture = this.texture;
            this.debugPlane.transform.localScale = new Vector3(0.1f * this.width, 1, 0.1f * this.height);
            this.debugPlane.transform.position = transform.position + new Vector3(this.width / 2, -1, this.height / 2);
            this.debugPlane.transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        }

        return this.noise;
    }

    private void Update() {
        if (debug && noise != null) {
            Vector3 mousePos = VectorUtil.GetPlaneMousePos(new Vector3(0, 0, 0));

            Vector2 slope = noise.GetSlope(mousePos.x / this.width, mousePos.z / this.height);
            float value = noise.GetValue(mousePos.x / this.width, mousePos.z / this.height);

            DrawUtil.DebugDrawCircle(Vector3.zero, 2, new Color(0, 0, 1));
            DrawUtil.DebugDrawCircle(transform.position, 2, new Color(1, 0, 1));

            Vector3 mPos = mousePos + Vector3.up * value * 10;
            DrawUtil.DebugDrawCircle(mPos, 2, new Color(1, 0, 0));
            Debug.DrawLine(mPos, mPos + VectorUtil.Vector2To3(slope) * 10, new Color(0, 1, 0));

            Debug.DrawLine(Vector3.zero, new Vector3(this.textureWidth, 0, 0), new Color(1, 0, 0));
            Debug.DrawLine(Vector3.zero, new Vector3(0, 0, this.textureHeight), new Color(1, 0, 0));
        }
    }
}
