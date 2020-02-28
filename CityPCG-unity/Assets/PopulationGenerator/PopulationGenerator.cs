using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopulationGenerator : MonoBehaviour {
    [SerializeField] float width = 10;
    [SerializeField] float height = 10;
    [SerializeField] bool randomSeed = true;
    [SerializeField] int textureWidth = 512;
    [SerializeField] int textureHeight = 512;
    [SerializeField] GameObject plane = null;
    [SerializeField] Layer[] layers;

    private float[] map;
    private Texture2D texture;

    [System.Serializable]
    public class Layer {
        public Vector2 offset;
        public float scale = 1;
        public float magnitude = 1;
        public float exponent = 1;

        public Layer(Vector2 offset, float scale, float exponent) {
            this.offset = offset;
            this.scale = scale;
            this.exponent = exponent;
        }
    }

    public void Generate() {
        if (this.texture == null)
            this.texture = new Texture2D(this.textureWidth, this.textureHeight);

        this.texture.Resize(this.textureWidth, this.textureHeight);

        this.map = new float[this.textureWidth * this.textureHeight];
        Color[] colors = new Color[this.textureWidth * this.textureHeight];

        float seed = randomSeed ? Random.Range(-100000, 100000) : 0;

        for (int x = 0; x < this.textureWidth; x++) {
            for (int y = 0; y < this.textureHeight; y++) {
                float value = 0;

                foreach (Layer layer in layers) {
                    float nx = x / (float) this.textureWidth * layer.scale + layer.offset.x + seed;
                    float ny = y / (float) this.textureHeight * layer.scale + layer.offset.y + seed;

                    float pvalue = Mathf.Pow(2 * Mathf.PerlinNoise(nx, ny) * layer.magnitude, layer.exponent) / 2;
                    value += pvalue;
                }

                value = Mathf.Clamp(value / layers.Length, 0, 1);

                int index = x + y * this.textureWidth;

                this.map[index] = value;
                colors[index] = new Color(value, value, value);
            }
        }

        this.texture.SetPixels(colors);
        this.texture.Apply();

        if (plane != null) {
            plane.GetComponent<Renderer>().sharedMaterial.mainTexture = this.texture;
            plane.transform.localScale = new Vector3(0.1f * this.width, 1, 0.1f * this.height);
            plane.transform.position = transform.position + new Vector3(this.width / 2, 0, this.height / 2);
        }
    }

    public bool IsInBounds(float x, float y) {
        float mx = x / this.width;
        float my = y / this.height;

        return mx >= 0 && mx < 1 && my >= 0 && my < 1;
    }

    public Vector2Int MapCoordinates(float x, float y, bool useRelative = false) {
        if (useRelative) {
            x = x - transform.position.x;
            y = y - transform.position.z;
        }

        int mx = (int) (Mathf.Clamp(x / this.width, 0, 1) * this.textureWidth);
        int my = (int) (Mathf.Clamp(y / this.height, 0, 1) * this.textureHeight);
        return new Vector2Int(mx, my);
    }

    public float GetValue(float x, float y)  {
        Vector2Int mapped = this.MapCoordinates(x, y);
        return this.map[mapped.x + mapped.y * this.textureWidth];
    }

    public Vector2 GetSlope(float x, float y) {
        float maxValue = float.MaxValue;
        Vector2 max = Vector2.zero;

        for (float i = 0; i <= Mathf.PI * 2; i += (Mathf.PI * 2) / 10) {
            Vector2 n = new Vector2(Mathf.Cos(i), Mathf.Sin(i));
            float value = this.GetValue(x + n.x * 10, y + n.y * 10);

            if (value > maxValue) {
                max = n;
                maxValue = value;
            }
        }

        return max;
    }

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
        Vector3 mousePos = Util.GetPlaneMousePos(new Vector3(0, 0, 0));

        Vector2Int mapped = MapCoordinates(mousePos.x, mousePos.z, true);

        Util.DebugDrawCircle(Vector3.zero, 2, new Color(0, 0, 1));
        Util.DebugDrawCircle(transform.position, 2, new Color(1, 0, 1));

        Util.DebugDrawCircle(mousePos, 2, new Color(1, 1, 0));
        Util.DebugDrawCircle(new Vector3(mapped.x, 0, mapped.y), 1, new Color(0, 1, 0));

        Debug.DrawLine(Vector3.zero, new Vector3(this.textureWidth, 0, 0), new Color(1, 0, 0));
        Debug.DrawLine(Vector3.zero, new Vector3(0, 0, this.textureHeight), new Color(1, 0, 0));
    }
}
