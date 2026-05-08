using UnityEngine;

public class AudioSynthesizer : MonoBehaviour
{
    // --- Audio Sources ---
    [SerializeField] private AudioSource guitarSource;
    [SerializeField] private AudioSource pianoSource;
    [SerializeField] private AudioSource drumSource;

    // --- Audio Clips ---
    [SerializeField] private AudioClip guitarLoop;
    [SerializeField] private AudioClip[] pianoNotes;  // 4 adet (Do, Re, Mi, Fa)
    [SerializeField] private AudioClip drumKick;

    // --- Pitch Kontrolü ---
    private float currentPitch = 1f;
    private float targetPitch = 1f;
    private const float PITCH_SMOOTH = 8f;  // Yumuşatma hızı

    // --- Inspector Debug ---
    [SerializeField] private float debugCurrentPitch = 1f;
    [SerializeField] private float debugNormalizedLux = 0f;
    [SerializeField] private bool debugIsGuitarMuted = false;

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

        guitarSource.clip = guitarLoop;
        guitarSource.loop = true;
        guitarSource.volume = 0.7f;
        guitarSource.pitch = 1f;
        guitarSource.Play();

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

        Debug.Log("[AUDIO] ✅ AudioSources hazır!");
    }

    private void Update()
    {
        // --- Smooth Pitch Interpolation ---
        currentPitch = Mathf.Lerp(currentPitch, targetPitch,
            Time.deltaTime * PITCH_SMOOTH);

        if (guitarSource != null)
            guitarSource.pitch = currentPitch;

        debugCurrentPitch = currentPitch;
    }

    // --- Sensörden gelen 0-1 değeri pitch'e dönüştür ---
    public void SetGuitarPitchFromSensor(float normalizedValue)
    {
        debugNormalizedLux = normalizedValue;

        // 0.5 → pitch=1.0 (normal)
        // 0.0 → pitch=0.5 (kalın)
        // 1.0 → pitch=2.0 (ince)
        targetPitch = Mathf.Lerp(0.5f, 2.0f, normalizedValue);
    }

    // --- Gitar Mute ---
    public void SetGuitarMuted(bool muted)
    {
        debugIsGuitarMuted = muted;

        if (guitarSource == null) return;

        if (muted)
        {
            guitarSource.volume = 0f;
            Debug.Log("[AUDIO] 🎸 Gitar MUTE");
        }
        else
        {
            guitarSource.volume = 0.7f;
            Debug.Log("[AUDIO] 🎸 Gitar AÇIK");
        }
    }

    // --- Piyano Nota Çal ---
    public void PlayPianoNote(int keyIndex)
    {
        if (pianoNotes == null || pianoNotes.Length == 0)
        {
            Debug.LogWarning("[AUDIO] ⚠️ Piano clip yok!");
            return;
        }

        keyIndex = Mathf.Clamp(keyIndex, 0, pianoNotes.Length - 1);

        if (pianoNotes[keyIndex] == null)
        {
            Debug.LogWarning($"[AUDIO] ⚠️ Piano clip[{keyIndex}] atanmamış!");
            return;
        }

        pianoSource.pitch = pianoNotePitches[keyIndex];
        pianoSource.PlayOneShot(pianoNotes[keyIndex], 0.8f);
        Debug.Log($"[AUDIO] 🎹 Nota çalındı: {keyIndex} (pitch:{pianoNotePitches[keyIndex]})");
    }

    // --- Davul Kick ---
    public void PlayDrumKick()
    {
        if (drumKick == null)
        {
            Debug.LogWarning("[AUDIO] ⚠️ Drum clip yok!");
            return;
        }

        drumSource.PlayOneShot(drumKick, 1f);
        Debug.Log("[AUDIO] 🥁 Kick!");
    }

    // --- Getters ---
    public float GetCurrentPitch() => currentPitch;
}