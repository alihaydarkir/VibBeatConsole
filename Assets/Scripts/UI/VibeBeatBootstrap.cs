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
    private GameObject orientationScreen;
    private GameObject splashScreen;
    private GameObject onboardingScreen;
    private GameObject calibrationScreen;
    private GameObject mainConsoleScreen;
    private GameObject settingsScreen;
    private GameObject soundStudioScreen;
    private GameObject recordStudioScreen;

    // ─────────────────────────────────────────
    // UI REFERANSLARI
    // ─────────────────────────────────────────
    private TextMeshProUGUI sensorValueText;
    private TextMeshProUGUI calStepText;
    private TextMeshProUGUI calPercentText;
    private TextMeshProUGUI calLuxText;
    private TextMeshProUGUI calStatusText;
    private TextMeshProUGUI metronomeBPMText;
    private GameObject      loopRecordGO;
    private GameObject      loopStopRecGO;
    private GameObject      loopPlayGO;
    private GameObject      loopStopPlayGO;

    private VibeBeatScreenManager screenManager;
    private bool  hapticEnabled = true;
    private int   effectLevel   = 1;
    private float masterVolume  = 0.7f;
    private bool  guitarMuted   = false;
    private Coroutine calibRoutine = null; // Retry çakışmasını önler

    // ─────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────
    private void Awake()
    {
        FixEventSystem();

        screenManager = GetComponent<VibeBeatScreenManager>();

        // MasterController sahnede yoksa otomatik bul
        if (masterController == null)
            masterController = FindFirstObjectByType<VibBeatMasterController>();

        if (masterController == null)
            Debug.LogError("[BOOTSTRAP] [HATA] VibBeatMasterController sahnede bulunamadı! Inspector'dan ata.");
        else
            Debug.Log("[BOOTSTRAP] [OK] MasterController bulundu.");

        Transform t        = transform;
        orientationScreen  = FindChild(t, "OrientationScreen");
        splashScreen       = FindChild(t, "SplashScreen");
        onboardingScreen   = FindChild(t, "OnboardingScreen");
        calibrationScreen  = FindChild(t, "CalibrationScreen");
        mainConsoleScreen  = FindChild(t, "MainConsoleScreen");
        settingsScreen     = FindChild(t, "SettingsScreen");
        soundStudioScreen  = FindChild(t, "SoundStudioScreen");
        recordStudioScreen = FindChild(t, "RecordStudioScreen");

        screenManager?.Init(orientationScreen, splashScreen, onboardingScreen, calibrationScreen,
                            mainConsoleScreen, settingsScreen, soundStudioScreen,
                            recordStudioScreen);

        LogScreenStatus();

        ActivateAll(true);
        BindAllUI();
        ActivateAll(false);

        ShowOrientation();
        Debug.Log("[BOOTSTRAP] [OK] Başlatma tamamlandı.");
    }

    private static void FixEventSystem()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return;

        // InputSystemUIInputModule varsa hemen kaldır
        var inputSysModule = es.GetComponent("InputSystemUIInputModule");
        if (inputSysModule != null)
        {
            DestroyImmediate(inputSysModule);
            Debug.Log("[BOOTSTRAP] InputSystemUIInputModule kaldırıldı.");
        }

        // Eski StandaloneInputModule varsa da kaldır — MobileInputModule ile değiştir
        var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (oldModule != null && oldModule.GetType() != typeof(MobileInputModule))
        {
            DestroyImmediate(oldModule);
            Debug.Log("[BOOTSTRAP] StandaloneInputModule kaldırıldı.");
        }

        // MobileInputModule: Android'de double-fire'ı önler
        if (es.GetComponent<MobileInputModule>() == null)
        {
            es.gameObject.AddComponent<MobileInputModule>();
            Debug.Log("[BOOTSTRAP] MobileInputModule eklendi (Android double-fire koruması).");
        }
    }

    private void LogScreenStatus()
    {
        Debug.Log($"[BOOTSTRAP] Ekranlar — " +
            $"Splash:{splashScreen != null} " +
            $"Onboard:{onboardingScreen != null} " +
            $"Calib:{calibrationScreen != null} " +
            $"Main:{mainConsoleScreen != null} " +
            $"Settings:{settingsScreen != null} " +
            $"Studio:{soundStudioScreen != null}");
    }

    // ─────────────────────────────────────────
    // UI BAĞLANTILARI
    // ─────────────────────────────────────────
    private void BindAllUI()
    {
        BindOrientation();
        BindSplash();
        BindOnboarding();
        BindCalibration();
        BindMainConsole();
        BindSettings();
        BindSoundStudio();
        BindRecordStudio();
    }

    private void BindOrientation()
    {
        // Buton yok — 2 saniye sonra otomatik geçiş
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

    private Button calContinueButton;

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

        Transform contT = card.Find("ContinueButton");
        if (contT != null) calContinueButton = contT.GetComponent<Button>();

        BindBtn(card, "RetryButton", () => {
            if (calibRoutine != null) StopCoroutine(calibRoutine);
            calibRoutine = StartCoroutine(CalibrationUIRoutine());
        });
        BindBtn(card, "ContinueButton", ShowMainConsole);
    }

    private void BindMainConsole()
    {
        if (mainConsoleScreen == null) { Debug.LogWarning("[BOOTSTRAP] MainConsoleScreen null!"); return; }

        // TopBar
        Transform topBar = mainConsoleScreen.transform.Find("TopBar");
        if (topBar != null)
        {
            BindBtn(topBar, "SettingsButton", ShowSettings);
            BindBtn(topBar, "StudioButton",  ShowSoundStudio);
            BindBtn(topBar, "RecordButton",  ShowRecordStudio);
        }
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
            // Her tuşun son basım zamanı — double-fire koruması
            float[] pianoLastPress = new float[notes.Length];
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
                        if (UnityEngine.Time.unscaledTime - pianoLastPress[idx] < 0.08f) return;
                        pianoLastPress[idx] = UnityEngine.Time.unscaledTime;
                        Debug.Log($"[BOOTSTRAP] [PIANO] Piyano tuş basıldı: {idx}");
                        if (masterController != null)
                            masterController.HandlePianoKeyFromUI(idx);
                        else
                            Debug.LogError("[BOOTSTRAP] masterController NULL — piyano çalınamadı!");
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
                float drumLastPress = 0f;
                padBtn.onClick.RemoveAllListeners();
                padBtn.onClick.AddListener(() =>
                {
                    if (UnityEngine.Time.unscaledTime - drumLastPress < 0.08f) return;
                    drumLastPress = UnityEngine.Time.unscaledTime;
                    Debug.Log("[BOOTSTRAP] [DAVUL] Davul basıldı!");
                    if (masterController != null)
                        masterController.HandleDrumHitFromUI();
                    else
                        Debug.LogError("[BOOTSTRAP] masterController NULL — davul çalınamadı!");
                });
                Debug.Log("[BOOTSTRAP] [OK] DrumPad bağlandı.");
            }
            else Debug.LogWarning("[BOOTSTRAP] DrumPad butonu bulunamadı!");
        }
        else Debug.LogWarning("[BOOTSTRAP] DrumPanel bulunamadı!");

        // Loop Recorder + BPM — DonguBar veya MetronomePanel altında aranır
        Transform loopParent = mainConsoleScreen.transform.Find("DonguBar")
                            ?? mainConsoleScreen.transform.Find("MetronomePanel");
        if (loopParent != null)
        {
            metronomeBPMText = loopParent.Find("BPMText")?.GetComponent<TextMeshProUGUI>();

            loopRecordGO   = loopParent.Find("LoopRecordButton")?.gameObject;
            loopStopRecGO  = loopParent.Find("LoopStopRecButton")?.gameObject;
            loopPlayGO     = loopParent.Find("LoopPlayButton")?.gameObject;
            loopStopPlayGO = loopParent.Find("LoopStopPlayButton")?.gameObject;

            BindBtn(loopParent, "LoopRecordButton",   () => { LoopToggle(); UpdateLoopVisuals(); });
            BindBtn(loopParent, "LoopStopRecButton",  () => { LoopToggle(); UpdateLoopVisuals(); });
            BindBtn(loopParent, "LoopPlayButton",     () => { LoopPlay();   UpdateLoopVisuals(); });
            BindBtn(loopParent, "LoopStopPlayButton", () => { LoopPlay();   UpdateLoopVisuals(); });
            BindBtn(loopParent, "DecreaseButton", MetronomeDecrease);
            BindBtn(loopParent, "IncreaseButton", MetronomeIncrease);

            UpdateLoopVisuals();
            Debug.Log("[BOOTSTRAP] [OK] Loop Recorder + BPM bağlandı.");
        }
        else Debug.LogWarning("[BOOTSTRAP] DonguBar veya MetronomePanel bulunamadı!");
    }

    private void UpdateLoopVisuals()
    {
        var  loop      = LoopRecorder.Instance;
        bool recording = loop != null && loop.CurrentState == LoopRecorder.State.Recording;
        bool playing   = loop != null && loop.CurrentState == LoopRecorder.State.Playing;
        bool hasLoop   = loop != null && loop.HasLoop;

        if (loopRecordGO  != null) loopRecordGO.SetActive(!recording);
        if (loopStopRecGO != null) loopStopRecGO.SetActive(recording);

        if (loopPlayGO     != null) loopPlayGO.SetActive(!playing);
        if (loopStopPlayGO != null) loopStopPlayGO.SetActive(playing);

        if (loopPlayGO != null)
        {
            var btn = loopPlayGO.GetComponent<Button>();
            if (btn != null) btn.interactable = hasLoop && !recording;
        }
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

        // Sesli okuma toggle — AccessibilityToggleRow bileşeni AccessibilityRow üzerinde
        // kendi Start()'ında bağlanır, burada ek işlem gerekmez.

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
    public void ShowOrientation()
    {
        if (screenManager != null) screenManager.ShowOnly(orientationScreen);
        StartCoroutine(AutoAdvanceOrientation());
    }

    private IEnumerator AutoAdvanceOrientation()
    {
        yield return new WaitForSeconds(2f);
        ShowSplash();
    }

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
        masterController?.StartGuitarLoop();
    }

    public void ShowSettings()
    {
        Debug.Log("[BOOTSTRAP] → SettingsScreen");
        screenManager?.ShowSettings();
    }

    public void ShowSoundStudio()
    {
        Debug.Log("[BOOTSTRAP] → SoundStudioScreen");
        screenManager?.ShowSoundStudio();
    }

    public void ShowRecordStudio()
    {
        Debug.Log("[BOOTSTRAP] → RecordStudioScreen");
        masterController?.StopGuitarLoop(); // kayıt ekranında gitar çalmasın
        screenManager?.ShowRecordStudio();
    }

    private void BindRecordStudio()
    {
        if (recordStudioScreen == null)
        {
            Debug.LogWarning("[BOOTSTRAP] RecordStudioScreen null!");
            return;
        }

        Transform topBar = recordStudioScreen.transform.Find("TopBar");
        if (topBar != null)
            BindBtn(topBar, "BackButton", ShowMainConsole);

        Debug.Log("[BOOTSTRAP] [OK] RecordStudio TopBar bağlandı.");
    }

    private void BindSoundStudio()
    {
        if (soundStudioScreen == null)
        { Debug.LogWarning("[BOOTSTRAP] SoundStudioScreen null!"); return; }

        // Geri butonu — TopBar icinde
        Transform studioTopBar = soundStudioScreen.transform.Find("TopBar");
        if (studioTopBar != null)
            BindBtn(studioTopBar, "BackToMainButton", ShowMainConsole);
        else
            BindBtn(soundStudioScreen.transform, "BackToMainButton", ShowMainConsole);

        // Uygula butonu
        BindBtn(soundStudioScreen.transform, "ApplyButton", () =>
        {
            SoundPresetManager.Instance?.ApplyToSynthesizer();
            AccessibilityManager.Instance?.Speak("Ses ayarlari kaydedildi.");
            Debug.Log("[BOOTSTRAP] Ses ayarlari uygulandı.");
        });

        // Gitar butonlari
        var gSection = soundStudioScreen.transform.Find("GuitarSection");
        if (gSection != null)
        {
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var btn = gSection.Find($"GuitarBtn_{i}")?.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        SoundPresetManager.Instance?.SetGuitarIndex(idx);
                        UpdateStudioVisuals();
                    });
                }
            }
        }

        // Piyano butonlari
        var pSection = soundStudioScreen.transform.Find("PianoSection");
        if (pSection != null)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                for (int ci = 0; ci < 4; ci++)
                {
                    int s = slot, c = ci;
                    var btn = pSection.Find($"PianoBtn_{s}_{c}")?.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() =>
                        {
                            SoundPresetManager.Instance?.SetPianoNote(s, c);
                            UpdateStudioVisuals();
                        });
                    }
                }
            }
        }

        // Davul butonlari
        var dSection = soundStudioScreen.transform.Find("DrumSection");
        if (dSection != null)
        {
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var btn = dSection.Find($"DrumBtn_{idx}")?.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        SoundPresetManager.Instance?.SetDrumIndex(idx);
                        UpdateStudioVisuals();
                    });
                }
            }
        }

        Debug.Log("[BOOTSTRAP] [OK] SoundStudio baglandi.");
    }

    private void UpdateStudioVisuals()
    {
        if (soundStudioScreen == null || SoundPresetManager.Instance == null) return;

        var preset = SoundPresetManager.Instance;
        Color activeC = new Color(0f, 0.9f, 1f, 1f);
        Color inactC  = new Color(0.1f, 0.15f, 0.2f, 1f);

        // Gitar butonlari
        var gSection = soundStudioScreen.transform.Find("GuitarSection");
        if (gSection != null)
        {
            for (int i = 0; i < 4; i++)
            {
                var img = gSection.Find($"GuitarBtn_{i}")?.GetComponent<Image>();
                if (img != null) img.color = (i == preset.GetGuitarIndex()) ? activeC : inactC;
            }
        }

        // Piyano butonlari
        var pSection = soundStudioScreen.transform.Find("PianoSection");
        if (pSection != null)
        {
            Color[] nc = {
                new Color(1f,0.58f,0f,1f), new Color(1f,0.85f,0f,1f),
                new Color(0f,0.75f,1f,1f), new Color(0.75f,0.25f,1f,1f)
            };
            for (int slot = 0; slot < 4; slot++)
            {
                for (int ci = 0; ci < 4; ci++)
                {
                    var img = pSection.Find($"PianoBtn_{slot}_{ci}")?.GetComponent<Image>();
                    if (img != null)
                        img.color = (ci == preset.GetPianoIndex(slot))
                            ? nc[slot] : inactC;
                }
            }
        }

        // Davul butonlari
        var dSection = soundStudioScreen.transform.Find("DrumSection");
        if (dSection != null)
        {
            for (int i = 0; i < 4; i++)
            {
                var img = dSection.Find($"DrumBtn_{i}")?.GetComponent<Image>();
                if (img != null)
                    img.color = (i == preset.GetDrumIndex())
                        ? new Color(1f,0f,0.6f,1f) : inactC;
            }
        }
    }

    public void ShowCalibration()
    {
        Debug.Log("[BOOTSTRAP] → CalibrationScreen");
        screenManager?.ShowCalibration();
        if (calibRoutine != null) StopCoroutine(calibRoutine);
        calibRoutine = StartCoroutine(CalibrationUIRoutine());
    }

    // ─────────────────────────────────────────
    // KALİBRASYON UI ANİMASYONU
    // ─────────────────────────────────────────
    private bool _calibDone = false;

    private IEnumerator CalibrationUIRoutine()
    {
        Image progressRing = null;
        if (calibrationScreen != null)
        {
            Transform ringT = calibrationScreen.transform.Find("CalibrationCard/RingContainer/ProgressRing");
            if (ringT != null) progressRing = ringT.GetComponent<Image>();
        }

        if (calContinueButton != null) calContinueButton.interactable = false;
        _calibDone = false;

        // CalibrationManager tamamlanınca flag'i set et
        if (masterController != null)
            masterController.OnCalibrationDone += OnCalibDone;

        SetText(calStepText,   "1/2  Sol elinizi sensörün üstüne kapatin");
        SetText(calPercentText,"0%");
        SetText(calLuxText,    "Lux: olculuyor...");
        SetText(calStatusText, "Durum: Bekliyor");
        if (progressRing != null) progressRing.fillAmount = 0f;

        if (masterController != null) masterController.StartCalibration();
        Debug.Log("[BOOTSTRAP] [CALIB] Kalibrasyon baslatildi.");

        // Faz 1: 2.5s — Min ölçümü (el kapalı)
        float duration = 2.5f, timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n = Mathf.Clamp01(timer / duration);
            SetText(calPercentText, Mathf.RoundToInt(n * 50f) + "%");
            float lux = masterController != null ? masterController.GetRawLux() : 0f;
            SetText(calLuxText, "Lux: " + lux.ToString("0"));
            SetText(calStatusText, "Durum: Min olculuyor");
            if (progressRing != null) progressRing.fillAmount = n * 0.5f;
            yield return null;
        }

        SetText(calStepText, "2/2  Elinizi sensorden uzaklastirin");

        // Faz 2: 2.5s — Max ölçümü (el açık)
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float n = Mathf.Clamp01(timer / duration);
            SetText(calPercentText, 50 + Mathf.RoundToInt(n * 50f) + "%");
            float lux = masterController != null ? masterController.GetRawLux() : 0f;
            SetText(calLuxText, "Lux: " + lux.ToString("0"));
            SetText(calStatusText, "Durum: Max olculuyor");
            if (progressRing != null) progressRing.fillAmount = 0.5f + n * 0.5f;
            yield return null;
        }

        // Event gelene kadar bekle — max 3s timeout
        float wait = 0f;
        while (!_calibDone && wait < 3f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        if (masterController != null)
            masterController.OnCalibrationDone -= OnCalibDone;

        bool success = masterController != null && masterController.IsCalibrated();
        Debug.Log($"[BOOTSTRAP] [CALIB] Sonuc: {(success ? "BASARILI" : "BASARISIZ")} | IsCalibrated={success} | EventGeldi={_calibDone}");

        if (success)
        {
            SetText(calStepText,   "Kalibrasyon tamamlandi!");
            SetText(calPercentText,"100%");
            SetText(calStatusText, "Durum: Hazir");
            if (progressRing != null) progressRing.fillAmount = 1f;
            if (calContinueButton != null) calContinueButton.interactable = true;
            if (AccessibilityManager.Instance != null) AccessibilityManager.Instance.AnnounceCalibrationComplete();
        }
        else
        {
            SetText(calStepText,   "HATA: Isik farki yetersiz — Tekrar dene");
            SetText(calPercentText,"!");
            SetText(calStatusText, "Durum: Basarisiz");
            if (progressRing != null) progressRing.fillAmount = 0f;
            Debug.LogWarning("[BOOTSTRAP] [CALIB] Basarisiz veya timeout.");
        }
    }

    private void OnCalibDone() => _calibDone = true;

    // ─────────────────────────────────────────
    // START — LoopRecorder durum olayına abone ol
    // ─────────────────────────────────────────
    private void Start()
    {
        if (LoopRecorder.Instance != null)
            LoopRecorder.Instance.OnStateChanged += _ => UpdateLoopVisuals();
        UpdateLoopVisuals();
    }

    // ─────────────────────────────────────────
    // LOOP RECORDER KONTROLÜ
    // ─────────────────────────────────────────
    private void LoopToggle()
    {
        var loop = LoopRecorder.Instance;
        if (loop == null) return;

        switch (loop.CurrentState)
        {
            case LoopRecorder.State.Idle:
            case LoopRecorder.State.Ready:
                loop.StartRecording();
                MetronomeController.Instance?.Play();
                break;
            case LoopRecorder.State.Recording:
                loop.StopRecording();
                MetronomeController.Instance?.Stop();
                break;
            case LoopRecorder.State.Playing:
                loop.StopPlayback();
                loop.StartRecording();
                MetronomeController.Instance?.Play();
                break;
        }
    }

    private void LoopPlay()
    {
        var loop = LoopRecorder.Instance;
        if (loop == null) return;

        if (loop.CurrentState == LoopRecorder.State.Playing)
            loop.StopPlayback();
        else if (loop.HasLoop)
            loop.StartPlayback();
    }

    // ─────────────────────────────────────────
    // METRONOM YARDIMCILARI (BPM kontrolü için)
    // ─────────────────────────────────────────
    public void MetronomeToggle()           => MetronomeController.Instance?.Toggle();
    public void MetronomeIncrease()         => MetronomeController.Instance?.IncreaseBPM();
    public void MetronomeDecrease()         => MetronomeController.Instance?.DecreaseBPM();
    public void MetronomeSetBPM(float bpm)  => MetronomeController.Instance?.SetBPM(bpm);
    public bool MetronomeIsPlaying()        => MetronomeController.Instance?.IsPlaying ?? false;
    public float MetronomeBPM()             => MetronomeController.Instance?.BPM ?? 120f;

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

        if (metronomeBPMText != null)
            metronomeBPMText.text = MetronomeBPM().ToString("0") + " BPM";
    }

    // ─────────────────────────────────────────
    // YARDIMCILAR
    // ─────────────────────────────────────────
    private void ActivateAll(bool active)
    {
        SetActive(orientationScreen,  active);
        SetActive(splashScreen,       active);
        SetActive(onboardingScreen,   active);
        SetActive(calibrationScreen,  active);
        SetActive(mainConsoleScreen,  active);
        SetActive(settingsScreen,     active);
        SetActive(soundStudioScreen,  active);
        SetActive(recordStudioScreen, active);
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

        // Loop butonlarında ripple yok
        if (childName.Contains("Loop") || childName.Contains("Decrease") || childName.Contains("Increase"))
        {
            Debug.Log($"[BOOTSTRAP] [OK] '{childName}' butonu bağlandı.");
            return;
        }

        // Diğer butonlara otomatik ripple — butonun kendi pozisyonundan yayilir
        Transform finalChildT = childT;
        btn.onClick.AddListener(() =>
        {
            Color rippleColor = RippleEffect.ColorGuitar; // varsayilan: cyan

            if (childName.Contains("Settings") || childName.Contains("Back"))
                rippleColor = new Color(0.7f, 0.4f, 1f, 1f);

            RippleEffect.Instance?.SpawnFromScreenPos(
                RectTransformUtility.WorldToScreenPoint(Camera.main,
                    finalChildT.position),
                rippleColor, interrupt: false
            );
        });

        Debug.Log($"[BOOTSTRAP] [OK] '{childName}' butonu + ripple bağlandı.");
    }

    private static void UpdateAccessLabel(TextMeshProUGUI label, bool enabled)
    {
        if (label == null) return;
        label.text  = enabled ? "Sesli Okuma: AÇIK" : "Sesli Okuma: KAPALI";
        label.color = Hex(enabled ? "#00F0FF" : "#8899AA");
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
