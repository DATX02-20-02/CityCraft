using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParkGenerator))]
public class ParkGeneratorInspector : Editor
{

    void OnSceneGUI()
    {
        ParkGenerator parkgen = target as ParkGenerator;
        for(int i = 0; i < parkgen.plot.area.Length -1; i++) {
        	parkgen.plot.area[i] = Handles.DoPositionHandle(parkgen.plot.area[i],parkgen.transform.rotation);
        }
    }
}