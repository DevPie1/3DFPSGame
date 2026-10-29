using UnityEngine;
using UnityEngine.UI;

// 해상도에 맞춰 원형 시야와 조준선을 그린다. HUD는 이 그래픽 위에 표시한다.
[RequireComponent(typeof(CanvasRenderer))]
public class UI_ScopeOverlay : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.38f;
        float outer = rect.size.magnitude;
        Ring(vh, center, radius, outer, Color.black);
        Color ink = new Color(0f, 0f, 0f, 0.9f);
        Ring(vh, center, radius * 0.75f, radius * 0.75f + 2f, ink);
        Ring(vh, center, radius * 0.48f, radius * 0.48f + 1.5f, ink);
        Line(vh, center + Vector2.left * radius, center + Vector2.right * radius, 2f, ink);
        Line(vh, center + Vector2.down * radius, center + Vector2.up * radius, 2f, ink);
        for (int i = -4; i <= 4; i++)
        {
            if (i == 0) continue;
            float offset = i * radius * 0.09f;
            float tick = radius * (i % 2 == 0 ? 0.035f : 0.02f);
            Line(vh, center + new Vector2(offset, -tick), center + new Vector2(offset, tick), 1.5f, ink);
        }
    }

    private static void Ring(VertexHelper vh, Vector2 center, float inner, float outer, Color color)
    {
        const int segments = 128;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Quad(vh, center + p * inner, center + p * outer, center + q * outer, center + q * inner, color);
        }
    }

    private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
        Quad(vh, a - normal, a + normal, b + normal, b - normal, color);
    }

    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.zero);
        vh.AddVert(c, color, Vector2.zero);
        vh.AddVert(d, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
