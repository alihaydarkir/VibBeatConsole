using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Sesli Okuma" toggle satırı — HapticRow ile aynı görsel yapı.
/// AccessibilityRow GameObject'ine attach et, Start()'ta kendi kendini kurar.
/// Inspector'dan çocuk referansları atanmışsa onları kullanır; yoksa oluşturur.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AccessibilityToggleRow : MonoBehaviour
{
    [SerializeField] private Button          rowButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image           toggleTrack;
    [SerializeField] private Image           toggleHandle;

    private static readonly Color CyanOn   = new Color(0f,   0.94f, 1f,   1f);
    private static readonly Color GrayOff  = new Color(0.53f,0.60f, 0.67f,1f);
    private static readonly Color TrackOn  = new Color(0f,   0.94f, 1f,   1f);
    private static readonly Color TrackOff = new Color(0.09f,0.14f, 0.20f,1f);
    private static readonly Color BgCard   = new Color(0.06f,0.10f, 0.15f,0.85f);

    private void Start()
    {
        EnsureBackground();
        EnsureButton();
        EnsureChildren();
        WireButton();
        Refresh();
    }

    // ─── Yapı ─────────────────────────────────────────────────────────────
    private void EnsureBackground()
    {
        var img = GetComponent<Image>();
        if (img == null) img = gameObject.AddComponent<Image>();
        if (img.color == Color.white) img.color = BgCard;
    }

    private void EnsureButton()
    {
        if (rowButton == null)
            rowButton = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        rowButton.transition = Selectable.Transition.None;
    }

    private void EnsureChildren()
    {
        // Icon (sol)
        if (transform.Find("Icon") == null)
            MakeTMP("Icon", "♪", 26, CyanOn,  new Vector2(0.02f, 0.09f), new Vector2(0.10f, 0.90f));

        // Label (başlık)
        if (transform.Find("Label") == null)
            MakeTMP("Label", "SESLI OKUMA", 22, GrayOff, new Vector2(0.11f, 0.65f), new Vector2(0.10f, 0.90f), bold: true);

        // ToggleTrack + ToggleHandle
        if (toggleTrack == null)
        {
            var track = MakePanel("ToggleTrack", TrackOff, new Vector2(0.71f, 0.87f), new Vector2(0.22f, 0.78f));
            toggleTrack = track.GetComponent<Image>();
            MakePanel("ToggleHandle", Color.white,
                new Vector2(0.52f, 0.96f), new Vector2(0.08f, 0.92f),
                track.transform);
            toggleHandle = track.transform.Find("ToggleHandle").GetComponent<Image>();
        }
        else
        {
            toggleHandle = toggleTrack.transform.Find("ToggleHandle")?.GetComponent<Image>();
        }

        // StatusText (sağ)
        if (statusText == null)
        {
            var t = transform.Find("StatusText");
            if (t != null)
                statusText = t.GetComponent<TextMeshProUGUI>();
            else
                statusText = MakeTMP("StatusText", "KAPALI", 20, GrayOff,
                    new Vector2(0.87f, 0.99f), new Vector2(0.20f, 0.80f), bold: true);
        }

        // Ayraç çizgisi
        if (transform.Find("Sep") == null)
            MakePanel("Sep", new Color(1f,1f,1f,0.03f), new Vector2(0f,1f), new Vector2(0f,0.03f));
    }

    private void WireButton()
    {
        rowButton.onClick.RemoveAllListeners();
        rowButton.onClick.AddListener(OnToggle);
    }

    // ─── Tıklama ──────────────────────────────────────────────────────────
    private void OnToggle()
    {
        bool next = !(AccessibilityManager.Instance?.IsAccessibilityEnabled() ?? false);
        AccessibilityManager.Instance?.SetAccessibilityEnabled(next);
        Refresh();
        Debug.Log($"[ACCESSIBILITY] Sesli okuma: {(next ? "AÇIK" : "KAPALI")}");
    }

    // ─── Görsel ───────────────────────────────────────────────────────────
    private void Refresh()
    {
        bool on = AccessibilityManager.Instance?.IsAccessibilityEnabled() ?? false;

        if (statusText   != null) { statusText.text  = on ? "AÇIK" : "KAPALI";
                                    statusText.color = on ? CyanOn : GrayOff; }
        if (toggleTrack  != null)   toggleTrack.color  = on ? TrackOn  : TrackOff;
        if (toggleHandle != null)
        {
            var rt = toggleHandle.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = on ? new Vector2(0.52f, 0.08f) : new Vector2(0.04f, 0.08f);
                rt.anchorMax = on ? new Vector2(0.96f, 0.92f) : new Vector2(0.48f, 0.92f);
            }
        }
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────
    private TextMeshProUGUI MakeTMP(string childName, string text, int size, Color color,
        Vector2 anchorX, Vector2 anchorY, bool bold = false)
    {
        var go  = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(anchorX.x, anchorY.x);
        rt.anchorMax        = new Vector2(anchorX.y, anchorY.y);
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = size;
        tmp.color           = color;
        tmp.fontStyle       = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment       = TextAlignmentOptions.Midline;
        tmp.overflowMode    = TextOverflowModes.Overflow;
        return tmp;
    }

    private GameObject MakePanel(string childName, Color color,
        Vector2 anchorX, Vector2 anchorY, Transform parent = null)
    {
        var go  = new GameObject(childName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent ?? transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin  = new Vector2(anchorX.x, anchorY.x);
        rt.anchorMax  = new Vector2(anchorX.y, anchorY.y);
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }
}
