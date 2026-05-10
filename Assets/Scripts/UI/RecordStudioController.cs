using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kayıt Stüdyosu ekranının UI mantığı.
/// NoteRecorder ile konuşur, buton durumlarını günceller.
/// AutoUIBuilder tarafından sahneye eklenir.
/// </summary>
public class RecordStudioController : MonoBehaviour
{
    // ─── UI Referansları ───
    private Button   recButton;
    private Button   playButton;
    private Button   stopButton;
    private Button   clearButton;
    private TMP_Text statusText;
    private TMP_Text durationText;
    private TMP_Text noteCountText;
    private TMP_Text loopText;

    // ─── Renkler ───
    private static readonly Color ColReady   = Hex("#FF0044");
    private static readonly Color ColRecording = Hex("#FF0044");
    private static readonly Color ColPlaying = Hex("#00FF88");
    private static readonly Color ColStopped = Hex("#FFE600");
    private static readonly Color ColDisabled = Hex("#333355");

    private bool loopEnabled = false;
    private float displayTimer = 0f;

    // ─────────────────────────────────────────
    // KURULUM — AutoUIBuilder tarafından çağrılır
    // ─────────────────────────────────────────
    public void SetupReferences(
        Button rec, Button play, Button stop, Button clear,
        TMP_Text status, TMP_Text duration, TMP_Text noteCount, TMP_Text loop)
    {
        recButton     = rec;
        playButton    = play;
        stopButton    = stop;
        clearButton   = clear;
        statusText    = status;
        durationText  = duration;
        noteCountText = noteCount;
        loopText      = loop;

        BindButtons();
        UpdateUI();
    }

    private void BindButtons()
    {
        recButton?.onClick.AddListener(OnRecordPressed);
        playButton?.onClick.AddListener(OnPlayPressed);
        stopButton?.onClick.AddListener(OnStopPressed);
        clearButton?.onClick.AddListener(OnClearPressed);
        loopText?.GetComponentInParent<Button>()?.onClick.AddListener(OnLoopToggle);
    }

    private void OnEnable()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        if (NoteRecorder.Instance != null)
            NoteRecorder.Instance.OnStateChanged -= UpdateUI;
    }

    // ─────────────────────────────────────────
    // BUTON OLAYLARI
    // ─────────────────────────────────────────
    private void OnRecordPressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null) return;

        if (rec.IsRecording)
            rec.StopRecording();
        else
            rec.StartRecording();
    }

    private void OnPlayPressed()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null || !rec.HasRecording) return;

        if (loopEnabled)
            StartCoroutine(LoopPlayBack(rec));
        else
            rec.PlayBack();
    }

    private System.Collections.IEnumerator LoopPlayBack(NoteRecorder rec)
    {
        while (loopEnabled && rec.HasRecording)
        {
            rec.PlayBack();
            float dur = rec.GetRecordingDuration();
            yield return new WaitForSeconds(dur + 0.3f);
        }
    }

    private void OnStopPressed()
    {
        StopAllCoroutines();
        NoteRecorder.Instance?.StopPlayBack();
        NoteRecorder.Instance?.StopRecording();
    }

    private void OnClearPressed()
    {
        StopAllCoroutines();
        NoteRecorder.Instance?.ClearRecording();
    }

    private void OnLoopToggle()
    {
        loopEnabled = !loopEnabled;
        UpdateUI();
    }

    // ─────────────────────────────────────────
    // UI GÜNCELLEME
    // ─────────────────────────────────────────
    private void Update()
    {
        // Süreyi her frame güncelle
        displayTimer += Time.deltaTime;
        if (displayTimer < 0.1f) return;
        displayTimer = 0f;

        var rec = NoteRecorder.Instance;
        if (rec == null) return;

        if (durationText != null)
        {
            float dur = rec.GetRecordingDuration();
            int minutes = (int)(dur / 60f);
            int seconds = (int)(dur % 60f);
            durationText.text = $"{minutes:00}:{seconds:00}";
        }

        if (noteCountText != null && rec.IsRecording)
            noteCountText.text = rec.NoteCount.ToString();
    }

    private void UpdateUI()
    {
        var rec = NoteRecorder.Instance;
        if (rec == null) return;

        // Buton durumları
        bool hasRec = rec.HasRecording;

        // REC butonu — kayıt varsa kırmızı yanıp söner gibi metin
        if (recButton != null)
        {
            var lbl = recButton.GetComponentInChildren<TMP_Text>();
            if (lbl != null)
                lbl.text = rec.IsRecording ? "■ DURDUR" : "● KAYDET";
            SetBtnColor(recButton, rec.IsRecording ? ColRecording : ColReady);
        }

        // OYNAT — kayıt yoksa soluk
        if (playButton != null)
            SetBtnColor(playButton, hasRec && !rec.IsRecording ? ColPlaying : ColDisabled);

        // DURDUR
        if (stopButton != null)
            SetBtnColor(stopButton, (rec.IsRecording || rec.IsPlaying) ? ColStopped : ColDisabled);

        // SİL
        if (clearButton != null)
            SetBtnColor(clearButton, hasRec ? Hex("#FF4400") : ColDisabled);

        // Durum metni
        if (statusText != null)
        {
            if (rec.IsRecording)
            {
                statusText.text = "● KAYIT YAPILIYOR...";
                statusText.color = ColRecording;
            }
            else if (rec.IsPlaying)
            {
                statusText.text = "▶ OYNATILIYOR";
                statusText.color = ColPlaying;
            }
            else if (hasRec)
            {
                statusText.text = $"■ Kayıt Hazır — {rec.NoteCount} nota";
                statusText.color = ColStopped;
            }
            else
            {
                statusText.text = "● Kayıt Hazır";
                statusText.color = Hex("#555577");
            }
        }

        // Nota sayacı
        if (noteCountText != null)
            noteCountText.text = rec.NoteCount.ToString();

        // Döngü metni
        if (loopText != null)
        {
            loopText.text  = loopEnabled ? "AÇIK" : "KAPALI";
            loopText.color = loopEnabled ? ColPlaying : Hex("#555577");
        }
    }

    private void SetBtnColor(Button btn, Color c)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 1f);

        var lbl = btn.GetComponentInChildren<TMP_Text>();
        if (lbl != null) lbl.color = c;

        // Alt border rengini güncelle
        var border = btn.transform.Find(btn.name.Replace("Button", "") + "Border")
                  ?? btn.transform.GetChild(0);
        if (border != null)
        {
            var bi = border.GetComponent<Image>();
            if (bi != null) bi.color = c;
        }
    }

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c); return c;
    }
}
