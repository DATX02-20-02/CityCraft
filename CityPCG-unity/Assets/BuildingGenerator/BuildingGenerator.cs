using System.Collections;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
	public Plot plot;
	private Mesh mesh;
	
	void Start() {
		LineRenderer lr = new LineRenderer();
		lr.SetPositions(plot.area);
		lr.BakeMesh(mesh, true);

	}
	void Update() {
		for (var i = 0; i < plot.area.Length - 1; i++)
		{
			Debug.DrawLine(plot.area[i], plot.area[i + 1], Color.blue, 10000);
		}
		Debug.DrawLine(plot.area[plot.area.Length - 1], plot.area[0], Color.blue, 10000);
	} 
}
