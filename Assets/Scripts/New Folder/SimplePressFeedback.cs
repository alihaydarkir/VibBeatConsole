using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(RectTransform))]
public class SimplePressFeedback : MonoBehaviour, IPointerDownHandler
{
    public Color idleColor = Color.white;
    public Color pressColor = Color.cyan;
    public float colorReturnDuration = 0.18f;
    public float pulseDuration = 0.14f;
    public float pulseScale = 1.04f;

    private Image targetImage;
    private RectTransform rectTransform;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        if (targetImage != null) targetImage.color = idleColor;
    }

    public void OnPointerDown(PointerEventData eventData) { PlayFeedback(); }

    public void PlayFeedback()
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FeedbackRoutine());
    }

    private IEnumerator FeedbackRoutine()
    {
        if (targetImage == null || rectTransform == null) yield break;

        targetImage.color = pressColor;
        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = Vector3.one * pulseScale;
        float halfPulse = Mathf.Max(0.01f, pulseDuration * 0.5f);
        float timer = 0f;

        while (timer < halfPulse)
        {
            timer += Time.unscaledDeltaTime;
            rectTransform.localScale = Vector3.Lerp(baseScale, targetScale, Mathf.Clamp01(timer / halfPulse));
            yield return null;
        }

        timer = 0f;
        while (timer < halfPulse)
        {
            timer += Time.unscaledDeltaTime;
            rectTransform.localScale = Vector3.Lerp(targetScale, baseScale, Mathf.Clamp01(timer / halfPulse));
            yield return null;
        }

        timer = 0f;
        Color startColor = targetImage.color;
        while (timer < colorReturnDuration)
        {
            timer += Time.unscaledDeltaTime;
            targetImage.color = Color.Lerp(startColor, idleColor, Mathf.Clamp01(timer / colorReturnDuration));
            yield return null;
        }

        targetImage.color = idleColor;
        rectTransform.localScale = Vector3.one;
        feedbackRoutine = null;
    }
}