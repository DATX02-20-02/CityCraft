using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class ParkingGenerator : MonoBehaviour {
    [SerializeField] private GameObject whiteLine = null;
    [SerializeField] private GameObject square = null;
    [SerializeField] private GameObject lot = null;

    [SerializeField] private float lotWidth = 0.1f;
    [SerializeField] private float lotHeight = 0.2f;

    Rectangle rect;
    TerrainModel model;

    public void Reset() {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
    }

    public Rectangle Generate(TerrainModel terrain, Plot plot) {
        List<Vector2> polygon = plot.vertices.Select(VectorUtil.Vector3To2).ToList();

        rect = ApproximateLargestRectangle(polygon);
        if(rect.height < 0.4f || rect.width < 0.4f)
            return new Rectangle();

        var c = terrain.GetMeshIntersection(rect.Center.x, rect.Center.y);

        int xResolution = 16;
        int yResolution = 2;

        GameObject obj = Instantiate(lot, c.point, Quaternion.identity, transform);
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        Vector2 rectRightDir = new Vector2(Mathf.Cos(rect.angle), Mathf.Sin(rect.angle)).normalized;
        Vector2 rectUpDir = new Vector2(-Mathf.Sin(rect.angle), Mathf.Cos(rect.angle)).normalized;

        float quadWidth = Mathf.Floor(rect.width / lotWidth) * lotWidth;
        float quadHeight = lotHeight * 2;

        int amountOfLines = (int) Mathf.Max(1, Mathf.Floor(rect.height / (quadHeight * 2)));
        Debug.Log(quadWidth);

        Vector3[] meshVertices = new Vector3[(xResolution + 1) * (yResolution + 1) * amountOfLines];
        Vector2[] meshUVs = new Vector2[(xResolution + 1) * (yResolution + 1) * amountOfLines];
        int[] meshIndices = new int[xResolution * yResolution * 6 * amountOfLines];

        for (int i = 0; i < amountOfLines; i++) {
            int index = i * meshIndices.Length / amountOfLines;
            int meshIndexStart = i * meshVertices.Length / amountOfLines;

            Vector2 origin = Vector2.Lerp(rect.botLeft, rect.topLeft, (i + 1) * 1 / (float) (amountOfLines + 1));
            Debug.DrawLine(VectorUtil.Vector2To3(origin), VectorUtil.Vector2To3(origin) + Vector3.up, Color.green, 10);

            for (int y = 0; y < yResolution + 1; y++) {
                for (int x = 0; x < xResolution + 1; x++) {

                    Vector2 localPos = origin
                        + rectRightDir * (x * (quadWidth / xResolution) + rect.width / 2 - quadWidth / 2)
                        + rectUpDir * (y * (quadHeight / yResolution) - quadHeight / 2);

                    var intersection = terrain.GetMeshIntersection(localPos.x, localPos.y);
                    Vector3 pos = intersection.point + intersection.normal * 0.01f;

                    meshVertices[meshIndexStart + x + y * (xResolution + 1)] = obj.transform.InverseTransformPoint(pos);
                    meshUVs[meshIndexStart + x + y * (xResolution + 1)] = new Vector2(x / (float) xResolution * (quadWidth / lotWidth), y / (float) yResolution);

                    if (x < xResolution && y < yResolution)  {
                        meshIndices[index + 2] = meshIndexStart + x + y * (xResolution + 1);
                        meshIndices[index + 1] = meshIndexStart + (x + 1) + y * (xResolution + 1);
                        meshIndices[index + 0] = meshIndexStart + x + (y + 1) * (xResolution + 1);

                        meshIndices[index + 5] = meshIndexStart + (x + 1) + (y + 1) * (xResolution + 1);
                        meshIndices[index + 4] = meshIndexStart + x + (y + 1) * (xResolution + 1);
                        meshIndices[index + 3] = meshIndexStart + (x + 1) + y * (xResolution + 1);
                        index += 6;
                    }
                }
            }
        }

        for (int i = 0; i < meshIndices.Length; i+=3) {
            Debug.DrawLine(meshVertices[meshIndices[i]], meshVertices[meshIndices[i + 1]], Color.green, 10);
            Debug.DrawLine(meshVertices[meshIndices[i + 1]], meshVertices[meshIndices[i + 2]], Color.green, 10);
            Debug.DrawLine(meshVertices[meshIndices[i + 2]], meshVertices[meshIndices[i]], Color.green, 10);
        }

        mesh.vertices = meshVertices;
        mesh.triangles = meshIndices;
        mesh.uv = meshUVs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        this.model = terrain;

        return rect;
    }

    public Rectangle Generate2(TerrainModel terrain, Plot plot) {
        List<Vector2> polygon = plot.vertices.Select(VectorUtil.Vector3To2).ToList();

        var rect = ApproximateLargestRectangle(polygon);
        if(rect.height < 0.4f || rect.width < 0.4f)
            return new Rectangle();
        var c = terrain.GetMeshIntersection(rect.Center.x, rect.Center.y);

        Quaternion rot = Quaternion.Euler(0, -rect.angle * Mathf.Rad2Deg, 0);
        GameObject g = Instantiate(square, c.point + c.normal * 0.03f, Quaternion.FromToRotation(square.transform.up, c.normal) * rot, this.transform);
        g.transform.localScale = new Vector3(rect.width / 2 * 0.2f, g.transform.localScale.y, rect.height / 2 * 0.2f);
        Transform area = g.transform;
        float offset = 0.1f;
        float border = 0.2f;

        Vector2 rectRightDir = (rect.botRight - rect.botLeft).normalized;
        Vector2 rectUpDir = (rect.topLeft - rect.botLeft).normalized;
        Vector2 origin = rect.botLeft;

        if (rect.height / 2 < 0.4f) {
            origin = (rect.botLeft + rect.topLeft) / 2;
            border = 0;
        }
        while (offset <= (rect.width / 2) - border) {
            Vector2 localV1 = rectRightDir * (offset + border) + rectUpDir * (rect.height - border); // top left
            Vector2 localV2 = rectRightDir * (rect.width - offset - border) + rectUpDir * (rect.height - border); // top right
            Vector2 localV3 = rectRightDir * (offset + border) + rectUpDir * border;                 // bottom left
            Vector2 localV4 = rectRightDir * (rect.width - offset - border) + rectUpDir * border;                 // bottom right

            var v1 = terrain.GetMeshIntersection(origin.x + localV1.x, origin.y + localV1.y);
            var v2 = terrain.GetMeshIntersection(origin.x + localV2.x, origin.y + localV2.y);
            var v3 = terrain.GetMeshIntersection(origin.x + localV3.x, origin.y + localV3.y);
            var v4 = terrain.GetMeshIntersection(origin.x + localV4.x, origin.y + localV4.y);

            GameObject botLeftObj = Instantiate(whiteLine, v3.point + v3.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v3.normal) * rot, this.transform);
            botLeftObj.transform.Rotate(0, -90, 0);
            botLeftObj.transform.localScale = new Vector3(botLeftObj.transform.localScale.x, botLeftObj.transform.localScale.y, botLeftObj.transform.localScale.z / 4);
            botLeftObj.transform.SetParent(area);

            GameObject botRightObj = Instantiate(whiteLine, v4.point + v4.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v4.normal) * rot, this.transform);
            botRightObj.transform.Rotate(0, -90, 0);
            botRightObj.transform.localScale = new Vector3(botRightObj.transform.localScale.x, botRightObj.transform.localScale.y, botRightObj.transform.localScale.z / 4);
            botRightObj.transform.SetParent(area);

            if (rect.height / 2 >= 0.4f) {

                GameObject topLeftObj = Instantiate(whiteLine, v1.point + v1.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v1.normal) * rot, this.transform);
                topLeftObj.transform.Rotate(0, -90, 0);
                topLeftObj.transform.localScale = new Vector3(topLeftObj.transform.localScale.x, topLeftObj.transform.localScale.y, topLeftObj.transform.localScale.z / 4);
                topLeftObj.transform.SetParent(area);

                GameObject topRightObj = Instantiate(whiteLine, v2.point + v2.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v2.normal) * rot, this.transform);
                topRightObj.transform.Rotate(0, -90, 0);
                topRightObj.transform.localScale = new Vector3(topRightObj.transform.localScale.x, topRightObj.transform.localScale.y, topRightObj.transform.localScale.z / 4);
                topRightObj.transform.SetParent(area);

                GameObject topLeftVertical = Instantiate(whiteLine, v1.point + v1.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v1.normal) * rot, this.transform);
                topLeftVertical.transform.SetParent(area);
                
                GameObject topRightVertical = Instantiate(whiteLine, v2.point + v2.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v2.normal) * rot, this.transform);
                topRightVertical.transform.SetParent(area);

            }

            GameObject botLeftVertical = Instantiate(whiteLine, v3.point + v3.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v3.normal) * rot, this.transform);
            botLeftVertical.transform.SetParent(area);
            
            GameObject botRightVertical = Instantiate(whiteLine, v4.point + v4.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v4.normal) * rot, this.transform);
            botRightVertical.transform.SetParent(area);

            offset += 0.1f;
        }
        /*  tl--tm--tr
            |---|---|
            |---|---|
            bl--bm---br
        */
        return rect;
    }

    public Rectangle ApproximateLargestRectangle(List<Vector2> polygon) {
        return Utils.PolygonUtil.ApproximateLargestRectangle(polygon, Random.Range(1.0f, 3.0f), 0.1f, 6, 10, 10);
    }
}
