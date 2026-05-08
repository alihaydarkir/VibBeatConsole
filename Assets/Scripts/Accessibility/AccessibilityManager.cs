#pragma warning disable 0414
using System.Collections;
using UnityEngine;

/// <summary>
/// VibBeat Erişilebilirlik Yöneticisi
///
/// İBE Prensipleri:
///   - Feedback    : Her etkileşim sesli geri bildirim alır (TTS + ses tonu)
///   - Affordance  : Dokunma bölgeleri sesli olarak tarif edilir
///   - Accessibility: Görme engelli kullanıcı görsel UI'a ihtiyaç duymaz
///   - Multimodality: Ses + titreşim + dokunuş birlikte çalışır
///
/// Kullanım:
///   AccessibilityManager.Instance.Speak("Gitar bölgesi");
///   AccessibilityManager.Instance.PlayEarcon(EarconType.PianoKey);
/// </summary>
public class AccessibilityManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────
    public static AccessibilityManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────
    [Header("Erişilebilirlik Ayarları")]
    [SerializeField] private bool accessibilityEnabled = true;
    [Tooltip("TTS konuşma hızı (0.5 = yavaş, 1.0 = normal, 1.5 = hızlı)")]
    [SerializeField] [Range(0.5f, 2f)] private float speechRate  = 1.0f;
    [Tooltip("TTS ses yüksekliği (0.5 = alçak, 1.0 = normal, 2.0 = yüksek)")]
    [SerializeField] [Range(0.5f, 2f)] private float speechPitch = 1.0f;

    [Header("Earcon Ses Klipleri")]
    [SerializeField] private AudioClip earconGuitarZone;   // Gitar bölgesine girince
    [SerializeField] private AudioClip earconPianoZone;    // Piyano bölgesi
    [SerializeField] private AudioClip earconDrumZone;     // Davul bölgesi
    [SerializeField] private AudioClip earconSuccess;      // Kalibrasyon/işlem başarılı
    [SerializeField] private AudioClip earconError;        // Hata durumu
    [SerializeField] private AudioClip earconNavigation;   // Ekran geçişi

    [Header("Debug")]
    [SerializeField] private string debugLastSpokenText = "";
    [SerializeField] private bool   debugTtsReady       = false;

    // ─────────────────────────────────────────
    // ÖZEL ALANLAR
    // ─────────────────────────────────────────
    private AudioSource earconSource;
    private bool        ttsReady     = false;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject ttsEngine = null;
    private AndroidJavaObject unityActivity = null;
#endif

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        SetupEarconSource();

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidTTS();
#else
        ttsReady = true;
        debugTtsReady = true;
        Debug.Log("[ACCESSIBILITY] Editor modu — TTS simüle edilecek.");
#endif
    }

    private void SetupEarconSource()
    {
        earconSource = gameObject.AddComponent<AudioSource>();
        earconSource.playOnAwake = false;
        earconSource.volume      = 0.85f;
        earconSource.spatialBlend = 0f;  // 2D ses — her zaman net duyulsun
    }

    private void InitializeAndroidTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var playerClass  = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity    = playerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // Android TextToSpeech başlat
            ttsEngine = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                unityActivity,
                new TtsInitListener(this)   // init callback
            );
            Debug.Log("[ACCESSIBILITY] TTS engine başlatılıyor...");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ACCESSIBILITY] TTS başlatılamadı: {ex.Message}");
        }
#endif
    }

    // Android TTS init callback (inner class)
#if UNITY_ANDROID && !UNITY_EDITOR
    private class TtsInitListener : AndroidJavaProxy
    {
        private readonly AccessibilityManager manager;

        public TtsInitListener(AccessibilityManager m)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            manager = m;
        }

        // Android callback — TTS hazır olunca çağrılır
        public void onInit(int status)
        {
            // 0 = SUCCESS
            bool success = (status == 0);
            manager.OnTtsInitialized(success);
        }
    }
