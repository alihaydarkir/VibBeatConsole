using UnityEngine;

/// <summary>
/// Ses Studyosu — kullanicinin gitar, piyano ve davul seslerini
/// secmesini saglar. Secimler PlayerPrefs'e kaydedilir.
///
/// AudioSynthesizer bu siniftan aktif klipleri alir.
/// </summary>
public class SoundPresetManager : MonoBehaviour
{
    public static SoundPresetManager Instance { get; private set; }

    // ─────────────────────────────────────────
    // INSPECTOR — tüm ses klipleri buraya atanır
    // ─────────────────────────────────────────
    [Header("Gitar Sesleri")]
    public AudioClip[] guitarClips;   // guitar_01_near, 02_midn, 03_midfar, 04_far
    public string[]    guitarNames;   // "Yakin", "Orta", "Uzak", "Cok Uzak"

    [Header("Piyano Notaları")]
    public AudioClip[] pianoClips;    // piano_C4, D4, E4, F4
    public string[]    pianoNames;    // "Do", "Re", "Mi", "Fa"

    [Header("Davul Sesleri")]
    public AudioClip[] drumClips;     // drum_kick (+ eklenecekler)
    public string[]    drumNames;     // "Kick", ...

    // ─────────────────────────────────────────
    // KAYITLI SEÇİMLER
    // ─────────────────────────────────────────
    private int guitarIndex = 0;
    private int[] pianoIndexes = new int[4] { 0, 1, 2, 3 }; // Do=0,Re=1,Mi=2,Fa=3
    private int drumIndex   = 0;

    private const string KEY_GUITAR = "preset_guitar";
    private const string KEY_PIANO  = "preset_piano_";
    private const string KEY_DRUM   = "preset_drum";

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadPrefs();
    }

    private void Start()
    {
        ApplyToSynthesizer();
        Debug.Log($"[SOUND_PRESET] Yuklendi — Gitar:{guitarIndex} Davul:{drumIndex}");
    }

    // ─────────────────────────────────────────
    // UYGULA
    // ─────────────────────────────────────────
    public void ApplyToSynthesizer()
    {
        var synth = FindFirstObjectByType<AudioSynthesizer>();
        if (synth == null) return;

        if (guitarClips != null && guitarIndex < guitarClips.Length)
            synth.SetGuitarClip(guitarClips[guitarIndex]);

        if (pianoClips != null && pianoClips.Length > 0)
        {
            AudioClip[] selected = new AudioClip[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = Mathf.Clamp(pianoIndexes[i], 0, pianoClips.Length - 1);
                selected[i] = pianoClips[idx];
            }
            synth.SetPianoClips(selected);
        }

        if (drumClips != null && drumIndex < drumClips.Length)
            synth.SetDrumClip(drumClips[drumIndex]);
    }

    // ─────────────────────────────────────────
    // SETTERLAR — UI butonlari bunlari cagirir
    // ─────────────────────────────────────────
    public void SetGuitarIndex(int idx)
    {
        guitarIndex = Mathf.Clamp(idx, 0, guitarClips.Length - 1);
        SavePrefs();
        ApplyToSynthesizer();
        Debug.Log($"[SOUND_PRESET] Gitar: {guitarNames[guitarIndex]}");
        AccessibilityManager.Instance?.Speak($"Gitar sesi: {guitarNames[guitarIndex]}");
    }

    public void SetPianoNote(int noteSlot, int clipIdx)
    {
        pianoIndexes[noteSlot] = Mathf.Clamp(clipIdx, 0, pianoClips.Length - 1);
        SavePrefs();
        ApplyToSynthesizer();
        string[] slots = {"Do","Re","Mi","Fa"};
        Debug.Log($"[SOUND_PRESET] Piyano {slots[noteSlot]}: {pianoNames[clipIdx]}");
        AccessibilityManager.Instance?.Speak($"{slots[noteSlot]} sesi degistirildi");
    }

    public void SetDrumIndex(int idx)
    {
        drumIndex = Mathf.Clamp(idx, 0, drumClips.Length - 1);
        SavePrefs();
        ApplyToSynthesizer();
        Debug.Log($"[SOUND_PRESET] Davul: {drumNames[drumIndex]}");
        AccessibilityManager.Instance?.Speak($"Davul sesi: {drumNames[drumIndex]}");
    }

    // ─────────────────────────────────────────
    // GETTERLAR
    // ─────────────────────────────────────────
    public int   GetGuitarIndex()            => guitarIndex;
    public int   GetPianoIndex(int slot)     => pianoIndexes[slot];
    public int   GetDrumIndex()              => drumIndex;
    public int   GetGuitarCount()            => guitarClips?.Length ?? 0;
    public int   GetPianoClipCount()         => pianoClips?.Length ?? 0;
    public int   GetDrumCount()              => drumClips?.Length ?? 0;
    public string GetGuitarName(int i)       => (guitarNames != null && i < guitarNames.Length) ? guitarNames[i] : $"Gitar {i+1}";
    public string GetPianoName(int i)        => (pianoNames  != null && i < pianoNames.Length)  ? pianoNames[i]  : $"Nota {i+1}";
    public string GetDrumName(int i)         => (drumNames   != null && i < drumNames.Length)   ? drumNames[i]   : $"Davul {i+1}";

    // ─────────────────────────────────────────
    // KAYDET / YUKLE
    // ─────────────────────────────────────────
    private void SavePrefs()
    {
        PlayerPrefs.SetInt(KEY_GUITAR, guitarIndex);
        PlayerPrefs.SetInt(KEY_DRUM,   drumIndex);
        for (int i = 0; i < 4; i++)
            PlayerPrefs.SetInt(KEY_PIANO + i, pianoIndexes[i]);
        PlayerPrefs.Save();
    }

    private void LoadPrefs()
    {
        guitarIndex = PlayerPrefs.GetInt(KEY_GUITAR, 0);
        drumIndex   = PlayerPrefs.GetInt(KEY_DRUM,   0);
        for (int i = 0; i < 4; i++)
            pianoIndexes[i] = PlayerPrefs.GetInt(KEY_PIANO + i, i);
    }
}
