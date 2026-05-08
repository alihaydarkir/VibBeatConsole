using UnityEngine;
using System.Collections;

public class HapticFeedbackManager : MonoBehaviour
{
    // --- Ayarlar ---
    [SerializeField] private bool hapticEnabled = true;

    // --- Android Vibrator ---
    private AndroidJavaObject vibrator = null;
    private bool isAndroidReady = false;

    // --- Pattern Ayarları ---
    [Header("Drum Kick")]
    [SerializeField] private long drumKickDurationMs = 120;
    [SerializeField] private int drumKickAmplitude = 220;  // 0-255

    [Header("Piano Key")]
    [SerializeField] private long pianoKeyDurationMs = 40;
    [SerializeField] private int pianoKeyAmplitude = 120;

    [Header("Guitar Mute")]
    [SerializeField] private long guitarMuteDurationMs = 30;
    [SerializeField] private int guitarMuteAmplitude = 80;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidVibrator();
#else
        Debug.Log("[HAPTIC] Editor modu - titreşim simüle edilecek");
#endif
    }

    private void InitializeAndroidVibrator()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                vibrator = activity.Call<AndroidJavaObject>(
                    "getSystemService", "vibrator"
                );
                isAndroidReady = vibrator != null;
                Debug.Log($"[HAPTIC] ✅ Vibrator hazır: {isAndroidReady}");
            }
        }
        catch (System.Exception ex)
        {
            isAndroidReady = false;
            Debug.LogError($"[HAPTIC] ❌ Hata: {ex.Message}");
        }
    }

    // --- Davul Kick (Güçlü & Kısa) ---
    public void PlayDrumKickFeedback()
    {
        if (!hapticEnabled) return;
        Vibrate(drumKickDurationMs, drumKickAmplitude);
        Debug.Log("[HAPTIC] 🥁 Drum kick titreşimi!");
    }

    // --- Piyano Tuş (Hafif & Çok Kısa) ---
    public void PlayPianoKeyFeedback()
    {
        if (!hapticEnabled) return;
        Vibrate(pianoKeyDurationMs, pianoKeyAmplitude);
        Debug.Log("[HAPTIC] 🎹 Piano key titreşimi!");
    }

    // --- Gitar Mute (Çok Hafif) ---
    public void PlayGuitarMuteFeedback()
    {
        if (!hapticEnabled) return;
        Vibrate(guitarMuteDurationMs, guitarMuteAmplitude);
        Debug.Log("[HAPTIC] 🎸 Guitar mute titreşimi!");
    }

    // --- Ana Vibrate Fonksiyonu ---
    private void Vibrate(long durationMs, int amplitude)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isAndroidReady || vibrator == null) return;

        try
        {
            // Android 8.0+ (API 26+) → Amplitude destekli
            if (IsAndroidAPI26OrHigher())
            {
                using (var effectClass = new AndroidJavaClass(
                    "android.os.VibrationEffect"))
                {
                    var effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", durationMs, amplitude
                    );
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                // Eski API → Sadece süre
                vibrator.Call("vibrate", durationMs);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HAPTIC] ❌ Vibrate hatası: {ex.Message}");
        }
#else
        // Editor simülasyon logu
        Debug.Log($"[HAPTIC] 📳 Simüle: {durationMs}ms amplitude:{amplitude}");
#endif
    }

    private bool IsAndroidAPI26OrHigher()
    {
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = version.GetStatic<int>("SDK_INT");
                return sdkInt >= 26;
            }
        }
        catch
        {
            return false;
        }
    }

    // --- Ayarlar ---
    public void SetHapticEnabled(bool enabled)
    {
        hapticEnabled = enabled;
        Debug.Log($"[HAPTIC] Haptic: {(enabled ? "AÇIK" : "KAPALI")}");
    }

    public bool IsHapticEnabled() => hapticEnabled;

    private void OnDestroy()
    {
        vibrator?.Dispose();
    }
}