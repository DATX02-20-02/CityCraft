using UnityEngine;
using System.Collections.Generic;


public class ParkPath {
    public Vector3 start;
    public Vector3 goal;
    public List<Vector3> nodes;
    public List<Vector3> goals;

    public ParkPath(Vector3 start, Vector3 goal, List<Vector3> nodes) {
        this.start = start;
        this.goal = goal;
        this.nodes = nodes;
    }
    public ParkPath(List<Vector3> goals) {
        this.goals = goals;
        this.nodes = new List<Vector3>();
    }

}
