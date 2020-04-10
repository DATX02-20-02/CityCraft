using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Utils.PolygonSplitter;

public class PathGenerator : MonoBehaviour {
    public RoadMesh road;
    private Vector2[] polygon;

    public void GeneratePlotPath(TerrainModel terrain, Block block, Plot plot) {
        List<Vector2> polygonPlot = new List<Vector2>();
        List<Vector2> goalPoints2D = new List<Vector2>();
        List<Vector3> goalPoints3D = new List<Vector3>();
        int stuck = 0;
        Vector3 prev = new Vector3();
        int index = 0;

        foreach (Vector3 edge in plot.vertices) 
            polygonPlot.Add(new Vector2(edge.x, edge.z));

        int numberOfPoints = 3;
        Polygon plotArea = new Polygon(polygonPlot);
        
        if (plotArea.GetArea() < 10) {
            return;
        }
        if (plotArea.GetArea() > 20)
            numberOfPoints = 4;

        goalPoints2D.Add(plotArea.points[Random.Range(0, plotArea.points.Count - 1)]);
        goalPoints3D.Add(terrain.GetPosition(goalPoints2D[0]));
        
        for (int i = 0; i < numberOfPoints; i++) {
            Vector2 pointToAdd = getRandomPoint(plotArea, goalPoints2D);
            goalPoints2D.Add(pointToAdd);
            goalPoints3D.Add(terrain.GetPosition(pointToAdd));
        }
        prev = goalPoints3D[0];
        goalPoints3D.Add(prev);
        int pointsToVisit = goalPoints3D.Count;
        Vector3 tryVec = new Vector3();
        ParkPath p = new ParkPath(goalPoints3D);
        int currGoal = 1;
        p.nodes.Add(prev);
        int maxTries = 10000;
        int tries = 0; 
        int visitedPoints = 1; 
        while (visitedPoints < pointsToVisit) {
            tries += 1;
            if (tries == maxTries) {
                break;
            }

            if (stuck > 10) {
                prev = p.goals[currGoal-1];
                p.goals.RemoveAt(currGoal);
                pointsToVisit--;
                visitedPoints ++;
                stuck = 0;
            }

            float newX = prev.x + Random.Range(-0.1f, 0.1f);
            float newZ = prev.z + Random.Range(-0.1f, 0.1f);
            float newY = terrain.GetPosition(newX, newZ).y;
            tryVec = new Vector3(newX, newY, newZ);
            
            if (index >= 2) {
                Vector3 prevprev = p.nodes[index - 2];
                Vector3 dir = (p.goals[currGoal] - prev).normalized;
                float oldAng = Vector3.Angle(tryVec, prev);
                Vector3 newDir = Quaternion.Euler(0, oldAng - Random.Range(-30f, 30f), 0) * dir;
                tryVec = prev + newDir * Random.Range(0.1f, 0.3f);

            }

            if (plotArea.Contains(new Vector2(tryVec.x, tryVec.z))) {
                if (CloseEnough(tryVec, p.goals[currGoal])) {
                    p.nodes.Add(p.goals[currGoal]);
                    index += 1;
                    currGoal += 1;
                    stuck = 0;
                    visitedPoints += 1;
                }

                else if ((tryVec - p.goals[currGoal]).magnitude < (prev - p.goals[currGoal]).magnitude && !p.nodes.Contains(tryVec)) {
                    p.nodes.Add(tryVec);
                    index += 1;
                }

                else if ((tryVec - p.goals[currGoal]).magnitude > (prev - p.goals[currGoal]).magnitude) {
                    tryVec = prev;
                    stuck+= 1;
                }
                prev = tryVec;
            }

        }

        this.transform.position = p.nodes[0];
        var mesh = Instantiate(road, p.nodes[0], Quaternion.identity, this.transform).GetComponent<RoadMesh>();
        if (p.goals.Count >= numberOfPoints) {
            for (int i = 0; i < p.nodes.Count; i += 2)
                 mesh.Spline.AddPoint(p.nodes[i]);

        mesh.GenerateRoadMesh((float x, float z) => {
            return terrain.GetMeshIntersection(x, z);
        });
            var filter = mesh.RoadMeshFilter;
            var collider = mesh.GetComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private bool CloseEnough(Vector3 start, Vector3 end) {
        return (end - start).magnitude < 0.1f;
    }

    private bool isValidDistance(List<Vector2> points, Vector2 point) {
        if (points.Count < 1)
            return true;

        foreach (Vector2 p in points) {
            if ((p - point).magnitude < 2)
                return false;
        }
        return true;
    }

    private Vector2 getRandomPoint(Polygon p, List<Vector2> points) {
        Vector2 vec = new Vector2();
        float maxX = int.MinValue;
        float maxY = int.MinValue;
        float minY = int.MaxValue;
        float minX = int.MaxValue;

        foreach (Vector2 v in p.points) {

            if (v.x > maxX)
                maxX = v.x;
            if (v.x < minX)
                minX = v.x;
            if (v.y > maxY)
                maxY = v.y;
            if (v.y < minY)
                minY = v.y;
        }

        do {
            vec = new Vector2((Random.Range(minX, maxX)), (Random.Range(minY, maxY)));
        }
        while (!(p.Contains(vec) && isValidDistance(points,vec)));
        return vec;
    }
    
}
