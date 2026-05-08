using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VibeBeatBootstrap : MonoBehaviour
{
    // ─────────────────────────────────────────
    // EKRANLAR
    // ─────────────────────────────────────────
    private GameObject splashScreen;
    private GameObject onboardingScreen;
    private GameObject calibrationScreen;
    private GameObject mainConsoleScreen;
    private GameObject settingsScreen;

    // ─────────────────────────────────────────
    // UI REFERANSLARI
    // ─────────────────────────────────────────
    private TextMeshProUGUI sensorValueText;
    private TextMeshProUGUI calStepText;
    private TextMeshProUGUI calPercentText;
    private TextMeshProUGUI calLuxText;
    private TextMeshProUGUI calStatusText;

    // ─────────────────────────────────────────
    // SENSÖR
    // ─────────────────────────────────────────
    private float normalizedSensorValue = 0f;
    private bool sensorActive = false;

    // ─────────────────────────────────────────
    // SES
    // ─────────────────────────────────────────
    private AudioSource guitarAudioSource;
    private AudioSource pianoAudioSource;
    private AudioSource drumAudioSource;
    private bool guitarMuted = false;

    // ─────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────
    private void Awake()
    {
        // 1 — Ekranları bul
        Transform t = transform;
        splashScreen = FindChild(t, "SplashScreen");
        onboardingScreen = FindChild(t, "OnboardingScreen");
        calibrationScreen = FindChild(t, "CalibrationScreen");
        mainConsoleScreen = FindChild(t, "MainConsoleScreen");
        settingsScreen = FindChild(t, "SettingsScreen");

        // 2 — Hepsini geçici aç (Find() çalışsın diye)
        SetActive(splashScreen, true);
        SetActive(onboardingScreen, true);
        SetActive(calibrationScreen, true);
        SetActive(mainConsoleScreen, true);
        SetActive(settingsScreen, true);

        // 3 — UI bağlantıları kur
        BindUIReferences();

        // 4 — Ses kaynaklarını oluştur
        SetupAudio();

        // 5 — SplashScreen'i göster
        ShowSplash();

        Debug.Log("[VibeBeat] ✅ Bootstrap tamamlandı.");
    }

    // ─────────────────────────────────────────
    // UI BAĞLANTILARI
    // ─────────────────────────────────────────
    private void BindUIReferences()
    {
        BindSplashButtons();
        BindOnboardingButtons();
        BindCalibrationButtons();
        BindMainConsoleButtons();
        BindSettingsButtons();
    }

    private void BindSplashButtons()
    {
        if (splashScreen == null) return;
        BindButton(splashScreen.transform, "StartButton", ShowOnboarding);
    }

    private void BindOnboardingButtons()
    {
        if (onboardingScreen == null) return;
        BindButton(onboardingScreen.transform, "ContinueButton", ShowCalibration);
    }

    private void BindCalibrationButtons()
    {
        if (calibrationScreen == null) return;

        Transform card = calibrationScreen.transform.Find("CalibrationCard");
        if (card == null) return;

        calStepText = card.Find("StepText")?.GetComponent<TextMeshProUGUI>();
        calPercentText = card.Find("PercentText")?.GetComponent<TextMeshProUGUI>();

        Transform bar = card.Find("InfoBar");
        if (bar != null)
        {
            calLuxText = bar.Find("LuxText")?.GetComponent<TextMeshProUGUI>();
            calStatusText = bar.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        }

        BindButtonDirect(card, "RetryButton", () => StartCoroutine(CalibrationRoutine()));
        BindButtonDirect(card, "ContinueButton", ShowMainConsole);
    }

    private void BindMainConsoleButtons()
    {
        if (mainConsoleScreen == null) return;

        // TopBar
        Transform topBar = mainConsoleScreen.transform.Find("TopBar");
        if (topBar != null)
            BindButtonDirect(topBar, "SettingsButton", ShowSettings);

        // Guitar Panel
        Transform guitar = mainConsoleScreen.transform.Find("GuitarPanel");
        if (guitar != null)
        {
            sensorValueText = guitar.Find("SensorValueText")?.GetComponent<TextMeshProUGUI>();
            BindButtonDirect(guitar, "MuteButton", ToggleGuitarMute);
            BindButtonDirect(guitar, "CalibrateButton", ShowCalibration);
        }

        // Piano + Drum
        Transform right = mainConsoleScreen.transform.Find("RightPanel");
        if (right != null)
        {
            Transform piano = right.Find("PianoPanel");
            if (piano != null)
            {
                string[] notes = { "C4", "D4", "E4", "F4" };
                for (int i = 0; i < notes.Length; i++)
                {
                    int idx = i;
                    Transform keyT = piano.Find("PianoKey_" + notes[i]);
                    Button key = keyT?.GetComponent<Button>();
                    if (key != null)
                        key.onClick.AddListener(() => PlayPianoNote(idx));
                }
            }

            Transform drum = right.Find("DrumPanel");
            if (drum != null)
                BindButtonDirect(drum, "DrumPad", PlayDrumKick);
        }
    }

    // ─────────────────────────────────────────
    // AYARLAR STATE
    // ─────────────────────────────────────────
    private bool hapticEnabled = true;
    private int effectLevel = 1; // 0=Düşük 1=Orta 2=Yüksek
    private float masterVolume = 0.7f;
    private void BindSettingsButtons()
    {
        if (settingsScreen == null) return;

        Button backBtn = settingsScreen.transform
            .Find("BackToMainButton")?.GetComponent<Button>();
        if (backBtn != null)
        {
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(ShowMainConsole);
        }

        Transform sp = settingsScreen.transform.Find("SettingsPanel");
        if (sp == null) return;

        // Tekrar kalibre et
        Button recalBtn = sp.Find("RecalibrateRow")?.GetComponent<Button>();
        if (recalBtn != null)
        {
            recalBtn.onClick.RemoveAllListeners();
            recalBtn.onClick.AddListener(ShowCalibration);
        }

        // Haptic toggle
        Button hapticBtn = sp.Find("HapticRow")?.GetComponent<Button>();
        if (hapticBtn != null)
        {
            hapticBtn.onClick.RemoveAllListeners();
            hapticBtn.onClick.AddListener(() =>
            {
                hapticEnabled = !hapticEnabled;
                TextMeshProUGUI t = hapticBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                    t.text = "HAPTIC FEEDBACK   " + (hapticEnabled ? "AÇIK" : "KAPALI");
                Debug.Log($"[VibeBeat] Haptic: {hapticEnabled}");
            });
        }

        // Efekt yoğunluğu
        Button effectBtn = sp.Find("EffectIntensityRow")?.GetComponent<Button>();
        if (effectBtn != null)
        {
            effectBtn.onClick.RemoveAllListeners();
            effectBtn.onClick.AddListener(() =>
            {
                effectLevel = (effectLevel + 1) % 3;
                string[] levels = { "DÜŞÜK", "ORTA", "YÜKSEK" };
                TextMeshProUGUI t = effectBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                    t.text = "EFEKT YOĞUNLUĞU   " + levels[effectLevel];
                Debug.Log($"[VibeBeat] Efekt: {levels[effectLevel]}");
            });
        }

        // Ses seviyesi
        Button volumeBtn = sp.Find("VolumeRow")?.GetComponent<Button>();
        if (volumeBtn != null)
        {
            volumeBtn.onClick.RemoveAllListeners();
            volumeBtn.onClick.AddListener(() =>
            {
                masterVolume = masterVolume >= 1f ? 0f : masterVolume + 0.1f;
                if (guitarAudioSource != null) guitarAudioSource.volume = masterVolume;
                if (pianoAudioSource != null) pianoAudioSource.volume = masterVolume;
                if (drumAudioSource != null) drumAudioSource.volume = masterVolume;
                TextMeshProUGUI t = volumeBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                    t.text = "SES SEVİYESİ   %" + Mathf.RoundToInt(masterVolume * 100f);
                Debug.Log($"[VibeBeat] Volume: {masterVolume}");
            });
        }
    }

    // ─────────────────────────────────────────
    // SES KURULUM
    // ─────────────────────────────────────────
    private void SetupAudio()
    {
        guitarAudioSource = gameObject.AddComponent<AudioSource>();
        guitarAudioSource.loop = true;
        guitarAudioSource.volume = 0.7f;

        pianoAudioSource = gameObject.AddComponent<AudioSource>();
        pianoAudioSource.loop = false;

        drumAudioSource = gameObject.AddComponent<AudioSource>();
        drumAudioSource.loop = false;
    }

    // ─────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────
    private void Update()
    {
        if (!sensorActive) return;
        ReadSensor();
        UpdateGuitarPitch();
        UpdateSensorUI();
    }

    private void ReadSensor()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android build'de buraya gerçek lux değeri gelecek
        normalizedSensorValue = Mathf.Clamp01(normalizedSensorValue);
#else
        float target = Input.mousePosition.y / Screen.height;
        normalizedSensorValue = Mathf.Lerp(normalizedSensorValue, target, Time.deltaTime * 8f);
        normalizedSensorValue = Mathf.Round(normalizedSensorValue * 100f) / 100f;
#endif
    }

    private void UpdateGuitarPitch()
    {
        if (guitarMuted || guitarAudioSource == null) return;
        guitarAudioSource.pitch = Mathf.Lerp(0.5f, 2.0f, normalizedSensorValue);
    }

    private void UpdateSensorUI()
    {
        if (sensorValueText != null)
            sensorValueText.text = normalizedSensorValue.ToString("0.00");
    }

    // ─────────────────────────────────────────
    // EKRAN GEÇİŞLERİ
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        sensorActive = false;
        ShowOnly(splashScreen);
    }

    public void ShowOnboarding()
    {
        sensorActive = false;
        ShowOnly(onboardingScreen);
    }

    public void ShowCalibration()
    {
        sensorActive = false;
        ShowOnly(calibrationScreen);
        StartCoroutine(CalibrationRoutine());
    }

    public void ShowMainConsole()
    {
        sensorActive = true;
        ShowOnly(mainConsoleScreen);
        Debug.Log("[VibeBeat] 🎵 Ana konsol açıldı — sensör aktif.");
    }

    public void ShowSettings()
    {
        sensorActive = false;
        ShowOnly(settingsScreen);
    }

    private void ShowOnly(GameObject target)
    {
        SetActive(splashScreen, splashScreen == target);
        SetActive(onboardingScreen, onboardingScreen == target);
        SetActive(calibrationScreen, calibrationScreen == target);
        SetActive(mainConsoleScreen, mainConsoleScreen == target);
        SetActive(settingsScreen, settingsScreen == target);
    }

    // ─────────────────────────────────────────
    // MÜZİK
    // ─────────────────────────────────────────
    private void PlayPianoNote(int index)
    {
#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
        Debug.Log($"[VibeBeat] 🎹 Piyano nota: {index}");
    }

    private void PlayDrumKick()
    {
#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
        Debug.Log("[VibeBeat] 🥁 Davul vuruldu!");
    }

    private void ToggleGuitarMute()
    {
        guitarMuted = !guitarMuted;
        if (guitarAudioSource != null)
            guitarAudioSource.mute = guitarMuted;
        Debug.Log($"[VibeBeat] 🎸 Gitar mute: {guitarMuted}");
    }

    // ─────────────────────────────────────────
    // KALİBRASYON
    // ─────────────────────────────────────────
    private IEnumerator CalibrationRoutine()
    {
        SetText(calStepText, "1/2 Elini sensörün üstüne kapat");
        SetText(calPercentText, "0%");
        SetText(calLuxText, "Lux: 12.4");
        SetText(calStatusText, "Durum: Ölçülüyor");

        float duration = 2.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n = Mathf.Clamp01(timer / duration);
            int pct = Mathf.RoundToInt(n * 100f);

            SetText(calPercentText, pct + "%");
            SetText(calLuxText, "Lux: " + Mathf.Lerp(12.4f, 86.7f, n).ToString("0.0"));

            if (pct < 50)
            {
                SetText(calStepText, "1/2 Elini sensörün üstüne kapat");
                SetText(calStatusText, "Durum: Ölçülüyor");
            }
            else
            {
                SetText(calStepText, "2/2 Elini sensörden uzaklaştır");
                SetText(calStatusText, "Durum: Analiz ediliyor");
            }

            yield return null;
        }

        SetText(calStepText, "Kalibrasyon tamamlandı ✓");
        SetText(calPercentText, "100%");
        SetText(calStatusText, "Durum: Hazır");

        Debug.Log("[VibeBeat] ✅ Kalibrasyon tamamlandı.");
    }

    // ─────────────────────────────────────────
    // YARDIMCI METODLAR
    // ─────────────────────────────────────────
    private void BindButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        Button btn = child.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private void BindButtonDirect(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        Button btn = child.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private GameObject FindChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }
}