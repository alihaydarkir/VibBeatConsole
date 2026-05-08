using System.Collections;
using TMPro;
using UnityEngine;

public class CalibrationDemoController : MonoBehaviour
{
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI percentText;
    public TextMeshProUGUI luxText;
    public TextMeshProUGUI statusText;
    public float animationDuration = 2.2f;

    private Coroutine calibrationRoutine;

    private void OnEnable() { RestartCalibration(); }

    private void OnDisable()
    {
        if (calibrationRoutine != null)
        {
            StopCoroutine(calibrationRoutine);
            calibrationRoutine = null;
        }
    }

    public void RestartCalibration()
    {
        if (calibrationRoutine != null) StopCoroutine(calibrationRoutine);
        calibrationRoutine = StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        SetText(stepText, "1/2 Elini sensörün üstüne kapat");
        SetText(percentText, "0%");
        SetText(luxText, "Lux: 12.4");
        SetText(statusText, "Durum: Ölçülüyor");

        float timer = 0f;
        while (timer < animationDuration)
        {
            timer += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(timer / animationDuration);
            int percent = Mathf.RoundToInt(normalized * 100f);
            SetText(percentText, percent + "%");
            SetText(luxText, "Lux: " + Mathf.Lerp(12.4f, 86.7f, normalized).ToString("0.0"));

            if (percent < 50)
            {
                SetText(stepText, "1/2 Elini sensörün üstüne kapat");
                SetText(statusText, "Durum: Ölçülüyor");
            }
            else if (percent < 100)
            {
                SetText(stepText, "2/2 Elini sensörden uzaklaþtýr");
                SetText(statusText, "Durum: Analiz ediliyor");
            }
            yield return null;
        }

        SetText(stepText, "Kalibrasyon tamamlandý");
        SetText(percentText, "100%");
        SetText(statusText, "Durum: Hazýr");
        calibrationRoutine = null;
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }
}