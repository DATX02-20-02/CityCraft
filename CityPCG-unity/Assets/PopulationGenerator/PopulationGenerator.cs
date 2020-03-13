using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopulationGenerator : MonoBehaviour {
    private Noise noise;

    public Noise Generate() {
        NoiseGenerator generator = gameObject.GetComponent<NoiseGenerator>() as NoiseGenerator;

        this.noise = generator.Generate();

        return this.noise;
    }

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
    }
}
