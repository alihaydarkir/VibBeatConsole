#pragma warning disable 0414
using System.Collections;
using UnityEngine;

/// <summary>
/// VibBeat Erişilebilirlik Yöneticisi
///
/// İBE Prensipleri:
///   - Feedback    : Her etkileşim sesli geri bildirim alır (TTS + earcon)
///   - Affordance  : Dokunma bölgeleri sesli olarak tarif edilir
///   - Accessibility: Görme engelli kullanıcı görsel UI'a ihtiyaç duymaz
///   - Multimodality: Ses + titreşim + dokunuş birlikte çalışır
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
    [SerializeField] private bool  accessibilityEnabled = true;
    [SerializeField] [Range(0.5f, 2f)] private float speechRate  = 1.0f;
    [SerializeField] [Range(0.5f, 2f)] private float speechPitch = 1.0f;

    [Header("Earcon Ses Klipleri")]
    [SerializeField] private AudioClip earconGuitarZone;
    [SerializeField] private AudioClip earconPianoZone;
    [SerializeField] private AudioClip earconDrumZone;
    [SerializeField] private AudioClip earconSuccess;
    [SerializeField] private AudioClip earconError;
    [SerializeField] private AudioClip earconNavigation;

    [Header("Debug")]
    [SerializeField] private string debugLastSpokenText = "";
    [SerializeField] private bool   debugTtsReady       = false;

    // ─────────────────────────────────────────
    // ÖZEL ALANLAR
    // AndroidJavaObject her zaman tanımlı — runtime'da kontrol edilir
    // ─────────────────────────────────────────
    private AudioSource    earconSource;
    private bool           ttsReady   = false;
    private AndroidJavaObject ttsEngine  = null;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        SetupEarconSource();

        if (Application.platform == RuntimePlatform.Android)
            InitAndroidTTS();
        else
        {
            ttsReady      = true;
            debugTtsReady = true;
            Debug.Log("[ACCESSIBILITY] Editor modu — TTS simüle edilecek.");
        }
    }

    private void SetupEarconSource()
    {
        earconSource              = gameObject.AddComponent<AudioSource>();
        earconSource.playOnAwake  = false;
        earconSource.volume       = 0.85f;
        earconSource.spatialBlend = 0f;
    }

    private void InitAndroidTTS()
    {
        try
        {
            var playerClass   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");

            ttsEngine = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                unityActivity,
                new TtsInitListener(this)
            );
            Debug.Log("[ACCESSIBILITY] TTS engine başlatılıyor...");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ACCESSIBILITY] TTS başlatılamadı: {ex.Message}");
        }
    }

    // Android TTS init callback
    private class TtsInitListener : AndroidJavaProxy
    {
        private readonly AccessibilityManager manager;
        public TtsInitListener(AccessibilityManager m)
            : base("android.speech.tts.TextToSpeech$OnInitListener") { manager = m; }

        public void onInit(int status)
        {
            manager.OnTtsInitialized(status == 0);
        }
    }

    public void OnTtsInitialized(bool success)
    {
        ttsReady      = success;
        debugTtsReady = success;

        if (success)
        {
            if (Application.platform == RuntimePlatform.Android && ttsEngine != null)
            {
                var locale = new AndroidJavaObject("java.util.Locale", "tr", "TR");
                ttsEngine.Call<int>("setLanguage", locale);
            }
            Debug.Log("[ACCESSIBILITY] ✅ TTS hazır — Türkçe.");
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
    public void Speak(string text, bool interrupt = true)
    {
        if (!accessibilityEnabled || string.IsNullOrEmpty(text)) return;

        debugLastSpokenText = text;
        Debug.Log($"[TTS] 🔊 \"{text}\"");

        if (Application.platform == RuntimePlatform.Android)
        {
            if (!ttsReady || ttsEngine == null) return;
            try
            {
                ttsEngine.Call<int>("setSpeechRate", speechRate);
                ttsEngine.Call<int>("setPitch",      speechPitch);   // speechPitch burada kullanılıyor
                int queueMode = interrupt ? 1 : 0;
                ttsEngine.Call<int>("speak", text, queueMode, null, "vb_utt");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ACCESSIBILITY] Speak hatası: {ex.Message}");
            }
        }
        else
        {
            Debug.Log($"[TTS SIMÜLE] 💬 rate:{speechRate} pitch:{speechPitch} \"{text}\"");
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API — EARCON
    // ─────────────────────────────────────────
    public enum EarconType { GuitarZone, PianoZone, DrumZone, Success, Error, Navigation }

    public void PlayEarcon(EarconType type)
    {
        if (!accessibilityEnabled || earconSource == null) return;
        if (!gameObject.activeInHierarchy) return;  // inactive ise coroutine baslatma

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
            earconSource.PlayOneShot(clip);
        else
            StartCoroutine(PlayGeneratedBeep(type));
    }

    private IEnumerator PlayGeneratedBeep(EarconType type)
    {
        float freq = type switch
        {
            EarconType.GuitarZone => 440f,
            EarconType.PianoZone  => 523f,
            EarconType.DrumZone   => 220f,
            EarconType.Success    => 880f,
            EarconType.Error      => 150f,
            EarconType.Navigation => 660f,
            _                     => 440f
        };

        float duration = (type == EarconType.Error) ? 0.4f : 0.12f;
        int   samples  = (int)(AudioSettings.outputSampleRate * duration);
        var   clip     = AudioClip.Create("beep", samples, 1, AudioSettings.outputSampleRate, false);
        float[] data   = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t        = (float)i / samples;
            float envelope = Mathf.Sin(Mathf.PI * t);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / AudioSettings.outputSampleRate)
                      * envelope * 0.6f;
        }

        clip.SetData(data, 0);
        earconSource.PlayOneShot(clip);
        yield return new WaitForSeconds(duration);
    }

    // ─────────────────────────────────────────
    // BÖLGE DUYURULARI
    // ─────────────────────────────────────────
    public void AnnounceGuitarZone()
    {
        PlayEarcon(EarconType.GuitarZone);
        Speak("Gitar. Sensörü kapatın veya açın.", interrupt: false);
    }

    public void AnnouncePianoZone(int keyIndex)
    {
        string[] noteNames = { "Do", "Re", "Mi", "Fa" };
        string   note      = (keyIndex >= 0 && keyIndex < noteNames.Length) ? noteNames[keyIndex] : "nota";
        PlayEarcon(EarconType.PianoZone);
        Speak($"Piyano, {note}", interrupt: true);
    }

    public void AnnounceDrumZone()        => PlayEarcon(EarconType.DrumZone);

    public void AnnounceCalibrationStep(string message)  => Speak(message, interrupt: true);

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

    // ─────────────────────────────────────────
    // AYARLAR
    // ─────────────────────────────────────────
    public void SetAccessibilityEnabled(bool enabled) => accessibilityEnabled = enabled;
    public void SetSpeechRate(float rate)             => speechRate = Mathf.Clamp(rate, 0.5f, 2f);
    public bool IsAccessibilityEnabled()              => accessibilityEnabled;

    // ─────────────────────────────────────────
    // TEMİZLİK
    // ─────────────────────────────────────────
    private void OnDestroy()
    {
        if (Application.platform == RuntimePlatform.Android && ttsEngine != null)
        {
            try
            {
                ttsEngine.Call("stop");
                ttsEngine.Call("shutdown");
                ttsEngine.Dispose();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ACCESSIBILITY] TTS temizleme hatası: {ex.Message}");
            }
        }
    }
}
