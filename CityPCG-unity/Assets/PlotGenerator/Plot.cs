using System;
using System.Collections.Generic;
using UnityEngine;

public class Plot {
    public List<Vector2> vertices;

    public Plot(List<Vector2> vertices) {
        this.vertices = vertices;
    }
}

public class ElevatedPlot {
    public List<Vector3> vertices;

    public ElevatedPlot(List<Vector3> vertices) {
        this.vertices = vertices;
    }
}
