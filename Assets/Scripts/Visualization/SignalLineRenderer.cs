using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI üzerinde sürekli çizgi çizen custom Graphic.
/// Nokta dizisini alır, aralarına kalın çizgi segmentleri çizer.
/// WaveformArea RectTransform içine konumlanır.
/// </summary>
public class SignalLineRenderer : Graphic
{
    [HideInInspector] public int   resolution    = 128;
    [HideInInspector] public float lineThickness = 2.5f;
    [HideInInspector] public Color lineColor     = Color.cyan;

    private Vector2[] points;
    private RectTransform containerRect;

    public void SetContainer(RectTransform container)
    {
        containerRect = container;

        // Bu bileşeni container'ın altına taşı
        transform.SetParent(container, false);
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        raycastTarget = false;
    }

    public void SetPoints(Vector2[] newPoints, Color col, float thickness)
    {
        points        = newPoints;
        lineColor     = col;
        lineThickness = thickness;
        color         = col;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Length < 2) return;

        Rect r = rectTransform.rect;

        for (int i = 0; i < points.Length - 1; i++)
        {
            // Normalize (0-1) → rect koordinatına çevir
            Vector2 a = new Vector2(
                r.x + points[i].x * r.width,
                r.y + points[i].y * r.height);
            Vector2 b = new Vector2(
                r.x + points[i+1].x * r.width,
                r.y + points[i+1].y * r.height);

            // Çizgi yönü ve dik vektörü
            Vector2 dir  = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * (lineThickness * 0.5f);

            // 4 köşe — ince dikdörtgen segment
            int baseIdx = i * 4;
            vh.AddVert(new Vector3(a.x - perp.x, a.y - perp.y), lineColor, Vector2.zero);
            vh.AddVert(new Vector3(a.x + perp.x, a.y + perp.y), lineColor, Vector2.one);
            vh.AddVert(new Vector3(b.x + perp.x, b.y + perp.y), lineColor, Vector2.one);
            vh.AddVert(new Vector3(b.x - perp.x, b.y - perp.y), lineColor, Vector2.zero);

            vh.AddTriangle(baseIdx,     baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx,     baseIdx + 2, baseIdx + 3);
        }
    }
}
