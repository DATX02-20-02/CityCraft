using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingGenerator : MonoBehaviour {
    [SerializeField] private GameObject whiteLine;
    void Start() {
    }

    public void Generate(TerrainModel terrain, Plot plot) {
        List<Vector2> polygon = new List<Vector2>();
        Vector3 center = Vector3.zero;
        foreach (var v in plot.vertices) {
            center += v;
            polygon.Add(VectorUtil.Vector3To2(v));
        }
        center /= plot.vertices.Count;

        var rect = ApproximateLargestRectangle(polygon);

        transform.position = center;

        rect.topLeft.x = terrain.GetPosition(rect.topLeft).x;
        rect.topRight.x = terrain.GetPosition(rect.topRight).x;
        rect.botLeft.x = terrain.GetPosition(rect.botLeft).x;
        rect.botRight.x = terrain.GetPosition(rect.botRight).x;

        rect.topLeft.y = terrain.GetPosition(rect.topLeft).z;
        rect.topRight.y = terrain.GetPosition(rect.topRight).z;
        rect.botLeft.y = terrain.GetPosition(rect.botLeft).z;
        rect.botRight.y = terrain.GetPosition(rect.botRight).z;

        float offset = 0.1f;
        float border = Mathf.Abs(rect.topRight.x - rect.topLeft.x) / 4;
        Debug.DrawLine(terrain.GetPosition(rect.topLeft), terrain.GetPosition(rect.topRight), Color.cyan, 1000);
        Debug.DrawLine(terrain.GetPosition(rect.topRight), terrain.GetPosition(rect.botRight), Color.cyan, 1000);
        Debug.DrawLine(terrain.GetPosition(rect.botRight), terrain.GetPosition(rect.botLeft), Color.cyan, 1000);
        Debug.DrawLine(terrain.GetPosition(rect.botLeft), terrain.GetPosition(rect.topLeft), Color.cyan, 1000);

        Debug.DrawLine(new Vector3(rect.topLeft.x, terrain.GetPosition(rect.topLeft).y, rect.topLeft.y), new Vector3(rect.topRight.x, terrain.GetPosition(rect.botLeft).y, rect.topLeft.y), Color.white, 10000);
        Debug.DrawLine(new Vector3(rect.botRight.x, terrain.GetPosition(rect.topRight).y, rect.botRight.y), new Vector3(rect.botLeft.x, terrain.GetPosition(rect.botRight).y, rect.botRight.y), Color.white, 10000);
        Vector3 top = new Vector3(rect.topLeft.x, terrain.GetPosition(rect.topLeft).y, rect.topLeft.y);
        Vector3 bot = new Vector3(rect.botRight.x, terrain.GetPosition(rect.topRight).y, rect.botRight.y);
        // GameObject g = Instantiate(whiteLine,transform);
        //  g.transform.localScale = new Vector3(5f,0.1f,2f);
        Instantiate(whiteLine, top, Quaternion.identity);

        Instantiate(whiteLine, bot, Quaternion.identity);

        while (offset <= rect.width) {
            Debug.DrawLine(new Vector3(rect.topLeft.x + offset, terrain.GetPosition(rect.topLeft).y, rect.topLeft.y + 0.2f), new Vector3(rect.botLeft.x + offset, terrain.GetPosition(rect.botLeft).y, rect.topLeft.y - 0.2f), Color.white, 10000);
            Debug.DrawLine(new Vector3(rect.topRight.x - offset, terrain.GetPosition(rect.topRight).y, rect.topRight.y + 0.2f), new Vector3(rect.botRight.x - offset, terrain.GetPosition(rect.botRight).y, rect.topRight.y - 0.2f), Color.white, 10000);

            Debug.DrawLine(new Vector3(rect.topLeft.x + offset, terrain.GetPosition(rect.topLeft).y, rect.botLeft.y + 0.2f), new Vector3(rect.botLeft.x + offset, terrain.GetPosition(rect.botLeft).y, rect.botLeft.y - 0.2f), Color.white, 10000);
            Vector3 v1 = new Vector3(rect.topLeft.x + offset, terrain.GetPosition(rect.topLeft).y, rect.topLeft.y);
            Vector3 v2 = new Vector3(rect.topRight.x - offset, terrain.GetPosition(rect.topRight).y, rect.topRight.y);
            Vector3 v3 = new Vector3(rect.botRight.x - offset, terrain.GetPosition(rect.botRight).y, rect.botRight.y);
            Vector3 v4 = new Vector3(rect.botLeft.x + offset, terrain.GetPosition(rect.botLeft).y, rect.botLeft.y);
            Instantiate(whiteLine, v1, Quaternion.identity);
            Instantiate(whiteLine, v2, Quaternion.identity);
            Instantiate(whiteLine, v3, Quaternion.identity);
            Instantiate(whiteLine, v4, Quaternion.identity);
            Debug.DrawLine(new Vector3(rect.topRight.x - offset, terrain.GetPosition(rect.topRight).y, rect.botRight.y + 0.2f), new Vector3(rect.botRight.x - offset, terrain.GetPosition(rect.botRight).y, rect.botRight.y - 0.2f), Color.white, 10000);
            offset += 0.1f;
        }


        /*   Debug.DrawLine(new Vector3(rect.topLeft.x + border,terrain.GetPosition(rect.topLeft).y, zHigh ),new Vector3(rect.topLeft.x + border,terrain.GetPosition(rect.topLeft).y,zHigh - zHigh /2.5f),Color.white,10000);
           Debug.DrawLine(new Vector3(rect.botLeft.x + border,terrain.GetPosition(rect.botLeft).y, zLow),new Vector3(rect.topLeft.x + border,terrain.GetPosition(rect.botLeft).y,zLow + zHigh/2.5f),Color.white,10000);
           Debug.DrawLine(new Vector3(rect.topRight.x - border,terrain.GetPosition(rect.topRight).y, zHigh ),new Vector3(rect.topRight.x - border,terrain.GetPosition(rect.topRight).y,zHigh - zHigh/2.5f),Color.white,10000);
           Debug.DrawLine(new Vector3(rect.botRight.x - border,terrain.GetPosition(rect.botRight).y, zLow),new Vector3(rect.topRight.x - border,terrain.GetPosition(rect.botRight).y,zLow + zHigh/2.5f),Color.white,10000);

           while(zHigh >= rect.topLeft.y - rect.topLeft.y/2.5f) {
               Debug.DrawLine(new Vector3(rect.topLeft.x + border-offset,terrain.GetPosition(rect.topLeft).y,zHigh), new Vector3(rect.topLeft.x + border + offset,terrain.GetPosition(rect.topLeft).y,zHigh),Color.white,1000);
               Debug.DrawLine(new Vector3(rect.topRight.x - border-offset,terrain.GetPosition(rect.topRight).y,zHigh), new Vector3(rect.topRight.x - border + offset,terrain.GetPosition(rect.topRight).y,zHigh),Color.white,1000);
               zHigh -= 4;
           }
           while(zLow <= rect.botLeft.y + rect.topLeft.y/2.5f) {
               Debug.DrawLine(new Vector3(rect.botLeft.x + border-offset,terrain.GetPosition(rect.botLeft).y,zLow), new Vector3(rect.botLeft.x + border + offset,terrain.GetPosition(rect.botLeft).y,zLow),Color.white,1000);
               Debug.DrawLine(new Vector3(rect.botRight.x - border-offset,terrain.GetPosition(rect.botRight).y,zLow), new Vector3(rect.botRight.x - border + offset,terrain.GetPosition(rect.botRight).y,zLow),Color.white,1000);
               zLow += 4;
           } */



        /*  tl--tm--tr
            |---|---|
            |---|---|
            bl--bm---br
        */
    }
    private Rectangle ApproximateLargestRectangle(List<Vector2> polygon) {
        return Utils.PolygonUtil.ApproximateLargestRectangle(polygon, Random.Range(1.0f, 5.0f), 0.25f, 10, 14);
    }

}
