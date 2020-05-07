using UnityEngine;

public class SkyscraperGenerator : MonoBehaviour, IBuildingGenerator {

    public GameObject skyscraperPrefab;

    public GameObject Generate(Plot plot, GameObject buildings, float population) {
        var skyscraper = Instantiate(skyscraperPrefab, buildings.transform);
        skyscraper.GetComponent<Skyscraper>().Generate(plot);
        return skyscraper;
    }
}

