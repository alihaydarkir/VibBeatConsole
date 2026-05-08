using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sprite gerektirmeden kod ile daire cizen UI bileşeni.
/// Image yerine kullanilir — OnPopulateMesh override ederek
/// verilen segmentSayisi kadar ucgen ile dolu daire uretir.
///
/// RippleEffect tarafindan kullanilir.
/// </summary>
public class CircleImage : Image
{
    [Range(16, 128)]
    [SerializeField] private int segments = 64;  // daire yumusakligi

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float w = rectTransform.rect.width  * 0.5f;
        float h = rectTransform.rect.height * 0.5f;
        float r = Mathf.Min(w, h);  // kare degilse kucuk kenarı kullan

        // Merkez vertex
        UIVertex center = UIVertex.simpleVert;
        center.color    = color;
        center.position = Vector3.zero;
        center.uv0      = new Vector2(0.5f, 0.5f);
        vh.AddVert(center);

        // Cevre vertexleri
        float angleStep = 360f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float   angle = i * angleStep * Mathf.Deg2Rad;
            float   x     = Mathf.Sin(angle) * r;
            float   y     = Mathf.Cos(angle) * r;

            UIVertex v = UIVertex.simpleVert;
            v.color    = color;
            v.position = new Vector3(x, y, 0f);
            v.uv0      = new Vector2(x / r * 0.5f + 0.5f, y / r * 0.5f + 0.5f);
            vh.AddVert(v);
        }

        // Ucgenler: merkez(0) + cevre[i] + cevre[i+1]
        for (int i = 1; i <= segments; i++)
        {
            vh.AddTriangle(0, i, i + 1 > segments ? 1 : i + 1);
        }
    }
}
