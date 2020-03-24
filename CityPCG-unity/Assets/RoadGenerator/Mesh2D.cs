using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Mesh2D : ScriptableObject {
    [System.Serializable]
    public class Vertex {
        public Vector2 point;
        public Vector2 normal;
        public float u; // UVs, except for V component
    }

    public Vertex[] vertices;
    public int[] lineIndices;

    public int VertexCount => vertices.Length;
    public int LineCount => lineIndices.Length;
}
