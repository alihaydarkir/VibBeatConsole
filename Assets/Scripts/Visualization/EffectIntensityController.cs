using UnityEngine;

/// <summary>
/// Efekt yogunlugunu tüm görsel sistemlere dagitan merkezi kontrol.
///
/// Ayarlar ekranindaki Dusuk/Orta/Yuksek butonlari bu scripti cagirır.
/// Bu script da GuitarWaveVisualizer, RippleEffect vb. sistemlere
/// multiplier degerini iletir.
///
/// Seviyeler:
///   Dusuk  (0) → barlar az buyur, ripple soluk, kalibrasyon sakin
///   Orta   (1) → varsayilan denge
///   Yuksek (2) → barlar cok buyur, ripple parlak, kalibrasyon enerjik
/// </summary>
public class EffectIntensityController : MonoBehaviour
{
    public static EffectIntensityController Instance { get; private set; }

    // ─── Seviye Tanimlari ────────────────────────────────────────────────────
    [System.Serializable]
    public class IntensityLevel
    {
        public string label;

        [Header("Dalga (Guitar)")]
        [Range(0.1f, 2f)] public float waveMultiplier   = 1f;
        [Range(0f, 0.5f)] public float waveNoise        = 0.12f;
        [Range(0.1f, 2f)] public float waveSpeed        = 1.8f;

        [Header("Ripple")]
        [Range(0.1f, 1f)] public float rippleAlpha      = 0.75f;
        [Range(0.1f, 2f)] public float rippleSpeed      = 0.6f;
        [Range(1, 5)]     public int   rippleCount      = 1;
        [Range(0.05f,0.5f)]public float rippleWidth     = 0.12f;

        [Header("Kalibrasyon Ring")]
        [Range(0.1f, 2f)] public float calibPulseSpeed  = 1f;
        [Range(0.5f, 2f)] public float calibRingScale   = 1f;
    }

    [Header("Efekt Seviyeleri")]
    public IntensityLevel[] levels = new IntensityLevel[3]
    {
        new IntensityLevel {
            label          = "Dusuk",
            waveMultiplier = 0.5f,
            waveNoise      = 0.05f,
            waveSpeed      = 1.2f,
            rippleAlpha    = 0.40f,
            rippleSpeed    = 0.9f,
            rippleCount    = 1,
            rippleWidth    = 0.08f,
            calibPulseSpeed= 0.7f,
            calibRingScale = 0.8f
        },
        new IntensityLevel {
            label          = "Orta",
            waveMultiplier = 1.0f,
            waveNoise      = 0.12f,
            waveSpeed      = 1.8f,
            rippleAlpha    = 0.75f,
            rippleSpeed    = 0.6f,
            rippleCount    = 1,
            rippleWidth    = 0.12f,
            calibPulseSpeed= 1.0f,
            calibRingScale = 1.0f
        },
        new IntensityLevel {
            label          = "Yuksek",
            waveMultiplier = 1.6f,
            waveNoise      = 0.22f,
            waveSpeed      = 2.5f,
            rippleAlpha    = 1.00f,
            rippleSpeed    = 0.35f,
            rippleCount    = 3,
            rippleWidth    = 0.18f,
            calibPulseSpeed= 1.5f,
            calibRingScale = 1.3f
        }
    };

    [Header("Baslangiç Seviyesi")]
    [SerializeField] private int currentLevel = 1; // Orta

    // Sistem referanslari (otomatik bulunur)
    private GuitarWaveVisualizer guitarWave;
    private RippleEffect         ripple;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        guitarWave = FindFirstObjectByType<GuitarWaveVisualizer>();
        ripple     = FindFirstObjectByType<RippleEffect>();
        ApplyLevel(currentLevel);
        Debug.Log($"[EFFECT] Baslangic seviye: {levels[currentLevel].label}");
    }

    // ─────────────────────────────────────────
    /// <summary>Bootstrap settings butonlari bu metodu cagirir.</summary>
    public void SetLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
        { Debug.LogWarning($"[EFFECT] Gecersiz seviye: {levelIndex}"); return; }

        currentLevel = levelIndex;
        ApplyLevel(levelIndex);
        Debug.Log($"[EFFECT] Seviye: {levels[levelIndex].label}");
    }

    public int GetCurrentLevel() => currentLevel;

    private void ApplyLevel(int idx)
    {
        if (idx < 0 || idx >= levels.Length) return;
        var lv = levels[idx];

        // Gitar dalgasi
        if (guitarWave != null)
        {
            guitarWave.EffectMultiplier = lv.waveMultiplier;
            guitarWave.NoiseAmount      = lv.waveNoise;
            guitarWave.NoiseSpeed       = lv.waveSpeed;
        }

        // Ripple
        if (ripple != null)
        {
            ripple.StartAlpha     = lv.rippleAlpha;
            ripple.ExpansionSpeed = lv.rippleSpeed;
            ripple.RingCount      = lv.rippleCount;
            ripple.RingWidth      = lv.rippleWidth;
        }
    }
}
