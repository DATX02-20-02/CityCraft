using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionInfo {
    public Node from;
    public Node.NodeConnection connection;
    public Vector2 point;
    public bool isProjection;

    public IntersectionInfo(Node from, Node.NodeConnection connection, Vector2 point, bool isProjection) {
        this.from = from;
        this.connection = connection;
        this.point = point;
        this.isProjection = isProjection;
    }
}
