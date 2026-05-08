using UnityEngine;

public class VibBeatMasterController : MonoBehaviour
{
    // --- Sistemler ---
    [Header("Core Systems")]
    [SerializeField] private SensorController sensorController;
    [SerializeField] private CalibrationManager calibrationManager;
    [SerializeField] private TouchZoneController touchZoneController;
    [SerializeField] private AudioSynthesizer audioSynthesizer;
    [SerializeField] private HapticFeedbackManager hapticManager;
    [SerializeField] private VisualizationController visualController;

    // --- State ---
    private bool isRunning = false;

    // --- Inspector Debug ---
    [Header("Debug")]
    [SerializeField] private float debugNormalizedLux = 0f;
    [SerializeField] private float debugRawLux = 0f;
    [SerializeField] private bool debugGuitarMuted = false;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        ValidateSystems();
        SubscribeEvents();
        isRunning = true;
        Debug.Log("[MASTER] ✅ VibBeat başlatıldı!");
    }

    private void ValidateSystems()
    {
        if (sensorController == null) Debug.LogError("[MASTER] ❌ SensorController eksik!");
        if (calibrationManager == null) Debug.LogError("[MASTER] ❌ CalibrationManager eksik!");
        if (touchZoneController == null) Debug.LogError("[MASTER] ❌ TouchZoneController eksik!");
        if (audioSynthesizer == null) Debug.LogError("[MASTER] ❌ AudioSynthesizer eksik!");
        if (hapticManager == null) Debug.LogError("[MASTER] ❌ HapticManager eksik!");
        if (visualController == null) Debug.LogError("[MASTER] ❌ VisualizationController eksik!");
    }

    private void SubscribeEvents()
    {
        // Touch events
        touchZoneController.OnGuitarMuteChanged += HandleGuitarMuteChanged;
        touchZoneController.OnPianoKeyPressed += HandlePianoKeyPressed;
        touchZoneController.OnDrumHit += HandleDrumHit;

        // Kalibrasyon events
        calibrationManager.OnCalibrationMessage += HandleCalibrationMessage;
        calibrationManager.OnCalibrationComplete += HandleCalibrationComplete;

        Debug.Log("[MASTER] ✅ Eventler bağlandı!");
    }

    // ─────────────────────────────────────────
    // GÜNCELLEME (Her Frame)
    // ─────────────────────────────────────────
    private void Update()
    {
        if (!isRunning) return;

        UpdateSensorData();
    }

    private void UpdateSensorData()
    {
        // Ham lux değerini al
        float rawLux = sensorController.GetCurrentLux();
        debugRawLux = rawLux;

        // Normalize et (0-1)
        float normalized = calibrationManager.NormalizeLuxValue(rawLux);
        debugNormalizedLux = normalized;

        // Gitar mute değilse sensör verisini işle
        if (!touchZoneController.IsGuitarMuted)
        {
            audioSynthesizer.SetGuitarPitchFromSensor(normalized);
            visualController.UpdateGuitarVisualization(normalized);
        }
    }

    // ─────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────

    // --- Gitar Mute ---
    private void HandleGuitarMuteChanged(bool isMuted)
    {
        debugGuitarMuted = isMuted;

        audioSynthesizer.SetGuitarMuted(isMuted);
        visualController.SetGuitarMuteVisual(isMuted);
        hapticManager.PlayGuitarMuteFeedback();

        Debug.Log($"[MASTER] 🎸 Gitar mute: {isMuted}");
    }

    // --- Piyano ---
    private void HandlePianoKeyPressed(int keyIndex)
    {
        audioSynthesizer.PlayPianoNote(keyIndex);
        visualController.PlayPianoKeyVisualization(keyIndex);
        hapticManager.PlayPianoKeyFeedback();

        Debug.Log($"[MASTER] 🎹 Piyano tuş: {keyIndex}");
    }

    // --- Davul ---
    private void HandleDrumHit()
    {
        audioSynthesizer.PlayDrumKick();
        visualController.PlayDrumImpactVisualization();
        hapticManager.PlayDrumKickFeedback();

        Debug.Log("[MASTER] 🥁 Davul!");
    }

    // --- Kalibrasyon ---
    private void HandleCalibrationMessage(string message)
    {
        Debug.Log($"[CALIBRATION] 📢 {message}");
    }

    private void HandleCalibrationComplete()
    {
        Debug.Log("[MASTER] ✅ Kalibrasyon tamamlandı, sistem hazır!");
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────
    public void StartCalibration()
    {
        calibrationManager.StartCalibration();
    }

    public void SetHapticEnabled(bool enabled)
    {
        hapticManager.SetHapticEnabled(enabled);
    }

    // ─────────────────────────────────────────
    // TEMİZLİK
    // ─────────────────────────────────────────
    private void OnDestroy()
    {
        if (touchZoneController != null)
        {
            touchZoneController.OnGuitarMuteChanged -= HandleGuitarMuteChanged;
            touchZoneController.OnPianoKeyPressed -= HandlePianoKeyPressed;
            touchZoneController.OnDrumHit -= HandleDrumHit;
        }

        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationMessage -= HandleCalibrationMessage;
            calibrationManager.OnCalibrationComplete -= HandleCalibrationComplete;
        }

        Debug.Log("[MASTER] 🛑 VibBeat durduruldu.");
    }
}