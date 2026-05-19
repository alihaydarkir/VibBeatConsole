using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Çalınan notaları zamanlama ile kaydeder ve geri oynatır.
/// UI'a bağlı değil — AudioSynthesizer üzerinden tetiklenir.
/// </summary>
public class NoteRecorder : MonoBehaviour
{
    public static NoteRecorder Instance { get; private set; }

    // ─── Kayıt olayı yapısı ───
    private struct NoteEvent
    {
        public float  time;
        public string instrument; // "piano" | "drum" | "guitar"
        public int    noteIndex;  // piyano: 0-3, diğer: -1
        public float  pitchValue; // gitar normalize değeri
    }

    private List<NoteEvent> recordedNotes = new List<NoteEvent>();
    private bool  isRecording = false;
    private bool  isPlaying   = false;
    private float recordStart = 0f;


    // Public durum sorguları — UI butonları bunlara bakacak
    public bool IsRecording => isRecording;
    public bool IsPlaying   => isPlaying;
    public int  NoteCount   => recordedNotes.Count;
    public bool HasRecording => recordedNotes.Count > 0;

    // Durum değiştiğinde UI'ı haberdar et
    public event System.Action OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────
    // KAYIT KONTROLÜ
    // ─────────────────────────────────────────

    public void StartRecording()
    {
        if (isPlaying) StopPlayBack();
        recordedNotes.Clear();
        recordStart = Time.time;
        isRecording = true;
        // Her kayıtta dinamik aralık sıfırlansın — taze öğrensin
        AudioSynthesizer.Instance?.ResetSensorRange();
        OnStateChanged?.Invoke();
        Debug.Log("[RECORDER] ● Kayıt başladı.");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        OnStateChanged?.Invoke();
        Debug.Log($"[RECORDER] ■ Kayıt durduruldu. {recordedNotes.Count} nota.");
    }

    public void ClearRecording()
    {
        StopPlayBack();
        StopRecording();
        recordedNotes.Clear();
        OnStateChanged?.Invoke();
        Debug.Log("[RECORDER] Kayıt silindi.");
    }

    // ─────────────────────────────────────────
    // NOTA KAYDETME — AudioSynthesizer'dan çağrılır
    // ─────────────────────────────────────────

    public void RecordPiano(int keyIndex)
    {
        if (!isRecording) return;
        recordedNotes.Add(new NoteEvent
        {
            time       = Time.time - recordStart,
            instrument = "piano",
            noteIndex  = keyIndex
        });
    }

    public void RecordDrum()
    {
        if (!isRecording) return;
        recordedNotes.Add(new NoteEvent
        {
            time       = Time.time - recordStart,
            instrument = "drum",
            noteIndex  = -1
        });
    }

    // MasterController tarafından ton değişince çağrılır
    public void RecordGuitarTone(int toneIndex)
    {
        if (!isRecording) return;
        recordedNotes.Add(new NoteEvent
        {
            time       = Time.time - recordStart,
            instrument = "guitar",
            noteIndex  = toneIndex
        });
    }

    // ─────────────────────────────────────────
    // GERİ OYNATMA
    // ─────────────────────────────────────────

    public void PlayBack()
    {
        if (isRecording) StopRecording();
        if (isPlaying || recordedNotes.Count == 0)
        {
            Debug.LogWarning("[RECORDER] Kayıt yok veya zaten oynatılıyor.");
            return;
        }
        StartCoroutine(PlayBackCoroutine());
    }

    private IEnumerator PlayBackCoroutine()
    {
        isPlaying = true;
        OnStateChanged?.Invoke();
        Debug.Log($"[RECORDER] ▶ Geri oynatma — {recordedNotes.Count} nota.");

        var synth = AudioSynthesizer.Instance;
        if (synth == null)
        {
            Debug.LogError("[RECORDER] AudioSynthesizer bulunamadı!");
            isPlaying = false;
            OnStateChanged?.Invoke();
            yield break;
        }

        float playStart = Time.time;

        foreach (var note in recordedNotes)
        {
            float waitUntil = playStart + note.time;
            while (Time.time < waitUntil)
                yield return null;

            switch (note.instrument)
            {
                case "piano":  synth.PlayPianoNote(note.noteIndex);          break;
                case "drum":   synth.PlayDrumKick();                         break;
                case "guitar": synth.PlayGuitarTone(note.noteIndex); break;
            }
        }

        isPlaying = false;
        OnStateChanged?.Invoke();
        Debug.Log("[RECORDER] ■ Oynatma tamamlandı.");
    }

    public void StopPlayBack()
    {
        if (!isPlaying) return;
        isPlaying = false;
        StopAllCoroutines();
        OnStateChanged?.Invoke();
    }

    // ─────────────────────────────────────────
    // SÜRE BİLGİSİ — UI için
    // ─────────────────────────────────────────

    public float GetRecordingDuration()
    {
        if (isRecording) return Time.time - recordStart;
        if (recordedNotes.Count == 0) return 0f;
        return recordedNotes[recordedNotes.Count - 1].time;
    }
}
