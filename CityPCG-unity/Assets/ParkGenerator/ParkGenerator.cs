using UnityEngine;
using System.Collections.Generic;

public class ParkGenerator : MonoBehaviour {
	public Plot plot;
	public GameObject tree;
	public GameObject bush;
	public GameObject rock;
	public GameObject ball;

	void Start() {
		Coordinates(plot.area);
	}
	void Coordinates(Vector3[] area) {
		Triangulator triangulator = new Triangulator(area);
		int[] triangulated = triangulator.Triangulate();
		Triangle[] triangles = ParkGenerator.FromTriangulator(area, triangulated);

		int count = 1000;
		int accumulator = count;
		foreach (Triangle triangle in triangles)
		{
			int amount = (int) Mathf.Ceil((float) count / (float) triangles.Length);
			for (int i = 0; i < Mathf.Min(accumulator, amount); i++)
			{
				Vector3 point = triangle.RandomPoint();
				NoiseEvaluate(point.x, point.z);
			}
			accumulator -= amount;
		}
	}
	void NoiseEvaluate(float x, float y) 
	{
		float place = 0f;
		float OffsetX = Random.Range(0f, 9999f);
        float OffsetY = Random.Range(0f, 9999f);
		float z = Mathf.PerlinNoise(OffsetX * x, OffsetY * y);
		float scale = 1;
		if (z >= 0.5 && z < 1) // Generate Tree 
		{
			scale = Random.Range(0.7f, 1.1f);
			InitMesh(tree, x, y, scale);
			
		}
		else if (z >= 0.3 && z < 0.5) // Generate Bush 
		{
			scale = Random.Range(0.10f, 0.20f);

			InitMesh(bush, x, y, scale);
		}
		else if (z >= 0 && z < 0.3) // Generate Rock 
		{
			scale = Random.Range(0.01f, 0.02f);

			InitMesh(rock, x, y, scale, UnityEngine.Random.rotation);
		}
	}

    void InitMesh(GameObject g, float x, float y, float scale, Quaternion rotation)
	{
		GameObject obj = Instantiate(g, transform);
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		obj.transform.position = new Vector3(x, 0f, y);
		obj.transform.localScale = new Vector3(scale, scale, scale);
		obj.transform.rotation = rotation;
		Rigidbody body = obj.AddComponent<Rigidbody>();
		obj.AddComponent <MeshCollider>();
		body.isKinematic = true;
	}

    void InitMesh(GameObject g, float x, float y, float scale)
	{
		InitMesh(g, x, y, scale, Quaternion.identity);
	}

	private Vector2[] ToVector2Array(Vector3[] v3)
	{
		return System.Array.ConvertAll<Vector3, Vector2>(v3, GetV3FromV2);
	}

	private Vector2 GetV3FromV2(Vector3 v3)
	{
		return new Vector2(v3.x, v3.z);
	}
	void Update()
	{
		for (int i = 0; i < plot.area.Length; i++)
		{
			Vector3 cur = plot.area[i];
			Vector3 next = plot.area[(i + 1) % plot.area.Length];

			Debug.DrawLine(cur, next, new Color(1, 0, 0));
		}
        
	}
    public static Triangle[] FromTriangulator(Vector3[] area, int[] indices)
	{
		Triangle[] result = new Triangle[indices.Length / 3];

        for(int i = 0; i < indices.Length; i+= 3)
		{
			result[i / 3] = new Triangle(
				area[indices[i + 0]],
				area[indices[i + 1]],
				area[indices[i + 2]]
			);
		}

		return result;
	} 
}



