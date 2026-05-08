using UnityEngine;
using System.Collections;

public class CalibrationManager : MonoBehaviour
{
    // --- Bağımlılıklar ---
    private SensorController sensorController;

    // --- Kalibrasyon Verileri ---
    private float minLux = 0f;
    private float maxLux = 50000f;
    private bool isCalibrated = false;
    private const string SAVE_KEY = "VibBeat_Calibration";

    // --- Inspector ---
    [SerializeField] private float debugMinLux = 0f;
    [SerializeField] private float debugMaxLux = 50000f;
    [SerializeField] private float debugNormalized = 0f;

    // --- Events ---
    public delegate void OnCalibrationMessageDelegate(string message);
    public event OnCalibrationMessageDelegate OnCalibrationMessage;

    public delegate void OnCalibrationCompleteDelegate();
    public event OnCalibrationCompleteDelegate OnCalibrationComplete;

    private void Awake()
    {
        sensorController = FindFirstObjectByType<SensorController>();
        if (sensorController == null)
            Debug.LogError("[CALIBRATION] [HATA] SensorController sahnede bulunamadı!");
        else
            Debug.Log("[CALIBRATION] [OK] SensorController bulundu.");
    }

    private void Start()
    {
        LoadCalibrationData();
    }

    // --- Kalibrasyon Başlat ---
    public void StartCalibration()
    {
        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        // Editor'da gerçek sensör yok — simüle değerler kullan
        bool isEditor = Application.platform != RuntimePlatform.Android;

        // ADIM 1: Minimum Lux (el kapalı)
        OnCalibrationMessage?.Invoke("Elini kapat ve bekle...");
        yield return new WaitForSeconds(isEditor ? 0.5f : 2f);

        if (isEditor)
        {
            // Editor: mouse solda = kapalı = düşük lux simülasyonu
            minLux = 0f;
        }
        else
        {
            float minSum = 0f;
            int samples = 60;
            for (int i = 0; i < samples; i++)
            {
                minSum += sensorController.GetCurrentLux();
                yield return null;
            }
            minLux = minSum / samples;
        }
        debugMinLux = minLux;
        OnCalibrationMessage?.Invoke($"Min kaydedildi: {minLux:F1} lux");

        yield return new WaitForSeconds(isEditor ? 0.3f : 1f);

        // ADIM 2: Maximum Lux (el açık)
        OnCalibrationMessage?.Invoke("Elini ac, 15cm yukari kaldir...");
        yield return new WaitForSeconds(isEditor ? 0.5f : 2f);

        if (isEditor)
        {
            // Editor: simüle max = 50000 lux (tam açık)
            maxLux = 50000f;
        }
        else
        {
            float maxSum = 0f;
            int samples = 60;
            for (int i = 0; i < samples; i++)
            {
                maxSum += sensorController.GetCurrentLux();
                yield return null;
            }
            maxLux = maxSum / samples;
        }
        debugMaxLux = maxLux;
        OnCalibrationMessage?.Invoke($"Max kaydedildi: {maxLux:F1} lux");

        yield return new WaitForSeconds(isEditor ? 0.3f : 1f);

        // Tamamlandı
        isCalibrated = true;
        SaveCalibrationData();
        OnCalibrationMessage?.Invoke("Kalibrasyon tamamlandi!");
        OnCalibrationComplete?.Invoke();

        Debug.Log($"[CALIBRATION] Min:{minLux:F1} Max:{maxLux:F1}");
    }

    // --- Normalize Fonksiyonu ---
    public float NormalizeLuxValue(float luxValue)
    {
        if (!isCalibrated)
            return 0.5f;

        if (Mathf.Approximately(maxLux, minLux))
            return 0.5f;

        float normalized = (luxValue - minLux) / (maxLux - minLux);
        normalized = Mathf.Clamp01(normalized);
        debugNormalized = normalized;
        return normalized;
    }

    // --- Kaydet / Yükle ---
    private void SaveCalibrationData()
    {
        PlayerPrefs.SetFloat(SAVE_KEY + "_min", minLux);
        PlayerPrefs.SetFloat(SAVE_KEY + "_max", maxLux);
        PlayerPrefs.SetInt(SAVE_KEY + "_done", 1);
        PlayerPrefs.Save();
        Debug.Log("[CALIBRATION] [KAYDET] Kaydedildi!");
    }

    private void LoadCalibrationData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY + "_done"))
        {
            float savedMin = PlayerPrefs.GetFloat(SAVE_KEY + "_min");
            float savedMax = PlayerPrefs.GetFloat(SAVE_KEY + "_max");

            // Validasyon: Min >= Max ise kalibrasyon bozuk — sil ve sıfırla
            if (savedMin >= savedMax)
            {
                Debug.LogWarning($"[CALIBRATION] [UYARI] Bozuk kalibrasyon (Min:{savedMin:F1} >= Max:{savedMax:F1}) — silindi, yeniden kalibrasyon gerekli!");
                ClearCalibrationData();
            }
            else
            {
                minLux = savedMin;
                maxLux = savedMax;
                isCalibrated = true;
                debugMinLux = minLux;
                debugMaxLux = maxLux;
                Debug.Log($"[CALIBRATION] [YUKLE] Yüklendi! Min:{minLux:F1} Max:{maxLux:F1}");
            }
        }
        else
        {
            Debug.Log("[CALIBRATION] [UYARI] Kayıt yok, kalibrasyon gerekli!");
        }
    }

    public void ClearCalibrationData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY + "_min");
        PlayerPrefs.DeleteKey(SAVE_KEY + "_max");
        PlayerPrefs.DeleteKey(SAVE_KEY + "_done");
        PlayerPrefs.Save();
        minLux       = 0f;
        maxLux       = 50000f;
        isCalibrated = false;
        debugMinLux  = minLux;
        debugMaxLux  = maxLux;
        Debug.Log("[CALIBRATION] [SIL] Kalibrasyon verisi silindi.");
    }

    // --- Getters ---
    public bool IsCalibrated() => isCalibrated;
    public float GetMinLux() => minLux;
    public float GetMaxLux() => maxLux;
}