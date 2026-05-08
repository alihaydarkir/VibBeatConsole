using UnityEngine;

public class SensorController : MonoBehaviour
{
    // --- Sensör Değerleri ---
    private float currentLuxValue = 0f;
    private bool isSensorReady = false;

    // --- Android JNI (her zaman tanımlı, sadece Android'de kullanılır) ---
    private AndroidJavaObject lightSensorBridge = null;

    // --- Event System ---
    public delegate void OnLuxChangedDelegate(float luxValue);
    public event OnLuxChangedDelegate OnLuxChanged;

    // --- Inspector ---
    [SerializeField] private float debugLuxValue = 0f;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidSensor();
#else
        isSensorReady = true;
        Debug.Log("[SENSOR] Editor modu - simülasyon aktif");
#endif
    }

    private void InitializeAndroidSensor()
    {
        try
        {
            var androidPlugin = new AndroidJavaClass(
                "com.vibbeat.sensors.AndroidLightSensorBridge"
            );
            lightSensorBridge = androidPlugin.CallStatic<AndroidJavaObject>("getInstance");
            lightSensorBridge.Call("startListening");

            isSensorReady = true;
            Debug.Log("[SENSOR] ✅ Light Sensor başlatıldı!");
        }
        catch (System.Exception ex)
        {
            isSensorReady = false;
            Debug.LogError($"[SENSOR] ❌ Hata: {ex.Message}");
        }
    }

    // --- Java'dan çağrılır ---
    public void OnLuxValueChanged(string luxValueStr)
    {
        if (float.TryParse(luxValueStr,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float lux))
        {
            currentLuxValue = lux;
            debugLuxValue = lux;
            OnLuxChanged?.Invoke(currentLuxValue);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        // Mouse X → Lux simülasyonu
        float mouseX = Input.mousePosition.x / Screen.width;
        currentLuxValue = mouseX * 50000f;
        debugLuxValue = currentLuxValue;
        OnLuxChanged?.Invoke(currentLuxValue);
#endif
    }

    public float GetCurrentLux() => currentLuxValue;
    public bool IsSensorReady() => isSensorReady;   

    private void OnDestroy()
    {
        if (lightSensorBridge != null)
        {
            lightSensorBridge.Call("stopListening");
            Debug.Log("[SENSOR] Sensör durduruldu.");
        }
    }
}