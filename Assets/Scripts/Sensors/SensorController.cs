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
    [SerializeField] private float  debugRawLux       = 0f;
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
    // AndroidJavaObject her zaman tanımlı — sadece Android'de kullanılır
    // ─────────────────────────────────────────
    private float             currentLux  = 0f;
    private bool              sensorReady = false;
    private AndroidJavaObject bridge      = null;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            InitAndroid();
        }
        else
        {
            sensorReady       = true;
            debugSensorStatus = "Editor/PC simülasyon aktif";
            Debug.Log("[SENSOR] Editor modu — mouse X ile lux simüle ediliyor.");
        }
    }

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

    // ─────────────────────────────────────────
    // UPDATE — Editor simülasyonu
    // ─────────────────────────────────────────
    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android) return;

        float mouseNorm = Input.mousePosition.x / Screen.width;
        float simLux    = mouseNorm * editorMaxSimLux;

        if (Mathf.Abs(simLux - currentLux) > 0.5f)
        {
            currentLux    = simLux;
            debugRawLux   = currentLux;
            OnLuxChanged?.Invoke(currentLux);
        }
    }

    // ─────────────────────────────────────────
    // UYGULAMA YAŞAM DÖNGÜSÜ — pil tasarrufu
    // ─────────────────────────────────────────
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
        if (bridge == null) return;
        bridge.Call("stopListening");
        bridge.Dispose();
        bridge = null;
    }

    // ─────────────────────────────────────────
    // JAVA CALLBACK'LERİ
    // UnitySendMessage bu metodları isimle çağırır — her zaman public olmalı
    // ─────────────────────────────────────────
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
