using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OnboardingCardPulse : MonoBehaviour
{
    public Color accentColor = Color.cyan;
    public Image cardImage;

    private Color baseColor;
    private Color glowColor;

    private void Start()
    {
        if (cardImage == null) return;
        baseColor = cardImage.color;
        glowColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.08f);
        StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float offset = Random.Range(0f, Mathf.PI * 2f);

        while (true)
        {
            float t = (Mathf.Sin(Time.time * 1.4f + offset) + 1f) * 0.5f;
            Color cur = Color.Lerp(baseColor, glowColor, t * 0.6f);

            if (cardImage != null)
                cardImage.color = cur;

            yield return null;
        }
    }
}