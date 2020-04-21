using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.LSystems;
using static ManhattanFloorType;

public class StraightManhattanFloorsGenerator : MonoBehaviour, IManhattanFloorsGenerator {

    private static LSystem<ManhattanFloorType, StraightManhattanFloorsGeneratorData> lSystem;

    static StraightManhattanFloorsGenerator() {
        lSystem = new LSystem<ManhattanFloorType, StraightManhattanFloorsGeneratorData>();

        lSystem.ShouldContinue(v => v.floors < v.maxFloors);

        lSystem.CreateRules(Normal)
            .Add(1f, Normal)
            .OnAccepted(v => new StraightManhattanFloorsGeneratorData(v));
    }

    public List<ManhattanFloorType> Generate(float population) {
        var floors = (int) Math.Floor(population * 5) - 2;
        return lSystem.Run(new List<ManhattanFloorType>() {First, Normal}, new StraightManhattanFloorsGeneratorData(floors));
    }

    class StraightManhattanFloorsGeneratorData {
        public readonly int floors;
        public readonly int maxFloors;

        public StraightManhattanFloorsGeneratorData(int maxFloors) {
            this.floors = 0;
            this.maxFloors = maxFloors;
        }

        public StraightManhattanFloorsGeneratorData(StraightManhattanFloorsGeneratorData c) {
            this.floors = c.floors + 1;
            this.maxFloors = c.maxFloors;
        }
    }

}
