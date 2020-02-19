using UnityEngine;

public interface ICityGenerator {
    void Generate(RoadGenerator generator, Vector3 origin);
}
