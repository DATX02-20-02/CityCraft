using UnityEngine;
using System.Collections.Generic;

public class ParkGenerator : MonoBehaviour {
	[Range(0, 1000)]
	public int count = 10;

	public Plot plot;

	public GameObject tree;
	public GameObject rock;
	public GameObject[] bushes;

	
	void Start() {
		Coordinates(plot.area);
	}
	void Coordinates(Vector3[] area) {
		Triangulator triangulator = new Triangulator(area);
		int[] triangulated = triangulator.Triangulate();
		Triangle[] triangles = ParkGenerator.FromTriangulator(area, triangulated);

		float seed = Random.Range(0, 10000.0f);

		int accumulator = count;
		foreach (Triangle triangle in triangles)
		{
			int amount = (int) Mathf.Ceil((float) count / (float) triangles.Length);
			for (int i = 0; i < Mathf.Min(accumulator, amount); i++)
			{
				Vector3 point = triangle.RandomPoint();
				NoiseEvaluate(point.x, point.z, seed);
			}
			accumulator -= amount;
		}
	}
	void NoiseEvaluate(float x, float y, float seed) 
	{
		float place = 0f;
		float perlinScale = 0.05f;
		float z = Mathf.PerlinNoise(x * perlinScale + seed, y * perlinScale + seed);
		float scale = 1;
		if (z >= 0.5 && z < 1) // Generate Tree 
		{
			scale = Random.Range(0.7f, 1.1f);
			InitMesh(tree, x, y, scale);
			
		}
		else if (z >= 0 && z < 0.5) // Generate Bush 
		{
            if (Random.Range(0, 1.0f) < 0.2f)
			{
				scale = Random.Range(0.01f, 0.02f);

				InitMesh(rock, x, y, scale, UnityEngine.Random.rotation);
			}
            else
			{
				scale = Random.Range(0.10f, 0.20f);
				GameObject bush = bushes[(int)Random.Range(0, bushes.Length) % bushes.Length];
				InitMesh(bush, x, y, scale);
			}
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



