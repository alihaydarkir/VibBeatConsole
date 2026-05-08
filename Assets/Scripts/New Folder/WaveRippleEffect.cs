using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Müzik uygulamasý için geliþmiþ dalga þeklinde ripple efekti
/// Advanced wave ripple effect with line-based waves, color presets, and opacity control
/// </summary>
public class WaveRippleEffect : MonoBehaviour, IPointerDownHandler
{
    // Renk Ön Ayarlarý
    public enum ColorPreset
    {
        Cyan,           // Guitar
        Orange,         // Piano
        Magenta,        // Drum
        Rainbow,        // Spektrum
        Custom          // Özel renk
    }

    // Dalga Þekli Seçeneði
    public enum WaveStyle
    {
        SolidCircle,    // Dolu daire
        LineRings,      // Çizgi halkalar
        DashCircle,     // Kesikli daire
        WaveLines       // Dalgalý çizgiler
    }

    [System.Serializable]
    public class WaveSettings
    {
        [Header("Renk Ayarlarý")]
        public ColorPreset colorPreset = ColorPreset.Cyan;
        public Color customColor = Color.cyan;

        [Header("Dalga Þekli")]
        public WaveStyle waveStyle = WaveStyle.LineRings;
        public float waveWidth = 2.5f;        // Çizgi kalýnlýðý
        public float lineSegments = 32f;     // Dairenin kaç segmente bölüneceði

        [Header("Boyut & Hýz")]
        public float expandRadius = 1.8f;
        public float duration = 0.6f;
        public float initialSize = 15f;

        [Header("Opaklýk Kontrolü")]
        public float maxAlpha = 0.9f;
        public float minAlpha = 0f;
        public AnimationCurve opacityCurve;

        [Header("Dalga Parametreleri")]
        public int waveCount = 3;            // Ardýþýk dalga sayýsý
        public float waveDelay = 0.12f;      // Dalgalar arasý gecikme
        public AnimationCurve fadeCurve;

        [Header("Ek Seçenekler")]
        public bool usePulse = true;
        public bool useGlow = false;
        public float glowIntensity = 1.2f;
    }

    public WaveSettings settings = new WaveSettings();
    public float buttonPulseScale = 1.06f;
    public float buttonPulseDuration = 0.35f;

    private RectTransform rectTransform;
    private Image buttonImage;

    private void Awake()
    {
        if (settings.fadeCurve == null)
            settings.fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        if (settings.opacityCurve == null)
            settings.opacityCurve = AnimationCurve.Linear(0, 1, 1, 0);

        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayWaveRipple();
    }

    public void PlayWaveRipple()
    {
        StartCoroutine(MultiWaveRoutine());
        if (settings.usePulse)
            StartCoroutine(ButtonPulseRoutine());
    }

    private IEnumerator MultiWaveRoutine()
    {
        // Her dalga için bir gecikme ile baþlat
        for (int i = 0; i < settings.waveCount; i++)
        {
            StartCoroutine(SingleWaveRoutine(i * settings.waveDelay));
            yield return new WaitForSeconds(settings.waveDelay);
        }
    }

    private IEnumerator SingleWaveRoutine(float startDelay)
    {
        // Baþlama gecikmesi
        yield return new WaitForSeconds(startDelay);

        // Dalga objesi oluþtur
        GameObject waveGO = new GameObject($"WaveRipple_{startDelay:F2}");
        waveGO.transform.SetParent(transform, false);

        RectTransform waveRT = waveGO.AddComponent<RectTransform>();
        waveRT.anchorMin = new Vector2(0.5f, 0.5f);
        waveRT.anchorMax = new Vector2(0.5f, 0.5f);
        waveRT.anchoredPosition = Vector2.zero;

        Color waveColor = GetWaveColor();
        
        // Dalga þekline göre farklý görsel oluþtur
        switch (settings.waveStyle)
        {
            case WaveStyle.SolidCircle:
                CreateSolidWave(waveGO, waveColor);
                break;
            case WaveStyle.LineRings:
                CreateLineRingWave(waveGO, waveColor);
                break;
            case WaveStyle.DashCircle:
                CreateDashWave(waveGO, waveColor);
                break;
            case WaveStyle.WaveLines:
                CreateWaveLinesWave(waveGO, waveColor);
                break;
        }

        Image waveImage = waveGO.GetComponent<Image>();
        if (waveImage != null)
            waveImage.raycastTarget = false;

        float elapsed = 0f;
        Vector2 parentSize = rectTransform.rect.size;
        float maxSize = Mathf.Max(parentSize.x, parentSize.y) * settings.expandRadius * 2f;
        
        waveRT.sizeDelta = new Vector2(settings.initialSize, settings.initialSize);

        while (elapsed < settings.duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settings.duration);
            float easedT = settings.fadeCurve.Evaluate(t);

            // Opaklýk kontrolü
            float currentAlpha = Mathf.Lerp(settings.maxAlpha, settings.minAlpha, 
                settings.opacityCurve.Evaluate(t));

            // Boyut artýþý
            float currentSize = Mathf.Lerp(settings.initialSize, maxSize, t);

            waveRT.sizeDelta = new Vector2(currentSize, currentSize);
            
            if (waveImage != null)
            {
                waveImage.color = new Color(
                    waveColor.r, 
                    waveColor.g, 
                    waveColor.b, 
                    currentAlpha
                );
            }

            yield return null;
        }