#endif

    /// <summary>TTS hazır olunca Java callback tarafından çağrılır.</summary>
    public void OnTtsInitialized(bool success)
    {
        ttsReady      = success;
        debugTtsReady = success;

        if (success)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Dil: Türkçe
            var locale = new AndroidJavaObject("java.util.Locale", "tr", "TR");
            ttsEngine?.Call<int>("setLanguage", locale);
#endif
            Debug.Log("[ACCESSIBILITY] ✅ TTS hazır — Türkçe dil ayarlandı.");

            // İlk karşılama mesajı
            Speak("VibBeat Console açıldı. Sol elinizi sensörün üzerine tutun.");
        }
        else
        {
            Debug.LogError("[ACCESSIBILITY] ❌ TTS başlatılamadı.");
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API — KONUŞMA
    // ─────────────────────────────────────────

    /// <summary>
    /// Metni sesli okur (Text-to-Speech).
    /// Görme engelli kullanıcının mevcut durumu anlaması için temel mekanizma.
    /// </summary>
    /// <param name="text">Okunacak metin</param>
    /// <param name="interrupt">True → önceki konuşmayı keser; False → kuyruğa ekler</param>
    public void Speak(string text, bool interrupt = true)
    {
        if (!accessibilityEnabled || string.IsNullOrEmpty(text)) return;

        debugLastSpokenText = text;
        Debug.Log($"[TTS] 🔊 \"{text}\"");

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ttsReady || ttsEngine == null)
        {
            Debug.LogWarning("[ACCESSIBILITY] TTS henüz hazır değil, mesaj atlandı.");
            return;
        }

        try
        {
            // Konuşma hızı ve tonu ayarla
            ttsEngine.Call<int>("setSpeechRate",  speechRate);
            ttsEngine.Call<int>("setPitch",       speechPitch);

            // QUEUE_FLUSH (1) → öncekini kes; QUEUE_ADD (0) → kuyruğa ekle
            int queueMode = interrupt ? 1 : 0;
            ttsEngine.Call<int>("speak", text, queueMode, null, "vb_utt");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ACCESSIBILITY] Speak hatası: {ex.Message}");
        }
#else
        // Editor: konsolda göster (gerçek seste simüle edemeyiz)
        Debug.Log($"[TTS SIMÜLE] 💬 \"{text}\"");
