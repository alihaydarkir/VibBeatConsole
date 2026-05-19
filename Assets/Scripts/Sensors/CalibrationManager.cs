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
    [SerializeField] private float debugMinLux    = 0f;
    [SerializeField] private float debugMaxLux    = 50000f;
    [SerializeField] private float debugFloor     = 0f;
    [SerializeField] private float debugRange     = 0f;
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
            Debug.LogError("[CALIBRATION] [HATA] SensorController sahnede bulunamadi!");
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
        // Önceki çalışan kalibrasyon varsa durdur — Retry'da çakışmayı önler
        StopAllCoroutines();
        CancelInvoke(nameof(DelayedStart));
        isCalibrated = false;

        if (gameObject.activeInHierarchy)
            StartCoroutine(CalibrationRoutine());
        else
        {
            Debug.LogWarning("[CALIBRATION] GameObject inactive — coroutine gecikmeli baslatildi.");
            Invoke(nameof(DelayedStart), 0.1f);
        }
    }

    private void DelayedStart()
    {
        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
            StartCoroutine(CalibrationRoutine());
        else
            Debug.LogError("[CALIBRATION] GameObject hala inactive!");
    }

    private IEnumerator CalibrationRoutine()
    {
        bool isEditor = Application.platform != RuntimePlatform.Android;

        // ── ADIM 1: Minimum (el kapalı) ──────────────────────────────────
        // Faz 1 toplam 2.5s: 1.5s sensör stabilize + 1s örnekle
        OnCalibrationMessage?.Invoke("Elini kapat ve bekle...");
        yield return new WaitForSeconds(isEditor ? 0.3f : 1.5f); // sensör stabilize olsun

        if (isEditor)
        {
            minLux = 0f;
        }
        else
        {
            // Zaman bazlı örnekleme — FPS'ten bağımsız
            float minSum   = 0f;
            int   minCount = 0;
            float elapsed  = 0f;
            while (elapsed < 1.0f)
            {
                minSum  += sensorController.GetCurrentLux();
                minCount++;
                elapsed += Time.deltaTime;
                yield return null;
            }
            minLux = minCount > 0 ? minSum / minCount : 0f;
        }
        debugMinLux = minLux;
        OnCalibrationMessage?.Invoke($"Min: {minLux:F1} lux");
        Debug.Log($"[CALIBRATION] Min ölçüldü: {minLux:F1} lux");

        // ── ADIM 2: Maximum (el açık) ─────────────────────────────────────
        // Faz 2 toplam 2.5s: 1.5s el açılsın + 1s örnekle
        OnCalibrationMessage?.Invoke("Elini ac, 15cm yukari kaldir...");
        yield return new WaitForSeconds(isEditor ? 0.3f : 1.5f); // el tamamen açılsın

        if (isEditor)
        {
            maxLux = 50000f;
        }
        else
        {
            float maxSum   = 0f;
            int   maxCount = 0;
            float elapsed  = 0f;
            while (elapsed < 1.0f)
            {
                maxSum  += sensorController.GetCurrentLux();
                maxCount++;
                elapsed += Time.deltaTime;
                yield return null;
            }
            maxLux = maxCount > 0 ? maxSum / maxCount : 50000f;
        }
        debugMaxLux = maxLux;
        OnCalibrationMessage?.Invoke($"Max: {maxLux:F1} lux");
        Debug.Log($"[CALIBRATION] Max ölçüldü: {maxLux:F1} lux");

        // Validasyon — max en az 3 lux olmalı VE max > min + 3
        float diff = maxLux - minLux;
        Debug.Log($"[CALIBRATION] Olculdu — Min:{minLux:F1}  Max:{maxLux:F1}  Fark:{diff:F1}");
        if (maxLux < 2f || diff < 2f)
        {
            Debug.LogWarning($"[CALIBRATION] Basarisiz — fark:{diff:F1} (min 2 gerekli)");
            OnCalibrationMessage?.Invoke("Hata: Isik farki yetersiz. El kapatma/acmayi tekrar dene.");
            yield break;
        }

        isCalibrated = true;
        SaveCalibrationData();
        OnCalibrationMessage?.Invoke("Kalibrasyon tamamlandi!");
        OnCalibrationComplete?.Invoke();
        Debug.Log($"[CALIBRATION] ✓ Min:{minLux:F1} Max:{maxLux:F1} Fark:{maxLux - minLux:F1}");
    }

    // Min bu eşiğin altındaysa taban olarak 0 kullan (karanlık ortamlar için geniş tut)
    private const float MIN_FLOOR_THRESHOLD = 10f;

    // --- Normalize Fonksiyonu ---
    public float NormalizeLuxValue(float luxValue)
    {
        if (!isCalibrated)
            return 0.5f;

        // min < 10 → taban=0 (karanlık/az ışık), min ≥ 10 → taban=minLux (normal/parlak)
        float floor = minLux < MIN_FLOOR_THRESHOLD ? 0f : minLux;
        float range = maxLux - floor;

        if (range < 1f)
            return 0.5f;

        debugFloor = floor;
        debugRange = range;

        float normalized = (luxValue - floor) / range;
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