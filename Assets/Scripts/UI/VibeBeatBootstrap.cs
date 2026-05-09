using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VibeBeat Bootstrap — Temizlenmiş Versiyon
/// Yalnızca UI bağlantıları, kalibrasyon animasyonu ve ekran yönetiminden sorumludur.
/// Ses üretimi → VibBeatMasterController → AudioSynthesizer.
/// </summary>
public class VibeBeatBootstrap : MonoBehaviour
{
    // ─────────────────────────────────────────
    // BAĞIMLILIKLAR
    // ─────────────────────────────────────────
    [Header("Çekirdek Sistem Referansları")]
    [SerializeField] private VibBeatMasterController masterController;

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

    private VibeBeatScreenManager screenManager;
    private bool  hapticEnabled = true;
    private int   effectLevel   = 1;
    private float masterVolume  = 0.7f;
    private bool  guitarMuted   = false;

    // ─────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────
    private void Awake()
    {
        screenManager = GetComponent<VibeBeatScreenManager>();

        // MasterController sahnede yoksa otomatik bul
        if (masterController == null)
            masterController = FindFirstObjectByType<VibBeatMasterController>();

        if (masterController == null)
            Debug.LogError("[BOOTSTRAP] [HATA] VibBeatMasterController sahnede bulunamadı! Inspector'dan ata.");
        else
            Debug.Log("[BOOTSTRAP] [OK] MasterController bulundu.");

        Transform t      = transform;
        splashScreen     = FindChild(t, "SplashScreen");
        onboardingScreen = FindChild(t, "OnboardingScreen");
        calibrationScreen= FindChild(t, "CalibrationScreen");
        mainConsoleScreen= FindChild(t, "MainConsoleScreen");
        settingsScreen   = FindChild(t, "SettingsScreen");

        // ScreenManager'a ekran referanslarını aktar
        screenManager?.Init(splashScreen, onboardingScreen, calibrationScreen,
                            mainConsoleScreen, settingsScreen);

        LogScreenStatus();

        // Bağlama için hepsini geçici aç
        ActivateAll(true);
        BindAllUI();
        ActivateAll(false);

        ShowSplash();
        Debug.Log("[BOOTSTRAP] [OK] Başlatma tamamlandı.");
    }

    private void LogScreenStatus()
    {
        Debug.Log($"[BOOTSTRAP] Ekranlar — " +
            $"Splash:{splashScreen != null} " +
            $"Onboard:{onboardingScreen != null} " +
            $"Calib:{calibrationScreen != null} " +
            $"Main:{mainConsoleScreen != null} " +
            $"Settings:{settingsScreen != null}");
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
        if (splashScreen == null) { Debug.LogWarning("[BOOTSTRAP] SplashScreen null!"); return; }
        BindBtn(splashScreen.transform, "StartButton", ShowOnboarding);
    }

    private void BindOnboarding()
    {
        if (onboardingScreen == null) { Debug.LogWarning("[BOOTSTRAP] OnboardingScreen null!"); return; }
        BindBtn(onboardingScreen.transform, "ContinueButton", ShowCalibration);
    }

    private void BindCalibration()
    {
        if (calibrationScreen == null) { Debug.LogWarning("[BOOTSTRAP] CalibrationScreen null!"); return; }

        Transform card = calibrationScreen.transform.Find("CalibrationCard");
        if (card == null) { Debug.LogWarning("[BOOTSTRAP] CalibrationCard bulunamadı!"); return; }

        calStepText    = card.Find("StepText")?.GetComponent<TextMeshProUGUI>();
        calPercentText = card.Find("PercentText")?.GetComponent<TextMeshProUGUI>();
        Transform bar  = card.Find("InfoBar");
        calLuxText     = bar?.Find("LuxText")?.GetComponent<TextMeshProUGUI>();
        calStatusText  = bar?.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        BindBtn(card, "RetryButton",    () => StartCoroutine(CalibrationUIRoutine()));
        BindBtn(card, "ContinueButton", ShowMainConsole);
    }

