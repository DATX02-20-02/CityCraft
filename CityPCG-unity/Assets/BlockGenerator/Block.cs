using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Block {
    public List<Vector3> vertices;

    public List<Vector2> Vertices2D() {
        List<Vector2> res = new List<Vector2>();
        foreach (var v in vertices)
            res.Add(new Vector2(v.x, v.z));
        return res;
    }

    public Block(List<Vector3> vertices) {
        this.vertices = vertices;
    }
}
