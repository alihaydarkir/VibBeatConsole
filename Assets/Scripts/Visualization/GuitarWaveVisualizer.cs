using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gitar paneli sinyal dalgası — sürekli çizgi (osiloskop stili).
/// SignalLineRenderer'ı WaveformArea içinde dinamik olarak oluşturur.
/// </summary>
public class GuitarWaveVisualizer : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private RectTransform waveContainer;

    [Header("Sinyal")]
    [Range(64, 256)]
    [SerializeField] private int   resolution    = 128;
    [Range(0f, 0.48f)]
    [SerializeField] private float maxAmplitude  = 0.40f;
    [Range(0f, 0.06f)]
    [SerializeField] private float minAmplitude  = 0.015f;
    [SerializeField] private float freq1         = 1.8f;
    [SerializeField] private float freq2         = 3.1f;
    [SerializeField] private float freq3         = 5.0f;
    [SerializeField] private float speed1        = 2.0f;
    [SerializeField] private float speed2        = 3.5f;
    [SerializeField] private float smoothSpeed   = 5f;
    [Range(1f, 8f)]
    [SerializeField] private float lineThickness = 2.5f;

    [Header("Renk")]
    [SerializeField] private Color colorLow  = new Color(0.00f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color colorMid  = new Color(0.00f, 1.00f, 0.75f, 1f);
    [SerializeField] private Color colorHigh = new Color(1.00f, 0.55f, 0.00f, 1f);
    [SerializeField] private Color colorPeak = new Color(1.00f, 0.10f, 0.45f, 1f);

    public float EffectMultiplier { get; set; } = 1f;
    public float NoiseAmount { get => noiseAmt; set => noiseAmt = value; }
    public float NoiseSpeed  { get => speed1;   set => speed1   = value; }

    private SignalLineRenderer lineRenderer;
    private float currentVal = 0f;
    private float targetVal  = 0f;
    private float noiseAmt   = 0.08f;

    private void Start()
    {
        FindAndInit();
    }

    /// <summary>
    /// Build VibeBeat UI sonrası WaveformArea değişebilir.
    /// MasterController her frame'de sensör verisi verirken burası
    /// ilk çağrıda da yeniden init edebilir.
    /// </summary>
    private void FindAndInit()
    {
        // Container bul
        if (waveContainer == null)
        {
            var go = GameObject.Find("WaveformArea");
            if (go != null) waveContainer = go.GetComponent<RectTransform>();
        }

        if (waveContainer == null)
        {
            Debug.LogError("[GUITAR_WAVE] WaveformArea bulunamadi!");
            return;
        }

        // Eski SignalLineRenderer varsa temizle
        var existing = waveContainer.GetComponentInChildren<SignalLineRenderer>();
        if (existing != null) Destroy(existing.gameObject);

        // Yeni SignalLineRenderer objesi WaveformArea içine yarat
        var go2 = new GameObject("SignalLine");
        go2.transform.SetParent(waveContainer, false);

        var rt = go2.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        lineRenderer = go2.AddComponent<SignalLineRenderer>();
        lineRenderer.resolution    = resolution;
        lineRenderer.lineThickness = lineThickness;
        lineRenderer.lineColor     = colorLow;
        lineRenderer.raycastTarget = false;

        Debug.Log("[GUITAR_WAVE] [OK] Sinyal cizgisi WaveformArea'ya eklendi.");
    }

    private void Update()
    {
        // lineRenderer yoksa veya container değiştiyse yeniden init
        if (lineRenderer == null || !lineRenderer.gameObject.activeInHierarchy)
        {
            FindAndInit();
            return;
        }

        currentVal = Mathf.Lerp(currentVal, targetVal, Time.deltaTime * smoothSpeed);

        float t   = Time.time;
        float eff = Mathf.Clamp01(EffectMultiplier);
        float amp = Mathf.Lerp(minAmplitude, maxAmplitude * eff, currentVal);

        Vector2[] points = new Vector2[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float p = (float)i / (resolution - 1);

            float s1 = Mathf.Sin(p * Mathf.PI * 2f * freq1 + t * speed1);
            float s2 = Mathf.Sin(p * Mathf.PI * 2f * freq2 - t * speed2) * 0.45f;
            float s3 = Mathf.Sin(p * Mathf.PI * 2f * freq3 + t * speed1 * 1.3f) * 0.2f;
            float noise = (Mathf.PerlinNoise(p * 3f, t * 0.7f) - 0.5f) * noiseAmt * 2f;

            float combined = (s1 + s2 * currentVal + s3 * currentVal + noise * currentVal)
                           / (1f + 0.65f * currentVal);

            float y = 0.5f + combined * amp;
            points[i] = new Vector2(p, Mathf.Clamp(y, 0.05f, 0.95f));
        }

        float ct = currentVal;
        Color c;
        if      (ct < 0.33f) c = Color.Lerp(colorLow,  colorMid,  ct / 0.33f);
        else if (ct < 0.66f) c = Color.Lerp(colorMid,  colorHigh, (ct-0.33f)/0.33f);
        else                 c = Color.Lerp(colorHigh, colorPeak,  (ct-0.66f)/0.34f);
        c.a = Mathf.Lerp(0.4f, 1f, currentVal);

        lineRenderer.SetPoints(points, c, lineThickness);
    }

    public void SetSensorValue(float v) => targetVal = Mathf.Clamp01(v);
}
