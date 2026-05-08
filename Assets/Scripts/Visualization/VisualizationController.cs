using UnityEngine;
using System.Collections;

public class VisualizationController : MonoBehaviour
{
    // --- Particle Systems ---
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem guitarWaveEffect;
    [SerializeField] private ParticleSystem pianoKeyEffect;
    [SerializeField] private ParticleSystem drumImpactEffect;

    // --- Materyaller ---
    [Header("Materials")]
    [SerializeField] private Material guitarMaterial;
    [SerializeField] private Material[] pianoKeyMaterials;  // 4 tuş için

    // --- Gitar Dalga Ayarları ---
    [Header("Guitar Wave")]
    [SerializeField] private float minEmissionRate = 5f;
    [SerializeField] private float maxEmissionRate = 80f;
    [SerializeField] private float minParticleSpeed = 0.5f;
    [SerializeField] private float maxParticleSpeed = 5f;

    // --- Piyano Işık Ayarları ---
    [Header("Piano Emissive")]
    [SerializeField] private float emissivePeakIntensity = 3f;
    [SerializeField] private float emissiveFadeDuration = 0.4f;

    // --- Renk Paleti ---
    [Header("Colors")]
    [SerializeField] private Color guitarColorLow = new Color(0f, 0.5f, 1f);
    [SerializeField] private Color guitarColorHigh = new Color(1f, 0f, 0.5f);
    [SerializeField]
    private Color[] pianoKeyColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),  // Do → Kırmızı
        new Color(0.3f, 1f, 0.3f),  // Re → Yeşil
        new Color(0.3f, 0.3f, 1f),  // Mi → Mavi
        new Color(1f, 1f, 0.3f)     // Fa → Sarı
    };

    // --- Inspector Debug ---
    [Header("Debug")]
    [SerializeField] private float debugNormalizedValue = 0f;

    private void Start()
    {
        InitializeEffects();
    }

    private void InitializeEffects()
    {
        // Gitar efektini başlat ama durdur
        if (guitarWaveEffect != null)
        {
            guitarWaveEffect.Stop();
            Debug.Log("[VFX] ✅ Guitar wave hazır");
        }

        if (pianoKeyEffect != null)
        {
            pianoKeyEffect.Stop();
            Debug.Log("[VFX] ✅ Piano effect hazır");
        }

        if (drumImpactEffect != null)
        {
            drumImpactEffect.Stop();
            Debug.Log("[VFX] ✅ Drum impact hazır");
        }
    }

    // --- Gitar Görselleştirme (Sensör Bazlı) ---
    public void UpdateGuitarVisualization(float normalizedValue)
    {
        debugNormalizedValue = normalizedValue;

        // Particle emission rate güncelle
        if (guitarWaveEffect != null)
        {
            var emission = guitarWaveEffect.emission;
            emission.rateOverTime = Mathf.Lerp(
                minEmissionRate, maxEmissionRate, normalizedValue
            );

            var main = guitarWaveEffect.main;
            main.startSpeed = Mathf.Lerp(
                minParticleSpeed, maxParticleSpeed, normalizedValue
            );

            // Renk geçişi (Kalın ses → Mavi, İnce ses → Pembe)
            main.startColor = Color.Lerp(
                guitarColorLow, guitarColorHigh, normalizedValue
            );

            // Efekt çalışmıyorsa başlat
            if (!guitarWaveEffect.isPlaying)
                guitarWaveEffect.Play();
        }

        // Material shader güncelle
        if (guitarMaterial != null)
        {
            guitarMaterial.SetFloat("_WaveAmplitude", normalizedValue);
            guitarMaterial.SetFloat("_WaveFrequency",
                Mathf.Lerp(0.5f, 4f, normalizedValue));
            guitarMaterial.SetColor("_EmissionColor",
                Color.Lerp(guitarColorLow, guitarColorHigh, normalizedValue)
                * normalizedValue);
        }
    }

    // --- Gitar Mute Görselleştirme ---
    public void SetGuitarMuteVisual(bool isMuted)
    {
        if (guitarWaveEffect == null) return;

        if (isMuted)
        {
            guitarWaveEffect.Stop();
            Debug.Log("[VFX] 🎸 Guitar wave DURDURULDU");
        }
        else
        {
            guitarWaveEffect.Play();
            Debug.Log("[VFX] 🎸 Guitar wave BAŞLATILDI");
        }
    }

    // --- Piyano Tuş Görselleştirme ---
    public void PlayPianoKeyVisualization(int keyIndex)
    {
        keyIndex = Mathf.Clamp(keyIndex, 0, 3);

        // Particle burst
        if (pianoKeyEffect != null)
        {
            var main = pianoKeyEffect.main;
            main.startColor = pianoKeyColors[keyIndex];
            pianoKeyEffect.Stop();
            pianoKeyEffect.Play();
            Debug.Log($"[VFX] 🎹 Piano effect: tuş {keyIndex}");
        }

        // Emissive parlaması
        if (pianoKeyMaterials != null && keyIndex < pianoKeyMaterials.Length)
        {
            if (pianoKeyMaterials[keyIndex] != null)
            {
                StartCoroutine(FlashEmissive(
                    pianoKeyMaterials[keyIndex],
                    pianoKeyColors[keyIndex],
                    emissiveFadeDuration
                ));
            }
        }
    }

    // --- Davul Görselleştirme ---
    public void PlayDrumImpactVisualization()
    {
        if (drumImpactEffect != null)
        {
            drumImpactEffect.Stop();
            drumImpactEffect.Play();
            Debug.Log("[VFX] 🥁 Drum impact effect!");
        }
    }

    // --- Emissive Parlama Efekti ---
    private IEnumerator FlashEmissive(Material mat, Color color, float duration)
    {
        // Parla
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * emissivePeakIntensity);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Kademeli sön
            float intensity = Mathf.Lerp(emissivePeakIntensity, 0f, t);
            mat.SetColor("_EmissionColor", color * intensity);

            yield return null;
        }

        // Tamamen söndür
        mat.SetColor("_EmissionColor", Color.black);
    }

    private void OnDestroy()
    {
        // Material instance'larını temizle
        if (guitarMaterial != null)
            guitarMaterial.SetColor("_EmissionColor", Color.black);
    }
}