    private void BindMainConsole()
    {
        if (mainConsoleScreen == null) { Debug.LogWarning("[BOOTSTRAP] MainConsoleScreen null!"); return; }

        // TopBar
        Transform topBar = mainConsoleScreen.transform.Find("TopBar");
        if (topBar != null)
            BindBtn(topBar, "SettingsButton", ShowSettings);
        else
            Debug.LogWarning("[BOOTSTRAP] TopBar bulunamadı!");

        // Guitar Panel
        Transform guitar = mainConsoleScreen.transform.Find("GuitarPanel");
        if (guitar != null)
        {
            sensorValueText = guitar.Find("SensorValueText")?.GetComponent<TextMeshProUGUI>();
            BindBtn(guitar, "MuteButton",     ToggleGuitarMute);
            BindBtn(guitar, "CalibrateButton",ShowCalibration);
            Debug.Log("[BOOTSTRAP] [OK] GuitarPanel bağlandı.");
        }
        else Debug.LogWarning("[BOOTSTRAP] GuitarPanel bulunamadı!");

        // Right Panel → Piano + Drum
        Transform right = mainConsoleScreen.transform.Find("RightPanel");
        if (right == null) { Debug.LogWarning("[BOOTSTRAP] RightPanel bulunamadı!"); return; }

        // Piano
        Transform piano = right.Find("PianoPanel");
        if (piano != null)
        {
            string[] notes = { "C4", "D4", "E4", "F4" };
            int bound = 0;
            for (int i = 0; i < notes.Length; i++)
            {
                int     idx  = i;
                string  name = "PianoKey_" + notes[i];
                Button  key  = piano.Find(name)?.GetComponent<Button>();
                if (key != null)
                {
                    key.onClick.RemoveAllListeners();
                    key.onClick.AddListener(() =>
                    {
                        Debug.Log($"[BOOTSTRAP] [PIANO] Piyano tuş basıldı: {idx}");
                        if (masterController != null)
                            masterController.HandlePianoKeyFromUI(idx);
                        else
                            Debug.LogError("[BOOTSTRAP] masterController NULL — piyano çalınamadı!");

                        // WaterDrop: TouchSpawnHandler hallediyor (dokunma noktasindan)
                    });
                    bound++;
                }
                else Debug.LogWarning($"[BOOTSTRAP] {name} butonu bulunamadı!");
            }
            Debug.Log($"[BOOTSTRAP] [OK] Piano: {bound}/4 tuş bağlandı.");
        }
        else Debug.LogWarning("[BOOTSTRAP] PianoPanel bulunamadı!");

        // Drum
        Transform drum = right.Find("DrumPanel");
        if (drum != null)
        {
            Button padBtn = drum.Find("DrumPad")?.GetComponent<Button>();
            if (padBtn != null)
            {
                padBtn.onClick.RemoveAllListeners();
                padBtn.onClick.AddListener(() =>
                {
                    Debug.Log("[BOOTSTRAP] [DAVUL] Davul basıldı!");
                    if (masterController != null)
                        masterController.HandleDrumHitFromUI();
                    else
                        Debug.LogError("[BOOTSTRAP] masterController NULL — davul çalınamadı!");

                    // WaterDrop: TouchSpawnHandler hallediyor (dokunma noktasindan)
                });
                Debug.Log("[BOOTSTRAP] [OK] DrumPad bağlandı.");
            }
            else Debug.LogWarning("[BOOTSTRAP] DrumPad butonu bulunamadı!");
        }
        else Debug.LogWarning("[BOOTSTRAP] DrumPanel bulunamadı!");
    }

