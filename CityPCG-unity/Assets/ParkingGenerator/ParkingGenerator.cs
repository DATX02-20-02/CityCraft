using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class ParkingGenerator : MonoBehaviour {
    [SerializeField] private GameObject whiteLine = null;
    [SerializeField] private GameObject square = null;

    public void Reset() {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
    }

    public Rectangle Generate(TerrainModel terrain, Plot plot) {
        List<Vector2> polygon = plot.vertices.Select(VectorUtil.Vector3To2).ToList();

        var rect = ApproximateLargestRectangle(polygon);
        if (rect.height < 0.4f || rect.width < 0.4f)
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
