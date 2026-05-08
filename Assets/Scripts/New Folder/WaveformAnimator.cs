using UnityEngine;
using UnityEngine.UI;

public class WaveformAnimator : MonoBehaviour
{
    public GameObject[] bars;
    public int barCount = 32;
    public float speed = 2.2f;
    public float intensity = 1f;

    private float[] phases;
    private RectTransform[] rects;

    private void Start()
    {
        if (bars == null || bars.Length == 0) return;
        phases = new float[bars.Length];
        rects = new RectTransform[bars.Length];

        for (int i = 0; i < bars.Length; i++)
        {
            phases[i] = Random.Range(0f, Mathf.PI * 2f);
            if (bars[i] != null)
                rects[i] = bars[i].GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (rects == null) return;
        float t = Time.time * speed;

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] == null) continue;

            float wave1 = Mathf.Sin(t + phases[i]);
            float wave2 = Mathf.Sin(t * 1.7f + i * 0.4f) * 0.4f;
            float wave3 = Mathf.Sin(t * 0.5f + i * 0.15f) * 0.2f;
            float combined = (wave1 + wave2 + wave3) * 0.5f + 0.5f;

            float minH = 0.04f;
            float maxH = 0.92f;
            float height = Mathf.Lerp(minH, maxH, combined * intensity);

            float anchorMin = 0.5f - height * 0.5f;
            float anchorMax = 0.5f + height * 0.5f;

            Vector2 curMin = rects[i].anchorMin;
            Vector2 curMax = rects[i].anchorMax;

            rects[i].anchorMin = new Vector2(curMin.x,
                Mathf.Lerp(curMin.y, anchorMin, Time.deltaTime * 12f));
            rects[i].anchorMax = new Vector2(curMax.x,
                Mathf.Lerp(curMax.y, anchorMax, Time.deltaTime * 12f));
        }
    }

    // Dýþarýdan tetiklenebilir — basýldýðýnda spike
    public void TriggerHit(float hitIntensity = 1.5f)
    {
        StartCoroutine(HitRoutine(hitIntensity));
    }

    private System.Collections.IEnumerator HitRoutine(float hitIntensity)
    {
        float prev = intensity;
        intensity = hitIntensity;
        yield return new WaitForSeconds(0.12f);
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            intensity = Mathf.Lerp(hitIntensity, prev, t / 0.3f);
            yield return null;
        }
        intensity = prev;
    }
}