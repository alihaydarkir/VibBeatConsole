using UnityEngine;

public class AudioSynthesizer : MonoBehaviour
{
    public static AudioSynthesizer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // --- Audio Sources ---
    [SerializeField] private AudioSource guitarSource;
    [SerializeField] private AudioSource pianoSource;
    [SerializeField] private AudioSource drumSource;

    // --- Audio Clips ---
    [Header("Gitar Tonları (Inspector'dan ata — az=kalın, çok=ince)")]
    [SerializeField] private AudioClip[] guitarToneClips; // farklı gitar tonları
    [SerializeField] private AudioClip[] pianoNotes;      // 4 adet (Do, Re, Mi, Fa)
    [SerializeField] private AudioClip drumKick;

    // --- Gitar Durumu ---
    private int  currentGuitarToneIndex = -1;
    private bool guitarLoopActive       = false;

    // --- Inspector Debug ---
    [SerializeField] private float debugNormalizedLux   = 0f;
    [SerializeField] private int   debugGuitarToneIndex = -1;
    [SerializeField] private bool  debugIsGuitarMuted   = false;

    // --- Piyano Frekansları (pitch çarpanı) ---
    private readonly float[] pianoNotePitches = new float[]
    {
        1.00f,   // Do  (C4)
        1.12f,   // Re  (D4)
        1.26f,   // Mi  (E4)
        1.33f    // Fa  (F4)
    };

    private void Start()
    {
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        // Guitar Source
        if (guitarSource == null)
            guitarSource = gameObject.AddComponent<AudioSource>();

        guitarSource.loop   = true;
        guitarSource.volume = 0.7f;
        guitarSource.pitch  = 1f;
        // Play() çağrılmıyor — StartGuitarLoop() ile main ekranda başlatılır

        // Piano Source
        if (pianoSource == null)
            pianoSource = gameObject.AddComponent<AudioSource>();

        pianoSource.loop = false;
        pianoSource.volume = 0.8f;

        // Drum Source
        if (drumSource == null)
            drumSource = gameObject.AddComponent<AudioSource>();

        drumSource.loop = false;
        drumSource.volume = 1f;

        Debug.Log("[AUDIO] [OK] AudioSources hazır!");
    }

    // Sensörün gözlemlenen min/max değerleri — dinamik aralık öğrenmesi
    private float sensorObservedMin =  1f;
    private float sensorObservedMax =  0f;
    private const float GUITAR_MUTE_THRESHOLD = 0.02f;
    private const float TONE_HYSTERESIS       = 0.025f;

    // Dwell loop: aynı tonda bu kadar kalınca loop'a girer
    private float _dwellTimer                 = 0f;
    private const float DWELL_TO_LOOP         = 0.35f; // saniye

    [SerializeField] private float debugDwellTimer = 0f;

    // --- Sensörden gelen 0-1 değeri gitar tonuna dönüştür ---
    public void SetGuitarToneFromSensor(float normalizedValue)
    {
        debugNormalizedLux = normalizedValue;

        if (normalizedValue < sensorObservedMin) sensorObservedMin = normalizedValue;
        if (normalizedValue > sensorObservedMax) sensorObservedMax = normalizedValue;

        float range    = sensorObservedMax - sensorObservedMin;
        float rescaled = range > 0.05f
            ? (normalizedValue - sensorObservedMin) / range
            : normalizedValue;

        // Mute
        if (rescaled < GUITAR_MUTE_THRESHOLD)
        {
            if (currentGuitarToneIndex != -1)
            {
                currentGuitarToneIndex = -1;
                debugGuitarToneIndex   = -1;
                _dwellTimer            = 0f;
                debugDwellTimer        = 0f;
                if (guitarLoopActive && guitarSource != null)
                {
                    guitarSource.loop = false;
                    guitarSource.Stop();
                }
            }
            return;
        }

        if (guitarToneClips == null || guitarToneClips.Length == 0) return;

        int   count        = guitarToneClips.Length;
        float exactPos     = rescaled * count;
        int   desiredIndex = Mathf.Clamp(Mathf.FloorToInt(exactPos), 0, count - 1);

        // Hysteresis
        float fracPart = exactPos - Mathf.Floor(exactPos);
        if (desiredIndex > currentGuitarToneIndex && fracPart < TONE_HYSTERESIS)      return;
        if (desiredIndex < currentGuitarToneIndex && fracPart > 1f - TONE_HYSTERESIS) return;

        if (desiredIndex != currentGuitarToneIndex)
        {
            // ── Ton değişti: dwell sıfırla, yeni tonu çal (henüz loop değil) ──
            currentGuitarToneIndex = desiredIndex;
            debugGuitarToneIndex   = desiredIndex;
            _dwellTimer            = 0f;
            debugDwellTimer        = 0f;

            NoteRecorder.Instance?.RecordGuitarTone(desiredIndex);
            LoopRecorder.Instance?.RecordGuitar(desiredIndex);

            if (guitarLoopActive)
            {
                guitarSource.loop = false; // önce tek seferlik çal
                SwitchToTone(desiredIndex);
            }
        }
        else
        {
            // ── Aynı tonda kalıyor: dwell timer ilerlet ──
            _dwellTimer     += Time.deltaTime;
            debugDwellTimer  = _dwellTimer;

            if (guitarLoopActive && _dwellTimer >= DWELL_TO_LOOP && !guitarSource.loop)
            {
                // Dwell süresi doldu → loop'a gir
                guitarSource.loop = true;
                if (!guitarSource.isPlaying)
                    guitarSource.Play(); // clip bitmişse yeniden başlat
                Debug.Log($"[AUDIO] Ton {desiredIndex} loop'a girdi.");
            }
        }
    }

