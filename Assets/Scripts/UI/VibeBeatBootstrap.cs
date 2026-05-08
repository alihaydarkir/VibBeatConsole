using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VibeBeat Bootstrap — Temizlenmiş Versiyon
///
/// ÖNCEKI SORUN: Bu script kendi AudioSource'larını yaratıyordu ve
/// AudioSynthesizer ile çakışıyordu (ikili ses mimarisi).
///
/// ÇÖZÜM: Bootstrap artık ses üretmez. Tüm ses işlemleri
/// VibBeatMasterController → AudioSynthesizer zincirinde kalır.
/// Bootstrap yalnızca şunlardan sorumludur:
///   1. UI bağlantıları (buton listener'ları)
///   2. Kalibrasyon UI animasyonu (coroutine)
///   3. Ekran yönetimi (VibeBeatScreenManager delegasyonu)
/// </summary>
public class VibeBeatBootstrap : MonoBehaviour
{
    // ─────────────────────────────────────────
    // BAĞIMLILIKLAR
    // ─────────────────────────────────────────
    [Header("Çekirdek Sistem Referansları")]
    [Tooltip("Sahnede VibBeatMasterController'ı barındıran GameObject")]
    [SerializeField] private VibBeatMasterController masterController;

    // ─────────────────────────────────────────
    // EKRANLAR (Awake'de otomatik bulunur)
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
    // EKRAN MANAGER (SESsiz ekran geçişleri için)
    // ─────────────────────────────────────────
    private VibeBeatScreenManager screenManager;

    // ─────────────────────────────────────────
    // AYARLAR STATE (Ses Bootstrap'ta DEĞİL MasterController'da)
    // ─────────────────────────────────────────
    private bool  hapticEnabled = true;
    private int   effectLevel   = 1;    // 0=Düşük 1=Orta 2=Yüksek
    private float masterVolume  = 0.7f;

    // ─────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────
    private void Awake()
    {
        screenManager = GetComponent<VibeBeatScreenManager>();

        // Ekranları isimle bul
        Transform t     = transform;
        splashScreen     = FindChild(t, "SplashScreen");
        onboardingScreen = FindChild(t, "OnboardingScreen");
        calibrationScreen= FindChild(t, "CalibrationScreen");
        mainConsoleScreen= FindChild(t, "MainConsoleScreen");
        settingsScreen   = FindChild(t, "SettingsScreen");

        // UI referansları bağlamak için hepsini geçici aç
        ActivateAllForBinding(true);
        BindAllUI();
        ActivateAllForBinding(false);

        // Başlangıç ekranı
        ShowSplash();

        Debug.Log("[BOOTSTRAP] ✅ Temiz başlatma tamamlandı. (Ses: MasterController'da)");
    }

    // ─────────────────────────────────────────
    // UI BAĞLANTILARI
    // ─────────────────────────────────────────
    private void BindAllUI()
    {
        BindSplash();
        BindOnboarding();
        BindCalibration();
        BindMainConsole();
        BindSettings();
    }

    private void BindSplash()
    {
        if (splashScreen == null) return;
        BindButton(splashScreen.transform, "StartButton", ShowOnboarding);
    }

    private void BindOnboarding()
    {
        if (onboardingScreen == null) return;
        BindButton(onboardingScreen.transform, "ContinueButton", ShowCalibration);
    }

    private void BindCalibration()
    {
        if (calibrationScreen == null) return;
        Transform card = calibrationScreen.transform.Find("CalibrationCard");
        if (card == null) return;

        calStepText    = card.Find("StepText")?.GetComponent<TextMeshProUGUI>();
        calPercentText = card.Find("PercentText")?.GetComponent<TextMeshProUGUI>();

        Transform bar = card.Find("InfoBar");
        if (bar != null)
        {
            calLuxText    = bar.Find("LuxText")?.GetComponent<TextMeshProUGUI>();
            calStatusText = bar.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        }

        BindButtonDirect(card, "RetryButton",    () => StartCoroutine(CalibrationUIRoutine()));
        BindButtonDirect(card, "ContinueButton", ShowMainConsole);
    }

    private void BindMainConsole()
    {
        if (mainConsoleScreen == null) return;

        Transform topBar = mainConsoleScreen.transform.Find("TopBar");
        topBar?.GetComponent<Button>(); // null check
        BindButtonDirect(topBar, "SettingsButton", ShowSettings);

        Transform guitar = mainConsoleScreen.transform.Find("GuitarPanel");
        if (guitar != null)
        {
            sensorValueText = guitar.Find("SensorValueText")?.GetComponent<TextMeshProUGUI>();
            BindButtonDirect(guitar, "MuteButton",     ToggleGuitarMute);
            BindButtonDirect(guitar, "CalibrateButton",ShowCalibration);
        }

        Transform right = mainConsoleScreen.transform.Find("RightPanel");
        if (right != null)
        {
            // Piano tuşları — ses VibBeatMasterController'a iletilir
            Transform piano = right.Find("PianoPanel");
            if (piano != null)
            {
                string[] notes = { "C4", "D4", "E4", "F4" };
                for (int i = 0; i < notes.Length; i++)
                {
                    int idx = i;
                    Button key = piano.Find("PianoKey_" + notes[i])?.GetComponent<Button>();
                    if (key != null)
                        key.onClick.AddListener(() => masterController?.HandlePianoKeyFromUI(idx));
                }
            }

            // Davul pad
            Transform drum = right.Find("DrumPanel");
            if (drum != null)
                BindButtonDirect(drum, "DrumPad", () => masterController?.HandleDrumHitFromUI());
        }
    }

