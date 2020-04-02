using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlotType {
    Apartments,
    Skyscraper,
    Park,
    Empty
}

public class Plot {
    public List<Vector3> vertices;
    public PlotType type;

    public Plot(List<Vector3> vertices, PlotType type) {
        this.vertices = vertices;
        this.type = type;
    }

    public static PlotType FromBlockType(BlockType type) {
        switch (type) {
            case BlockType.Building:
                return UnityEngine.Random.Range(0f, 1f) < 0.25 ? PlotType.Skyscraper : PlotType.Apartments;
            case BlockType.Park:
                return PlotType.Park;
            default:
                return PlotType.Empty;
        }
    }
}
