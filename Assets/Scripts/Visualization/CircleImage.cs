using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ici bos halka (ring) cizen UI bileseni.
/// InnerRadius ile dis yaricap arasindaki alan dolu, ici bos.
/// RippleEffect tarafindan kullanilir.
/// </summary>
public class CircleImage : Image
{
    [Range(32, 128)]
    public int segments = 64;

    [Range(0f, 1f)]
    [Tooltip("0 = tam dolu daire, 0.9 = ince halka, 0.7 = orta kalinlik")]
    public float innerRadius = 0.75f;  // dis yaricapin yuzde kaci ic yaricap

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float w = rectTransform.rect.width  * 0.5f;
        float h = rectTransform.rect.height * 0.5f;
        float outerR = Mathf.Min(w, h);
        float innerR = outerR * innerRadius;

        float angleStep = 360f / segments * Mathf.Deg2Rad;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i       * angleStep;
            float a1 = (i + 1) * angleStep;

            // Dis cevre
            Vector2 outerA = new Vector2(Mathf.Sin(a0) * outerR, Mathf.Cos(a0) * outerR);
            Vector2 outerB = new Vector2(Mathf.Sin(a1) * outerR, Mathf.Cos(a1) * outerR);
            // Ic cevre
            Vector2 innerA = new Vector2(Mathf.Sin(a0) * innerR, Mathf.Cos(a0) * innerR);
            Vector2 innerB = new Vector2(Mathf.Sin(a1) * innerR, Mathf.Cos(a1) * innerR);

            int base_ = i * 4;
            vh.AddVert(ToVert(outerA)); // 0 dis sol
            vh.AddVert(ToVert(outerB)); // 1 dis sag
            vh.AddVert(ToVert(innerB)); // 2 ic sag
            vh.AddVert(ToVert(innerA)); // 3 ic sol

            // Iki ucgen = bir trapez dilimi
            vh.AddTriangle(base_,     base_ + 1, base_ + 2);
            vh.AddTriangle(base_,     base_ + 2, base_ + 3);
        }
    }

    private UIVertex ToVert(Vector2 pos)
    {
        UIVertex v = UIVertex.simpleVert;
        v.color    = color;
        v.position = new Vector3(pos.x, pos.y, 0f);
        v.uv0      = new Vector2(pos.x + 0.5f, pos.y + 0.5f);
        return v;
    }
}
