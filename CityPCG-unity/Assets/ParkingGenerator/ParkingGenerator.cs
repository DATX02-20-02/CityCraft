using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class ParkingGenerator : MonoBehaviour {
    [SerializeField] private GameObject whiteLine = null;
    [SerializeField] private GameObject square = null;
    public void Generate(TerrainModel terrain, Plot plot) {
        List<Vector2> polygon = new List<Vector2>();
        Vector3 center = Vector3.zero;
        foreach (var v in plot.vertices) {
            center += v;
            polygon.Add(VectorUtil.Vector3To2(v));
        }
        center /= plot.vertices.Count;
        var c = terrain.GetMeshIntersection(center.x, center.z);
        var rect = ApproximateLargestRectangle(polygon);
        Quaternion rot = Quaternion.Euler(0, -rect.angle * Mathf.Rad2Deg, 0);
        GameObject g = Instantiate(square, c.point + c.normal * 0.03f, Quaternion.FromToRotation(square.transform.up, c.normal) * rot, this.transform);
        g.transform.localScale = new Vector3(rect.width * 0.2f, g.transform.localScale.y, rect.height * 0.2f);
        float offset = 0.1f;
        float border = 0.2f;
        Vector2 rectRightDir = (rect.botRight - rect.botLeft).normalized;
        Vector2 rectUpDir = (rect.topLeft - rect.botLeft).normalized;
        Vector2 origin = rect.botLeft;

        if (rect.height < 0.4f) {
            origin = (rect.botLeft + rect.topLeft) / 2;
            border = 0;
        }
        while (offset <= rect.width - border) {
            Vector2 localV1 = rectRightDir * (offset + border) + rectUpDir * (rect.height * 2 - border); // top left
            Vector2 localV2 = rectRightDir * (rect.width * 2 - offset - border) + rectUpDir * (rect.height * 2 - border); // top right
            Vector2 localV3 = rectRightDir * (offset + border) + rectUpDir * border;                 // bottom left
            Vector2 localV4 = rectRightDir * (rect.width * 2 - offset - border) + rectUpDir * border;                 // bottom right

            var v1 = terrain.GetMeshIntersection(origin.x + localV1.x, origin.y + localV1.y);
            var v2 = terrain.GetMeshIntersection(origin.x + localV2.x, origin.y + localV2.y);
            var v3 = terrain.GetMeshIntersection(origin.x + localV3.x, origin.y + localV3.y);
            var v4 = terrain.GetMeshIntersection(origin.x + localV4.x, origin.y + localV4.y);

            GameObject lowRightObj = Instantiate(whiteLine, v3.point + v3.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v3.normal) * rot, this.transform);
            lowRightObj.transform.Rotate(0, -90, 0);
            lowRightObj.transform.localScale = new Vector3(lowRightObj.transform.localScale.x, lowRightObj.transform.localScale.y, lowRightObj.transform.localScale.z / 4);

            GameObject lowLeftObj = Instantiate(whiteLine, v4.point + v4.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v4.normal) * rot, this.transform);
            lowLeftObj.transform.Rotate(0, -90, 0);
            lowLeftObj.transform.localScale = new Vector3(lowLeftObj.transform.localScale.x, lowLeftObj.transform.localScale.y, lowLeftObj.transform.localScale.z / 4);

            if (rect.height >= 0.4f) {

                GameObject topLeftObj = Instantiate(whiteLine, v1.point + v1.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v1.normal) * rot, this.transform);
                topLeftObj.transform.Rotate(0, -90, 0);
                topLeftObj.transform.localScale = new Vector3(topLeftObj.transform.localScale.x, topLeftObj.transform.localScale.y, topLeftObj.transform.localScale.z / 4);

                GameObject topRightObj = Instantiate(whiteLine, v2.point + v2.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v2.normal) * rot, this.transform);
                topRightObj.transform.Rotate(0, -90, 0);
                topRightObj.transform.localScale = new Vector3(topRightObj.transform.localScale.x, topRightObj.transform.localScale.y, topRightObj.transform.localScale.z / 4);

                Instantiate(whiteLine, v1.point + v1.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v1.normal) * rot, this.transform);
                Instantiate(whiteLine, v2.point + v2.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v2.normal) * rot, this.transform);

            }

            Instantiate(whiteLine, v3.point + v3.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v3.normal) * rot, this.transform);
            Instantiate(whiteLine, v4.point + v4.normal * 0.05f, Quaternion.FromToRotation(whiteLine.transform.up, v4.normal) * rot, this.transform);

            offset += 0.1f;
        }
        /*  tl--tm--tr
            |---|---|
            |---|---|
            bl--bm---br
        */
    }
    private Rectangle ApproximateLargestRectangle(List<Vector2> polygon) {
        return Utils.PolygonUtil.ApproximateLargestRectangle(polygon, Random.Range(1.0f, 3.0f), 0.1f, 6, 10);
    }

}
