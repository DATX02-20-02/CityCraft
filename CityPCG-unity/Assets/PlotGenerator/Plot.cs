using System;
using System.Linq;
using System.Collections.Generic;
using Utils;

using UnityEngine;

public enum PlotType {
    Apartments,
    Skyscraper,
    Park,
    Parking,
    Empty
}

public class Plot {
    public List<Vector3> vertices;
    public PlotType type;

    public Plot(List<Vector3> vertices, PlotType type) {
        var sanitizedVertices = vertices.Distinct().ToList(); // Remove evil duplicate vertices
        sanitizedVertices.Add(vertices[0]); // Re-add first vertex to form loop

        this.vertices = sanitizedVertices;
        this.type = type;
    }

    public Vector3 Center {
        get {
            return PolygonUtil.PolygonCenter(vertices);
        }
    }
}
