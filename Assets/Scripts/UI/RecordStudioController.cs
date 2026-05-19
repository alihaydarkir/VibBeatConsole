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

    // ─── Döngü (NoteRecorder tekrar oynatma toggle) ───
    private bool loopEnabled = false;

    // ─── Son kaydedilmiş kayıt bilgisi ───
    private bool  hasSaved       = false;
    private int   savedNoteCount = 0;
    private float savedDuration  = 0f;

    // ─── Süre güncelleme throttle ───
    private float displayTimer = 0f;

    // ─── Ana ekrana kayıt için geçildiğinde loop durdurulmasın ───
    private bool _recordingGoToMain = false;

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
        StopGuitar();
        _recordingGoToMain = false;

        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged += UpdateUI;

        // LoopRecorder backing track: loop varsa otomatik başlat
        var loop = LoopRecorder.Instance;
        if (loop != null)
        {
            loop.OnStateChanged += OnLoopStateChanged;
            if (loop.HasLoop && loop.CurrentState != LoopRecorder.State.Playing)
                loop.StartPlayback();
        }

        UpdateUI();
    }

    private void OnDisable()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged -= UpdateUI;

        var loop = LoopRecorder.Instance;
        if (loop != null)
        {
            loop.OnStateChanged -= OnLoopStateChanged;
            // Kayıt için ana ekrana geçiliyorsa loop durmaya devam etsin
            if (!_recordingGoToMain)
                loop.StopPlayback();
        }
        _recordingGoToMain = false;
    }

    private void OnLoopStateChanged(LoopRecorder.State _) => UpdateUI();

    private static void StopGuitar()
    {
        if (AudioSynthesizer.Instance != null)
            AudioSynthesizer.Instance.StopGuitarLoop();
    }

    private static void StartGuitar()
    {
        if (AudioSynthesizer.Instance != null)
            AudioSynthesizer.Instance.StartGuitarLoop();
    }

    // ─────────────────────────────────────────
    // BUTON OLAYLARI
    // ─────────────────────────────────────────

    // BAŞLAT — loop çalarken üstüne kayıt için ana ekrana geç
    private void OnStartPressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || rec.IsRecording) return;

        if (rec.IsPlaying) rec.StopPlayBack();

        // Loop backing track — varsa başlat, yoksa da devam et
        var loop = LoopRecorder.Instance;
        if (loop != null && loop.HasLoop && loop.CurrentState != LoopRecorder.State.Playing)
            loop.StartPlayback();

        StartGuitar();
        rec.StartRecording();

        // Ana ekrana geç — loop orada çalmaya devam eder
        _recordingGoToMain = true;
        screenManager?.ShowMainConsole();
    }

    // DİNLE — loop + kayıt birlikte oynat
    private void OnListenPressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || !rec.HasRecording || rec.IsRecording || rec.IsPlaying) return;

        StartGuitar();

        // Loop backing track başlat
        var loop = LoopRecorder.Instance;
        if (loop != null && loop.HasLoop && loop.CurrentState != LoopRecorder.State.Playing)
            loop.StartPlayback();

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

    // DURDUR — kayıt, oynatma ve backing loop durdur
    private void OnStopPressed()
    {
        StopAllCoroutines();
        if (NoteRecorder.Instance != null) { NoteRecorder.Instance.StopPlayBack(); NoteRecorder.Instance.StopRecording(); }
        if (LoopRecorder.Instance != null) LoopRecorder.Instance.StopPlayback();
    }

    // KAYDET
    private void OnSavePressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || !rec.HasRecording || rec.IsRecording || rec.IsPlaying) return;

        savedNoteCount = rec.NoteCount;
        savedDuration  = rec.GetRecordingDuration();
        hasSaved       = true;

        UpdateUI();
        Debug.Log($"[STUDIO] Kaydedildi: {savedNoteCount} nota, {FormatDuration(savedDuration)}");
    }

    // SİL
    private void OnClearPressed()
    {
        StopAllCoroutines();
        if (NoteRecorder.Instance != null) NoteRecorder.Instance.ClearRecording();
        if (LoopRecorder.Instance != null) LoopRecorder.Instance.StopPlayback();
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

        var  loop        = LoopRecorder.Instance;
        bool hasLoop     = loop != null && loop.HasLoop;
        bool loopPlaying = loop != null && loop.CurrentState == LoopRecorder.State.Playing;

        // ── Buton aktifliği ─────────────────────────────────────────────────
        SetInteractable(startButton,  startCG,  idle);
        SetInteractable(listenButton, listenCG, hasRec && idle);
        SetInteractable(stopButton,   stopCG,   recording || playing || loopPlaying);
        SetInteractable(saveButton,   saveCG,   hasRec && idle);
        SetInteractable(clearButton,  clearCG,  (hasRec || hasSaved) && idle);

        // ── Durum metni ─────────────────────────────────────────────────────
        if (statusText != null)
        {
            if (recording)
                statusText.text = "● KAYIT YAPILIYOR...";
            else if (playing)
                statusText.text = "DİNLENİYOR";
            else if (hasRec && hasSaved)
                statusText.text = $"Kaydedildi — {savedNoteCount} nota";
            else if (hasRec)
                statusText.text = $"Durduruldu — {rec.NoteCount} nota (kaydet?)";
            else
                statusText.text = "Kayit Bekleniyor";
        }

        // ── Sayaçlar ────────────────────────────────────────────────────────
        if (noteCountText != null) noteCountText.text = rec.NoteCount.ToString();

        // ── Döngü metni ─────────────────────────────────────────────────────
        if (loopText != null)
            loopText.text = loopEnabled ? "AÇIK" : "KAPALI";

        // ── Bilgi paneli — loop backing durumu ──────────────────────────────
        if (infoText != null)
        {
            string loopLine = hasLoop
                ? (loopPlaying ? "Loop: ÇALIYOR (backing track)" : "Loop: HAZIR")
                : "Loop yok — ana ekranda loop olustur";

            if (hasSaved)
                infoText.text = $"Notalar: {savedNoteCount}\nSure: {FormatDuration(savedDuration)}\n\n{loopLine}";
            else if (hasRec)
                infoText.text = $"Kayit durduruldu.\nKaydetmek icin KAYDET'e bas.\n\n{loopLine}";
            else
                infoText.text = $"Henuz kayit yok.\nBaslatip durdurduktan sonra KAYDET'e bas.\n\n{loopLine}";
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
