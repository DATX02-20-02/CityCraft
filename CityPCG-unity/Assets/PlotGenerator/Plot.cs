using System;
using System.Collections.Generic;
using Utils;

using UnityEngine;

public enum PlotType {
    Manhattan,
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

    public Vector3 Center {
        get {
            return PolygonUtil.PolygonCenter(vertices);
        }
    }
}
