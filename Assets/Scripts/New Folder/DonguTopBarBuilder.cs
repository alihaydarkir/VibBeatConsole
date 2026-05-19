using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas kökünde "DonguBar" paneli oluşturur.
/// Kullanım: Herhangi bir GameObject'e ekle → Inspector sağ tık → "Build Dongu Bar"
/// Oluşan paneli MainConsoleScreen altına taşı, sonra bu scripti sil.
///
/// Bootstrap şu isimleri arar:
///   LoopRecordButton    → kayıt başlat   (başlangıçta Active)
///   LoopStopRecButton   → kayıt durdur   (başlangıçta Inactive)
///   LoopPlayButton      → loop oynat     (başlangıçta Active)
///   LoopStopPlayButton  → loop durdur    (başlangıçta Inactive)
///   DecreaseButton      → BPM azalt
///   IncreaseButton      → BPM artır
///   BPMText             → BPM göstergesi
///
/// Her buton altında "Icon" Image'ı var — sprite'ını Inspector'dan ata.
/// </summary>
public class DonguTopBarBuilder : MonoBehaviour
{
    [ContextMenu("Build Dongu Bar")]
    public void Build()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[DONGU] Canvas bulunamadı!"); return; }

        var old = canvas.transform.Find("DonguBar");
        if (old != null) DestroyImmediate(old.gameObject);

        BuildBar(canvas.transform);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[DONGU] DonguBar oluşturuldu — MainConsoleScreen altına taşı, Ctrl+S.");
#endif
    }

    private void BuildBar(Transform parent)
    {
        var navy     = Hex("#0B1624");
        var cyan     = Hex("#00E5FF");
        var green    = Hex("#1A6B3A");
        var greenDim = Hex("#0C3D20");
        var red      = Hex("#7A1A1A");
        var redDim   = Hex("#3E0C0C");
        var btnGray  = Hex("#1A2D40");
        var white    = Hex("#EEEEFF");
        var titleCol = new Color(0.9f, 0.92f, 1f, 0.32f);

        // ── Kök panel ──────────────────────────────────────────────────────────
        var root = new GameObject("DonguBar");
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.90f);
        rt.anchorMax = new Vector2(1f, 1.00f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = navy;

        var t = root.transform;

        MakeRect("TopGlow",    t, new Color(cyan.r, cyan.g, cyan.b, 0.55f), 0f, 1f, 0.93f, 1f);
        MakeRect("BottomLine", t, new Color(1f, 1f, 1f, 0.05f),             0f, 1f, 0f,    0.03f);

        float by0 = 0.12f, by1 = 0.88f;

        // ── SOL: KAYIT ──────────────────────────────────────────────────────────
        // Kayıt başlat — başlangıçta görünür
        MakeIconBtn("LoopRecordButton",  t, red,    0.010f, 0.115f, by0, by1, "REC",  white, active: true);
        // Kayıt durdur — başlangıçta gizli
        MakeIconBtn("LoopStopRecButton", t, redDim, 0.010f, 0.115f, by0, by1, "STOP", white, active: false);

        MakeRect("Divider1", t, new Color(cyan.r, cyan.g, cyan.b, 0.12f), 0.120f, 0.124f, 0.20f, 0.80f);

        // ── SOL: OYNAT ──────────────────────────────────────────────────────────
        // Loop oynat — başlangıçta görünür
        MakeIconBtn("LoopPlayButton",     t, green,    0.130f, 0.235f, by0, by1, "PLAY", white, active: true);
        // Loop durdur — başlangıçta gizli
        MakeIconBtn("LoopStopPlayButton", t, greenDim, 0.130f, 0.235f, by0, by1, "STOP", white, active: false);

        // ── MERKEZ: başlık ───────────────────────────────────────────────────────
        MakeTMP("TitleText", t, "DONGU", 20, titleCol,
            0.30f, 0.70f, 0.08f, 0.92f, bold: true, spacing: 10f);

        // ── SAĞ: BPM ────────────────────────────────────────────────────────────
        MakeRect("BPMGroup", t, new Color(0.06f, 0.12f, 0.20f, 0.85f), 0.720f, 0.990f, 0.10f, 0.90f);

        var decGO = MakeRect("DecreaseButton", t, btnGray, 0.725f, 0.790f, by0, by1);
        decGO.AddComponent<Button>().transition = Selectable.Transition.None;
        MakeTMP("Label", decGO.transform, "-", 22, cyan, 0f, 1f, 0f, 1f, bold: true);

        MakeTMP("BPMText", t, "120 BPM", 14, cyan, 0.790f, 0.925f, 0.08f, 0.92f, bold: true);

        var incGO = MakeRect("IncreaseButton", t, btnGray, 0.928f, 0.988f, by0, by1);
        incGO.AddComponent<Button>().transition = Selectable.Transition.None;
        MakeTMP("Label", incGO.transform, "+", 22, cyan, 0f, 1f, 0f, 1f, bold: true);
    }

    // Buton + Icon Image (sprite placeholder) + Label metin
    private static GameObject MakeIconBtn(string name, Transform parent, Color bg,
        float x0, float x1, float y0, float y1, string label, Color textCol, bool active)
    {
        var go  = MakeRect(name, parent, bg, x0, x1, y0, y1);
        go.AddComponent<Button>().transition = Selectable.Transition.None;

        // Icon — sprite'ını Inspector'dan ata
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.15f, 0.15f);
        iconRT.anchorMax = new Vector2(0.85f, 0.85f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        iconGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // şeffaf — sprite atayınca görünür

        // Label — yedek metin
        MakeTMP("Label", go.transform, label, 11,
            new Color(textCol.r, textCol.g, textCol.b, 0.55f),
            0f, 1f, 0f, 0.35f, bold: true);

        go.SetActive(active);
        return go;
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
        float size, Color col, float x0, float x1, float y0, float y1,
        bool bold = false, float spacing = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = size;
        tmp.color            = col;
        tmp.fontStyle        = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment        = TextAlignmentOptions.Midline;
        tmp.raycastTarget    = false;
        tmp.characterSpacing = spacing;
        return tmp;
    }

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }
}
