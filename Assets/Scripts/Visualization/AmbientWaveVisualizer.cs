using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Splash ekrani icin ambient dalga gorsellestirmesi.
/// Sensor bagli degil — tamamen Perlin noise + sin ile
/// muzik ritmi gibi organik hareket uretir.
/// </summary>
public class AmbientWaveVisualizer : MonoBehaviour
{
    [Header("Container")]
    [Tooltip("Dalganin icine cizilecegi RectTransform")]
    [SerializeField] private RectTransform container;

    [Header("Cubuk Ayarlari")]
    [Range(8, 256)]
    [SerializeField] private int   barCount     = 24;
    [Range(0f, 0.5f)]
    [SerializeField] private float barGapRatio  = 0.18f;
    [Range(0f, 0.48f)]
    [SerializeField] private float maxHalfHeight = 0.42f;
    [Range(0f, 0.08f)]
    [SerializeField] private float minHalfHeight = 0.015f;

    [Header("Hareket")]
    [SerializeField] private float baseSpeed    = 1.2f;   // ana ritim hizi
    [SerializeField] private float beatSpeed    = 2.8f;   // vurus hizi
    [SerializeField] private float beatStrength = 0.55f;  // vurus gucu
    [SerializeField] private float waveTravel   = 3.5f;   // dalgayi ortadan kenara yayan katsayi

    [Header("Renk")]
    [SerializeField] private Color colorA = new Color(0.00f, 0.55f, 1.00f, 1f); // mavi
    [SerializeField] private Color colorB = new Color(0.55f, 0.00f, 1.00f, 1f); // mor
    [SerializeField] private Color colorC = new Color(0.00f, 0.90f, 0.75f, 1f); // cyan

    private RectTransform[] bars;
    private Image[]         barImages;
    private float[]         noiseSeeds;
    private float           updateTimer;
    private const float     UPDATE_INTERVAL = 0.033f; // ~30fps

    // ─────────────────────────────────────────
    private void Start()
    {
        if (container == null)
        {
            Debug.LogError("[AMBIENT_WAVE] Container atanmadi!");
            return;
        }
        Build();
        Debug.Log($"[AMBIENT_WAVE] [OK] {barCount} cubuk baslatildi.");
    }

    private void Build()
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        bars      = new RectTransform[barCount];
        barImages = new Image[barCount];
        noiseSeeds = new float[barCount];

        float gap  = barGapRatio / barCount;
        float barW = (1f - gap * barCount) / barCount;
        if (barW < 0.008f) barW = 0.008f;

        for (int i = 0; i < barCount; i++)
        {
            float x0 = i * (barW + gap);
            float x1 = x0 + barW;

            var go  = new GameObject($"ABar_{i}");
            go.transform.SetParent(container, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = colorA;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, 0.5f - minHalfHeight);
            rt.anchorMax = new Vector2(x1, 0.5f + minHalfHeight);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            bars[i]      = rt;
            barImages[i] = img;
            noiseSeeds[i] = Random.Range(0f, 100f);
        }
    }

    private void Update()
    {
        if (bars == null) return;

        updateTimer += Time.deltaTime;
        if (updateTimer < UPDATE_INTERVAL) return;
        updateTimer = 0f;

        float t    = Time.time;
        float beat = Mathf.Abs(Mathf.Sin(t * beatSpeed)); // 0-1 arasi vuruslar

        for (int i = 0; i < barCount; i++)
        {
            if (bars[i] == null) continue;

            float norm   = (float)i / barCount;          // 0-1
            float center = Mathf.Abs(norm - 0.5f) * 2f; // 0=orta 1=kenar
            float seed   = noiseSeeds[i];

            // Dalgayi ortadan kenara yayan faz kayma
            float phase  = center * waveTravel;

            // Cok katmanli noise — organik muzik hissi
            float layer1 = Mathf.Sin(t * baseSpeed  + phase + seed * 0.3f);
            float layer2 = Mathf.Sin(t * baseSpeed * 1.7f - phase * 0.6f + seed * 0.5f) * 0.5f;
            float layer3 = (Mathf.PerlinNoise(seed * 0.08f, t * 0.6f) - 0.5f) * 2f;

            // Vurus katkisi — tum cubuklari ayni anda etkiler ama kenarlar daha az
            float beatContrib = beat * beatStrength * (1f - center * 0.5f);

            float combined = (layer1 + layer2 + layer3) / 3f; // -1 ile 1 arasi
            float halfH    = minHalfHeight
                + (maxHalfHeight - minHalfHeight)
                * Mathf.Clamp01((combined + 1f) * 0.5f + beatContrib);

            halfH = Mathf.Clamp(halfH, minHalfHeight, maxHalfHeight);

            // Cift yon
            bars[i].anchorMin = new Vector2(bars[i].anchorMin.x, 0.5f - halfH);
            bars[i].anchorMax = new Vector2(bars[i].anchorMax.x, 0.5f + halfH);

            // Renk — zamana ve yukseklige gore
            float ct = (Mathf.Sin(t * 0.4f + norm * 2f) + 1f) * 0.5f; // 0-1 yavAS renk kayma
            float ht = halfH / maxHalfHeight;

            Color c;
            if (ct < 0.5f)
                c = Color.Lerp(colorA, colorB, ct * 2f);
            else
                c = Color.Lerp(colorB, colorC, (ct - 0.5f) * 2f);

            // Parlaklik: yuksek cubuklar daha parlak
            c   *= (0.4f + ht * 1.0f);
            c.a  = Mathf.Lerp(0.2f, 0.95f, ht + beat * 0.3f);

            barImages[i].color = c;
        }
    }
}
