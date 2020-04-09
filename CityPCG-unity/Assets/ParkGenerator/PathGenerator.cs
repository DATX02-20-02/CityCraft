using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Utils.PolygonSplitter;

public class PathGenerator : MonoBehaviour {
    [SerializeField] public Plot plot;
    [SerializeField] private ParkPath path;
    private List<Vector3> pathsToStart;
    public GameObject step;
    private Vector2[] polygon;

    public void GeneratePaths(Block block, TerrainModel terrain, List<Plot> plots) {
        if (plots.Count == 1) {
            GeneratePlotPath(terrain, block, plots[0]);
        }
        else {
            int index = 0;
            List<Vector3> randomPoints = new List<Vector3>();
            List<Polygon> polygons = new List<Polygon>();
            List<Vector3> worldPos = new List<Vector3>();
            List<Vector2> toPolygon = new List<Vector2>();
            List<Vector3> blockPolygon = new List<Vector3>();
            List<Vector2> polygonBlock = new List<Vector2>();

            foreach (Vector3 edge in block.vertices) {
                Vector3 realCoord = terrain.GetPosition(edge.x, edge.z);
                blockPolygon.Add(realCoord);
                polygonBlock.Add(new Vector2(realCoord.x, realCoord.z));
            }

            Polygon blockArea = new Polygon(polygonBlock);

            foreach (Plot plot in plots) {
                foreach (Vector3 vec in plot.vertices) {
                    Vector3 wp = terrain.GetPosition(vec.x, vec.y);
                    worldPos.Add(wp);
                }
                foreach (Vector3 worldVec in worldPos) {
                    toPolygon.Add(new Vector2(worldVec.x, worldVec.z));
                }
                Polygon polygon = new Polygon(toPolygon);
                polygons.Add(polygon);
                Vector2 randomPoint = getRandomPoint(polygon);
                Vector3 worldRandom = terrain.GetPosition(randomPoint);
                randomPoints.Add(worldRandom);
            }

            randomPoints.Add(randomPoints[0]);
            int pointsToVisit = randomPoints.Count;
            int visitedPoints = 0;
            Vector3 tryVec = new Vector3();
            List<Vector3> nodes = new List<Vector3>();
            Vector3 prev = randomPoints[0];
            ParkPath p = new ParkPath(randomPoints, nodes);
            int currGoal = 0;
            p.nodes.Add(prev);

            for (int i = 0; i < 10; i++) {
                if (currGoal == p.points.Count)
                    break;

                float newX = prev.x + Random.Range(-0.1f, 0.1f);
                float newZ = prev.z + Random.Range(-0.1f, 0.1f);
                float newY = terrain.GetPosition(newX, newZ).y;
                tryVec = new Vector3(newX, newY, newZ);

                if (index >= 2) {
                    Vector3 prevprev = p.nodes[index - 2];
                    Vector3 dir = (p.points[currGoal] - prev).normalized;
                    float oldAng = Vector3.Angle(tryVec, prev);
                    Vector3 newDir = Quaternion.Euler(0, oldAng - Random.Range(-30f, 30f), 0) * dir;
                    tryVec = prev + newDir * Random.Range(0.1f, 0.3f);
                }

                if (blockArea.Contains(new Vector2(tryVec.x, tryVec.z))) {
                    if (CloseEnough(tryVec, p.points[currGoal])) {
                        p.nodes.Add(p.points[currGoal]);
                        index += 1;
                        visitedPoints += 1;
                        currGoal += 1;
                    }

                    else if ((tryVec - p.points[currGoal]).magnitude < (prev - p.points[currGoal]).magnitude && !p.nodes.Contains(tryVec)) {
                        p.nodes.Add(tryVec);
                        index += 1;
                    }

                    else if ((tryVec - p.points[currGoal]).magnitude > (prev - p.points[currGoal]).magnitude) {
                        tryVec = prev;
                    }
                    prev = tryVec;
                }
            }
            var mesh = Instantiate(step, p.nodes[0], Quaternion.identity).GetComponent<RoadMesh>();
            for (int i = 0; i < p.nodes.Count; i++) {
                mesh.Spline.AddPoint(p.nodes[i]);
            }
            mesh.GenerateRoadMesh();
            var filter = mesh.GetComponent<MeshFilter>();
            var collider = mesh.GetComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            Debug.DrawLine(randomPoints[0], randomPoints[1], Color.cyan, 1000);
        }
    }
    private bool CloseEnough(Vector3 start, Vector3 end) {
        return (end - start).magnitude < 0.3f;
    }

