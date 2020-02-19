/*using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParkGenerator))]
public class ParkGeneratorInspector : Editor
{

    void OnSceneGUI()
    {
        ParkGenerator parkgen = target as ParkGenerator;
        for(int i = 0; i < parkgen.plot.vertices.Count -1; i++) {
        	parkgen.plot.vertices[i] = Handles.DoPositionHandle(parkgen.plot.vertices[i],parkgen.transform.rotation);
        }
    }
}
*/