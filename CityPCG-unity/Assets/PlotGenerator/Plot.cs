using System;
using System.Collections.Generic;
using UnityEngine;

public class Plot : MonoBehaviour
{
	public List<Vector3> vertices;

    public Plot(List<Vector3> vertices) {
		this.vertices = vertices;
	}
}
