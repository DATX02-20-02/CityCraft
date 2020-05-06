using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.LSystems;
using static ManhattanFloorType;
using Random = UnityEngine.Random;

public class StraightManhattanFloorsGenerator : MonoBehaviour, IManhattanFloorsGenerator {

    private static LSystem<ManhattanFloorType, StraightManhattanFloorsGeneratorData> lSystem;

    static StraightManhattanFloorsGenerator() {
        lSystem = new LSystem<ManhattanFloorType, StraightManhattanFloorsGeneratorData>();

        lSystem.ShouldContinue(v => v.floors < v.maxFloors);

        lSystem.CreateRules(Normal)
            .Add(1f, Normal)
            .OnAccepted(v => new StraightManhattanFloorsGeneratorData(v));

        lSystem.CreateRules(EveryOther)
            .Add(1f, EveryOther)
            .OnAccepted(v => new StraightManhattanFloorsGeneratorData(v));

        lSystem.CreateRules(RepeatWindow)
            .Add(1f, RepeatWindow)
            .OnAccepted(v => new StraightManhattanFloorsGeneratorData(v));
    }

    public List<ManhattanFloorType> Generate(float population) {
        var floors = (int)Math.Floor(population * 10) - 2;
        var possibleFloorTypes = new[] { Normal, EveryOther, RepeatWindow };
        var index = Random.Range(0, possibleFloorTypes.Length);

        return lSystem.Run(new List<ManhattanFloorType>() { First, possibleFloorTypes[index] }, new StraightManhattanFloorsGeneratorData(floors));
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
