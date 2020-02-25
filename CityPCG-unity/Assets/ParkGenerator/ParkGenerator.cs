using UnityEngine;
using System.Collections.Generic;

public class ParkGenerator : MonoBehaviour {
	[Range(0, 1000)]
	[SerializeField] private int count = 10;
	[SerializeField] private GameObject[] trees;
	[SerializeField] private GameObject rock;
	[SerializeField] private GameObject[] bushes;
	[SerializeField] private int layerMask;
	[SerializeField] private int layerMasked;
	public Plot plot;


	// Coordinates calls the Triangulator function in order to divide polygons into triangles
	void Coordinates(Vector3[] area) {
		Triangulator triangulator = new Triangulator(area);
		int[] triangulated = triangulator.Triangulate();
		Triangle[] triangles = ParkGenerator.FromTriangulator(area, triangulated);
		float seed = Random.Range(0, 10000.0f);
		int accumulator = count;

		foreach (Triangle triangle in triangles) {
			int amount = (int)Mathf.Ceil((float)count / (float)triangles.Length);

			for (int i = 0; i < Mathf.Min(accumulator, amount); i++) {
				Vector3 point = triangle.RandomPoint();
				NoiseEvaluate(point.x, point.z, seed);
			}
			accumulator -= amount;
		}
	}
	// NoiseEvaluate creates a pseudorandom value using Perlin Noise and determines what object to spawn based on it
	void NoiseEvaluate(float x, float y, float seed) {
		float perlinScale = 0.05f;
		float z = Mathf.PerlinNoise(x * perlinScale + seed, y * perlinScale + seed);
		float scale = 1;
		if (z >= 0.5 && z < 1) {
			scale = Random.Range(0.7f, 1.1f);
			GameObject tree = trees[(int)Random.Range(0, trees.Length) % trees.Length];
			tree.layer = 2;
			layerMask = 1 << 2;
			InitMesh(tree, x, y, scale);
		}
		else if (z >= 0 && z < 0.5)  {
			if (Random.Range(0, 1.0f) < 0.2f) {
				scale = Random.Range(0.01f, 0.02f);
				rock.layer = 1;
				layerMasked = 1 <<1;
				InitMesh(rock, x, y, scale, UnityEngine.Random.rotation);
			}
			else {
				scale = Random.Range(0.10f, 0.20f);
				GameObject bush = bushes[(int)Random.Range(0, bushes.Length) % bushes.Length];
				bush.layer = 1;
				layerMasked = 1 <<1;
				InitMesh(bush, x, y, scale);
			}
		}
	}

	// InitMesh is called for spawning Game objects, assigning them a scale, position, and rotation.
	void InitMesh(GameObject g, float x, float y, float scale, Quaternion rotation) {
		int segments = 0;
		GameObject obj = Instantiate(g, transform);
		obj.AddComponent<MeshCollider>();
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		obj.transform.position = new Vector3(x, 0f, y);
		obj.transform.localScale = new Vector3(scale, scale, scale);
		obj.transform.rotation = rotation;
		Rigidbody body = obj.AddComponent<Rigidbody>();
		body.isKinematic = true;
		float treeRadius = 3f;
		float miscRadius = 0.5f;
		if(obj.layer == 1 ) {
			Collider[] miscCollisions = Physics.OverlapSphere(obj.transform.position, miscRadius, layerMasked);
			if (miscCollisions.Length > 1) {
				Destroy(obj);
			}
			/*else {
				segments = 50;
				obj.AddComponent<LineRenderer>();
				line = obj.GetComponent<LineRenderer>();
        		line.SetVertexCount (segments + 1);
        		line.useWorldSpace = false;
        		CreatePoints(miscRadius,segments);
        	}*/

		}
		if (obj.layer == 2) {
			Collider[] treeCollisions = Physics.OverlapSphere(obj.transform.position, treeRadius, layerMask);
			if (treeCollisions.Length > 1) {
				Destroy(obj);
			}
			else {
				segments = 50;
				obj.AddComponent<LineRenderer>();
				line = obj.GetComponent<LineRenderer>();
        		line.SetVertexCount (segments + 1);
        		line.useWorldSpace = false;
        		CreatePoints (treeRadius,segments);
			}
			

		}

	}
	
	void InitMesh(GameObject g, float x, float y, float scale) {
		InitMesh(g, x, y, scale, Quaternion.identity);
	}

	private Vector2[] ToVector2Array(Vector3[] v3) {
		return System.Array.ConvertAll<Vector3, Vector2>(v3, GetV3FromV2);
	}
	private Vector2 GetV3FromV2(Vector3 v3) {
		return new Vector2(v3.x, v3.z);
	}
	public static Triangle[] FromTriangulator(Vector3[] area, int[] indices) {
		Triangle[] result = new Triangle[indices.Length / 3];

		for (int i = 0; i < indices.Length; i += 3) {
			result[i / 3] = new Triangle(
				area[indices[i + 0]],
				area[indices[i + 1]],
				area[indices[i + 2]]
			);
		}

		return result;
	}
    LineRenderer line;
	void CreatePoints (float radius,int segments)
    {
       float x;
       float z;
       float angle = 20f;
        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin (Mathf.Deg2Rad * angle) * radius;
            z = Mathf.Cos (Mathf.Deg2Rad * angle) * radius;
            line.SetPosition (i,new Vector3(x,0,z) );
            angle += (360f / segments);
        }
    }
	void Start() {
		Coordinates(plot.area);
	}
	void Update() {
		for (int i = 0; i < plot.area.Length; i++) {
			Vector3 cur = plot.area[i];
			Vector3 next = plot.area[(i + 1) % plot.area.Length];
			Debug.DrawLine(cur, next, new Color(1, 0, 0));
		}

	}

}



