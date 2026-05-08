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

    private bool  isRunning       = false;
    private float normalizedSensor = 0f;

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
    }

    private T FindAndLog<T>(string label) where T : MonoBehaviour
    {
        T result = FindFirstObjectByType<T>();
        if (result == null)
            Debug.LogWarning($"[MASTER] ⚠️ {label} sahnede bulunamadı — bu sistem devre dışı kalacak.");
        else
            Debug.Log($"[MASTER] ✅ {label} bulundu: {result.gameObject.name}");
        return result;
    }

    // ─────────────────────────────────────────
    // START — event'leri bağla
    // ─────────────────────────────────────────
    private void Start()
    {
        SubscribeEvents();
        isRunning = true;
        Debug.Log("[MASTER] ✅ VibBeat başlatıldı ve çalışıyor.");
    }

    private void SubscribeEvents()
    {
        if (touchZoneController != null)
        {
            touchZoneController.OnGuitarMuteChanged += HandleGuitarMuteChanged;
            touchZoneController.OnPianoKeyPressed   += HandlePianoKeyPressed;
            touchZoneController.OnDrumHit           += HandleDrumHit;
            Debug.Log("[MASTER] ✅ TouchZone event'leri bağlandı.");
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

        if (touchZoneController != null && !touchZoneController.IsGuitarMuted)
        {
            audioSynthesizer?.SetGuitarPitchFromSensor(normalized);
            visualController?.UpdateGuitarVisualization(normalized);
        }
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
        Debug.Log($"[MASTER] 🎸 Gitar mute: {isMuted}");
    }

    private void HandlePianoKeyPressed(int keyIndex)
    {
        audioSynthesizer?.PlayPianoNote(keyIndex);
        visualController?.PlayPianoKeyVisualization(keyIndex);
        hapticManager?.PlayPianoKeyFeedback();
        AccessibilityManager.Instance?.AnnouncePianoZone(keyIndex);
        Debug.Log($"[MASTER] 🎹 Piyano: {keyIndex}");
    }

    private void HandleDrumHit()
    {
        audioSynthesizer?.PlayDrumKick();
        visualController?.PlayDrumImpactVisualization();
        hapticManager?.PlayDrumKickFeedback();
        AccessibilityManager.Instance?.AnnounceDrumZone();
        Debug.Log("[MASTER] 🥁 Davul!");
    }

    private void HandleCalibrationMessage(string message)
    {
        Debug.Log($"[MASTER] 📢 Kalibrasyon: {message}");
        AccessibilityManager.Instance?.AnnounceCalibrationStep(message);
    }

    private void HandleCalibrationComplete()
    {
        Debug.Log("[MASTER] ✅ Kalibrasyon tamamlandı.");
        AccessibilityManager.Instance?.AnnounceCalibrationComplete();
    }

    private void HandleSensorStatus(string status)
    {
        debugSensorStatus = status;
        if (status.StartsWith("ERROR"))
        {
            Debug.LogError($"[MASTER] ❌ Sensör hatası: {status}");
            AccessibilityManager.Instance?.Speak("Sensör hatası. Lütfen uygulamayı yeniden başlatın.");
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API — Bootstrap ve UI için
    // ─────────────────────────────────────────
    public float GetNormalizedSensorValue() => normalizedSensor;

    public void HandlePianoKeyFromUI(int keyIndex)
    {
        Debug.Log($"[MASTER] 🎹 UI'dan piyano: {keyIndex}");
        HandlePianoKeyPressed(keyIndex);
    }

    public void HandleDrumHitFromUI()
    {
        Debug.Log("[MASTER] 🥁 UI'dan davul!");
        HandleDrumHit();
    }

    public void SetGuitarMuteFromUI(bool muted)
    {
        audioSynthesizer?.SetGuitarMuted(muted);
        visualController?.SetGuitarMuteVisual(muted);
        hapticManager?.PlayGuitarMuteFeedback();
        debugGuitarMuted = muted;
        Debug.Log($"[MASTER] 🎸 UI'dan mute: {muted}");
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        Debug.Log($"[MASTER] 🔊 Volume: {Mathf.RoundToInt(volume * 100f)}%");
    }

    public void StartCalibration()
    {
        if (calibrationManager == null)
        {
            Debug.LogError("[MASTER] ❌ CalibrationManager yok — kalibrasyon başlatılamadı!");
            return;
        }
        calibrationManager.StartCalibration();
    }

    public void SetHapticEnabled(bool enabled)
    {
        hapticManager?.SetHapticEnabled(enabled);
        Debug.Log($"[MASTER] 📳 Haptic: {enabled}");
    }

    // ─────────────────────────────────────────
    // TEMİZLİK
    // ─────────────────────────────────────────
    private void OnDestroy()
    {
        if (touchZoneController != null)
        {
            touchZoneController.OnGuitarMuteChanged -= HandleGuitarMuteChanged;
            touchZoneController.OnPianoKeyPressed   -= HandlePianoKeyPressed;
            touchZoneController.OnDrumHit           -= HandleDrumHit;
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
