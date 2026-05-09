using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Gitar paneli sinüs dalga gorsellestirmesi.
/// Barlar yerine ince dikey cubuklar sinüs egrisini takip eder.
/// Sensor arttikca: dalga genisi, frekansi ve parlaklik artar.
/// Cok katmanli sinüs = organik muzik hissi.
/// </summary>
public class GuitarWaveVisualizer : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private RectTransform waveContainer;

    [Header("Sinüs Ayarlari")]
    [Range(16, 64)]
    [SerializeField] private int   barCount      = 32;    // daha fazla bar = daha yumusak egri
    [Range(0f, 0.48f)]
    [SerializeField] private float maxAmplitude  = 0.40f; // max dalga yuksekligi (0-0.5)
    [Range(0f, 0.06f)]
    [SerializeField] private float minAmplitude  = 0.02f; // idle genlik
    [SerializeField] private float waveFreq1     = 1.8f;  // ana dalga frekansi
    [SerializeField] private float waveFreq2     = 3.1f;  // ikinci harmonik
    [SerializeField] private float waveFreq3     = 5.0f;  // uc uncu harmonik
    [SerializeField] private float waveSpeed1    = 2.0f;  // ana dalga hizi
    [SerializeField] private float waveSpeed2    = 3.5f;  // ikinci dalga hizi
    [SerializeField] private float smoothSpeed   = 5f;

    [Header("Renk")]
    [SerializeField] private Color colorLow  = new Color(0.00f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color colorMid  = new Color(0.00f, 1.00f, 0.75f, 1f);
    [SerializeField] private Color colorHigh = new Color(1.00f, 0.55f, 0.00f, 1f);
    [SerializeField] private Color colorPeak = new Color(1.00f, 0.10f, 0.45f, 1f);

    // EffectIntensityController tarafindan set edilir
    public float EffectMultiplier { get; set; } = 1f;
    public float NoiseAmount      { get => noiseAmt;  set => noiseAmt  = value; }
    public float NoiseSpeed       { get => waveSpeed1; set => waveSpeed1 = value; }

    private RectTransform[] bars;
    private Image[]         barImages;
    private float           currentVal  = 0f;
    private float           targetVal   = 0f;
    private float           noiseAmt    = 0.08f;
    private float           updateTimer;
    private const float     UPDATE_INTERVAL = 0.033f;

    private void Start()
    {
        if (waveContainer == null)
            waveContainer = GameObject.Find("WaveformArea")?.GetComponent<RectTransform>();

        if (waveContainer == null)
        { Debug.LogError("[GUITAR_WAVE] WaveformArea bulunamadi!"); return; }

        Build();
        Debug.Log($"[GUITAR_WAVE] [OK] {barCount} sinüs cubugu hazir.");
    }

    private void Build()
    {
        for (int i = waveContainer.childCount - 1; i >= 0; i--)
            Destroy(waveContainer.GetChild(i).gameObject);

        bars      = new RectTransform[barCount];
        barImages = new Image[barCount];

        float barW = 1f / barCount;

        for (int i = 0; i < barCount; i++)
        {
            float x0 = i * barW;
            float x1 = x0 + barW * 0.82f; // ince bosluk

            var go  = new GameObject($"SBar_{i}");
            go.transform.SetParent(waveContainer, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            // Baslangicta merkez noktasi
            rt.anchorMin = new Vector2(x0, 0.5f - minAmplitude);
            rt.anchorMax = new Vector2(x1, 0.5f + minAmplitude);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            bars[i]      = rt;
            barImages[i] = img;
        }
    }

    private void Update()
    {
        if (bars == null) return;

        currentVal = Mathf.Lerp(currentVal, targetVal, Time.deltaTime * smoothSpeed);

        updateTimer += Time.deltaTime;
        if (updateTimer < UPDATE_INTERVAL) return;
        updateTimer = 0f;

        DrawSineWave();
    }

    private void DrawSineWave()
    {
        float t   = Time.time;
        float eff = Mathf.Clamp01(EffectMultiplier);
        float amp = Mathf.Lerp(minAmplitude, maxAmplitude * eff, currentVal);

        for (int i = 0; i < barCount; i++)
        {
            if (bars[i] == null) continue;

            // Normallestirilmis pozisyon (0-1)
            float p = (float)i / barCount;

            // Cok katmanli sinüs — muzik harmonigi hissi
            float s1 = Mathf.Sin(p * Mathf.PI * 2f * waveFreq1 + t * waveSpeed1);
            float s2 = Mathf.Sin(p * Mathf.PI * 2f * waveFreq2 - t * waveSpeed2) * 0.45f;
            float s3 = Mathf.Sin(p * Mathf.PI * 2f * waveFreq3 + t * waveSpeed1 * 1.3f) * 0.2f;

            // Sensor arttikca daha fazla harmonik katkisi
            float combined = s1 + s2 * currentVal + s3 * currentVal;
            combined /= (1f + 0.45f * currentVal + 0.2f * currentVal); // normalize

            // Noise katkisi
            float noise = (Mathf.PerlinNoise(p * 3f, t * 0.7f) - 0.5f) * noiseAmt * 2f;
            combined += noise * currentVal;

            float halfH = amp * Mathf.Abs(combined) + minAmplitude;
            halfH = Mathf.Clamp(halfH, minAmplitude, maxAmplitude);

            // Sinüs egrisine gore yukari-asagi offset
            float offset = combined * amp * 0.3f; // hafif offset — egri hissi
            float centerY = 0.5f + offset;

            bars[i].anchorMin = new Vector2(bars[i].anchorMin.x, centerY - halfH);
            bars[i].anchorMax = new Vector2(bars[i].anchorMax.x, centerY + halfH);

            // Renk — yükseklik + sensor değerine gore gradient
            float ct = currentVal * 0.6f + Mathf.Abs(combined) * 0.4f;
            Color c;
            if      (ct < 0.33f) c = Color.Lerp(colorLow,  colorMid,  ct / 0.33f);
            else if (ct < 0.66f) c = Color.Lerp(colorMid,  colorHigh, (ct-0.33f)/0.33f);
            else                 c = Color.Lerp(colorHigh, colorPeak,  (ct-0.66f)/0.34f);

            c   *= (0.4f + halfH / maxAmplitude * 1.0f);
            c.a  = Mathf.Lerp(0.15f, 1f, currentVal * 0.7f + Mathf.Abs(combined) * 0.3f);
            barImages[i].color = c;
        }
    }

    public void SetSensorValue(float v) => targetVal = Mathf.Clamp01(v);
}
