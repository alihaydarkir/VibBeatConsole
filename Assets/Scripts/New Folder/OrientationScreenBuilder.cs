using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "OrientationScreen" adlı ekranı Canvas kökünde oluşturur.
/// Kullanım: Herhangi bir GameObject'e ekle → Inspector sağ tık → "Build Orientation Screen"
/// Oluşan ekranı UICanvas (Bootstrap'ın bulunduğu obje) altına taşı.
/// RotateIcon Image'ına kendi sprite'ını ata.
/// </summary>
public class OrientationScreenBuilder : MonoBehaviour
{
    [ContextMenu("Build Orientation Screen")]
    public void Build()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[ORIENT] Canvas bulunamadı!"); return; }

        var old = canvas.transform.Find("OrientationScreen");
        if (old != null) DestroyImmediate(old.gameObject);

        BuildScreen(canvas.transform);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ORIENT] OrientationScreen oluşturuldu — Bootstrap'ın parent'ı altına taşı, Ctrl+S.");
#endif
    }

    private void BuildScreen(Transform parent)
    {
        var bg       = Hex("#080E18");
        var cyan     = Hex("#00E5FF");
        var white    = Hex("#EEEEFF");
        var dimWhite = new Color(1f, 1f, 1f, 0.45f);

        // ── Kök ──────────────────────────────────────────────────────────────
        var root = new GameObject("OrientationScreen");
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = bg;

        var t = root.transform;

        // ── Hafif ızgara deseni ───────────────────────────────────────────────
        MakeRect("GridOverlay", t, new Color(0f, 0.9f, 1f, 0.03f), 0f, 1f, 0f, 1f);

        // ── Döndür ikonu (sprite placeholder) ───────────────────────────────
        var iconGO = new GameObject("RotateIcon");
        iconGO.transform.SetParent(t, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.35f, 0.50f);
        iconRT.anchorMax = new Vector2(0.65f, 0.78f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        iconGO.AddComponent<Image>().color = new Color(cyan.r, cyan.g, cyan.b, 0.9f);

        // ── Başlık ────────────────────────────────────────────────────────────
        MakeTMP("TitleText", t, "Telefonu Yan Cevir", 26, white,
            0.05f, 0.95f, 0.34f, 0.50f, bold: true);

        // ── Alt açıklama ──────────────────────────────────────────────────────
        MakeTMP("SubText", t, "VibBeat yatay modda calisir", 16, dimWhite,
            0.10f, 0.90f, 0.26f, 0.34f);

        // ── Devam butonu ──────────────────────────────────────────────────────
        var btnGO = MakeRect("ContinueButton", t,
            new Color(cyan.r, cyan.g, cyan.b, 0.15f),
            0.25f, 0.75f, 0.10f, 0.22f);
        btnGO.AddComponent<Button>().transition = Selectable.Transition.None;

        // Buton kenarlığı
        MakeRect("Border", btnGO.transform, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
            0f, 1f, 0.92f, 1f);
        MakeRect("BorderB", btnGO.transform, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
            0f, 1f, 0f, 0.08f);

        MakeTMP("Label", btnGO.transform, "DEVAM", 18, cyan,
            0f, 1f, 0f, 1f, bold: true);
    }

    private static GameObject MakeRect(string name, Transform parent, Color col,
        float x0, float x1, float y0, float y1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = col;
        return go;
    }

    private static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, Color col, float x0, float x1, float y0, float y1, bool bold = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.color         = col;
        tmp.fontStyle     = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }
}