    private void BindSettings()
    {
        if (settingsScreen == null) return;

        BindButton(settingsScreen.transform, "BackToMainButton", ShowMainConsole);

        Transform sp = settingsScreen.transform.Find("SettingsPanel");
        if (sp == null) return;

        BindButtonDirect(sp, "RecalibrateRow", ShowCalibration);

        // Haptic toggle
        Button hapticBtn = sp.Find("HapticRow")?.GetComponent<Button>();
        if (hapticBtn != null)
        {
            TextMeshProUGUI hapticStatus = sp.Find("HapticRow/HapticStatusText")?.GetComponent<TextMeshProUGUI>();
            Image toggleTrack = sp.Find("HapticRow/ToggleTrack")?.GetComponent<Image>();

            hapticBtn.onClick.RemoveAllListeners();
            hapticBtn.onClick.AddListener(() =>
            {
                hapticEnabled = !hapticEnabled;
                masterController?.SetHapticEnabled(hapticEnabled);

                if (hapticStatus != null)
                {
                    hapticStatus.text  = hapticEnabled ? "AÇIK" : "KAPALI";
                    hapticStatus.color = hapticEnabled ? ParseHex("#00F0FF") : ParseHex("#8899AA");
                }
                if (toggleTrack != null)
                    toggleTrack.color = hapticEnabled ? ParseHex("#00F0FF") : ParseHex("#1A2535");

                AccessibilityManager.Instance?.Speak(
                    $"Titreşim {(hapticEnabled ? "açıldı" : "kapatıldı")}");
            });
        }

        // Efekt seviyeleri
        Transform effectRow = sp.Find("EffectIntensityRow");
        if (effectRow != null)
        {
            string[] effNames = { "EffectBtn_Low", "EffectBtn_Mid", "EffectBtn_High" };
            for (int i = 0; i < effNames.Length; i++)
            {
                int level = i;
                Button btn = effectRow.Find(effNames[i])?.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        effectLevel = level;
                        UpdateEffectButtonVisuals(effectRow, effNames);
                    });
                }
            }
            UpdateEffectButtonVisuals(effectRow, new[] { "EffectBtn_Low","EffectBtn_Mid","EffectBtn_High" });
        }

        // Ses seviyesi slider
        Transform volumeRow = sp.Find("VolumeRow");
        if (volumeRow != null)
        {
            Slider volSlider = volumeRow.Find("VolumeSlider")?.GetComponent<Slider>();
            TextMeshProUGUI volText = volumeRow.Find("VolumeValueText")?.GetComponent<TextMeshProUGUI>();

            if (volSlider != null)
            {
                volSlider.value = masterVolume;
                volSlider.onValueChanged.RemoveAllListeners();
                volSlider.onValueChanged.AddListener(val =>
                {
                    masterVolume = val;
                    masterController?.SetMasterVolume(val);   // MasterController üzerinden
                    if (volText != null)
                        volText.text = Mathf.RoundToInt(val * 100f) + "%";
                });
            }
        }
    }

    // ─────────────────────────────────────────
    // EKRAN GEÇİŞLERİ
    // ─────────────────────────────────────────
    public void ShowSplash()        => screenManager?.ShowSplash();
    public void ShowOnboarding()    => screenManager?.ShowOnboarding();
    public void ShowMainConsole()   => screenManager?.ShowMainConsole();
    public void ShowSettings()      => screenManager?.ShowSettings();

    public void ShowCalibration()
    {
        screenManager?.ShowCalibration();
        StartCoroutine(CalibrationUIRoutine());
    }

    // ─────────────────────────────────────────
    // KALİBRASYON UI ANİMASYONU
    // Gerçek kalibrasyon mantığı CalibrationManager'da;
    // bu coroutine yalnızca UI'ı günceller.
    // ─────────────────────────────────────────
    private IEnumerator CalibrationUIRoutine()
    {
        Image progressRing = calibrationScreen?.transform
            .Find("CalibrationCard/RingContainer/ProgressRing")
            ?.GetComponent<Image>();

        // ADIM 1
        SetText(calStepText,   "1/2  Sol elinizi sensörün üstüne kapatın");
        SetText(calPercentText,"0%");
        SetText(calLuxText,    "Lux: ölçülüyor...");
        SetText(calStatusText, "Durum: Bekliyor");
        if (progressRing) progressRing.fillAmount = 0f;

        AccessibilityManager.Instance?.AnnounceCalibrationStep(
            "Adım 1: Sol elinizi telefon ışık sensörünün üzerine kapatın ve bekleyin.");

        float duration = 2.5f, timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n   = Mathf.Clamp01(timer / duration);
            int   pct = Mathf.RoundToInt(n * 100f);

            SetText(calPercentText, pct + "%");
            SetText(calLuxText,     "Lux: " + Mathf.Lerp(0f, 12f, n).ToString("0.0"));
            if (progressRing) progressRing.fillAmount = n * 0.5f;  // 0 → %50 (adım 1)

            if (n >= 0.5f && n < 0.51f)  // Yarı nokta geçişi
            {
                SetText(calStepText, "2/2  Elinizi sensörden uzaklaştırın");
                AccessibilityManager.Instance?.AnnounceCalibrationStep(
                    "Adım 2: Elinizi sensörden yavaşça uzaklaştırın.");
            }

            SetText(calStatusText, n < 0.5f ? "Durum: Min ölçülüyor" : "Durum: Max ölçülüyor");
            yield return null;
        }

        // ADIM 2 — max lux
        for (float t2 = 0f; t2 < duration; t2 += Time.deltaTime)
        {
            float n   = Mathf.Clamp01(t2 / duration);
            int   pct = 50 + Mathf.RoundToInt(n * 50f);

            SetText(calPercentText, pct + "%");
            SetText(calLuxText,     "Lux: " + Mathf.Lerp(12f, 3500f, n).ToString("0.0"));
            if (progressRing) progressRing.fillAmount = 0.5f + n * 0.5f;  // %50 → %100
            yield return null;
        }

        SetText(calStepText,   "Kalibrasyon tamamlandı ✅");
        SetText(calPercentText,"100%");
        SetText(calStatusText, "Durum: Hazır");
        if (progressRing) progressRing.fillAmount = 1f;

        // MasterController'a gerçek kalibrasyonu da başlat
        masterController?.StartCalibration();

        AccessibilityManager.Instance?.AnnounceCalibrationComplete();
        Debug.Log("[BOOTSTRAP] ✅ Kalibrasyon UI tamamlandı.");
    }

    // ─────────────────────────────────────────
    // GITAR MUTE (UI → MasterController)
    // ─────────────────────────────────────────
    private bool guitarMuted = false;

    private void ToggleGuitarMute()
    {
        guitarMuted = !guitarMuted;
        masterController?.SetGuitarMuteFromUI(guitarMuted);
        AccessibilityManager.Instance?.Speak(guitarMuted ? "Gitar susturuldu" : "Gitar açıldı");
    }

    // ─────────────────────────────────────────
    // UPDATE — Sensör değerini UI'a yansıt
    // ─────────────────────────────────────────
    private void Update()
    {
        if (sensorValueText != null && masterController != null)
        {
            float norm = masterController.GetNormalizedSensorValue();
            sensorValueText.text = norm.ToString("0.00");
        }
    }

    // ─────────────────────────────────────────
    // YARDIMCILAR
    // ─────────────────────────────────────────
    private void ActivateAllForBinding(bool active)
    {
        SetActive(splashScreen,      active);
        SetActive(onboardingScreen,  active);
        SetActive(calibrationScreen, active);
        SetActive(mainConsoleScreen, active);
        SetActive(settingsScreen,    active);
    }

    private void BindButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Button btn = parent?.Find(childName)?.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private void BindButtonDirect(Transform parent, string childName, UnityEngine.Events.UnityAction action)
        => BindButton(parent, childName, action);

    private void UpdateEffectButtonVisuals(Transform effectRow, string[] names)
    {
        Color active   = ParseHex("#00F0FF");
        Color inactive = ParseHex("#1A2535");

        for (int i = 0; i < names.Length; i++)
        {
            Transform btnT = effectRow.Find(names[i]);
            if (btnT == null) continue;

            Image img = btnT.GetComponent<Image>();
            if (img != null)
                img.color = i == effectLevel
                    ? new Color(active.r, active.g, active.b, 0.18f) : inactive;

            TextMeshProUGUI lbl = btnT.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (lbl != null)
                lbl.color = i == effectLevel ? active : ParseHex("#8899AA");
        }
    }

    private GameObject FindChild(Transform parent, string name)
        => parent.Find(name)?.gameObject;

    private void SetActive(GameObject go, bool active)
    { if (go != null) go.SetActive(active); }

    private void SetText(TextMeshProUGUI tmp, string value)
    { if (tmp != null) tmp.text = value; }

    private static Color ParseHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
