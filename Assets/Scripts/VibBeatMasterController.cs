using UnityEngine;

/// <summary>
/// VibBeat Merkezi Orkestratör
///
/// Tüm sistemler arasındaki veri akışını yönetir.
/// Bootstrap ve UI katmanı bu sınıf üzerinden ses/sensör kontrolü yapar.
/// Bu sayede çift ses mimarisi sorunu ortadan kalkar.
///
/// Veri akışı:
///   SensorController → CalibrationManager → [normalize] → AudioSynthesizer + VisualizationController
///   TouchZoneController → [event] → AudioSynthesizer + HapticManager + VisualizationController
///   Bootstrap UI → [public API] → bu sınıf → ilgili sistem
/// </summary>
public class VibBeatMasterController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR — Sistem Referansları
    // ─────────────────────────────────────────
    [Header("Çekirdek Sistemler")]
    [SerializeField] private SensorController        sensorController;
    [SerializeField] private CalibrationManager      calibrationManager;
    [SerializeField] private TouchZoneController     touchZoneController;
    [SerializeField] private AudioSynthesizer        audioSynthesizer;
    [SerializeField] private HapticFeedbackManager   hapticManager;
    [SerializeField] private VisualizationController visualController;

    [Header("Debug (Read-Only)")]
    [SerializeField] private float  debugRawLux        = 0f;
    [SerializeField] private float  debugNormalizedLux  = 0f;
    [SerializeField] private bool   debugGuitarMuted    = false;
    [SerializeField] private string debugSensorStatus   = "";

    // ─────────────────────────────────────────
    // ÖZEL ALANLAR
    // ─────────────────────────────────────────
    private bool  isRunning         = false;
    private float normalizedSensor  = 0f;   // Bootstrap'in okuyabileceği normalize değer

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        ValidateSystems();
        SubscribeEvents();
        isRunning = true;
        Debug.Log("[MASTER] ✅ VibBeat başlatıldı.");
    }

    private void ValidateSystems()
    {
        if (!sensorController)     Debug.LogError("[MASTER] ❌ SensorController eksik!");
        if (!calibrationManager)   Debug.LogError("[MASTER] ❌ CalibrationManager eksik!");
        if (!touchZoneController)  Debug.LogError("[MASTER] ❌ TouchZoneController eksik!");
        if (!audioSynthesizer)     Debug.LogError("[MASTER] ❌ AudioSynthesizer eksik!");
        if (!hapticManager)        Debug.LogError("[MASTER] ❌ HapticManager eksik!");
        if (!visualController)     Debug.LogError("[MASTER] ❌ VisualizationController eksik!");
    }

    private void SubscribeEvents()
    {
        if (touchZoneController != null)
        {
            touchZoneController.OnGuitarMuteChanged += HandleGuitarMuteChanged;
            touchZoneController.OnPianoKeyPressed   += HandlePianoKeyPressed;
            touchZoneController.OnDrumHit           += HandleDrumHit;
        }

        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationMessage  += HandleCalibrationMessage;
            calibrationManager.OnCalibrationComplete += HandleCalibrationComplete;
        }

        if (sensorController != null)
        {
            sensorController.OnSensorStatusChanged += HandleSensorStatus;
        }

        Debug.Log("[MASTER] ✅ Eventler bağlandı.");
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
        float raw        = sensorController.GetCurrentLux();
        float normalized = calibrationManager.NormalizeLuxValue(raw);

        debugRawLux        = raw;
        debugNormalizedLux = normalized;
        normalizedSensor   = normalized;

        if (!touchZoneController.IsGuitarMuted)
        {
            audioSynthesizer.SetGuitarPitchFromSensor(normalized);
            visualController.UpdateGuitarVisualization(normalized);
        }
    }

    // ─────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────
    private void HandleGuitarMuteChanged(bool isMuted)
    {
        debugGuitarMuted = isMuted;
        audioSynthesizer.SetGuitarMuted(isMuted);
        visualController.SetGuitarMuteVisual(isMuted);
        hapticManager.PlayGuitarMuteFeedback();

        AccessibilityManager.Instance?.Speak(isMuted ? "Gitar susturuldu" : "Gitar açıldı");
    }

    private void HandlePianoKeyPressed(int keyIndex)
    {
        audioSynthesizer.PlayPianoNote(keyIndex);
        visualController.PlayPianoKeyVisualization(keyIndex);
        hapticManager.PlayPianoKeyFeedback();
        AccessibilityManager.Instance?.AnnouncePianoZone(keyIndex);
    }

    private void HandleDrumHit()
    {
        audioSynthesizer.PlayDrumKick();
        visualController.PlayDrumImpactVisualization();
        hapticManager.PlayDrumKickFeedback();
        AccessibilityManager.Instance?.AnnounceDrumZone();
    }

    private void HandleCalibrationMessage(string message)
    {
        Debug.Log($"[CALIBRATION] {message}");
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
            AccessibilityManager.Instance?.Speak("Sensör hatası. Lütfen uygulamayı yeniden başlatın.");
    }

    // ─────────────────────────────────────────
    // PUBLIC API — Bootstrap ve UI katmanı için
    // ─────────────────────────────────────────

    /// <summary>Bootstrap'in okuyabileceği normalize sensör değeri (0-1)</summary>
    public float GetNormalizedSensorValue() => normalizedSensor;

    /// <summary>UI'dan piyano tuşu tetiklemek için</summary>
    public void HandlePianoKeyFromUI(int keyIndex) => HandlePianoKeyPressed(keyIndex);

    /// <summary>UI'dan davul vuruşu tetiklemek için</summary>
    public void HandleDrumHitFromUI() => HandleDrumHit();

    /// <summary>UI'dan gitar mute'u ayarlamak için</summary>
    public void SetGuitarMuteFromUI(bool muted)
    {
        audioSynthesizer.SetGuitarMuted(muted);
        visualController.SetGuitarMuteVisual(muted);
        hapticManager.PlayGuitarMuteFeedback();
        debugGuitarMuted = muted;
    }

    /// <summary>Ses seviyesini tüm AudioSource'larda eş zamanlı ayarla</summary>
    public void SetMasterVolume(float volume)
    {
        // AudioSynthesizer bu metodu expose etmeli; şimdilik AudioListener üzerinden
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public void StartCalibration()     => calibrationManager?.StartCalibration();
    public void SetHapticEnabled(bool e) => hapticManager?.SetHapticEnabled(e);

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
        {
            sensorController.OnSensorStatusChanged -= HandleSensorStatus;
        }

        Debug.Log("[MASTER] 🛑 VibBeat durduruldu.");
    }
}
