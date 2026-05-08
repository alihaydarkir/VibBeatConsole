#pragma warning disable 0414
using UnityEngine;

/// <summary>
/// Android ışık sensöründen ham lux değerini okur ve Unity event sistemiyle yayar.
///
/// Kalibrasyon Senaryosu:
///   0 noktası → Sol el sensörün üstünü kapatır (karanlık = min lux)
///   1 noktası → El çekilir, sensör ortam ışığına açılır (max lux)
///
/// Bu script yalnızca ham veriyi toplar. Normalizasyon → CalibrationManager.
/// </summary>
public class SensorController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────
    [Header("Debug (Read-Only)")]
    [SerializeField] private float  debugRawLux      = 0f;
    [SerializeField] private string debugSensorStatus = "Bekleniyor";

    [Header("Editor Simülasyon")]
    [Tooltip("Mouse X ekseninin karşılık geldiği maksimum lux değeri")]
    [SerializeField] private float editorMaxSimLux = 50000f;

    // ─────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────
    public delegate void LuxChangedDelegate(float luxValue);
    public event LuxChangedDelegate OnLuxChanged;

    public delegate void SensorStatusDelegate(string status);
    public event SensorStatusDelegate OnSensorStatus;

    // ─────────────────────────────────────────
    // ÖZEL ALANLAR
    // ─────────────────────────────────────────
    private float currentLux  = 0f;
    private bool  sensorReady = false;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitAndroid();
#else
        sensorReady       = true;
        debugSensorStatus = "Editor simülasyon aktif";
        Debug.Log("[SENSOR] Editor modu — mouse X ile lux simüle ediliyor.");
#endif
    }

// ─────────────────────────────────────────
// ANDROID BLOĞU — tamamı #if içinde
// ─────────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR

    private AndroidJavaObject bridge = null;

    private void InitAndroid()
    {
        try
        {
            var cls = new AndroidJavaClass("com.vibbeat.sensors.AndroidLightSensorBridge");
            bridge  = cls.CallStatic<AndroidJavaObject>("getInstance");
            bridge.Call("startListening");

            sensorReady       = true;
            debugSensorStatus = "Android sensör aktif";
            Debug.Log("[SENSOR] ✅ AndroidLightSensorBridge başlatıldı.");
        }
        catch (System.Exception ex)
        {
            sensorReady       = false;
            debugSensorStatus = $"HATA: {ex.Message}";
            Debug.LogError($"[SENSOR] ❌ Bridge başlatılamadı: {ex.Message}");
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (bridge == null) return;
        if (paused)
            bridge.Call("stopListening");
        else if (sensorReady)
            bridge.Call("startListening");
    }

    private void OnDestroy()
    {
        bridge?.Call("stopListening");
        bridge?.Dispose();
        bridge = null;
    }

#endif
// ─────────────────────────────────────────
// EDITOR BLOĞU
// ─────────────────────────────────────────
#if UNITY_EDITOR
    private void Update()
    {
        float mouseNorm = Input.mousePosition.x / Screen.width;
        float simLux    = mouseNorm * editorMaxSimLux;

        if (Mathf.Abs(simLux - currentLux) > 0.5f)
        {
            currentLux    = simLux;
            debugRawLux   = currentLux;
            OnLuxChanged?.Invoke(currentLux);
        }
    }
#endif

    // ─────────────────────────────────────────
    // JAVA CALLBACK'LERİ — her platformda tanımlı olmalı
    // (UnitySendMessage string parametreli metodları her zaman public ve erişilebilir ister)
    // ─────────────────────────────────────────
    public void OnLuxValueChanged(string luxStr)
    {
        if (float.TryParse(
            luxStr,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float lux))
        {
            currentLux    = lux;
            debugRawLux   = lux;
            OnLuxChanged?.Invoke(currentLux);
        }
        else
        {
            Debug.LogWarning($"[SENSOR] Geçersiz lux değeri: '{luxStr}'");
        }
    }

    public void OnSensorStatusChanged(string status)
    {
        debugSensorStatus = status;
        Debug.Log($"[SENSOR] Durum: {status}");

        if (status == "ERROR:NO_LIGHT_SENSOR")
        {
            sensorReady = false;
            Debug.LogError("[SENSOR] ❌ Cihazda ışık sensörü bulunamadı!");
        }

        OnSensorStatus?.Invoke(status);
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────
    public float GetCurrentLux() => currentLux;
    public bool  IsSensorReady() => sensorReady;
}
