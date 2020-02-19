using UnityEngine;
public class BuildingGenerator : MonoBehaviour
{
	public Plot plot;
	public GameObject building;
	
	void Start() {
		var b = Instantiate(building, transform);
		var mesh = b.GetComponent<MeshFilter>().mesh;

		mesh.Clear();

		var meshVertices = new Vector3[plot.area.Length * 2];
		for (var i = 0; i < meshVertices.Length; i += 2)
		{
			meshVertices[i] = plot.area[i / 2];
		}
		
		var up = new Vector3(0, 5, 0);

		for (var i = 1; i < meshVertices.Length; i += 2)
		{
			meshVertices[i] = meshVertices[i - 1] + up;
		}
		
		var meshTriangles = new int[plot.area.Length * 6];
		var numVert = meshVertices.Length;
		
		for (var i = 0; i < numVert; i += 2)
		{
			var j = i * 3;
			
			meshTriangles[j] = i;
			meshTriangles[j + 1] = (i + 1) % numVert;
			meshTriangles[j + 2] = (i + 2) % numVert;

			meshTriangles[j + 3] = (i + 1) % numVert;
			meshTriangles[j + 4] = (i + 3) % numVert;
			meshTriangles[j + 5] = (i + 2) % numVert;
		}

		foreach (var index in meshTriangles)
		{
			Debug.Log(index);
		}
		
		mesh.vertices = meshVertices;
		mesh.triangles = meshTriangles;
	}
	
	void Update() {

	} 
}

