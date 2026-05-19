using UnityEngine;
using System;

/// <summary>
/// BPM tabanlı metronom.
/// UI'ı sen ekle — bu sınıf sadece beat mantığını yönetir.
///
/// Public API:
///   IsPlaying, BPM, OnBeat, OnBar
///   SetBPM(float), IncreaseBPM(), DecreaseBPM()
///   Play(), Stop(), Toggle()
/// </summary>
public class MetronomeController : MonoBehaviour
{
    public static MetronomeController Instance { get; private set; }

    // ─── Ayarlar ─────────────────────────────────────────────────────────
    [Header("Metronom")]
    [SerializeField] [Range(40, 220)] private float bpm         = 120f;
    [SerializeField]                  private int   beatsPerBar = 4;
    [SerializeField]                  private bool  playOnStart = false;

    [Header("Geri Bildirim")]
    [SerializeField] private bool hapticOnBeat    = true;
    [SerializeField] private bool accentFirstBeat = true;

    [Header("Debug (Read-Only)")]
    [SerializeField] private float debugBPM        = 120f;
    [SerializeField] private int   debugBeatNumber = 0;

    // ─── Events ──────────────────────────────────────────────────────────
    /// <summary>Her beat'te ateşlenir. beatNumber: 1..beatsPerBar</summary>
    public event Action<int> OnBeat;
    /// <summary>Her bar başında (1. beat) ateşlenir.</summary>
    public event Action OnBar;

    // ─── İç durum ────────────────────────────────────────────────────────
    private float                _beatInterval;
    private float                _nextBeatTime;
    private int                  _beatNumber = 0;
    private bool                 _isPlaying  = false;
    private HapticFeedbackManager _haptic    = null;

    public bool  IsPlaying => _isPlaying;
    public float BPM       => bpm;
    public int   BeatNumber => _beatNumber;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // ekran geçişlerinde deaktif olmasın
        RecalcInterval();
    }

    private void Start()
    {
        _haptic = FindFirstObjectByType<HapticFeedbackManager>();
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        if (Time.time >= _nextBeatTime)
        {
            _nextBeatTime += _beatInterval;
            _beatNumber    = (_beatNumber % beatsPerBar) + 1;
            debugBeatNumber = _beatNumber;
            FireBeat(_beatNumber);
        }
    }

    // ─── Beat tetikleme ──────────────────────────────────────────────────
    private void FireBeat(int beatNum)
    {
        bool isAccent = accentFirstBeat && beatNum == 1;

        // Titreşim — önce cache'e bak, yoksa bul
        if (hapticOnBeat)
        {
            if (_haptic == null) _haptic = FindFirstObjectByType<HapticFeedbackManager>();
            if (_haptic != null)
            {
                if (isAccent) _haptic.PlayDrumKickFeedback();
                else          _haptic.PlayGuitarMuteFeedback();
            }
        }


        OnBeat?.Invoke(beatNum);
        if (beatNum == 1) OnBar?.Invoke();

        Debug.Log($"[METRO] Beat {beatNum}/{beatsPerBar} @ {bpm:0} BPM");
    }

    // ─── Public API ──────────────────────────────────────────────────────
    public void Play()
    {
        if (_isPlaying) return;
        _isPlaying      = true;
        _beatNumber     = 0;
        _nextBeatTime   = Time.time + _beatInterval; // ilk vuruş hemen değil, 1 beat sonra
        Debug.Log($"[METRO] Başladı — {bpm:0} BPM");
    }

    public void Stop()
    {
        _isPlaying     = false;
        _beatNumber    = 0;
        debugBeatNumber = 0;
        Debug.Log("[METRO] Durdu.");
    }

    public void Toggle()
    {
        if (_isPlaying) Stop(); else Play();
    }

    public void SetBPM(float newBpm)
    {
        bpm      = Mathf.Clamp(newBpm, 40f, 220f);
        debugBPM = bpm;
        RecalcInterval();

        // Çalışırken interval güncellenir, mevcut beat zamanı korunur
        Debug.Log($"[METRO] BPM → {bpm:0}");
    }

    public void IncreaseBPM(float step = 5f) => SetBPM(bpm + step);
    public void DecreaseBPM(float step = 5f) => SetBPM(bpm - step);

    public void SetBeatsPerBar(int beats)
    {
        beatsPerBar = Mathf.Clamp(beats, 1, 8);
        _beatNumber = 0;
    }

    public void SetHapticOnBeat(bool value) => hapticOnBeat = value;

    // ─── Yardımcı ────────────────────────────────────────────────────────
    private void RecalcInterval()
    {
        _beatInterval = 60f / bpm;
        debugBPM      = bpm;
    }
}
