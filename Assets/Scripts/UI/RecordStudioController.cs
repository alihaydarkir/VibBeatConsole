using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordStudioController : MonoBehaviour
{
    // ─── Buton referansları ───
    private Button   startButton;
    private Button   listenButton;
    private Button   stopButton;
    private Button   saveButton;
    private Button   clearButton;

    // ─── CanvasGroup — disabled olunca alpha ile solar, Inspector renkleri bozulmaz ───
    private CanvasGroup startCG;
    private CanvasGroup listenCG;
    private CanvasGroup stopCG;
    private CanvasGroup saveCG;
    private CanvasGroup clearCG;

    // ─── Metin referansları ───
    private TMP_Text statusText;
    private TMP_Text durationText;
    private TMP_Text noteCountText;
    private TMP_Text loopText;
    private TMP_Text infoText;

    // Renkler kaldırıldı — buton renkleri Inspector'dan ayarlanır, kod override etmez

    // ─── Döngü ───
    private bool loopEnabled = false;

    // ─── Son kaydedilmiş kayıt bilgisi ───
    private bool  hasSaved       = false;
    private int   savedNoteCount = 0;
    private float savedDuration  = 0f;

    // ─── Süre güncelleme throttle ───
    private float displayTimer = 0f;

    private VibeBeatScreenManager screenManager;

    // ─────────────────────────────────────────
    // KURULUM
    // ─────────────────────────────────────────
    private void Awake()
    {
        var t = transform;
        startButton  = t.Find("RecordButton")?.GetComponent<Button>();
        listenButton = t.Find("PlayButton")?.GetComponent<Button>();
        stopButton   = t.Find("StopButton")?.GetComponent<Button>();
        saveButton   = t.Find("SaveButton")?.GetComponent<Button>();
        clearButton  = t.Find("ClearButton")?.GetComponent<Button>();

        statusText    = t.Find("StatusPanel/StatusText")?.GetComponent<TMP_Text>();
        durationText  = t.Find("DurationPanel/DurationText")?.GetComponent<TMP_Text>();
        noteCountText = t.Find("NoteCountPanel/CountText")?.GetComponent<TMP_Text>();
        loopText      = t.Find("LoopPanel/LoopText")?.GetComponent<TMP_Text>();
        infoText      = t.Find("InfoPanel/InfoText")?.GetComponent<TMP_Text>();

        // CanvasGroup yoksa otomatik ekle — disabled'da alpha ile solar
        startCG  = GetOrAddCG(startButton);
        listenCG = GetOrAddCG(listenButton);
        stopCG   = GetOrAddCG(stopButton);
        saveCG   = GetOrAddCG(saveButton);
        clearCG  = GetOrAddCG(clearButton);

        BindButtons();
    }

    private static CanvasGroup GetOrAddCG(Button btn)
    {
        if (btn == null) return null;
        var cg = btn.GetComponent<CanvasGroup>();
        return cg != null ? cg : btn.gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        screenManager = FindFirstObjectByType<VibeBeatScreenManager>();
    }

    // edit-time uyumluluğu için boş bırakıldı
    public void SetupReferences(
        Button rec, Button play, Button stop, Button clear,
        TMP_Text status, TMP_Text duration, TMP_Text noteCount, TMP_Text loop) { }

    private void BindButtons()
    {
        startButton?.onClick.AddListener(OnStartPressed);
        listenButton?.onClick.AddListener(OnListenPressed);
        stopButton?.onClick.AddListener(OnStopPressed);
        saveButton?.onClick.AddListener(OnSavePressed);
        clearButton?.onClick.AddListener(OnClearPressed);
        loopText?.GetComponentInParent<Button>()?.onClick.AddListener(OnLoopToggle);
    }

    private void OnEnable()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged -= UpdateUI;
    }

    // ─────────────────────────────────────────
    // BUTON OLAYLARI
    // ─────────────────────────────────────────

    // BAŞLAT — kayıt başlat ve ana ekrana geç
    private void OnStartPressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || rec.IsRecording) return;

        if (rec.IsPlaying) rec.StopPlayBack();
        rec.StartRecording();
        screenManager?.ShowMainConsole();
    }

    // DİNLE — kaydı oynat
    private void OnListenPressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || !rec.HasRecording || rec.IsRecording || rec.IsPlaying) return;

        if (loopEnabled)
            StartCoroutine(LoopPlayBack(rec));
        else
            rec.PlayBack();
    }

    private System.Collections.IEnumerator LoopPlayBack(NoteRecorder rec)
    {
        while (loopEnabled && rec.HasRecording)
        {
            rec.PlayBack();
            yield return new WaitForSeconds(rec.GetRecordingDuration() + 0.3f);
        }
    }

    // DURDUR — kayıt veya oynatmayı durdur
    private void OnStopPressed()
    {
        StopAllCoroutines();
        NoteRecorder.Instance?.StopPlayBack();
        NoteRecorder.Instance?.StopRecording();
    }

    // KAYDET — mevcut kaydı "Son Kayıt" paneline kaydet
    private void OnSavePressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || !rec.HasRecording || rec.IsRecording || rec.IsPlaying) return;

        savedNoteCount = rec.NoteCount;
        savedDuration  = rec.GetRecordingDuration();
        hasSaved       = true;

        UpdateUI();
        Debug.Log($"[STUDIO] Kayıt kaydedildi: {savedNoteCount} nota, {FormatDuration(savedDuration)}");
    }

    // SİL — her şeyi temizle
    private void OnClearPressed()
    {
        StopAllCoroutines();
        NoteRecorder.Instance?.ClearRecording();
        hasSaved       = false;
        savedNoteCount = 0;
        savedDuration  = 0f;
        UpdateUI();
    }

    private void OnLoopToggle()
    {
        loopEnabled = !loopEnabled;
        UpdateUI();
    }

    // ─────────────────────────────────────────
    // UI GÜNCELLEME
    // ─────────────────────────────────────────
    private void Update()
    {
        displayTimer += Time.deltaTime;
        if (displayTimer < 0.1f) return;
        displayTimer = 0f;

        var rec = NoteRecorder.Instance;
        if (rec == null) return;

        if (durationText != null)
            durationText.text = FormatDuration(rec.GetRecordingDuration());

        if (noteCountText != null && (rec.IsRecording || rec.IsPlaying))
            noteCountText.text = rec.NoteCount.ToString();
    }

    private void UpdateUI()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null) return;

        bool hasRec    = rec.HasRecording;
        bool recording = rec.IsRecording;
        bool playing   = rec.IsPlaying;
        bool idle      = !recording && !playing;

        // ── Buton aktifliği — alpha ile solar, Inspector renkleri bozulmaz ──
        SetInteractable(startButton,  startCG,  idle);
        SetInteractable(listenButton, listenCG, hasRec && idle);
        SetInteractable(stopButton,   stopCG,   recording || playing);
        SetInteractable(saveButton,   saveCG,   hasRec && idle);
        SetInteractable(clearButton,  clearCG,  (hasRec || hasSaved) && idle);

        // ── Durum metni ────────────────────────────────────────────────────
        if (statusText != null)
        {
            if (recording)
                statusText.text = "● KAYIT YAPILIYOR...";
            else if (playing)
                statusText.text = " DİNLENİYOR";
            else if (hasRec && hasSaved)
                statusText.text = $"Kaydedildi — {savedNoteCount} nota";
            else if (hasRec)
                statusText.text = $" Durduruldu — {rec.NoteCount} nota (kaydet?)";
            else
                statusText.text = "● Kayit Bekleniyor";
        }

        // ── Sayaçlar ───────────────────────────────────────────────────────
        if (noteCountText != null) noteCountText.text = rec.NoteCount.ToString();

        // ── Döngü metni ────────────────────────────────────────────────────
        if (loopText != null)
            loopText.text = loopEnabled ? "AÇIK" : "KAPALI";

        // ── Son Kayıt paneli ────────────────────────────────────────────────
        if (infoText != null)
        {
            if (hasSaved)
                infoText.text = $"Notalar: {savedNoteCount}\nSüre: {FormatDuration(savedDuration)}\n\nDİNLE butonuyla oynat.";
            else if (hasRec)
                infoText.text = "Kayit durduruldu.\nKaydetmek icin  KAYDET'e bas.";
            else
                infoText.text = "Henüz kayit yok.\nBaslatip durdurduktan sonra KAYDET'e bas.";
        }
    }

    private static void SetInteractable(Button btn, CanvasGroup cg, bool active)
    {
        if (btn == null) return;
        btn.interactable = active;
        if (cg != null) cg.alpha = active ? 1f : 0.05f;
    }

    private static string FormatDuration(float sec)
    {
        int m = (int)(sec / 60f), s = (int)(sec % 60f);
        return $"{m:00}:{s:00}";
    }
}
