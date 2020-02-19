using System.Collections;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
	public Plot plot;
	private Mesh mesh;
	private Material material;
	void Start() {
		LineRenderer lr = new LineRenderer();
		lr.SetPositions(plot.area);
		lr.BakeMesh(mesh, true);
		material = new Material();
		material.SetColor("_Color", Color.red);

	}
	void Update() {
		Graphics.DrawMesh(mesh,Vector3.zero, Quaternion.identity,material,0);
	} 
}
