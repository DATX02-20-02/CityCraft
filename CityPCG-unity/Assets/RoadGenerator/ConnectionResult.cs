using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionResult {
    public bool success;
    public bool didIntersect;
    public bool didSnap;
    public Node prevNode;

    public ConnectionResult(bool success, bool didIntersect, bool didSnap, Node prevNode) {
        this.success = success;
        this.didIntersect = didIntersect;
        this.didSnap = didSnap;
        this.prevNode = prevNode;
    }
}
