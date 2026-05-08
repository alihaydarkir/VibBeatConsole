using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Enhanced ripple effect with circular shape
/// Daire þeklinde ripple animasyonlarý saðlar
/// </summary>
public class EnhancedRippleEffect : MonoBehaviour, IPointerDownHandler
{
    [System.Serializable]
    public class RippleSettings
    {
        public Color rippleColor = Color.cyan;
        public float expandRadius = 1.5f;
        public float duration = 0.5f;
        public float maxAlpha = 0.8f;
        public AnimationCurve fadeCurve;
    }

    public RippleSettings settings = new RippleSettings();
    public float buttonPulseScale = 1.05f;
    public float buttonPulseDuration = 0.3f;
    
    // Daire þekli seçenekleri
    public bool useCircleShape = true;
    public bool useShader = false; // Shader kullanmak isterseniz true yap

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image buttonImage;
    private Color originalButtonColor;

    private void Awake()
    {
        if (settings.fadeCurve == null)
            settings.fadeCurve = AnimationCurve.Linear(0, 0, 1, 1);

        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
            originalButtonColor = buttonImage.color;
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayRippleEffect();
    }

    public void PlayRippleEffect()
    {
        StartCoroutine(RippleRoutine());
        StartCoroutine(ButtonPulseRoutine());
    }

    private IEnumerator RippleRoutine()
    {
        // Ripple objesi oluþtur
        GameObject rippleGO = new GameObject("RippleEffect");
        rippleGO.transform.SetParent(transform, false);

        RectTransform rt = rippleGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Image rippleImage = rippleGO.AddComponent<Image>();
        rippleImage.raycastTarget = false;
        
        // Daire þekli ayarla
        if (useCircleShape)
        {
            if (useShader)
            {
                // Shader-based daire
                CircleRippleUtility.ApplyCircleShader(rippleImage);
            }
            else
            {
                // Geometry-based daire (texture ile)
                CircleRippleUtility.MakeCircleImage(rippleImage, 128);
            }
        }

        float elapsed = 0f;
        Vector2 parentSize = rectTransform.rect.size;
        float maxSize = Mathf.Max(parentSize.x, parentSize.y) * settings.expandRadius * 2f;
        
        // Baþlangýç boyutu
        float initialSize = 30f;
        rt.sizeDelta = new Vector2(initialSize, initialSize);

        while (elapsed < settings.duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settings.duration);
            float easedT = settings.fadeCurve.Evaluate(t);

            // Boyut artýþý - daire þekilde ölçekle
            // sizeDelta'yý eþit deðerlerle set et (kare mantýðýyla daire oluþtur)
            float currentSize = Mathf.Lerp(initialSize, maxSize, t);

            // Alpha solmasý
            float currentAlpha = Mathf.Lerp(settings.maxAlpha, 0f, easedT);

            // Daire þekli korumak için width = height
            rt.sizeDelta = new Vector2(currentSize, currentSize);
            rippleImage.color = new Color(settings.rippleColor.r, settings.rippleColor.g, 
                                         settings.rippleColor.b, currentAlpha);

            yield return null;
        }

        Destroy(rippleGO);
    }

    private IEnumerator ButtonPulseRoutine()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 pulseScale = Vector3.one * buttonPulseScale;
        float halfDuration = buttonPulseDuration * 0.5f;
        float elapsed = 0f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(originalScale, pulseScale, 
                                               Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        // Scale down
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
