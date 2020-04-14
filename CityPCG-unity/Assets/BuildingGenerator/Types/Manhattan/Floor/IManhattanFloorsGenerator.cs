using System.Collections.Generic;

public interface IManhattanFloorsGenerator {
    List<ManhattanFloorType> Generate(float population);
}
