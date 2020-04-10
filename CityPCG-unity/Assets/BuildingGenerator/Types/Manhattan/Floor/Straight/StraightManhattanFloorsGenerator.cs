using System.Collections.Generic;
using UnityEngine;

public class StraightManhattanFloorsGenerator : MonoBehaviour, IManhattanFloorsGenerator{
    public List<ManhattanFloorType> Generate() {
        return new List<ManhattanFloorType>(){ManhattanFloorType.First, ManhattanFloorType.Normal, ManhattanFloorType.Normal};
    }
}
