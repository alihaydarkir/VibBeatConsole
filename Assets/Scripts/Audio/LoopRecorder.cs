using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beat grid'e kilitli loop recorder.
/// Kayıt sırasında çalınan piyano/davul/gitar notalarını zaman damgasıyla saklar.
/// Oynatmada loop uzunluğu kadar döngü oluşturur — üstüne ekleme yapılabilir.
/// </summary>
public class LoopRecorder : MonoBehaviour
{
    public static LoopRecorder Instance { get; private set; }

    public enum State { Idle, Recording, Ready, Playing }

    // ─── Events ──────────────────────────────────────────────────────────
    public event System.Action<State> OnStateChanged;

    // ─── Inspector ───────────────────────────────────────────────────────
    [Header("Debug (Read-Only)")]
    [SerializeField] private State  debugState      = State.Idle;
    [SerializeField] private int    debugNoteCount  = 0;
    [SerializeField] private float  debugLoopLength = 0f;
    [SerializeField] private float  debugTimer      = 0f;

    // ─── Not yapısı ──────────────────────────────────────────────────────
    private enum NoteType { Piano, Drum, Guitar }

    private struct LoopNote
    {
        public float    time;
        public NoteType type;
        public int      data; // piano key / guitar tone index (drum için kullanılmaz)
    }

    // ─── İç durum ────────────────────────────────────────────────────────
    private readonly List<LoopNote> _notes = new List<LoopNote>();
    private float  _recordStart = 0f;
    private float  _loopLength  = 0f;
    private float  _timer       = 0f;
    private State  _state       = State.Idle;

    public State CurrentState => _state;
    public bool  HasLoop      => _notes.Count > 0 && _loopLength > 0f;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (_state != State.Playing) return;

        float prev   = _timer;
        _timer      += Time.deltaTime;
        debugTimer   = _timer;

        bool looped = false;
        if (_timer >= _loopLength)
        {
            _timer  -= _loopLength;
            looped   = true;
            Debug.Log("[LOOP] Döngü tekrar başladı.");
        }

        // Zaman penceresindeki notaları çal
        foreach (var note in _notes)
        {
            bool fire = looped
                ? (note.time >= prev || note.time < _timer)
                : (note.time >= prev && note.time < _timer);

            if (fire) PlayNote(note);
        }
    }

    // ─── Kayıt ───────────────────────────────────────────────────────────
    public void StartRecording()
    {
        _notes.Clear();
        _recordStart    = Time.time;
        _loopLength     = 0f;
        _timer          = 0f;
        SetState(State.Recording);
        Debug.Log("[LOOP] Kayıt başladı.");
    }

    public void StopRecording()
    {
        if (_state != State.Recording) return;

        float elapsed = Time.time - _recordStart;

        // Bar'a snap et — en az 1 bar
        float barLength = GetBarLength();
        float bars      = Mathf.Max(1f, Mathf.Round(elapsed / barLength));
        _loopLength     = bars * barLength;
        debugLoopLength = _loopLength;
        debugNoteCount  = _notes.Count;

        SetState(State.Ready);
        Debug.Log($"[LOOP] Kayıt durdu — {_notes.Count} nota, {bars:0} bar, {_loopLength:0.00}s");
    }

    // ─── Oynatma ─────────────────────────────────────────────────────────
    public void StartPlayback()
    {
        if (_state == State.Idle) return;
        if (_loopLength <= 0f) { Debug.LogWarning("[LOOP] Loop uzunluğu sıfır — önce kayıt yap."); return; }

        _timer = 0f;
        SetState(State.Playing);
        Debug.Log("[LOOP] Oynatma başladı.");
    }

    public void StopPlayback()
    {
        if (_state != State.Playing) return;
        _timer = 0f;
        SetState(State.Ready);
        Debug.Log("[LOOP] Oynatma durdu.");
    }

    public void Clear()
    {
        _notes.Clear();
        _loopLength     = 0f;
        _timer          = 0f;
        debugNoteCount  = 0;
        debugLoopLength = 0f;
        SetState(State.Idle);
        Debug.Log("[LOOP] Temizlendi.");
    }

    // ─── Not kaydetme (MasterController çağırır) ─────────────────────────
    public void RecordPiano(int keyIndex)
    {
        if (_state != State.Recording) return;
        Add(NoteType.Piano, keyIndex);
    }

    public void RecordDrum()
    {
        if (_state != State.Recording) return;
        Add(NoteType.Drum, 0);
    }

    public void RecordGuitar(int toneIndex)
    {
        if (_state != State.Recording) return;
        Add(NoteType.Guitar, toneIndex);
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────
    private void Add(NoteType type, int data)
    {
        float t = Time.time - _recordStart;
        _notes.Add(new LoopNote { time = t, type = type, data = data });
        debugNoteCount = _notes.Count;
    }

    private void PlayNote(LoopNote note)
    {
        var synth = AudioSynthesizer.Instance;
        if (synth == null) return;

        switch (note.type)
        {
            case NoteType.Piano:  synth.PlayPianoNote(note.data);  break;
            case NoteType.Drum:   synth.PlayDrumKick();             break;
            case NoteType.Guitar: synth.PlayGuitarTone(note.data);  break;
        }
    }

    private float GetBarLength()
    {
        if (MetronomeController.Instance == null) return 2f; // fallback: 2s
        float bpm        = MetronomeController.Instance.BPM;
        int   beatsPerBar = 4; // standart 4/4
        return (60f / bpm) * beatsPerBar;
    }

    private void SetState(State s)
    {
        _state     = s;
        debugState = s;
        OnStateChanged?.Invoke(s);
    }
}
