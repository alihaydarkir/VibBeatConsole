using UnityEngine;

/// <summary>
/// Android ışık sensöründen ham lux değerini okur ve Unity event sistemiyle yayar.
///
/// Kalibrasyon Senaryosu:
///   0 noktası → Kullanıcı sol eliyle sensörün üstünü kapatır (karanlık = min lux)
///   1 noktası → El çekilir, sensör ortam ışığına tamamen açılır (max lux)
///
/// Bu script yalnızca ham veriyi toplar. Normalizasyon → CalibrationManager.
/// </summary>
public class SensorController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────
    [Header("Debug (Read-Only)")]
    [SerializeField] private float  debugRawLux       = 0f;
    [SerializeField] private string debugSensorStatus = "Bekleniyor";
    [SerializeField] private bool   debugSensorReady  = false;

    // Editor simülasyonu: mouse X → lux (0 – 50000)
    [Header("Editor Simülasyon")]
    [Tooltip("Mouse X ekseninin karşılık geldiği maksimum lux değeri")]
    [SerializeField] private float editorMaxSimLux = 50000f;

    // ─────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────
    public delegate void LuxChangedDelegate(float luxValue);
    public event LuxChangedDelegate OnLuxChanged;

    public delegate void SensorStatusDelegate(string status);
    public event SensorStatusDelegate OnSensorStatusChanged;

    // ─────────────────────────────────────────
    // ÖZEL ALANLAR
    // ─────────────────────────────────────────
    private float   currentLux    = 0f;
    private bool    sensorReady   = false;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject bridge = null;
#endif

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidBridge();
#else
        sensorReady = true;
        debugSensorReady  = true;
        debugSensorStatus = "Editor simülasyon aktif";
        Debug.Log("[SENSOR] Editor modu — mouse X ile lux simüle ediliyor.");
#endif
    }

    private void InitializeAndroidBridge()
    {
        try
        {
            var pluginClass = new AndroidJavaClass("com.vibbeat.sensors.AndroidLightSensorBridge");
            bridge = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
            bridge.Call("startListening");

            sensorReady = true;
            debugSensorReady  = true;
            debugSensorStatus = "Android sensör aktif";
            Debug.Log("[SENSOR] ✅ AndroidLightSensorBridge başlatıldı.");
        }
        catch (System.Exception ex)
        {
            sensorReady = false;
            debugSensorReady  = false;
            debugSensorStatus = $"HATA: {ex.Message}";
            Debug.LogError($"[SENSOR] ❌ Bridge başlatılamadı: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────
    // UPDATE — sadece Editor simülasyonu için
    // ─────────────────────────────────────────
    private void Update()
    {
#if UNITY_EDITOR
        // Mouse X'i 0–editorMaxSimLux aralığına map et
        float mouseNorm = Input.mousePosition.x / Screen.width;
        float simLux    = mouseNorm * editorMaxSimLux;

        if (Mathf.Abs(simLux - currentLux) > 0.5f)   // gereksiz event'leri filtrele
        {
            currentLux    = simLux;
            debugRawLux   = currentLux;
            OnLuxChanged?.Invoke(currentLux);
        }
#endif
    }

    // ─────────────────────────────────────────
    // JAVA'DAN ÇAĞRILAN CALLBACK'LER
    // (UnityPlayer.UnitySendMessage tarafından tetiklenir)
    // ─────────────────────────────────────────

    /// <summary>
    /// Java → Unity lux veri köprüsü.
    /// Parametre string olarak gelir çünkü UnitySendMessage yalnızca string destekler.
    /// </summary>
    public void OnLuxValueChanged(string luxStr)
    {
        if (float.TryParse(
            luxStr,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float lux))
        {
            currentLux  = lux;
            debugRawLux = lux;
            OnLuxChanged?.Invoke(currentLux);
        }
        else
        {
            Debug.LogWarning($"[SENSOR] Geçersiz lux değeri: '{luxStr}'");
        }
    }

    /// <summary>
    /// Java → Unity durum mesajı köprüsü.
    /// "READY", "LISTENING", "STOPPED", "ERROR:..." vb.
    /// </summary>
    public void OnSensorStatusChanged(string status)
    {
        debugSensorStatus = status;
        Debug.Log($"[SENSOR] Durum: {status}");

        if (status == "ERROR:NO_LIGHT_SENSOR")
        {
            sensorReady = false;
            debugSensorReady = false;
            Debug.LogError("[SENSOR] ❌ Cihazda ışık sensörü bulunamadı!");
        }

        OnSensorStatusChanged?.Invoke(status);
    }

    // ─────────────────────────────────────────
    // UYGULAMA YAŞAM DÖNGÜSÜ
    // Arka plana geçişte sensörü durdur → pil tasarrufu
    // ─────────────────────────────────────────
    private void OnApplicationPause(bool paused)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bridge == null) return;
        if (paused)
            bridge.Call("stopListening");
        else if (sensorReady)
            bridge.Call("startListening");
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bridge?.Call("stopListening");
        bridge?.Dispose();
        bridge = null;
#endif
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────
    public float GetCurrentLux() => currentLux;
    public bool  IsSensorReady() => sensorReady;
}
