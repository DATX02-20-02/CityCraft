using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Utils.PolygonSplitter;

public class PathGenerator : MonoBehaviour {
    public RoadMesh road;
    private Vector2[] polygon;
    private RoadNetwork network;
    [SerializeField] private RoadMeshGenerator meshGenerator = null;
    
    public void Reset() {
        meshGenerator.Reset();
    }
    public void GeneratePlotPath(TerrainModel terrain, Plot plot) {
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
        
        Vector2 cornerPoint = plotArea.points[Random.Range(0, plotArea.points.Count)];
        goalPoints2D.Add(cornerPoint);
        goalPoints3D.Add(terrain.GetMeshIntersection(goalPoints2D[0].x, goalPoints2D[0].y).point);
        
        for (int i = 0; i < numberOfPoints; i++) {
            Vector2 pointToAdd = getRandomPoint(plotArea, goalPoints2D);
            goalPoints2D.Add(pointToAdd);
        }
        Vector2 prev2D = goalPoints2D[0];
        bool samePoint = true;
        Vector2 testPoint = new Vector2();
        int decider = Random.Range(0, 2);
        if (decider >= 1) {
            goalPoints2D = OrderByDistance(goalPoints2D);
            goalPoints2D.Add(prev2D);
        }
        else {
            while (samePoint) {
                testPoint = polygonPlot[Random.Range(0, polygonPlot.Count)];
                if (testPoint != cornerPoint)
                    samePoint = false;
            }
            goalPoints2D.Add(testPoint);
            goalPoints2D = OrderByDistance(goalPoints2D);
        }
        foreach(Vector2 v in goalPoints2D) {
            goalPoints3D.Add(terrain.GetMeshIntersection(v.x,v.y).point);
        }
        prev = goalPoints3D[0];
        int pointsToVisit = goalPoints3D.Count;
        Vector3 tryVec = new Vector3();
        ParkPath p = new ParkPath(goalPoints3D);
        int currGoal = 1;
        p.nodes.Add(prev);
        int visitedPoints = 1;
        while (visitedPoints < pointsToVisit) {
            if (stuck > 10) {
                prev = p.goals[currGoal - 1];
                p.goals.RemoveAt(currGoal);
                pointsToVisit--;
                visitedPoints++;
                stuck = 0;
            }
            float newX = prev.x + Random.Range(-0.1f, 0.1f);
            float newZ = prev.z + Random.Range(-0.1f, 0.1f);
            float newY = terrain.GetMeshIntersection(newX, newZ).point.y;
            tryVec = new Vector3(newX, newY, newZ);
            if (visitedPoints >= pointsToVisit)
                break;
            if (index >= 2) {
                Vector3 prevprev = p.nodes[index - 2];
                Vector3 dir = (p.goals[currGoal] - prev).normalized;
                float oldAng = Vector3.Angle(tryVec, prev);
                Vector3 newDir = Quaternion.Euler(0, oldAng - Random.Range(-30f, 30f), 0) * dir;
                tryVec = prev + newDir * 0.20f;

            }

            if (plotArea.Contains(new Vector2(tryVec.x, tryVec.z))) {
                if (CloseEnough(tryVec, p.goals[currGoal])) {
                    p.nodes.Add(p.goals[currGoal]);
                    index += 1;
                    currGoal += 1;
                    stuck = 0;
                    visitedPoints += 1;
                }

                else if ((tryVec - p.goals[currGoal]).magnitude < (prev - p.goals[currGoal]).magnitude) {
                    p.nodes.Add(tryVec);
                    index += 1;
                    stuck = 0;
                }

                else if ((tryVec - p.goals[currGoal]).magnitude > (prev - p.goals[currGoal]).magnitude) {
                    tryVec = prev;
                    stuck += 1;
                }
                prev = tryVec;
            }
            stuck += 1;

        }

        if (p.goals.Count >= numberOfPoints && p.nodes.Count > 15) {
            AddPathExits(terrain,p.nodes,plot);
            meshGenerator.Generate(network,terrain,(List<RoadMesh> roads) => { });
        }
    }
    private bool CloseEnough(Vector3 start, Vector3 end) {
        return (end - start).magnitude < 0.5f;
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
        while (!(p.Contains(vec) && isValidDistance(points, vec)));
        return vec;
    }
    private List<Vector2> OrderByDistance(List<Vector2> goals) {
        int index = 0;
        List<Vector2> result = new List<Vector2>();
        int toAdd = 0;
        int numberOfGoals = goals.Count;
        result.Add(goals[0]);
        goals.RemoveAt(0);
        float minDist = int.MaxValue;
        while (result.Count < numberOfGoals - 1) {
            for (int i = 0; i < goals.Count; i++) {
                if ((result[index] - goals[i]).magnitude < minDist) {
                    toAdd = i;
                    minDist = (result[index] - goals[i]).magnitude;
                }
            }
            result.Add(goals[toAdd]);
            goals.RemoveAt(toAdd);
            minDist = int.MaxValue;
            index++;
        }
        result.Add(goals[0]);
        return result;
    }

    private void AddPathExits(TerrainModel terrain, List<Vector3> points, Plot plot) {
        Node prevNode; 

        List<Node> nodes = new List<Node>();
        network = new RoadNetwork(terrain,null,terrain.width,terrain.depth);
        Agent Alexander = new Agent(network,Vector3.zero,Vector3.zero,null);
        Alexander.config.snapRadius = 0.2f;
        for(int i = 0; i < points.Count; i += 5) {  
            prevNode = Alexander.PlaceNode(points[i],Node.NodeType.ParkPath,ConnectionType.ParkPath);
            nodes.Add(prevNode);
        }
        prevNode = Alexander.PlaceNode(points[points.Count-1],Node.NodeType.ParkPath,ConnectionType.ParkPath);
        nodes.Add(prevNode);
        for(int i = 1; i < nodes.Count-2; i += 2) {
            Vector2 origin = VectorUtil.Vector3To2(nodes[i].pos);
            Vector3 orgDir = (nodes[i-1].pos-nodes[i+1].pos).normalized;
            Vector3 newDir = -Vector3.Cross(orgDir,Vector3.up);
            if (Vector3.Dot(newDir, nodes[i-1].pos - nodes[i].pos) > 0) 
                newDir *= -1;

            float minDist = int.MaxValue;
            bool found = false;
            Vector2 best = new Vector2();
            for(int j = 0; j < plot.vertices.Count; j++) {
                Vector2 proj = VectorUtil.GetProjectedPointOnLine(
                    origin,
                    VectorUtil.Vector3To2(plot.vertices[j]),
                    VectorUtil.Vector3To2(plot.vertices[(j+1) % plot.vertices.Count])
                );
                if (proj != Vector2.negativeInfinity) {
                    float d = Vector2.Distance(proj, origin);
                    if ( d < minDist && d > 0.2f && d < 2) {
                        minDist = d;
                        found = true;
                        best = proj;
                    }
                }
            }
            if (found) {
                Alexander.PreviousNode = nodes[i];
                Alexander.PlaceNode(VectorUtil.Vector2To3(best),Node.NodeType.ParkPath,ConnectionType.ParkPath);
            }
        }
    }
    void OnDestroy() {
        Reset();
    }
    void Update() {
        if(network != null)
            network.DrawDebug(true);
    }
}

