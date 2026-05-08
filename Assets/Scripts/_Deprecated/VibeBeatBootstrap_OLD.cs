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

        // Haptic toggle — tüm satır butona tıklayınca toggle olur
        Button hapticBtn = sp.Find("HapticRow")?.GetComponent<Button>();
        if (hapticBtn != null)
        {
            TextMeshProUGUI hapticStatus =
                sp.Find("HapticRow/HapticStatusText")?.GetComponent<TextMeshProUGUI>();
            Image toggleTrack =
                sp.Find("HapticRow/ToggleTrack")?.GetComponent<Image>();

            hapticBtn.onClick.RemoveAllListeners();
            hapticBtn.onClick.AddListener(() =>
            {
                hapticEnabled = !hapticEnabled;
                if (hapticStatus != null)
                {
                    hapticStatus.text  = hapticEnabled ? "AÇIK" : "KAPALI";
                    hapticStatus.color = hapticEnabled ? ParseHex("#00F0FF") : ParseHex("#8899AA");
                }
                if (toggleTrack != null)
                    toggleTrack.color = hapticEnabled ? ParseHex("#00F0FF") : ParseHex("#1A2535");
                Debug.Log($"[VibeBeat] Haptic: {hapticEnabled}");
            });
        }

        // Efekt yoğunluğu — 3 ayrı buton (EffectBtn_Low / Mid / High)
        Transform effectRow = sp.Find("EffectIntensityRow");
        if (effectRow != null)
        {
            string[] effNames = { "EffectBtn_Low", "EffectBtn_Mid", "EffectBtn_High" };
            for (int i = 0; i < effNames.Length; i++)
            {
                int level = i;
                Button effBtn = effectRow.Find(effNames[i])?.GetComponent<Button>();
                if (effBtn != null)
                {
                    effBtn.onClick.RemoveAllListeners();
                    effBtn.onClick.AddListener(() =>
                    {
                        effectLevel = level;
                        UpdateEffectButtonVisuals(effectRow, effNames);
                        Debug.Log($"[VibeBeat] Efekt seviyesi: {level}");
                    });
                }
            }
            UpdateEffectButtonVisuals(effectRow,
                new string[] { "EffectBtn_Low", "EffectBtn_Mid", "EffectBtn_High" });
        }

        // Ses seviyesi — Slider bileşeni
        Transform volumeRow = sp.Find("VolumeRow");
        if (volumeRow != null)
        {
            Slider volSlider =
                volumeRow.Find("VolumeSlider")?.GetComponent<Slider>();
            TextMeshProUGUI volText =
                volumeRow.Find("VolumeValueText")?.GetComponent<TextMeshProUGUI>();

            if (volSlider != null)
            {
                volSlider.value = masterVolume;
                volSlider.onValueChanged.RemoveAllListeners();
                volSlider.onValueChanged.AddListener(val =>
                {
                    masterVolume = val;
                    if (guitarAudioSource != null) guitarAudioSource.volume = val;
                    if (pianoAudioSource  != null) pianoAudioSource.volume  = val;
                    if (drumAudioSource   != null) drumAudioSource.volume   = val;
                    if (volText           != null)
                        volText.text = Mathf.RoundToInt(val * 100f) + "%";
                });
            }
        }
    }

    private void UpdateEffectButtonVisuals(Transform effectRow, string[] names)
    {
        Color activeColor   = ParseHex("#00F0FF");
        Color inactiveColor = ParseHex("#1A2535");
        for (int i = 0; i < names.Length; i++)
        {
            Transform btnT = effectRow.Find(names[i]);
            if (btnT == null) continue;

            Image img = btnT.GetComponent<Image>();
            if (img != null)
                img.color = i == effectLevel
                    ? new Color(activeColor.r, activeColor.g, activeColor.b, 0.18f)
                    : inactiveColor;

            TextMeshProUGUI lbl = btnT.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (lbl != null)
                lbl.color = i == effectLevel ? activeColor : ParseHex("#8899AA");
        }
    }

    private static Color ParseHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
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
        // ProgressRing'i hiyerarşide isim ile bul
        Image progressRing = calibrationScreen?.transform
            .Find("CalibrationCard/RingContainer/ProgressRing")
            ?.GetComponent<Image>();

        SetText(calStepText,   "1/2  Elini sensörün üstüne kapat");
        SetText(calPercentText,"0%");
        SetText(calLuxText,    "Lux: --");
        SetText(calStatusText, "Durum: Olculuyor");
        if (progressRing) progressRing.fillAmount = 0f;

        float duration = 2.5f;
        float timer    = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n   = Mathf.Clamp01(timer / duration);
            int   pct = Mathf.RoundToInt(n * 100f);

            SetText(calPercentText, pct + "%");
            SetText(calLuxText,     "Lux: " + Mathf.Lerp(12.4f, 86.7f, n).ToString("0.0"));
            if (progressRing) progressRing.fillAmount = n;

            if (pct < 50)
            {
                SetText(calStepText,  "1/2  Elini sensörün üstüne kapat");
                SetText(calStatusText,"Durum: Olculuyor");
            }
            else
            {
                SetText(calStepText,  "2/2  Elini sensörden uzaklaştır");
                SetText(calStatusText,"Durum: Analiz ediliyor");
            }
            yield return null;
        }

        SetText(calStepText,   "Kalibrasyon tamamlandi");
        SetText(calPercentText,"100%");
        SetText(calStatusText, "Durum: Hazir");
        if (progressRing) progressRing.fillAmount = 1f;
        Debug.Log("[VibeBeat] ✅ Kalibrasyon tamamlandı.");
    }

    // ─────────────────────────────────────────
    // YARDIMCI METODLAR
    // ─────────────────────────────────────────
    private void BindButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Button btn = parent.Find(childName)?.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private void BindButtonDirect(Transform parent, string childName, UnityEngine.Events.UnityAction action)
        => BindButton(parent, childName, action);

    private GameObject FindChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private void SetActive(GameObject go, bool active) { if (go != null) go.SetActive(active); }
    private void SetText(TextMeshProUGUI tmp, string value) { if (tmp != null) tmp.text = value; }
}