#endif
    }

    // ─────────────────────────────────────────
    // PUBLIC API — EARCON (Anlık Ses İkonları)
    // ─────────────────────────────────────────

    public enum EarconType
    {
        GuitarZone,
        PianoZone,
        DrumZone,
        Success,
        Error,
        Navigation
    }

    /// <summary>
    /// Kısa, anlamlı ses ikonları (earcon) çalar.
    /// TTS'den daha hızlı geri bildirim sağlar; anlık etkileşimler için idealdir.
    /// </summary>
    public void PlayEarcon(EarconType type)
    {
        if (!accessibilityEnabled || earconSource == null) return;

        AudioClip clip = type switch
        {
            EarconType.GuitarZone  => earconGuitarZone,
            EarconType.PianoZone   => earconPianoZone,
            EarconType.DrumZone    => earconDrumZone,
            EarconType.Success     => earconSuccess,
            EarconType.Error       => earconError,
            EarconType.Navigation  => earconNavigation,
            _                      => null
        };

        if (clip != null)
        {
            earconSource.PlayOneShot(clip);
            Debug.Log($"[ACCESSIBILITY] 🔔 Earcon: {type}");
        }
        else
        {
            // Clip atanmamışsa programatik bip sesi üret
            StartCoroutine(PlayGeneratedBeep(type));
        }
    }

    /// <summary>
    /// Earcon clip atanmamışsa tondan bip üretir.
    /// Her bölgenin farklı frekansı = görme engelli kullanıcı bölgeyi tanır.
    /// </summary>
    private IEnumerator PlayGeneratedBeep(EarconType type)
    {
        float freq = type switch
        {
            EarconType.GuitarZone => 440f,   // La4 — gitar teli çağrışımı
            EarconType.PianoZone  => 523f,   // Do5
            EarconType.DrumZone   => 220f,   // La3 — derin/davul çağrışımı
            EarconType.Success    => 880f,   // La5 — yüksek, olumlu
            EarconType.Error      => 150f,   // Düşük, dikkat çekici
            EarconType.Navigation => 660f,   // Mi5
            _                     => 440f
        };

        float duration = (type == EarconType.Error) ? 0.4f : 0.12f;
        int   samples  = (int)(AudioSettings.outputSampleRate * duration);

        var clip = AudioClip.Create("beep", samples, 1, AudioSettings.outputSampleRate, false);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t       = (float)i / samples;
            float envelope = Mathf.Sin(Mathf.PI * t);   // yumuşak fade in/out
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / AudioSettings.outputSampleRate)
                      * envelope * 0.6f;
        }

        clip.SetData(data, 0);
        earconSource.PlayOneShot(clip);
        yield return new WaitForSeconds(duration);
    }

    // ─────────────────────────────────────────
    // PUBLIC API — BÖLGE ANOUNCEMENT'LARI
    // (TouchZoneController tarafından çağrılır)
    // ─────────────────────────────────────────

    public void AnnounceGuitarZone()
    {
        PlayEarcon(EarconType.GuitarZone);
        Speak("Gitar. Sensörü kapatın veya açın.", interrupt: false);
    }

    public void AnnouncePianoZone(int keyIndex)
    {
        string[] noteNames = { "Do", "Re", "Mi", "Fa" };
        string note = (keyIndex >= 0 && keyIndex < noteNames.Length)
            ? noteNames[keyIndex] : "nota";

        PlayEarcon(EarconType.PianoZone);
        Speak($"Piyano, {note}", interrupt: true);
    }

    public void AnnounceDrumZone()
    {
        PlayEarcon(EarconType.DrumZone);
        // Davulda TTS değil yalnızca earcon — çok hızlı bir etkileşim
    }

    public void AnnounceCalibrationStep(string message)
    {
        Speak(message, interrupt: true);
    }

    public void AnnounceCalibrationComplete()
    {
        PlayEarcon(EarconType.Success);
        Speak("Kalibrasyon tamamlandı. Müzik konsolu hazır.", interrupt: false);
    }

    public void AnnounceScreenChange(string screenName)
    {
        PlayEarcon(EarconType.Navigation);
        Speak(screenName, interrupt: true);
    }

    public void AnnounceSensorValue(float normalizedValue)
    {
        // 0-1 aralığını kullanıcıya anlamlı şekilde anlat
        // Sadece önemli eşik geçişlerinde konuş — sürekli konuşma rahatsız eder
        string description;
        if      (normalizedValue < 0.15f) description = "El tam kapalı";
        else if (normalizedValue < 0.40f) description = "Yarı kapalı";
        else if (normalizedValue < 0.65f) description = "Orta";
        else if (normalizedValue < 0.85f) description = "Açık";
        else                              description = "Tam açık";

        Speak(description, interrupt: true);
    }

    // ─────────────────────────────────────────
    // AYARLAR
    // ─────────────────────────────────────────
    public void SetAccessibilityEnabled(bool enabled)
    {
        accessibilityEnabled = enabled;
        Debug.Log($"[ACCESSIBILITY] Erişilebilirlik: {(enabled ? "AÇIK" : "KAPALI")}");
    }

    public void SetSpeechRate(float rate)
    {
        speechRate = Mathf.Clamp(rate, 0.5f, 2f);
    }

    public bool IsAccessibilityEnabled() => accessibilityEnabled;

    // ─────────────────────────────────────────
    // TEMİZLİK
    // ─────────────────────────────────────────
    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            ttsEngine?.Call("stop");
            ttsEngine?.Call("shutdown");
            ttsEngine?.Dispose();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ACCESSIBILITY] TTS temizleme hatası: {ex.Message}");
        }
#endif
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }
}