    private bool isValidDistance(List<Vector2> points, Polygon polygon, Vector2 point) {
        if (points.Count < 1)
            return true;

        foreach (Vector2 p in points) {
            if ((p - point).magnitude < 2)
                return false;
        }
        return true;
    }

    public static Vector2 getRandomPoint(Polygon p) {
        Vector2 vec = new Vector2();
        while (!p.Contains(vec))
            vec = new Vector2((Random.Range(p.points[0].x, p.points[p.points.Count - 1].x)), (Random.Range(p.points[0].y, p.points[p.points.Count - 1].y)));
        return vec;
    }
    void Start() {
    }
    void Update() {

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
        while (!(p.Contains(vec) && isValidDistance(points, p, vec)));
        return vec;
    }
    public void GeneratePlotPath(TerrainModel terrain, Block block, Plot plot) {
        List<Vector2> toPolygon = new List<Vector2>();
        List<Vector2> polygonPlot = new List<Vector2>();
        List<Vector2> randomPolyPoints = new List<Vector2>();
        List<Vector3> randomWorldPoints = new List<Vector3>();
        int stuck = 0;
        Vector3 prev = new Vector3();
        int index = 0;

        foreach (Vector3 edge in plot.vertices) {
            Vector3 realCoord = terrain.GetPosition(edge.x, edge.z);
            polygonPlot.Add(new Vector2(realCoord.x, realCoord.z));
        }
        int numberOfPoints = 3;
        Polygon plotArea = new Polygon(polygonPlot);
        if (plotArea.GetArea() < 16) {
            return;
        }
        if (plotArea.GetArea() > 20)
            numberOfPoints = 4;
        randomPolyPoints.Add(plotArea.points[Random.Range(0, plotArea.points.Count - 1)]);
        randomWorldPoints.Add(terrain.GetPosition(randomPolyPoints[0]));
        for (int i = 0; i < numberOfPoints; i++) {
            Vector2 pointToAdd = getRandomPoint(plotArea, randomPolyPoints);
            randomPolyPoints.Add(pointToAdd);
            randomWorldPoints.Add(terrain.GetPosition(pointToAdd));
        }
        randomPolyPoints.Add(randomPolyPoints[0]);
        prev = randomWorldPoints[0];
        randomWorldPoints.Add(prev);
        int pointsToVisit = randomWorldPoints.Count;
        Vector3 tryVec = new Vector3();
        List<Vector3> nodes = new List<Vector3>();
        ParkPath p = new ParkPath(randomWorldPoints, nodes);
        // List<Vector2> result = OrderNodes(randomPolyPoints,plotArea);
        int currGoal = 1;
        p.nodes.Add(prev);
        int count = 0;
        while (currGoal < pointsToVisit) {
            count++;
            if (count > 1000)
                break;

            if (stuck > 10) {
                p.points.RemoveAt(currGoal);
                pointsToVisit--;
            }

            float newX = prev.x + Random.Range(-0.1f, 0.1f);
            float newZ = prev.z + Random.Range(-0.1f, 0.1f);
            float newY = terrain.GetPosition(newX, newZ).y;
            tryVec = new Vector3(newX, newY, newZ);
            if (index >= 2) {
                Vector3 prevprev = p.nodes[index - 2];
                Vector3 dir = (p.points[currGoal] - prev).normalized;
                float oldAng = Vector3.Angle(tryVec, prev);
                Vector3 newDir = Quaternion.Euler(0, oldAng - Random.Range(-30f, 30f), 0) * dir;
                tryVec = prev + newDir * Random.Range(0.1f, 0.3f);

            }

            if (plotArea.Contains(new Vector2(tryVec.x, tryVec.z))) {
                if (CloseEnough(tryVec, p.points[currGoal])) {
                    p.nodes.Add(p.points[currGoal]);
                    index += 1;
                    currGoal += 1;
                    stuck = 0;
                }

                else if ((tryVec - p.points[currGoal]).magnitude < (prev - p.points[currGoal]).magnitude && !p.nodes.Contains(tryVec)) {
                    p.nodes.Add(tryVec);
                    stuck = 0;
                    index += 1;
                }

                else if ((tryVec - p.points[currGoal]).magnitude > (prev - p.points[currGoal]).magnitude) {
                    tryVec = prev;
                    stuck++;
                }
                prev = tryVec;
            }

        }

        var mesh = Instantiate(step, p.nodes[0], Quaternion.identity).GetComponent<RoadMesh>();
        if (p.points.Count > 3) {
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
}
