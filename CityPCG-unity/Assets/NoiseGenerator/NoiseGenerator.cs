using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour {

    [SerializeField] private float width = 10;
    [SerializeField] private float height = 10;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private GameObject debugPlane = null;
    [SerializeField] private Layer[] layers = null;

    [SerializeField] private bool debug = false;
    [SerializeField] private Noise noise = null;

    private Texture2D texture = null;


    public Noise Generate() {
        if (this.debugPlane != null) {
            if (this.texture == null)
                this.texture = new Texture2D(this.textureWidth, this.textureHeight);

            this.texture.Resize(this.textureWidth, this.textureHeight);
        }

        float[] map = new float[this.textureWidth * this.textureHeight];
        Color[] colors = new Color[this.textureWidth * this.textureHeight];

        float seed = randomSeed ? Random.Range(-100000, 100000) : 0;

        for (int x = 0; x < this.textureWidth; x++) {
            for (int y = 0; y < this.textureHeight; y++) {
                float value = 0;
                float maxMagnitude = 0;

                foreach (Layer layer in layers) {
                    float nx = x / (float)this.textureWidth * layer.scale + layer.offset.x + seed;
                    float ny = y / (float)this.textureHeight * layer.scale + layer.offset.y + seed;

                    float pvalue = Mathf.Pow(2 * Mathf.PerlinNoise(nx, ny) * layer.magnitude, layer.exponent) / 2;
                    value += pvalue;
                    maxMagnitude += layer.magnitude;
                }

                value = Mathf.Clamp(value / maxMagnitude, 0, 1);

                int index = x + y * this.textureWidth;

                map[index] = value;
                colors[index] = new Color(value, value, value);
            }
        }

        this.noise = new Noise(
            map, transform.position,
            this.width, this.height,
            this.textureWidth, this.textureHeight
        );

        if (this.debugPlane != null) {
            this.texture.SetPixels(colors);
            this.texture.Apply();

            this.debugPlane.GetComponent<Renderer>().sharedMaterial.mainTexture = this.texture;
            this.debugPlane.transform.localScale = new Vector3(0.1f * this.width, 1, 0.1f * this.height);
            this.debugPlane.transform.position = transform.position + new Vector3(this.width / 2, 0, this.height / 2);
        }

        return this.noise;
    }

    private void Update() {
        if (debug && noise != null) {
            Vector3 mousePos = VectorUtil.GetPlaneMousePos(new Vector3(0, 0, 0));

            Vector2Int mapped = noise.MapCoordinates(mousePos.x, mousePos.z, true);

            DrawUtil.DebugDrawCircle(Vector3.zero, 2, new Color(0, 0, 1));
            DrawUtil.DebugDrawCircle(transform.position, 2, new Color(1, 0, 1));

            DrawUtil.DebugDrawCircle(mousePos, 2, new Color(1, 1, 0));
            DrawUtil.DebugDrawCircle(new Vector3(mapped.x, 0, mapped.y), 1, new Color(0, 1, 0));

            Debug.DrawLine(Vector3.zero, new Vector3(this.textureWidth, 0, 0), new Color(1, 0, 0));
            Debug.DrawLine(Vector3.zero, new Vector3(0, 0, this.textureHeight), new Color(1, 0, 0));
        }
    }
}
