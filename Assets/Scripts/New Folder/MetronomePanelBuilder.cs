using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainConsoleScreen altına MetronomePanel oluşturur.
/// Kullanım: Bu scripti herhangi bir GameObject'e ekle →
///           Inspector'da sağ tık → "Build Metronome Panel"
/// Oluşturulduktan sonra bu scripti sil.
/// </summary>
public class MetronomePanelBuilder : MonoBehaviour
{
    [ContextMenu("Build Metronome Panel")]
    public void Build()
    {
        var main = GameObject.Find("MainConsoleScreen");
        if (main == null)
        {
            Debug.LogError("[METRO BUILDER] MainConsoleScreen bulunamadı!");
            return;
        }

        // Varsa önce sil
        var old = main.transform.Find("MetronomePanel");
        if (old != null) DestroyImmediate(old.gameObject);

        BuildPanel(main.transform);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[METRO BUILDER] MetronomePanel oluşturuldu — Ctrl+S ile kaydet.");
#endif
    }

    private void BuildPanel(Transform parent)
    {
        // ── Renkler ──────────────────────────────────────────────────────
        var cyan    = Hex("#00E5FF");
        var bgCard  = Hex("#111C2A");
        var bgDark  = Hex("#0D1520");
        var txtGray = Hex("#7A9AB5");
        var white   = Hex("#EEEEFF");

        // ── MetronomePanel — sağ alt köşe, GuitarPanel'in altı ──────────
        // Konumu sen ayarla — burası başlangıç pozisyonu
        var panel = MakeRect("MetronomePanel", parent, bgCard,
            0.01f, 0.38f, 0.01f, 0.22f);
        panel.GetComponent<Image>().color = new Color(0.07f, 0.11f, 0.17f, 0.95f);

        // İnce üst çizgi
        MakeRect("TopLine", panel.transform, new Color(cyan.r, cyan.g, cyan.b, 0.4f),
            0f, 1f, 0.93f, 1f);

        // ── Başlık ───────────────────────────────────────────────────────
        MakeTMP("TitleText", panel.transform, "LOOP", 16, txtGray,
            0.03f, 0.55f, 0.72f, 0.96f, bold: true);

        // ── BPM Göstergesi ───────────────────────────────────────────────
        MakeTMP("BPMText", panel.transform, "120 BPM", 22, cyan,
            0.55f, 0.98f, 0.68f, 0.96f, bold: true);

        // ── Kayıt Başlat (REC) — başlangıçta görünür ─────────────────────
        var recGO  = MakeRect("RecordButton", panel.transform,
            new Color(0.55f, 0.08f, 0.08f, 1f), 0.03f, 0.48f, 0.36f, 0.65f);
        var recBtn = recGO.AddComponent<Button>();
        recBtn.transition = Selectable.Transition.None;
        MakeTMP("Label", recGO.transform, "REC", 15, white,
            0f, 1f, 0f, 1f, bold: true);

        // ── Kayıt Durdur (STOP REC) — başlangıçta gizli ──────────────────
        var stopRecGO  = MakeRect("StopRecButton", panel.transform,
            new Color(0.7f, 0.12f, 0.12f, 1f), 0.03f, 0.48f, 0.36f, 0.65f);
        var stopRecBtn = stopRecGO.AddComponent<Button>();
        stopRecBtn.transition = Selectable.Transition.None;
        MakeTMP("Label", stopRecGO.transform, "STOP", 15, white,
            0f, 1f, 0f, 1f, bold: true);
        stopRecGO.SetActive(false);

        // ── − Butonu ──────────────────────────────────────────────────────
        var decGO  = MakeRect("DecreaseButton", panel.transform,
            bgDark, 0.50f, 0.72f, 0.36f, 0.65f);
        var decBtn = decGO.AddComponent<Button>();
        decBtn.transition = Selectable.Transition.None;
        MakeTMP("Text (TMP)", decGO.transform, "-", 28, cyan,
            0f, 1f, 0f, 1f, bold: true);

        // ── + Butonu ──────────────────────────────────────────────────────
        var incGO  = MakeRect("IncreaseButton", panel.transform,
            bgDark, 0.75f, 0.97f, 0.36f, 0.65f);
        var incBtn = incGO.AddComponent<Button>();
        incBtn.transition = Selectable.Transition.None;
        MakeTMP("Text (TMP)", incGO.transform, "+", 28, cyan,
            0f, 1f, 0f, 1f, bold: true);

        // ── Oynat (PLAY) — başlangıçta görünür ───────────────────────────
        var playGO  = MakeRect("PlayButton", panel.transform,
            new Color(0.05f, 0.38f, 0.18f, 1f), 0.03f, 0.97f, 0.06f, 0.32f);
        var playBtn = playGO.AddComponent<Button>();
        playBtn.transition = Selectable.Transition.None;
        MakeTMP("Label", playGO.transform, "PLAY", 15, white,
            0f, 1f, 0f, 1f, bold: true);

        // ── Oynatmayı Durdur (STOP PLAY) — başlangıçta gizli ─────────────
        var stopPlayGO  = MakeRect("StopPlayButton", panel.transform,
            new Color(0.08f, 0.50f, 0.24f, 1f), 0.03f, 0.97f, 0.06f, 0.32f);
        var stopPlayBtn = stopPlayGO.AddComponent<Button>();
        stopPlayBtn.transition = Selectable.Transition.None;
        MakeTMP("Label", stopPlayGO.transform, "STOP", 15, white,
            0f, 1f, 0f, 1f, bold: true);
        stopPlayGO.SetActive(false);

        // ── Alt ayraç çizgisi ─────────────────────────────────────────────
        MakeRect("Sep", panel.transform,
            new Color(1f, 1f, 1f, 0.04f), 0f, 1f, 0f, 0.03f);
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────
    private GameObject MakeRect(string name, Transform parent, Color col,
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

    private TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, Color col, float x0, float x1, float y0, float y1, bool bold = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = col;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }
}
