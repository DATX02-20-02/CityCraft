using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Block {
    public List<Vector3> vertices;

    public Block(List<Vector3> vertices) {
        this.vertices = vertices;
    }
}
