using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gitar paneli cok bantli dalga gorsellestirmesi.
/// Cubuklar ortadan iki yone dogru buyur (yukari + asagi).
/// Sensor degeri → yukseklik + renk gradient.
/// </summary>
public class GuitarWaveVisualizer : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private RectTransform waveContainer;

    [Header("Cubuk Ayarlari")]
    [Range(8, 32)]
    [SerializeField] private int   barCount     = 16;
    [Range(0f, 0.5f)]
    [SerializeField] private float barGapRatio  = 0.25f;  // bosluk orani
    [Range(0f, 0.48f)]
    [SerializeField] private float maxHalfHeight = 0.42f; // merkeze gore max yari yukseklik
    [Range(0f, 0.1f)]
    [SerializeField] private float minHalfHeight = 0.02f;
    [SerializeField] private float noiseSpeed    = 1.8f;
    [SerializeField] private float noiseAmount   = 0.12f;
    [SerializeField] private float smoothSpeed   = 6f;

    [Header("Renk Gradyani")]
    [SerializeField] private Color colorLow  = new Color(0.00f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color colorMid  = new Color(0.00f, 1.00f, 0.75f, 1f);
    [SerializeField] private Color colorHigh = new Color(1.00f, 0.55f, 0.00f, 1f);
    [SerializeField] private Color colorPeak = new Color(1.00f, 0.10f, 0.45f, 1f);

    // EffectIntensityController tarafindan set edilir
    public float EffectMultiplier { get; set; } = 1f;
    public float NoiseAmount      { get => noiseAmount; set => noiseAmount = value; }
    public float NoiseSpeed       { get => noiseSpeed;  set => noiseSpeed  = value; }

    private RectTransform[] bars;
    private Image[]         barImages;
    private float[]         noiseOffsets;
    private float           currentVal = 0f;
    private float           targetVal  = 0f;
    private float           updateTimer;
    private const float     UPDATE_INTERVAL = 0.04f;

    // ─────────────────────────────────────────
    private void Start()
    {
        if (waveContainer == null)
            waveContainer = GameObject.Find("WaveformArea")
                ?.GetComponent<RectTransform>();

        if (waveContainer == null)
        { Debug.LogError("[GUITAR_WAVE] WaveformArea bulunamadi!"); return; }

        Build();
        Debug.Log($"[GUITAR_WAVE] [OK] {barCount} cubuk (cift yon).");
    }

    private void Build()
    {
        // Temizle
        for (int i = waveContainer.childCount - 1; i >= 0; i--)
            Destroy(waveContainer.GetChild(i).gameObject);

        bars        = new RectTransform[barCount];
        barImages   = new Image[barCount];
        noiseOffsets = new float[barCount];

        float totalWidth = 1f;
        float gap        = barGapRatio / barCount;
        float barW       = (totalWidth - gap * barCount) / barCount;
        if (barW < 0.01f) barW = 0.01f;

        for (int i = 0; i < barCount; i++)
        {
            float x0 = i * (barW + gap);
            float x1 = x0 + barW;

            var go  = new GameObject($"Bar_{i}");
            go.transform.SetParent(waveContainer, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            // Baslangicta merkez noktasinda (0.5 y)
            rt.anchorMin = new Vector2(x0, 0.5f - minHalfHeight);
            rt.anchorMax = new Vector2(x1, 0.5f + minHalfHeight);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            bars[i]       = rt;
            barImages[i]  = img;
            noiseOffsets[i] = Random.Range(0f, 100f);
        }
    }

    private void Update()
    {
        if (bars == null) return;

        currentVal = Mathf.Lerp(currentVal, targetVal, Time.deltaTime * smoothSpeed);

        updateTimer += Time.deltaTime;
        if (updateTimer < UPDATE_INTERVAL) return;
        updateTimer = 0f;

        float time = Time.time;
        float eff  = Mathf.Clamp01(EffectMultiplier);

        for (int i = 0; i < barCount; i++)
        {
            if (bars[i] == null) continue;

            // Her cubuk icin biyolojik dalga
            float phase    = noiseOffsets[i];
            float wave     = Mathf.Sin(time * noiseSpeed + phase) * noiseAmount;
            float perlin   = (Mathf.PerlinNoise(phase * 0.1f, time * 0.8f) - 0.5f)
                             * noiseAmount * 0.8f;

            // Ortadan uzaklasinca biraz daha az aktif (kelebek kanati profili)
            float center   = Mathf.Abs((float)i / barCount - 0.5f) * 2f; // 0=orta 1=kenar
            float profile  = 1f - center * 0.3f;

            float halfH    = Mathf.Lerp(minHalfHeight,
                maxHalfHeight * profile * eff,
                currentVal) + wave + perlin;
            halfH = Mathf.Clamp(halfH, minHalfHeight, maxHalfHeight);

            // Cift yon: 0.5 - halfH → 0.5 + halfH
            bars[i].anchorMin = new Vector2(bars[i].anchorMin.x, 0.5f - halfH);
            bars[i].anchorMax = new Vector2(bars[i].anchorMax.x, 0.5f + halfH);

            // Renk
            float t = currentVal * 0.65f + (halfH / maxHalfHeight) * 0.35f;
            Color c;
            if      (t < 0.33f) c = Color.Lerp(colorLow,  colorMid,  t / 0.33f);
            else if (t < 0.66f) c = Color.Lerp(colorMid,  colorHigh, (t-0.33f)/0.33f);
            else                c = Color.Lerp(colorHigh, colorPeak,  (t-0.66f)/0.34f);

            c   *= (0.5f + halfH / maxHalfHeight * 0.8f);
            c.a  = Mathf.Lerp(0.25f, 1f, currentVal * eff + halfH / maxHalfHeight * 0.4f);
            barImages[i].color = c;
        }
    }

    public void SetSensorValue(float v) => targetVal = Mathf.Clamp01(v);
}