        Destroy(waveGO);
    }

    /// <summary>
    /// Dolu daire dalga
    /// </summary>
    private void CreateSolidWave(GameObject waveGO, Color color)
    {
        Image img = waveGO.AddComponent<Image>();
        img.color = color;
        CircleRippleUtility.MakeCircleImage(img, (int)settings.lineSegments);
    }

    /// <summary>
    /// Çizgi halkalar (ring) - çizgi çizgi daire
    /// </summary>
    private void CreateLineRingWave(GameObject waveGO, Color color)
    {
        int segments = (int)settings.lineSegments;
        
        // Container oluþtur
        RectTransform container = waveGO.GetComponent<RectTransform>();
        
        // Her segment için bir çizgi oluþtur
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            
            GameObject lineGO = new GameObject($"Line_{i}");
            lineGO.transform.SetParent(waveGO.transform, false);
            
            RectTransform lineRT = lineGO.AddComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0.5f, 0.5f);
            lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.anchoredPosition = Vector2.zero;
            lineRT.sizeDelta = new Vector2(settings.waveWidth, settings.waveWidth);
            lineRT.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            
            Image lineImg = lineGO.AddComponent<Image>();
            lineImg.color = color;
            lineImg.raycastTarget = false;
        }

        waveGO.AddComponent<CanvasGroup>(); // Tüm çizgilerin alpha'sýný kontrol et
    }

    /// <summary>
    /// Kesikli daire - dash circle
    /// </summary>
    private void CreateDashWave(GameObject waveGO, Color color)
    {
        int segments = (int)settings.lineSegments;
        RectTransform container = waveGO.GetComponent<RectTransform>();
        
        for (int i = 0; i < segments; i++)
        {
            // Her 2 segmentte bir çizgi (kesikli efekt)
            if (i % 2 == 0)
            {
                float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                
                GameObject dashGO = new GameObject($"Dash_{i}");
                dashGO.transform.SetParent(waveGO.transform, false);
                
                RectTransform dashRT = dashGO.AddComponent<RectTransform>();
                dashRT.anchorMin = new Vector2(0.5f, 0.5f);
                dashRT.anchorMax = new Vector2(0.5f, 0.5f);
                dashRT.anchoredPosition = Vector2.zero;
                dashRT.sizeDelta = new Vector2(settings.waveWidth * 1.5f, settings.waveWidth);
                dashRT.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                
                Image dashImg = dashGO.AddComponent<Image>();
                dashImg.color = color;
                dashImg.raycastTarget = false;
            }
        }

        waveGO.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Dalgalý çizgiler - sinüs dalgasý efekti
    /// </summary>
    private void CreateWaveLinesWave(GameObject waveGO, Color color)
    {
        int segments = (int)settings.lineSegments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            float waveOffset = Mathf.Sin(i / (float)segments * Mathf.PI * 2f) * 5f;
            
            GameObject lineGO = new GameObject($"WaveLine_{i}");
            lineGO.transform.SetParent(waveGO.transform, false);
            
            RectTransform lineRT = lineGO.AddComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0.5f, 0.5f);
            lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * waveOffset,
                Mathf.Sin(angle) * waveOffset
            );
            lineRT.sizeDelta = new Vector2(settings.waveWidth, settings.waveWidth);
            lineRT.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            
            Image lineImg = lineGO.AddComponent<Image>();
            lineImg.color = color;
            lineImg.raycastTarget = false;
        }

        waveGO.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Renk ön ayarýna göre rengi al
    /// </summary>
    private Color GetWaveColor()
    {
        return settings.colorPreset switch
        {
            ColorPreset.Cyan => new Color(0f, 1f, 1f, 1f),           // #00F0FF
            ColorPreset.Orange => new Color(1f, 0.65f, 0f, 1f),      // #FFA500
            ColorPreset.Magenta => new Color(1f, 0.1f, 0.68f, 1f),   // #FF1AAD
            ColorPreset.Rainbow => GetRainbowColor(),
            ColorPreset.Custom => settings.customColor,
            _ => Color.white
        };
    }

    /// <summary>
    /// Spektrum rengi - örnek olarak
    /// </summary>
    private Color GetRainbowColor()
    {
        float hue = Random.value;
        return Color.HSVToRGB(hue, 1f, 1f);
    }

    private IEnumerator ButtonPulseRoutine()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 pulseScale = Vector3.one * buttonPulseScale;
        float halfDuration = buttonPulseDuration * 0.5f;
        float elapsed = 0f;

        // Büyüme
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(originalScale, pulseScale, 
                                               Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        // Küçülme
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(pulseScale, originalScale, 
                                               Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        transform.localScale = originalScale;
    }
}