    // Kayıt başlarken öğrenilen aralığı sıfırla — taze başlasın
    public void ResetSensorRange()
    {
        sensorObservedMin = 1f;
        sensorObservedMax = 0f;
    }

    // Playback sırasında doğrudan ton çal
    public void PlayGuitarTone(int toneIndex)
    {
        if (guitarToneClips == null || toneIndex < 0 || toneIndex >= guitarToneClips.Length) return;
        currentGuitarToneIndex = toneIndex;
        debugGuitarToneIndex   = toneIndex;
        if (guitarLoopActive) SwitchToTone(toneIndex);
    }

    private void SwitchToTone(int toneIndex)
    {
        if (guitarSource == null || guitarToneClips == null) return;
        var clip = guitarToneClips[toneIndex];
        if (clip == null) return;
        guitarSource.clip = clip;
        guitarSource.Play();
    }

    public int GetCurrentGuitarToneIndex() => currentGuitarToneIndex;

    // --- Gitar Mute ---
    public void SetGuitarMuted(bool muted)
    {
        debugIsGuitarMuted = muted;

        if (guitarSource == null) return;

        if (muted)
        {
            guitarSource.volume = 0f;
            Debug.Log("[AUDIO] [GITAR] Gitar MUTE");
        }
        else
        {
            guitarSource.volume = 0.7f;
            Debug.Log("[AUDIO] [GITAR] Gitar AÇIK");
        }
    }

    // --- Piyano Nota Çal ---
    public void PlayPianoNote(int keyIndex)
    {
        if (pianoNotes == null || pianoNotes.Length == 0)
        {
            Debug.LogWarning("[AUDIO] [UYARI] Piano clip yok!");
            return;
        }

        keyIndex = Mathf.Clamp(keyIndex, 0, pianoNotes.Length - 1);

        if (pianoNotes[keyIndex] == null)
        {
            Debug.LogWarning($"[AUDIO] [UYARI] Piano clip[{keyIndex}] atanmamış!");
            return;
        }

        pianoSource.pitch = pianoNotePitches[keyIndex];
        pianoSource.PlayOneShot(pianoNotes[keyIndex], 0.8f);
        NoteRecorder.Instance?.RecordPiano(keyIndex);
        LoopRecorder.Instance?.RecordPiano(keyIndex);
        Debug.Log($"[AUDIO] [PIANO] Nota çalındı: {keyIndex} (pitch:{pianoNotePitches[keyIndex]})");
    }

    // --- Davul Kick ---
    public void PlayDrumKick()
    {
        if (drumKick == null)
        {
            Debug.LogWarning("[AUDIO] [UYARI] Drum clip yok!");
            return;
        }

        drumSource.PlayOneShot(drumKick, 1f);
        NoteRecorder.Instance?.RecordDrum();
        LoopRecorder.Instance?.RecordDrum();
        Debug.Log("[AUDIO] [DAVUL] Kick!");
    }

    // --- Gitar Loop Kontrolü ---
    public void StartGuitarLoop()
    {
        if (guitarSource == null) return;
        guitarLoopActive       = true;
        currentGuitarToneIndex = -1;
        _dwellTimer            = 0f;
        debugDwellTimer        = 0f;
        guitarSource.loop      = false;
        Debug.Log("[AUDIO] Gitar loop başladı.");
    }

    public void StopGuitarLoop()
    {
        if (guitarSource == null) return;
        guitarLoopActive       = false;
        _dwellTimer            = 0f;
        debugDwellTimer        = 0f;
        guitarSource.loop      = false;
        guitarSource.Stop();
        Debug.Log("[AUDIO] Gitar loop durduruldu.");
    }

    // Gitar ton kliplerini dışarıdan güncelle (ileride SoundPresetManager için)
    public void SetGuitarToneClips(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        guitarToneClips        = clips;
        currentGuitarToneIndex = -1;
        Debug.Log($"[AUDIO] Gitar tonları güncellendi: {clips.Length} ton.");
    }

    public void SetPianoClips(AudioClip[] clips)
    {
        if (clips == null) return;
        pianoNotes = clips;
        Debug.Log($"[AUDIO] Piyano sesleri guncellendi.");
    }

    public void SetDrumClip(AudioClip clip)
    {
        if (clip == null) return;
        drumKick = clip;
        Debug.Log($"[AUDIO] Davul sesi degisti: {clip.name}");
    }
}