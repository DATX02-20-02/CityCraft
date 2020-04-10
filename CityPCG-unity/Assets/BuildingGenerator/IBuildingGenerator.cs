using UnityEngine;

public interface IBuildingGenerator {
    GameObject Generate(Plot plot, GameObject buildings);
}
