using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ana ekranda kayıt/oynatma durumunu gösteren şerit.
/// UI'ı AddRecordStudioOnly() tarafından sahne hiyerarşisine inşa edilir —
/// Inspector'dan düzenlenebilir. Bu script sadece mantığı yönetir.
/// </summary>
public class RecordingStatusOverlay : MonoBehaviour
{
    // Awake'de hiyerarşiden bulunur — Inspector'dan da atanabilir
    private GameObject panel;
    private TMP_Text   statusLabel;
    private TMP_Text   timerLabel;
    private Button     stopBtn;
    private Button     studioBtn;

    private float blinkTimer;

    private static readonly Color RecRed    = new Color(1f,   0f,   0.27f, 1f);
    private static readonly Color PlayGreen = new Color(0f,   1f,   0.53f, 1f);

    // ─────────────────────────────────────────
    // KURULUM — UI hiyerarşiden bulunur
    // ─────────────────────────────────────────
    private void Awake()
    {
        var p = transform.Find("RecordingOverlayPanel");
        if (p == null) { Debug.LogWarning("[OVERLAY] RecordingOverlayPanel bulunamadı!"); return; }

        panel       = p.gameObject;
        statusLabel = p.Find("StatusLabel")?.GetComponent<TMP_Text>();
        timerLabel  = p.Find("TimerLabel")?.GetComponent<TMP_Text>();
        stopBtn     = p.Find("StopBtn")?.GetComponent<Button>();
        studioBtn   = p.Find("StudioBtn")?.GetComponent<Button>();

        stopBtn?.onClick.AddListener(OnStopPressed);
        studioBtn?.onClick.AddListener(OnStudioPressed);

        panel.SetActive(false);
    }

    private void Start()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged -= Refresh;
    }

    // ─────────────────────────────────────────
    // MANTIK
    // ─────────────────────────────────────────
    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        var rec = NoteRecorder.Instance;
        if (rec == null) return;

        // Süre güncelle
        if (timerLabel != null)
        {
            float dur = rec.GetRecordingDuration();
            timerLabel.text = $"{(int)(dur/60f):00}:{(int)(dur%60f):00}";
        }

        // Kayıt sırasında ● yanıp sönsün
        if (rec.IsRecording)
        {
            blinkTimer += Time.deltaTime;
            if (statusLabel != null)
                statusLabel.enabled = (blinkTimer % 0.8f) < 0.4f;
        }
        else if (statusLabel != null)
        {
            statusLabel.enabled = true;
        }
    }

    private void Refresh()
    {
        if (panel == null) return;
        var rec = NoteRecorder.Instance;
        bool show = rec != null && (rec.IsRecording || rec.IsPlaying);
        panel.SetActive(show);

        // Overlay açıkken TopBar REC butonunu gizle
        SetTopBarRecButtonVisible(!show);

        if (!show) { blinkTimer = 0f; return; }

        if (statusLabel != null)
        {
            statusLabel.text    = rec.IsRecording ? "● KAYIT YAPILIYOR" : "▶ OYNATILIYOR";
            statusLabel.color   = rec.IsRecording ? RecRed : PlayGreen;
            statusLabel.enabled = true;
        }

        // Kenar rengini güncelle
        var edge = panel.transform.Find("Edge")?.GetComponent<Image>();
        if (edge != null) edge.color = rec.IsRecording ? RecRed : PlayGreen;
    }

    private void SetTopBarRecButtonVisible(bool visible)
    {
        var recBtn = transform.Find("TopBar/RecordButton");
        if (recBtn != null) recBtn.gameObject.SetActive(visible);
    }

    private void OnStopPressed()
    {
        NoteRecorder.Instance?.StopRecording();
        NoteRecorder.Instance?.StopPlayBack();
    }

    private void OnStudioPressed()
    {
        FindFirstObjectByType<VibeBeatScreenManager>()?.ShowRecordStudio();
    }
}
