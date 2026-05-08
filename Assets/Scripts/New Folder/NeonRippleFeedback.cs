using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NeonRippleFeedback : MonoBehaviour, IPointerDownHandler
{
    public Color rippleColor = Color.cyan;
    public float expandRadius = 1.2f;
    public float pulseScale = 1.03f;
    public float duration = 0.45f;

    private RectTransform rectTransform;
    private List<GameObject> activeRipples = new List<GameObject>();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayRipple(eventData.position);
    }

    public void PlayRipple(Vector2 screenPos)
    {
        StartCoroutine(RippleRoutine(screenPos));
        StartCoroutine(PulseRoutine());
    }

    private IEnumerator RippleRoutine(Vector2 screenPos)
    {
        // Ripple objesi oluþtur
        GameObject rippleGO = new GameObject("Ripple");
        rippleGO.transform.SetParent(transform, false);

        RectTransform rt = rippleGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(20f, 20f);
        rt.anchoredPosition = Vector2.zero;

        Image img = rippleGO.AddComponent<Image>();
        img.color = new Color(rippleColor.r, rippleColor.g, rippleColor.b, 0.7f);
        img.raycastTarget = false;

        // Daire þekli için sprite — basit kare, ama scale ile büyür
        activeRipples.Add(rippleGO);

        float t = 0f;
        Vector2 parentSize = rectTransform.rect.size;
        float maxSize = Mathf.Max(parentSize.x, parentSize.y) * expandRadius * 2f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = t / duration;

            float size = Mathf.Lerp(20f, maxSize, n);
            float alpha = Mathf.Lerp(0.6f, 0f, n);

            rt.sizeDelta = new Vector2(size, size);
            img.color = new Color(rippleColor.r, rippleColor.g, rippleColor.b, alpha);

            yield return null;
        }

        activeRipples.Remove(rippleGO);
        Destroy(rippleGO);
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 base3 = Vector3.one;
        Vector3 peak = Vector3.one * pulseScale;
        float half = 0.08f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(base3, peak, t / half);
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(peak, base3, t / half);
            yield return null;
        }
        transform.localScale = base3;
    }
}