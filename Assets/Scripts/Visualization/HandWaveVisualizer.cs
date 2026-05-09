using UnityEngine;

/// <summary>
/// Eldeki veya sensördeki sine dalgasını (osiloskop) 3D uzayda çizen görselleştirici.
/// UI'daki görünmezlik sorunu yerine, doğrudan el modelinize veya sensör merkezinize
/// bu script'i (veya boş bir objeyi) ekleyip LineRenderer ile anlık çalıştırabilirsiniz.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class HandWaveVisualizer : MonoBehaviour
{
    [Header("Dalga Ayarları")]
    [Range(32, 128)]
    [SerializeField] private int resolution = 64;
    [SerializeField] private float length = 1.0f;
    [SerializeField] private float maxAmplitude = 0.2f;
    [SerializeField] private float minAmplitude = 0.02f;

    [Header("Dalga Karakteri")]
    [SerializeField] private float freq1 = 2.0f;
    [SerializeField] private float freq2 = 4.0f;
    [SerializeField] private float speed = 3.0f;

    [Header("Renk & Görünüm")]
    [SerializeField] private Color waveColor = new Color(0f, 0.9f, 1f, 1f); // Cyan
    [SerializeField] private float thickness = 0.05f;

    private LineRenderer line;
    private float currentSensorValue = 0f;
    private float targetSensorValue = 0f;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = resolution;
        line.useWorldSpace = false; // Objenin yönüne göre yerel koordinatlar kullanır
        
        line.startWidth = thickness;
        line.endWidth = thickness;

        // Varsayılan, parlayan bir materyal ayarlayalım
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = waveColor;
        line.endColor = waveColor;
    }

    private void Update()
    {
        // Hedefe yumuşak geçiş
        currentSensorValue = Mathf.Lerp(currentSensorValue, targetSensorValue, Time.deltaTime * 10f);

        float t = Time.time * speed;
        float amp = Mathf.Lerp(minAmplitude, maxAmplitude, currentSensorValue);
        
        for (int i = 0; i < resolution; i++)
        {
            float percent = (float)i / (resolution - 1);
            
            // X ekseni boyunca uzanır
            float x = percent * length - (length * 0.5f); // Merkeze ortalı
            
            // Y ekseni dalgalanma (sinüs)
            float s1 = Mathf.Sin(percent * Mathf.PI * 2f * freq1 + t);
            float s2 = Mathf.Sin(percent * Mathf.PI * 2f * freq2 - t * 1.5f) * 0.5f;
            
            float combined = s1 + (s2 * currentSensorValue);
            float y = combined * amp;

            // Z = 0
            line.SetPosition(i, new Vector3(x, y, 0f));
        }

        // Değer yükseldikçe parlaklık artsın
        Color c = waveColor;
        c.a = Mathf.Lerp(0.3f, 1f, currentSensorValue);
        line.startColor = c;
        line.endColor = c;
    }

    // MasterController bu fonksiyonu çağırarak besleyebilir
    public void SetSensorValue(float value)
    {
        targetSensorValue = Mathf.Clamp01(value);
    }
}
