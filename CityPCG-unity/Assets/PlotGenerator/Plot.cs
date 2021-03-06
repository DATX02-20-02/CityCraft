using System;
using System.Linq;
using System.Collections.Generic;
using Utils;

using UnityEngine;

public enum PlotType {
    Manhattan,
    Skyscraper,
    Park,
    Parking,
    Empty
}

[Serializable]
public class Plot {
    public List<Vector3> vertices;
    public PlotType type;

    public Plot(List<Vector3> vertices, PlotType type) {
        var sanitizedVertices = vertices.Distinct().ToList(); // Remove evil duplicate vertices

        this.vertices = sanitizedVertices;
        this.type = type;
    }

    public Vector3 Center {
        get {
            return PolygonUtil.PolygonCenter(vertices);
        }
    }
}
