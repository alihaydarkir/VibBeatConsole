using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Gitar panelinde cok bantli, renk gecisli dalga gorsellestirmesi.
///
/// Sensör degeri arttikca:
///   - Daha fazla cubuk aktiflesir
///   - Cubuklar yukari dogru buyur
///   - Renkler mavi/cyan → yesil → sari → turuncu → kirmizi
///   - Her cubuk biraz farkli hizada animasyon yapar (organik his)
///
/// Inspector'da WaveContainer atanmazsa GuitarPanel/WaveformArea'yi arar.
/// </summary>
public class GuitarWaveVisualizer : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("WaveformArea RectTransform — bos bırakılırsa otomatik aranır")]
    [SerializeField] private RectTransform waveContainer;

    [Header("Cubuk Ayarlari")]
    [Range(8, 24)]
    [SerializeField] private int   barCount    = 14;
    [SerializeField] private float barSpacing  = 0.7f;   // cubuklar arasi bosluk orani
    [SerializeField] private float minBarHeight = 0.04f; // minimum yukseklik (0-1)
    [SerializeField] private float maxBarHeight = 0.92f; // maksimum yukseklik (0-1)
    [SerializeField] private float animSpeed   = 0.12f;  // DOTween sure
    [SerializeField] private float noiseAmount = 0.15f;  // organik titreme miktari

    [Header("Renkler (dusukten yuksege)")]
    [SerializeField] private Color colorLow    = new Color(0.00f, 0.60f, 1.00f, 1f); // mavi
    [SerializeField] private Color colorMid    = new Color(0.00f, 1.00f, 0.80f, 1f); // cyan-yesil
    [SerializeField] private Color colorHigh   = new Color(1.00f, 0.60f, 0.00f, 1f); // turuncu
    [SerializeField] private Color colorPeak   = new Color(1.00f, 0.10f, 0.40f, 1f); // kirmizi-pembe

    // ─── Özel alanlar ───────────────────────────────────────────────────────
    private RectTransform[] bars;
    private Image[]         barImages;
    private float[]         noiseOffsets;
    private float           currentValue = 0f;
    private float           targetValue  = 0f;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        if (waveContainer == null)
        {
            // GuitarPanel/WaveformArea'yi bul
            var vController = FindFirstObjectByType<VisualizationController>();
            GameObject found = GameObject.Find("WaveformArea");
            if (found != null)
                waveContainer = found.GetComponent<RectTransform>();
        }

        if (waveContainer == null)
        {
            Debug.LogError("[GUITAR_WAVE] WaveformArea bulunamadı! Inspector'dan ata.");
            return;
        }

        BuildBars();
        Debug.Log($"[GUITAR_WAVE] [OK] {barCount} cubuk olusturuldu.");
    }

    private void BuildBars()
    {
        bars        = new RectTransform[barCount];
        barImages   = new Image[barCount];
        noiseOffsets = new float[barCount];

        // Eski cubuklari temizle
        for (int i = waveContainer.childCount - 1; i >= 0; i--)
            Destroy(waveContainer.GetChild(i).gameObject);

        float totalSpacing = barSpacing * (barCount - 1);
        float barWidth     = (1f - totalSpacing) / barCount;
        if (barWidth < 0.02f) barWidth = 0.02f;

        for (int i = 0; i < barCount; i++)
        {
            float xStart = i * (barWidth + barSpacing / barCount);

            var go = new GameObject($"Bar_{i}");
            go.transform.SetParent(waveContainer, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xStart, 0f);
            rt.anchorMax = new Vector2(xStart + barWidth * (1f - barSpacing * 0.1f), minBarHeight);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            bars[i]      = rt;
            barImages[i] = img;
            noiseOffsets[i] = Random.Range(0f, 100f); // her cubuğa farklı noise başlangıcı
        }
    }

    // ─────────────────────────────────────────
    // UPDATE — sensör verisine gore animasyon
    // ─────────────────────────────────────────
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.05f; // 20fps yeterli, daha az CPU

    private void Update()
    {
        if (bars == null) return;

        // Yumusak gecis
        currentValue = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * 8f);

        updateTimer += Time.deltaTime;
        if (updateTimer < UPDATE_INTERVAL) return;
        updateTimer = 0f;

        UpdateBars();
    }

    private void UpdateBars()
    {
        float time = Time.time;

        for (int i = 0; i < barCount; i++)
        {
            if (bars[i] == null) continue;

            // Her cubuk biraz farkli pozisyonda — ortadan kenara dogru dalga
            float centerDist = Mathf.Abs(i - barCount * 0.5f) / (barCount * 0.5f);
            float wave       = Mathf.Sin(time * 3f + noiseOffsets[i] * 0.5f) * noiseAmount;
            float noise      = Mathf.PerlinNoise(noiseOffsets[i], time * 1.5f) * noiseAmount;

            // Aktif cubuk sayisi: dusuk sensor = sadece ortadakiler aktif
            float barThreshold = (float)i / barCount;
            float activeRatio  = Mathf.Clamp01(currentValue * 1.2f - barThreshold * 0.3f + 0.1f);

            float height = Mathf.Lerp(minBarHeight,
                maxBarHeight * activeRatio + wave + noise,
                activeRatio);
            height = Mathf.Clamp(height, minBarHeight, maxBarHeight);

            // Cubugu alt merkezden yukari dogru buyut
            bars[i].anchorMax = new Vector2(bars[i].anchorMax.x, height);

            // Renk: sensor degeri + yukseklige gore gradient
            float colorT    = (currentValue * 0.7f + (height / maxBarHeight) * 0.3f);
            Color barColor;
            if (colorT < 0.33f)
                barColor = Color.Lerp(colorLow,  colorMid,  colorT * 3f);
            else if (colorT < 0.66f)
                barColor = Color.Lerp(colorMid,  colorHigh, (colorT - 0.33f) * 3f);
            else
                barColor = Color.Lerp(colorHigh, colorPeak, (colorT - 0.66f) * 3f);

            // Parlaklik: yuksek cubuklar daha parlak
            barColor *= (0.6f + height / maxBarHeight * 0.8f);
            barColor.a = Mathf.Lerp(0.3f, 1f, activeRatio);

            barImages[i].color = barColor;
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API — MasterController bu metodu çağırır
    // ─────────────────────────────────────────
    public void SetSensorValue(float normalizedValue)
    {
        targetValue = Mathf.Clamp01(normalizedValue);
    }
}
