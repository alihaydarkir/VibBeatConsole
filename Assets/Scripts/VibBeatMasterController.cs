using UnityEngine;

/// <summary>
/// VibBeat Merkezi Orkestratör
///
/// Tüm bağımlılıklar Awake'de sahneden otomatik bulunur.
/// Inspector'a elle atama gerekmez.
///
/// Veri akışı:
///   SensorController → CalibrationManager → [normalize] → AudioSynthesizer + VisualizationController
///   TouchZoneController → [event] → AudioSynthesizer + HapticManager + VisualizationController
///   Bootstrap UI → [public API] → bu sınıf → ilgili sistem
/// </summary>
public class VibBeatMasterController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // BAĞIMLILIKLAR — Awake'de otomatik bulunur
    // ─────────────────────────────────────────
    private SensorController        sensorController;
    private CalibrationManager      calibrationManager;
    private TouchZoneController     touchZoneController;
    private AudioSynthesizer        audioSynthesizer;
    private HapticFeedbackManager   hapticManager;
    private VisualizationController visualController;

    [Header("Debug (Read-Only)")]
    [SerializeField] private float  debugRawLux       = 0f;
    [SerializeField] private float  debugNormalizedLux = 0f;
    [SerializeField] private bool   debugGuitarMuted  = false;
    [SerializeField] private string debugSensorStatus = "";

    private bool  isRunning        = false;
    private float normalizedSensor = 0f;
    private GuitarWaveVisualizer guitarWave = null;
    private HandWaveVisualizer   handWave   = null;


    // ─────────────────────────────────────────
    // AWAKE — bağımlılıkları otomatik bul
    // ─────────────────────────────────────────
    private void Awake()
    {
        sensorController    = FindAndLog<SensorController>("SensorController");
        calibrationManager  = FindAndLog<CalibrationManager>("CalibrationManager");
        touchZoneController = FindAndLog<TouchZoneController>("TouchZoneController");
        audioSynthesizer    = FindAndLog<AudioSynthesizer>("AudioSynthesizer");
        hapticManager       = FindAndLog<HapticFeedbackManager>("HapticFeedbackManager");
        visualController    = FindAndLog<VisualizationController>("VisualizationController");
        guitarWave          = FindAndLog<GuitarWaveVisualizer>("GuitarWaveVisualizer");
        handWave            = FindFirstObjectByType<HandWaveVisualizer>();
    }

    private T FindAndLog<T>(string label) where T : MonoBehaviour
    {
        T result = FindFirstObjectByType<T>();
        if (result == null)
            Debug.LogWarning($"[MASTER] [UYARI] {label} sahnede bulunamadı — bu sistem devre dışı kalacak.");
        else
            Debug.Log($"[MASTER] [OK] {label} bulundu: {result.gameObject.name}");
        return result;
    }

    // ─────────────────────────────────────────
    // START — event'leri bağla
    // ─────────────────────────────────────────
    private void Start()
    {
        SubscribeEvents();
        isRunning = true;
        Debug.Log("[MASTER] [OK] VibBeat başlatıldı ve çalışıyor.");
    }

    private void SubscribeEvents()
    {
        if (touchZoneController != null)
        {
            touchZoneController.OnGuitarMuteChanged += HandleGuitarMuteChanged;
            Debug.Log("[MASTER] [OK] TouchZone event'leri bağlandı.");
        }

        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationMessage  += HandleCalibrationMessage;
            calibrationManager.OnCalibrationComplete += HandleCalibrationComplete;
        }

        if (sensorController != null)
            sensorController.OnSensorStatus += HandleSensorStatus;
    }

    // ─────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────
    private void Update()
    {
        if (!isRunning) return;
        ProcessSensorData();
    }

    private void ProcessSensorData()
    {
        if (sensorController == null || calibrationManager == null) return;

        float raw        = sensorController.GetCurrentLux();
        float normalized = calibrationManager.NormalizeLuxValue(raw);

        debugRawLux        = raw;
        debugNormalizedLux = normalized;
        normalizedSensor   = normalized;

        bool guitarActive = touchZoneController == null || !touchZoneController.IsGuitarMuted;
        if (guitarActive && audioSynthesizer != null)
        {
            audioSynthesizer.SetGuitarToneFromSensor(normalized);
            visualController?.UpdateGuitarVisualization(normalized);
        }

        // Gitar kayıt: AudioSynthesizer.SetGuitarToneFromSensor içinde ton değişince direkt yapılır

        // Her zaman gitar dalgasini besle (mute olsa da donuk dalga gosterilsin)
        guitarWave?.SetSensorValue(normalized);
        handWave?.SetSensorValue(normalized);
    }

    // ─────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────
    private void HandleGuitarMuteChanged(bool isMuted)
    {
        debugGuitarMuted = isMuted;
        audioSynthesizer?.SetGuitarMuted(isMuted);
        visualController?.SetGuitarMuteVisual(isMuted);
        hapticManager?.PlayGuitarMuteFeedback();
        Debug.Log($"[MASTER] [GITAR] Gitar mute: {isMuted}");
    }

    private void HandlePianoKeyPressed(int keyIndex)
    {
        audioSynthesizer?.PlayPianoNote(keyIndex);
        visualController?.PlayPianoKeyVisualization(keyIndex);
        hapticManager?.PlayPianoKeyFeedback();
        AccessibilityManager.Instance?.AnnouncePianoZone(keyIndex);
        Debug.Log($"[MASTER] [PIANO] Piyano: {keyIndex}");
    }

    private void HandleDrumHit()
    {
        audioSynthesizer?.PlayDrumKick();
        visualController?.PlayDrumImpactVisualization();
        hapticManager?.PlayDrumKickFeedback();
        AccessibilityManager.Instance?.AnnounceDrumZone();
        Debug.Log("[MASTER] [DAVUL] Davul!");
    }

    private void HandleCalibrationMessage(string message)
    {
        Debug.Log($"[MASTER]  Kalibrasyon: {message}");
        AccessibilityManager.Instance?.AnnounceCalibrationStep(message);
    }

    public event System.Action OnCalibrationDone;

    private void HandleCalibrationComplete()
    {
        Debug.Log("[MASTER] [OK] Kalibrasyon tamamlandı.");
        audioSynthesizer?.ResetSensorRange();
        OnCalibrationDone?.Invoke();
    }

    private void HandleSensorStatus(string status)
    {
        debugSensorStatus = status;
        if (status.StartsWith("ERROR"))
        {
            Debug.LogError($"[MASTER] [HATA] Sensör hatası: {status}");
            AccessibilityManager.Instance?.Speak("Sensör hatası. Lütfen uygulamayı yeniden başlatın.");
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API — Bootstrap ve UI için
    // ─────────────────────────────────────────
    public float GetNormalizedSensorValue() => normalizedSensor;
    public float GetRawLux() => sensorController != null ? sensorController.GetCurrentLux() : 0f;
    public bool  IsCalibrated() => calibrationManager != null && calibrationManager.IsCalibrated();

    public void HandlePianoKeyFromUI(int keyIndex)
    {
        Debug.Log($"[MASTER] [PIANO] UI'dan piyano: {keyIndex}");
        HandlePianoKeyPressed(keyIndex);
    }

    public void HandleDrumHitFromUI()
    {
        Debug.Log("[MASTER] [DAVUL] UI'dan davul!");
        HandleDrumHit();
    }

    public void SetGuitarMuteFromUI(bool muted)
    {
        audioSynthesizer?.SetGuitarMuted(muted);
        visualController?.SetGuitarMuteVisual(muted);
        hapticManager?.PlayGuitarMuteFeedback();
        debugGuitarMuted = muted;
        Debug.Log($"[MASTER] [GITAR] UI'dan mute: {muted}");
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        Debug.Log($"[MASTER] [SES] Volume: {Mathf.RoundToInt(volume * 100f)}%");
    }

    public void StartCalibration()
    {
        if (calibrationManager == null)
        {
            Debug.LogError("[MASTER] [HATA] CalibrationManager yok — kalibrasyon başlatılamadı!");
            return;
        }
        calibrationManager.StartCalibration();
    }

    public void SetHapticEnabled(bool enabled)
    {
        hapticManager?.SetHapticEnabled(enabled);
        Debug.Log($"[MASTER] [TITRESIM] Haptic: {enabled}");
    }

    public void StartGuitarLoop()
    {
        audioSynthesizer?.StartGuitarLoop();
    }

    public void StopGuitarLoop()
    {
        audioSynthesizer?.StopGuitarLoop();
    }

    // ─────────────────────────────────────────
    // TEMİZLİK
    // ─────────────────────────────────────────
    private void OnDestroy()
    {
        if (touchZoneController != null)
        {
            touchZoneController.OnGuitarMuteChanged -= HandleGuitarMuteChanged;
        }
        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationMessage  -= HandleCalibrationMessage;
            calibrationManager.OnCalibrationComplete -= HandleCalibrationComplete;
        }
        if (sensorController != null)
            sensorController.OnSensorStatus -= HandleSensorStatus;

        Debug.Log("[MASTER] 🛑 VibBeat durduruldu.");
    }
}
