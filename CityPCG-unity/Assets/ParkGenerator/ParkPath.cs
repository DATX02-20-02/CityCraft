using UnityEngine; 
using System.Collections.Generic;


public class ParkPath {
	public Vector3 start;
	public Vector3 goal;
	public List<Vector3> nodes;
	public List<Vector3> points;

	public ParkPath(Vector3 start, Vector3 goal, List<Vector3> nodes) {
		this.start = start;
		this.goal = goal;
		this.nodes = nodes; 
	}
	public ParkPath(List<Vector3> points, List<Vector3> nodes) {
		this.points = points;
		this.nodes = nodes;
	}
	
}