    private void BindSettings()
    {
        if (settingsScreen == null) { Debug.LogWarning("[BOOTSTRAP] SettingsScreen null!"); return; }

        // BackToMainButton doğrudan settingsScreen altında
        BindBtn(settingsScreen.transform, "BackToMainButton", ShowMainConsole);

        Transform sp = settingsScreen.transform.Find("SettingsPanel");
        if (sp == null) { Debug.LogWarning("[BOOTSTRAP] SettingsPanel bulunamadı!"); return; }

        Debug.Log("[BOOTSTRAP] [OK] SettingsPanel bulundu, satırlar bağlanıyor...");

        // Tekrar kalibre
        BindBtn(sp, "RecalibrateRow", ShowCalibration);

        // Haptic toggle
        Button hapticBtn = sp.Find("HapticRow")?.GetComponent<Button>();
        if (hapticBtn != null)
        {
            var hapticStatus = sp.Find("HapticRow/HapticStatusText")?.GetComponent<TextMeshProUGUI>();
            var toggleTrack  = sp.Find("HapticRow/ToggleTrack")?.GetComponent<Image>();
            hapticBtn.onClick.RemoveAllListeners();
            hapticBtn.onClick.AddListener(() =>
            {
                hapticEnabled = !hapticEnabled;
                Debug.Log($"[BOOTSTRAP] Haptic: {hapticEnabled}");
                masterController?.SetHapticEnabled(hapticEnabled);
                if (hapticStatus != null)
                {
                    hapticStatus.text  = hapticEnabled ? "AÇIK" : "KAPALI";
                    hapticStatus.color = Hex(hapticEnabled ? "#00F0FF" : "#8899AA");
                }
                if (toggleTrack != null)
                    toggleTrack.color = Hex(hapticEnabled ? "#00F0FF" : "#1A2535");
                AccessibilityManager.Instance?.Speak(hapticEnabled ? "Titreşim açıldı" : "Titreşim kapatıldı");
            });
            Debug.Log("[BOOTSTRAP] [OK] HapticRow bağlandı.");
        }
        else Debug.LogWarning("[BOOTSTRAP] HapticRow butonu bulunamadı!");

        // Efekt seviyeleri
        Transform effectRow = sp.Find("EffectIntensityRow");
        if (effectRow != null)
        {
            string[] effNames = { "EffectBtn_Low", "EffectBtn_Mid", "EffectBtn_High" };
            for (int i = 0; i < effNames.Length; i++)
            {
                int   level = i;
                Button btn  = effectRow.Find(effNames[i])?.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        effectLevel = level;
                        Debug.Log($"[BOOTSTRAP] Efekt seviyesi: {level}");
                        UpdateEffectVisuals(effectRow, effNames);
                        // EffectIntensityController'a ilet
                        EffectIntensityController.Instance?.SetLevel(level);
                    });
                }
                else Debug.LogWarning($"[BOOTSTRAP] {effNames[i]} bulunamadı!");
            }
            UpdateEffectVisuals(effectRow, new[]{"EffectBtn_Low","EffectBtn_Mid","EffectBtn_High"});
            Debug.Log("[BOOTSTRAP] [OK] EffectIntensityRow bağlandı.");
        }
        else Debug.LogWarning("[BOOTSTRAP] EffectIntensityRow bulunamadı!");

        // Ses slider
        Transform volumeRow = sp.Find("VolumeRow");
        if (volumeRow != null)
        {
            Slider          sl      = volumeRow.Find("VolumeSlider")?.GetComponent<Slider>();
            TextMeshProUGUI volTxt  = volumeRow.Find("VolumeValueText")?.GetComponent<TextMeshProUGUI>();
            if (sl != null)
            {
                sl.value = masterVolume;
                sl.onValueChanged.RemoveAllListeners();
                sl.onValueChanged.AddListener(val =>
                {
                    masterVolume = val;
                    masterController?.SetMasterVolume(val);
                    if (volTxt != null) volTxt.text = Mathf.RoundToInt(val * 100f) + "%";
                    Debug.Log($"[BOOTSTRAP] Ses: {Mathf.RoundToInt(val*100f)}%");
                });
                Debug.Log("[BOOTSTRAP] [OK] VolumeSlider bağlandı.");
            }
            else Debug.LogWarning("[BOOTSTRAP] VolumeSlider bulunamadı!");
        }
        else Debug.LogWarning("[BOOTSTRAP] VolumeRow bulunamadı!");
    }

    // ─────────────────────────────────────────
    // EKRAN GEÇİŞLERİ
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        Debug.Log("[BOOTSTRAP] → SplashScreen");
        screenManager?.ShowSplash();
    }

    public void ShowOnboarding()
    {
        Debug.Log("[BOOTSTRAP] → OnboardingScreen");
        screenManager?.ShowOnboarding();
    }

    public void ShowMainConsole()
    {
        Debug.Log("[BOOTSTRAP] → MainConsoleScreen");
        screenManager?.ShowMainConsole();
    }

    public void ShowSettings()
    {
        Debug.Log("[BOOTSTRAP] → SettingsScreen");
        screenManager?.ShowSettings();
    }

    public void ShowCalibration()
    {
        Debug.Log("[BOOTSTRAP] → CalibrationScreen");
        screenManager?.ShowCalibration();
        StartCoroutine(CalibrationUIRoutine());
    }

    // ─────────────────────────────────────────
    // KALİBRASYON UI ANİMASYONU
    // ─────────────────────────────────────────
    private IEnumerator CalibrationUIRoutine()
    {
        Image progressRing = calibrationScreen?.transform
            .Find("CalibrationCard/RingContainer/ProgressRing")?.GetComponent<Image>();

        SetText(calStepText,   "1/2  Sol elinizi sensörün üstüne kapatın");
        SetText(calPercentText,"0%");
        SetText(calLuxText,    "Lux: ölçülüyor...");
        SetText(calStatusText, "Durum: Bekliyor");
        if (progressRing) progressRing.fillAmount = 0f;

        AccessibilityManager.Instance?.AnnounceCalibrationStep(
            "Adım 1: Sol elinizi ışık sensörünün üzerine kapatın.");

        float duration = 2.5f, timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n   = Mathf.Clamp01(timer / duration);
            int   pct = Mathf.RoundToInt(n * 50f);
            SetText(calPercentText, pct + "%");
            SetText(calLuxText,     "Lux: " + Mathf.Lerp(0f, 12f, n).ToString("0.0"));
            SetText(calStatusText,  "Durum: Min ölçülüyor");
            if (progressRing) progressRing.fillAmount = n * 0.5f;
            yield return null;
        }

        SetText(calStepText, "2/2  Elinizi sensörden uzaklaştırın");
        AccessibilityManager.Instance?.AnnounceCalibrationStep(
            "Adım 2: Elinizi yavaşça uzaklaştırın.");

        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n   = Mathf.Clamp01(timer / duration);
            int   pct = 50 + Mathf.RoundToInt(n * 50f);
            SetText(calPercentText, pct + "%");
            SetText(calLuxText,     "Lux: " + Mathf.Lerp(12f, 3500f, n).ToString("0.0"));
            SetText(calStatusText,  "Durum: Max ölçülüyor");
            if (progressRing) progressRing.fillAmount = 0.5f + n * 0.5f;
            yield return null;
        }

        SetText(calStepText,   "Kalibrasyon tamamlandı [OK]");
        SetText(calPercentText,"100%");
        SetText(calStatusText, "Durum: Hazır");
        if (progressRing) progressRing.fillAmount = 1f;

        masterController?.StartCalibration();
        AccessibilityManager.Instance?.AnnounceCalibrationComplete();
        Debug.Log("[BOOTSTRAP] [OK] Kalibrasyon UI tamamlandı.");
    }

    // ─────────────────────────────────────────
    // GITAR MUTE
    // ─────────────────────────────────────────
    private void ToggleGuitarMute()
    {
        guitarMuted = !guitarMuted;
        Debug.Log($"[BOOTSTRAP] [GITAR] Guitar mute: {guitarMuted}");
        masterController?.SetGuitarMuteFromUI(guitarMuted);
        AccessibilityManager.Instance?.Speak(guitarMuted ? "Gitar susturuldu" : "Gitar acildi");
    }

    // ─────────────────────────────────────────
    // UPDATE — Sensör değerini UI'a yansıt
    // ─────────────────────────────────────────
    private void Update()
    {
        if (sensorValueText != null && masterController != null)
            sensorValueText.text = masterController.GetNormalizedSensorValue().ToString("0.00");
    }

    // ─────────────────────────────────────────
    // YARDIMCILAR
    // ─────────────────────────────────────────
    private void ActivateAll(bool active)
    {
        SetActive(splashScreen,      active);
        SetActive(onboardingScreen,  active);
        SetActive(calibrationScreen, active);
        SetActive(mainConsoleScreen, active);
        SetActive(settingsScreen,    active);
    }

    /// <summary>
    /// Transform hiyerarşisinde verilen isimli çocuğu bulur ve Button'unu bağlar.
    /// Bulunamazsa uyarı loglar — sessiz başarısızlık yok.
    /// </summary>
    private void BindBtn(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        if (parent == null) { Debug.LogWarning($"[BOOTSTRAP] BindBtn: parent null, aranıyor: {childName}"); return; }
        Transform childT = parent.Find(childName);
        if (childT == null) { Debug.LogWarning($"[BOOTSTRAP] '{childName}' bulunamadı (parent: {parent.name})"); return; }
        Button btn = childT.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning($"[BOOTSTRAP] '{childName}' üzerinde Button component yok!"); return; }
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);

        // Her butona otomatik ripple — butonun kendi pozisyonundan yayilir
        Transform finalChildT = childT;
        btn.onClick.AddListener(() =>
        {
            Color rippleColor = RippleEffect.ColorGuitar; // varsayilan: cyan

            // Butona gore renk sec
            if (childName.Contains("Start") || childName.Contains("Continue"))
                rippleColor = RippleEffect.ColorGuitar;
            else if (childName.Contains("Settings") || childName.Contains("Back"))
                rippleColor = new Color(0.7f, 0.4f, 1f, 1f);
            else if (childName.Contains("Calibr") || childName.Contains("Retry") || childName.Contains("Recal"))
                rippleColor = RippleEffect.ColorGuitar;
            else if (childName.Contains("Mute"))
                rippleColor = RippleEffect.ColorGuitar;

            RippleEffect.Instance?.SpawnFromScreenPos(
                RectTransformUtility.WorldToScreenPoint(Camera.main,
                    finalChildT.position),
                rippleColor, interrupt: false
            );
        });

        Debug.Log($"[BOOTSTRAP] [OK] '{childName}' butonu + ripple bağlandı.");
    }

    private void UpdateEffectVisuals(Transform effectRow, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform btnT = effectRow.Find(names[i]);
            if (btnT == null) continue;
            Image img = btnT.GetComponent<Image>();
            Color ac  = Hex("#00F0FF");
            if (img != null)
                img.color = i == effectLevel ? new Color(ac.r,ac.g,ac.b,0.18f) : Hex("#1A2535");
            TextMeshProUGUI lbl = btnT.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (lbl != null)
                lbl.color = i == effectLevel ? ac : Hex("#8899AA");
        }
    }

    private GameObject FindChild(Transform parent, string name)
        => parent.Find(name)?.gameObject;

    private void SetActive(GameObject go, bool active)
    { if (go != null) go.SetActive(active); }

    private void SetText(TextMeshProUGUI tmp, string value)
    { if (tmp != null) tmp.text = value; }

    private static Color Hex(string h)
    { ColorUtility.TryParseHtmlString(h, out Color c); return c; }
}
