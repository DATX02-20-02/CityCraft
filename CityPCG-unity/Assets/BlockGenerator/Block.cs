using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BlockType {
    Industrial,
    Suburbs,
    Downtown,
    Skyscrapers,
    Apartments,
    Parks,
    Parking,
    Empty
}

[Serializable]
public struct Block {
    public BlockType type;
    public List<Vector3> vertices;
    private List<Vector2> points;

    public List<Vector2> Points {
        get => points;
    }

    public Block(List<Vector3> vertices, BlockType type) {
        this.vertices = vertices;
        this.type = type;

        this.points = vertices.Select(VectorUtil.Vector3To2).ToList();
    }
}
