using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class RealTimeGraph : Graphic
{
    public Color lineColor = Color.yellow;
    public float thickness = 3f;
    public float minTemp = 30f;
    public float maxTemp = 38f;
    public float totalTimeSpan = 720f;

    private List<Vector2> points = new List<Vector2>();

    public void AddDataPoint(float timeInMinutes, float temperature)
    {
        // 儲存 0~1 的標準化比例
        float x = Mathf.Clamp01(timeInMinutes / totalTimeSpan);
        float y = Mathf.InverseLerp(minTemp, maxTemp, temperature);
        points.Add(new Vector2(x, y));
        SetVerticesDirty();
    }

    public void ClearGraph()
    {
        points.Clear();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        // 在 RealTimeGraph.cs 裡的 OnPopulateMesh 修正
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // 不論 Pivot 在哪，這行會強制找到 UI 框的最左邊緣（相對於 Pivot 的偏移量）
        float xStartOffset = rectTransform.pivot.x * width;
        float yStartOffset = -rectTransform.pivot.y * height;

        for (int i = 0; i < points.Count - 1; i++)
        {
            // 起點 = 左邊緣偏移量 + (比例 * 寬度)
            Vector2 start = new Vector2(xStartOffset - points[i].x * width, yStartOffset + points[i].y * height);
            Vector2 end = new Vector2(xStartOffset - points[i + 1].x * width, yStartOffset + points[i + 1].y * height);
            DrawLine(start, end, vh);
            Debug.Log($"繪製線段：起點({start.x}, {start.y}) -> 終點({end.x}, {end.y})");
        }
    }

    void DrawLine(Vector2 start, Vector2 end, VertexHelper vh)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;
        UIVertex v = UIVertex.simpleVert;
        v.color = lineColor;
        v.position = start - normal; vh.AddVert(v);
        v.position = start + normal; vh.AddVert(v);
        v.position = end + normal; vh.AddVert(v);
        v.position = end - normal; vh.AddVert(v);
        int index = vh.currentVertCount;
        vh.AddTriangle(index - 4, index - 3, index - 2);
        vh.AddTriangle(index - 2, index - 1, index - 4);
    }
}