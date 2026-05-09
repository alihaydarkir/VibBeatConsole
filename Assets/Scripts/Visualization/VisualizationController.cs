using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// VibBeat Görselleştirme Kontrolcüsü
///
/// İBE — Feedback ilkesi:
///   Her müzik etkileşimi anlık, orantılı ve çok duyusal geri bildirim üretir.
///   DOTween ile UI animasyonları, Cartoon FX ile particle efektler birleşir.
///
/// Bağlantı noktaları:
///   - Inspector'dan Cartoon FX prefab'larını EffectPrefab alanlarına sürükle
///   - Inspector'dan Piano key Image bileşenlerini PianoKeyImages dizisine ata
///   - GuitarWaveRect: MainConsoleScreen > GuitarPanel > WaveformArea RectTransform
/// </summary>
public class VisualizationController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // INSPECTOR — Cartoon FX Prefab'ları
    // ─────────────────────────────────────────
    [Header("Cartoon FX Prefab'ları")]
    [Tooltip("Piyano tuşuna basınca çıkacak efekt (CFX3_MagicPoof vb.)")]
    [SerializeField] private GameObject pianoEffectPrefab;

    [Tooltip("Davul vuruşunda çıkacak efekt (CFX_Explosion_B vb.)")]
    [SerializeField] private GameObject drumEffectPrefab;

    [Tooltip("Gitar mute açıldığında / kapandığında çıkacak efekt")]
    [SerializeField] private GameObject guitarMuteEffectPrefab;

    // ─────────────────────────────────────────
    // INSPECTOR — UI Referansları (DOTween için)
    // ─────────────────────────────────────────
    [Header("Piyano Tuş Image'ları (DOTween renk animasyonu)")]
    [Tooltip("MainConsoleScreen > RightPanel > PianoPanel altındaki 4 tuşun Image bileşeni")]
    [SerializeField] private Image[] pianoKeyImages = new Image[4];

    [Header("Davul Pad (DOTween scale animasyonu)")]
    [Tooltip("DrumPanel > DrumPad RectTransform")]
    [SerializeField] private RectTransform drumPadRect;

    [Header("Gitar Dalga Alanı (DOTween pulse animasyonu)")]
    [Tooltip("GuitarPanel > WaveformArea RectTransform")]
    [SerializeField] private RectTransform guitarWaveRect;

    [Header("Gitar Sensör Değeri Text (DOTween renk)")]
    [Tooltip("GuitarPanel > SensorValueText")]
    [SerializeField] private UnityEngine.UI.Graphic sensorValueGraphic;

    // ─────────────────────────────────────────
    // RENK PALETİ (UI Builder ile uyumlu)
    // ─────────────────────────────────────────
    private static readonly Color GuitarCyan   = new Color(0f,    0.94f, 1f,    1f);  // #00F0FF
    private static readonly Color PianoOrange  = new Color(1f,    0.65f, 0f,    1f);  // #FFA500
    private static readonly Color DrumMagenta  = new Color(1f,    0.10f, 0.68f, 1f);  // #FF1AAD
    private static readonly Color NeutralDark  = new Color(0.05f, 0.10f, 0.16f, 1f);  // #0D1A28

    // Her nota için renk
    private readonly Color[] noteColors = new Color[]
    {
        new Color(1f,    0.65f, 0f,    1f),  // Do  → Turuncu
        new Color(0.8f,  0.9f,  0f,    1f),  // Re  → Sarı-yeşil
        new Color(0f,    0.85f, 0.8f,  1f),  // Mi  → Turkuaz
        new Color(0.85f, 0.3f,  1f,    1f),  // Fa  → Mor
    };

    // ─────────────────────────────────────────
    // INSPECTOR DEBUG
    // ─────────────────────────────────────────
    [Header("Debug")]
    [SerializeField] private float debugNormalizedValue = 0f;

    // ─────────────────────────────────────────
    // ÖZEL ALANLAR
    // ─────────────────────────────────────────
    private Tweener guitarPulseTween;   // sürekli gitar pulse tweeni
    private bool    guitarMuted = false;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        // DOTween global ayarları
        DOTween.SetTweensCapacity(200, 50);

        // Inspector bos birakilmissa otomatik bul
        AutoFindReferences();

        StartGuitarIdlePulse();
        Debug.Log("[VFX] [OK] VisualizationController baslatildi.");
    }

    private void AutoFindReferences()
    {
        // Cartoon FX prefablarini Resources veya scene'den bul (atanmamissa)
        // Bunlar Inspector'dan atanmissa bu blogu atla
        // (Cartoon FX prefab'lari Assets/JMO Assets altinda)
        // Piano Key Images — PianoPanel altindaki 4 tusu bul
        if (pianoKeyImages == null || pianoKeyImages.Length == 0
            || pianoKeyImages[0] == null)
        {
            string[] noteNames = {"PianoKey_C4","PianoKey_D4","PianoKey_E4","PianoKey_F4"};
            pianoKeyImages = new Image[4];
            for (int i = 0; i < noteNames.Length; i++)
            {
                var go = GameObject.Find(noteNames[i]);
                if (go != null)
                    pianoKeyImages[i] = go.GetComponent<Image>();
            }
            bool allFound = System.Array.TrueForAll(pianoKeyImages, x => x != null);
            Debug.Log($"[VFX] Piano images: {(allFound ? "[OK]" : "[EKSIK]")}");
        }

        // Drum Pad Rect
        if (drumPadRect == null)
        {
            var go = GameObject.Find("DrumPad");
            if (go != null) drumPadRect = go.GetComponent<RectTransform>();
            Debug.Log($"[VFX] DrumPad: {(drumPadRect != null ? "[OK]" : "[EKSIK]")}");
        }

        // Guitar Wave Rect
        if (guitarWaveRect == null)
        {
            var go = GameObject.Find("WaveformArea");
            if (go != null) guitarWaveRect = go.GetComponent<RectTransform>();
            Debug.Log($"[VFX] WaveformArea: {(guitarWaveRect != null ? "[OK]" : "[EKSIK]")}");
        }

        // Sensor Value Graphic
        if (sensorValueGraphic == null)
        {
            var go = GameObject.Find("SensorValueText");
            if (go != null) sensorValueGraphic = go.GetComponent<UnityEngine.UI.Graphic>();
            Debug.Log($"[VFX] SensorValueText: {(sensorValueGraphic != null ? "[OK]" : "[EKSIK]")}");
        }
    }

    // ─────────────────────────────────────────
    // A) GİTAR GÖRSELLEŞTİRME — Her frame sensör verisiyle güncellenir
    // ─────────────────────────────────────────

    /// <summary>
    /// Normalizedvalue (0-1) → Gitar dalga alanının ölçeği ve rengi.
    /// DOTween ile anlık değişim yerine yumuşak geçiş sağlanır.
    /// </summary>
    public void UpdateGuitarVisualization(float normalizedValue)
    {
        debugNormalizedValue = normalizedValue;
        if (guitarMuted) return;

        // Dalga alanını sensör değeriyle orantılı olarak büyüt
        if (guitarWaveRect != null)
        {
            float targetScaleY = Mathf.Lerp(0.85f, 1.15f, normalizedValue);
            guitarWaveRect.DOScaleY(targetScaleY, 0.12f).SetEase(Ease.OutSine);
        }

        // Sensör değer text'inin rengi: düşük=koyu, yüksek=parlak cyan
        if (sensorValueGraphic != null)
        {
            Color targetColor = Color.Lerp(
                new Color(GuitarCyan.r, GuitarCyan.g, GuitarCyan.b, 0.4f),
                GuitarCyan,
                normalizedValue
            );
            sensorValueGraphic.DOColor(targetColor, 0.15f);
        }
    }

    /// <summary>
    /// Gitar mute olmadığında arka planda sürekli nefes alan pulse animasyonu.
    /// Affordance ilkesi: Gitar bölgesinin "canlı" olduğunu pasif olarak gösterir.
    /// </summary>
    private void StartGuitarIdlePulse()
    {
        if (guitarWaveRect == null) return;

        guitarPulseTween = guitarWaveRect
            .DOScaleY(1.08f, 1.2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void SetGuitarMuteVisual(bool isMuted)
    {
        guitarMuted = isMuted;

        if (guitarWaveRect != null)
        {
            if (isMuted)
            {
                guitarPulseTween?.Pause();
                // Mute: dalga alanı küçülsün ve soluklaşsın
                guitarWaveRect.DOScaleY(0.7f, 0.3f).SetEase(Ease.OutCubic);
            }
            else
            {
                guitarPulseTween?.Play();
                guitarWaveRect.DOScaleY(1f, 0.3f).SetEase(Ease.OutCubic);
            }
        }

        // Cartoon FX efekti
        if (guitarMuteEffectPrefab != null && guitarWaveRect != null)
            SpawnEffect(guitarMuteEffectPrefab, guitarWaveRect.position);

        // Ripple — gitar bölgesinden
        if (guitarWaveRect != null)
            RippleEffect.Instance?.SpawnGuitar(guitarWaveRect.position);

        Debug.Log($"[VFX] Gitar mute: {isMuted}");
    }

    // ─────────────────────────────────────────
    // B) PİYANO TUŞ GÖRSELLEŞTİRME
    // ─────────────────────────────────────────

    /// <summary>
    /// Tuşa basınca:
    ///   1. Tuş Image'ı nota rengine flash yapar (DOTween)
    ///   2. Cartoon FX particle efekti tuşun üzerinde spawn olur
    ///   3. Tuş hafifçe küçülüp geri gelir (press hissi)
    ///
    /// İBE — Feedback + Affordance: Kullanıcı dokunduğunda görsel ve renksel tepki
    /// alır; görme engelli kullanıcı için TTS zaten var, bu görme yetisi olanlar için.
    /// </summary>
    public void PlayPianoKeyVisualization(int keyIndex)
    {
        keyIndex = Mathf.Clamp(keyIndex, 0, 3);
        Color noteColor = noteColors[keyIndex];

        // 1. Scale press efekti (renk flash kaldirildi — Ripple yeterli)
        if (pianoKeyImages != null && keyIndex < pianoKeyImages.Length
            && pianoKeyImages[keyIndex] != null)
        {
            Image keyImg = pianoKeyImages[keyIndex];
            keyImg.rectTransform.DOKill();
            keyImg.rectTransform
                .DOScale(0.92f, 0.06f)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                    keyImg.rectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutBack)
                );
        }

        // 2. Cartoon FX particle
        if (pianoEffectPrefab != null && pianoKeyImages != null
            && keyIndex < pianoKeyImages.Length && pianoKeyImages[keyIndex] != null)
        {
            Vector3 spawnPos = pianoKeyImages[keyIndex].transform.position;
            SpawnEffect(pianoEffectPrefab, spawnPos);
        }

        // 3. Su damlasi efekti — tuşun üzerinde
        if (pianoKeyImages != null && keyIndex < pianoKeyImages.Length
            && pianoKeyImages[keyIndex] != null)
        {
            WaterDropEffect.Instance?.SpawnPianoDrop(
                pianoKeyImages[keyIndex].transform.position, keyIndex);
        }

        // 4. Ripple — tüm ekrana yayil (arka planda)
        if (pianoKeyImages != null && keyIndex < pianoKeyImages.Length
            && pianoKeyImages[keyIndex] != null)
        {
            RippleEffect.Instance?.SpawnPiano(
                pianoKeyImages[keyIndex].transform.position, keyIndex);
        }

        Debug.Log($"[VFX] Piano key animasyonu: {keyIndex}");
    }

    // ─────────────────────────────────────────
    // C) DAVUL VURUŞ GÖRSELLEŞTİRME
    // ─────────────────────────────────────────

    /// <summary>
    /// Davul vuruşunda:
    ///   1. Pad büyür ve geri döner (punch scale — DOTween)
    ///   2. Renk magenta'ya flash yapar
    ///   3. Cartoon FX explosion efekti spawn olur
    /// </summary>
    public void PlayDrumImpactVisualization()
    {
        // Scale press efekti — piyanoyla ayni mantik
        if (drumPadRect != null)
        {
            drumPadRect.DOKill();
            drumPadRect
                .DOScale(0.92f, 0.06f)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                    drumPadRect.DOScale(1f, 0.22f).SetEase(Ease.OutBack)
                );
        }

        // Su damlasi — davul yuzeyi eliptik damla
        if (drumPadRect != null)
            WaterDropEffect.Instance?.SpawnDrumDrop(drumPadRect.position);

        // Cartoon FX
        if (drumEffectPrefab != null && drumPadRect != null)
            SpawnEffect(drumEffectPrefab, drumPadRect.position);

        // Ripple — davul merkezinden halka
        if (drumPadRect != null)
            RippleEffect.Instance?.SpawnDrum(drumPadRect.position);

        Debug.Log("[VFX] Davul animasyonu!");
    }

    // ─────────────────────────────────────────
    // YARDIMCI — Cartoon FX Spawn
    // ─────────────────────────────────────────

    /// <summary>
    /// Verilen pozisyonda Cartoon FX prefab'ını spawn eder ve
    /// Particle System bitince otomatik yok eder.
    /// </summary>
    private void SpawnEffect(GameObject prefab, Vector3 worldPosition)
    {
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, worldPosition, Quaternion.identity);

        // Particle System süresini al, o kadar sonra yok et
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        float lifetime = (ps != null)
            ? ps.main.duration + ps.main.startLifetime.constantMax
            : 2f;

        Destroy(fx, lifetime);
    }

    // ─────────────────────────────────────────
    // TEMİZLİK
    // ─────────────────────────────────────────
    private void OnDestroy()
    {
        guitarPulseTween?.Kill();
        DOTween.KillAll();
    }
}
