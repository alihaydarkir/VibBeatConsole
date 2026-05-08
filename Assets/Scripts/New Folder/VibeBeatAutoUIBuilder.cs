using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VibeBeatAutoUIBuilder : MonoBehaviour
{
    private const string CanvasName = "VibeBeatCanvas";

    // Renk Paleti
    private readonly Color bgDeep = Hex("#020609");
    private readonly Color bgPanel = Hex("#071018");
    private readonly Color bgCard = Hex("#0A1520");
    private readonly Color guitarCyan = Hex("#00F0FF");
    private readonly Color pianoOrange = Hex("#FFA500");
    private readonly Color drumMagenta = Hex("#FF1AAD");
    private readonly Color textWhite = Hex("#F2F2F2");
    private readonly Color textGray = Hex("#8899AA");
    private readonly Color passiveGray = Hex("#1A2535");
    private readonly Color borderGlow = Hex("#00F0FF22");

    private VibeBeatScreenManager screenManager;

    [ContextMenu("Build VibeBeat UI")]
    public void BuildVibeBeatUI()
    {
        DeleteOldCanvas();
        EnsureEventSystem();

        GameObject canvasGO = CreateCanvas();
        screenManager = canvasGO.AddComponent<VibeBeatScreenManager>();

        GameObject splash = BuildSplashScreen(canvasGO.transform);
        GameObject onboarding = BuildOnboardingScreen(canvasGO.transform);
        GameObject calibration = BuildCalibrationScreen(canvasGO.transform);
        GameObject main = BuildMainConsoleScreen(canvasGO.transform);
        GameObject settings = BuildSettingsScreen(canvasGO.transform);

        screenManager.splashScreen = splash;
        screenManager.onboardingScreen = onboarding;
        screenManager.calibrationScreen = calibration;
        screenManager.mainConsoleScreen = main;
        screenManager.settingsScreen = settings;

        splash.SetActive(true);
        onboarding.SetActive(false);
        calibration.SetActive(false);
        main.SetActive(false);
        settings.SetActive(false);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[VibeBeat] ✅ UI inşa edildi. Ctrl+S ile kaydet!");
#endif
    }

    // ─────────────────────────────────────────
    // SPLASH SCREEN
    // ─────────────────────────────────────────
    private GameObject BuildSplashScreen(Transform parent)
    {
        GameObject root = CreateFullScreenPanel("SplashScreen", parent, bgDeep);

        // Başlık
        CreateText(root.transform, "TitleText", "VIBEBEAT CONSOLE", 72, textWhite,
            TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.80f), FontStyles.Bold);

        CreateText(root.transform, "SubtitleText", "SENSÖR TABANLI MÜZİK DENEYİMİ", 28, guitarCyan,
            TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.59f), new Vector2(0.9f, 0.66f), FontStyles.Normal);

        // Dalga görseli
        BuildAnimatedWaveform(root.transform, "SplashWave",
            new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.56f));

        // Başla butonu
        Button startBtn = CreateGlowButton(root.transform, "StartButton", "BAŞLA  ›",
            guitarCyan, new Vector2(0.38f, 0.26f), new Vector2(0.62f, 0.38f));
        startBtn.onClick.AddListener(screenManager.ShowOnboarding);

        // Ripple animasyonu buta ekle
        NeonRippleFeedback ripple = startBtn.gameObject.AddComponent<NeonRippleFeedback>();
        ripple.rippleColor = guitarCyan;

        CreateText(root.transform, "FooterText", "SAMSUNG S20 FE İÇİN OPTİMİZE EDİLDİ", 20, Hex("#334455"),
            TextAlignmentOptions.Center,
            new Vector2(0.2f, 0.06f), new Vector2(0.8f, 0.12f), FontStyles.Normal);

        return root;
    }

    // ─────────────────────────────────────────
    // ONBOARDING SCREEN
    // ─────────────────────────────────────────
    private GameObject BuildOnboardingScreen(Transform parent)
    {
        GameObject root = CreateFullScreenPanel("OnboardingScreen", parent, bgDeep);

        // TopBar
        BuildTopBar(root.transform, showSettings: false, showBack: false);

        // Başlık
        CreateText(root.transform, "TitleText", "NASIL KULLANILIR?", 60, textWhite,
            TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.90f), FontStyles.Bold);

        CreateText(root.transform, "SubText",
            "VibeBeat Console'u keşfetmeye başlamak çok kolay",
            26, textGray, TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.79f), FontStyles.Normal);

        // 3 kart
        BuildOnboardingCard(root.transform, "GuitarCard",
            "1", "GİTAR",
            "Elini sensöre yaklaştırarak\nsesi kontrol et",
            guitarCyan,
            new Vector2(0.04f, 0.28f), new Vector2(0.32f, 0.70f));

        BuildOnboardingCard(root.transform, "PianoCard",
            "2", "PİYANO",
            "Sağ üstteki tuşlara\ndokun",
            pianoOrange,
            new Vector2(0.36f, 0.28f), new Vector2(0.64f, 0.70f));

        BuildOnboardingCard(root.transform, "DrumCard",
            "3", "DAVUL",
            "Sağ alttaki pad ile\nritim vur",
            drumMagenta,
            new Vector2(0.68f, 0.28f), new Vector2(0.96f, 0.70f));

        // Devam butonu
        Button continueBtn = CreateGlowButton(root.transform, "ContinueButton", "DEVAM ET  ›",
            guitarCyan, new Vector2(0.72f, 0.10f), new Vector2(0.96f, 0.22f));
        continueBtn.onClick.AddListener(screenManager.ShowCalibration);

        NeonRippleFeedback ripple = continueBtn.gameObject.AddComponent<NeonRippleFeedback>();
        ripple.rippleColor = guitarCyan;

        return root;
    }

    private void BuildOnboardingCard(Transform parent, string name,
        string number, string title, string desc,
        Color accent, Vector2 aMin, Vector2 aMax)
    {
        // Ana kart
        GameObject card = CreatePanel(name, parent, bgPanel, aMin, aMax);
        Image cardImg = card.GetComponent<Image>();
        cardImg.color = bgPanel;

        // Sol kenar çizgisi (accent bar)
        GameObject bar = CreatePanel("AccentBar", card.transform, accent,
            new Vector2(0f, 0f), new Vector2(0.025f, 1f));

        // Büyük numara (arka planda)
        CreateText(card.transform, "BigNum", number, 120, new Color(accent.r, accent.g, accent.b, 0.08f),
            TextAlignmentOptions.Right,
            new Vector2(0.5f, 0.4f), new Vector2(0.98f, 0.95f), FontStyles.Bold);

        // Başlık
        CreateText(card.transform, "CardTitle", title, 42, accent,
            TextAlignmentOptions.Left,
            new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.80f), FontStyles.Bold);

        // Açıklama
        CreateText(card.transform, "CardDesc", desc, 26, textGray,
            TextAlignmentOptions.Left,
            new Vector2(0.1f, 0.20f), new Vector2(0.9f, 0.58f), FontStyles.Normal);

        // Kart animasyonu
        OnboardingCardPulse pulse = card.AddComponent<OnboardingCardPulse>();
        pulse.accentColor = accent;
        pulse.cardImage = cardImg;
    }

    // ─────────────────────────────────────────
    // CALIBRATION SCREEN
    // ─────────────────────────────────────────
    private GameObject BuildCalibrationScreen(Transform parent)
    {
        GameObject root = CreateFullScreenPanel("CalibrationScreen", parent, bgDeep);
        BuildTopBar(root.transform, showSettings: true, showBack: false);

        GameObject card = CreatePanel("CalibrationCard", root.transform, bgPanel,
            new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.90f));

        CreateText(card.transform, "TitleText", "SENSÖR KALİBRASYONU", 34, textWhite,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.93f), FontStyles.Bold);

        TextMeshProUGUI stepText = CreateText(card.transform, "StepText",
            "1/2  Elini sensörün üstüne kapat", 24, textGray,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.82f), FontStyles.Normal);

        TextMeshProUGUI percentText = CreateText(card.transform, "PercentText", "0%", 96, guitarCyan,
            TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.72f), FontStyles.Bold);

        CreateText(card.transform, "ProgressLabel", "İLERLİYOR", 20, Hex("#446677"),
            TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.46f), FontStyles.Normal);

        GameObject infoBar = CreatePanel("InfoBar", card.transform, bgCard,
            new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.38f));

        TextMeshProUGUI luxText = CreateText(infoBar.transform, "LuxText", "Lux: 12.4", 24, textGray,
            TextAlignmentOptions.Left,
            new Vector2(0.05f, 0f), new Vector2(0.48f, 1f), FontStyles.Normal);

        TextMeshProUGUI statusText = CreateText(infoBar.transform, "StatusText", "Durum: Ölçülüyor", 24, textGray,
            TextAlignmentOptions.Right,
            new Vector2(0.52f, 0f), new Vector2(0.95f, 1f), FontStyles.Normal);

        Button retryBtn = CreatePanel("RetryButton", card.transform, passiveGray,
            new Vector2(0.08f, 0.08f), new Vector2(0.44f, 0.22f)).AddComponent<Button>();
        retryBtn.transition = Selectable.Transition.None;
        CreateText(retryBtn.transform, "Label", "↺  TEKRAR DENE", 22, textGray,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);

        Button contBtn = CreateGlowButton(card.transform, "ContinueButton", "DEVAM  ›",
            guitarCyan, new Vector2(0.56f, 0.08f), new Vector2(0.92f, 0.22f));
        contBtn.onClick.AddListener(screenManager.ShowMainConsole);

        CalibrationDemoController ctrl = root.AddComponent<CalibrationDemoController>();
        ctrl.stepText = stepText;
        ctrl.percentText = percentText;
        ctrl.luxText = luxText;
        ctrl.statusText = statusText;

        retryBtn.onClick.AddListener(ctrl.RestartCalibration);

        return root;
    }

    // ─────────────────────────────────────────
    // MAIN CONSOLE
    // ─────────────────────────────────────────
    private GameObject BuildMainConsoleScreen(Transform parent)
    {
        GameObject root = CreateFullScreenPanel("MainConsoleScreen", parent, bgDeep);
        BuildTopBar(root.transform, showSettings: true, showBack: false);

        // Guitar Panel
        GameObject guitarPanel = CreatePanel("GuitarPanel", root.transform, bgPanel,
            new Vector2(0.015f, 0.05f), new Vector2(0.295f, 0.88f));
        BuildGuitarPanel(guitarPanel.transform);

        // Sağ panel
        GameObject rightPanel = CreatePanel("RightPanel", root.transform, bgDeep,
            new Vector2(0.310f, 0.05f), new Vector2(0.985f, 0.88f));

        GameObject pianoPanel = CreatePanel("PianoPanel", rightPanel.transform, bgPanel,
            new Vector2(0f, 0.52f), new Vector2(1f, 1f));
        BuildPianoPanel(pianoPanel.transform);

        GameObject drumPanel = CreatePanel("DrumPanel", rightPanel.transform, bgPanel,
            new Vector2(0f, 0f), new Vector2(1f, 0.48f));
        BuildDrumPanel(drumPanel.transform);

        return root;
    }

    private void BuildGuitarPanel(Transform parent)
    {
        CreateText(parent, "TitleText", "GUITAR", 44, guitarCyan,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.94f), FontStyles.Bold);

        CreateText(parent, "SensorLabel", "SENSÖR SEVİYESİ", 22, textGray,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.83f), FontStyles.Normal);

        CreateText(parent, "SensorValueText", "0.63", 80, textWhite,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.76f), FontStyles.Bold);

        // Canlı dalga alanı
        GameObject waveArea = CreatePanel("WaveformArea", parent, bgCard,
            new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.58f));
        BuildAnimatedWaveform(waveArea.transform, "GuitarWave",
            new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.9f));

        Button calBtn = CreateGlowButton(parent, "CalibrateButton", "⊙  KALİBRE ET",
            guitarCyan, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.30f));
        calBtn.onClick.AddListener(screenManager.ShowCalibration);

        Button muteBtn = CreatePanel("MuteButton", parent, passiveGray,
            new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.17f)).AddComponent<Button>();
        muteBtn.transition = Selectable.Transition.None;
        CreateText(muteBtn.transform, "Label", "⊘  MUTE", 24, textGray,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);

        NeonRippleFeedback mRipple = muteBtn.gameObject.AddComponent<NeonRippleFeedback>();
        mRipple.rippleColor = guitarCyan;
    }

    private void BuildPianoPanel(Transform parent)
    {
        CreateText(parent, "TitleText", "PIANO", 38, pianoOrange,
            TextAlignmentOptions.Left,
            new Vector2(0.03f, 0.76f), new Vector2(0.6f, 0.94f), FontStyles.Bold);

        string[] notes = { "C4", "D4", "E4", "F4" };
        for (int i = 0; i < notes.Length; i++)
        {
            float sx = 0.03f + i * 0.238f;
            float ex = sx + 0.210f;

            GameObject keyGO = CreatePanel("PianoKey_" + notes[i], parent, bgCard,
                new Vector2(sx, 0.10f), new Vector2(ex, 0.72f));

            Button keyBtn = keyGO.AddComponent<Button>();
            keyBtn.transition = Selectable.Transition.None;

            CreateText(keyGO.transform, "NoteLabel", notes[i], 24, Hex("#556677"),
                TextAlignmentOptions.Center,
                new Vector2(0f, 0.02f), new Vector2(1f, 0.18f), FontStyles.Normal);

            NeonRippleFeedback fb = keyGO.AddComponent<NeonRippleFeedback>();
            fb.rippleColor = pianoOrange;
            fb.expandRadius = 1.8f;

            SimplePressFeedback press = keyGO.AddComponent<SimplePressFeedback>();
            press.idleColor = bgCard;
            press.pressColor = new Color(pianoOrange.r, pianoOrange.g, pianoOrange.b, 0.15f);
        }
    }

    private void BuildDrumPanel(Transform parent)
    {
        CreateText(parent, "TitleText", "DRUM", 38, drumMagenta,
            TextAlignmentOptions.Left,
            new Vector2(0.03f, 0.76f), new Vector2(0.6f, 0.94f), FontStyles.Bold);

        GameObject padGO = CreatePanel("DrumPad", parent, bgCard,
            new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.70f));

        Button padBtn = padGO.AddComponent<Button>();
        padBtn.transition = Selectable.Transition.None;

        NeonRippleFeedback fb = padGO.AddComponent<NeonRippleFeedback>();
        fb.rippleColor = drumMagenta;
        fb.expandRadius = 3.5f;
        fb.pulseScale = 1.06f;

        SimplePressFeedback press = padGO.AddComponent<SimplePressFeedback>();
        press.idleColor = bgCard;
        press.pressColor = new Color(drumMagenta.r, drumMagenta.g, drumMagenta.b, 0.15f);
    }

    // ─────────────────────────────────────────
    // SETTINGS SCREEN
    // ─────────────────────────────────────────
    private GameObject BuildSettingsScreen(Transform parent)
    {
        GameObject root = CreateFullScreenPanel("SettingsScreen", parent, bgDeep);
        BuildTopBar(root.transform, showSettings: false, showBack: true);

        CreateText(root.transform, "TitleText", "AYARLAR", 60, textWhite,
            TextAlignmentOptions.Left,
            new Vector2(0.05f, 0.78f), new Vector2(0.6f, 0.90f), FontStyles.Bold);

        GameObject panel = CreatePanel("SettingsPanel", root.transform, bgPanel,
            new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.76f));

        float[] rowTops = { 0.80f, 0.62f, 0.44f, 0.26f, 0.08f };
        float[] rowBottoms = { 0.94f, 0.76f, 0.58f, 0.40f, 0.22f };
        string[] labels = {
            "TEKRAR KALİBRE ET",
            "HAPTIC FEEDBACK   AÇIK",
            "EFEKT YOĞUNLUĞU   ORTA",
            "SES SEVİYESİ   %70",
            "HAKKINDA"
        };

        for (int i = 0; i < labels.Length; i++)
        {
            Button row = CreateSettingsRow(panel.transform,
                "Row_" + i, labels[i],
                new Vector2(0.02f, rowBottoms[i] - 0.14f),
                new Vector2(0.98f, rowBottoms[i]));

            if (i == 0) row.onClick.AddListener(screenManager.ShowCalibration);
        }

        Button backBtn = CreateGlowButton(root.transform, "BackToMainButton", "⌂  ANA EKRANA DÖN",
            guitarCyan, new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.15f));
        backBtn.onClick.AddListener(screenManager.ShowMainConsole);

        NeonRippleFeedback ripple = backBtn.gameObject.AddComponent<NeonRippleFeedback>();
        ripple.rippleColor = guitarCyan;

        return root;
    }

    // ─────────────────────────────────────────
    // TOP BAR
    // ─────────────────────────────────────────
    private void BuildTopBar(Transform parent, bool showSettings, bool showBack)
    {
        GameObject bar = CreatePanel("TopBar", parent, bgPanel,
            new Vector2(0f, 0.91f), new Vector2(1f, 1f));

        CreateText(bar.transform, "MenuIcon", "☰", 38, guitarCyan,
            TextAlignmentOptions.Center,
            new Vector2(0.01f, 0.1f), new Vector2(0.06f, 0.9f), FontStyles.Normal);

        CreateText(bar.transform, "Title", "VIBEBEAT CONSOLE", 30, textWhite,
            TextAlignmentOptions.Left,
            new Vector2(0.07f, 0.1f), new Vector2(0.40f, 0.9f), FontStyles.Bold);

        CreateText(bar.transform, "ModeDots", "● GUITAR   ● PIANO   ● DRUM", 22, textGray,
            TextAlignmentOptions.Right,
            new Vector2(0.42f, 0.1f), new Vector2(0.82f, 0.9f), FontStyles.Normal);

        if (showSettings)
        {
            Button settingsBtn = CreatePanel("SettingsButton", bar.transform, passiveGray,
                new Vector2(0.85f, 0.15f), new Vector2(0.97f, 0.85f)).AddComponent<Button>();
            settingsBtn.transition = Selectable.Transition.None;
            CreateText(settingsBtn.transform, "Label", "AYARLAR", 20, textGray,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Normal);
            settingsBtn.onClick.AddListener(screenManager.ShowSettings);
        }

        if (showBack)
        {
            Button backBtn = CreatePanel("BackButton", bar.transform, passiveGray,
                new Vector2(0.85f, 0.15f), new Vector2(0.97f, 0.85f)).AddComponent<Button>();
            backBtn.transition = Selectable.Transition.None;
            CreateText(backBtn.transform, "Label", "←", 28, guitarCyan,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Normal);
            backBtn.onClick.AddListener(screenManager.ShowMainConsole);
        }
    }

    // ─────────────────────────────────────────
    // ANİMASYONLU DALGA
    // ─────────────────────────────────────────
    private void BuildAnimatedWaveform(Transform parent, string name,
        Vector2 aMin, Vector2 aMax)
    {
        GameObject container = CreateUIObject(name, parent);
        RectTransform cr = container.GetComponent<RectTransform>();
        cr.anchorMin = aMin; cr.anchorMax = aMax;
        cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero;

        WaveformAnimator anim = container.AddComponent<WaveformAnimator>();
        anim.barCount = 32;

        // Barları oluştur
        GameObject[] bars = new GameObject[32];
        for (int i = 0; i < 32; i++)
        {
            float x0 = i / 32f;
            float x1 = x0 + 0.022f;
            GameObject bar = CreateUIObject("Bar_" + i, container.transform);
            RectTransform r = bar.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(x0, 0.05f);
            r.anchorMax = new Vector2(x1, 0.95f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            Image img = bar.AddComponent<Image>();

            // Spektrum rengi — cyan'dan magenta'ya
            float t = i / 31f;
            Color c = Color.Lerp(guitarCyan, drumMagenta, t);
            img.color = new Color(c.r, c.g, c.b, 0.9f);

            bars[i] = bar;
        }

        anim.bars = bars;
    }

    // ─────────────────────────────────────────
    // YARDIMCI — GLOW BUTTON
    // ─────────────────────────────────────────
    private Button CreateGlowButton(Transform parent, string name, string label,
        Color glowColor, Vector2 aMin, Vector2 aMax)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.02f, 0.05f, 0.09f, 0.95f);

        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        // Kenarlık — outline image ile simüle
        GameObject border = CreateUIObject("Border", go.transform);
        RectTransform br = border.GetComponent<RectTransform>();
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero; br.offsetMax = Vector2.zero;
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f);
        borderImg.raycastTarget = false;

        // İç dolgu (border'ı örter, kenar kalır)
        GameObject inner = CreateUIObject("Inner", go.transform);
        RectTransform ir = inner.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(1.5f, 1.5f);
        ir.offsetMax = new Vector2(-1.5f, -1.5f);
        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.02f, 0.05f, 0.09f, 1f);
        innerImg.raycastTarget = false;

        TextMeshProUGUI lbl = CreateText(go.transform, "Label", label, 26, glowColor,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
        lbl.raycastTarget = false;

        return btn;
    }

    // ─────────────────────────────────────────
    // YARDIMCI — SETTINGS ROW
    // ─────────────────────────────────────────
    private Button CreateSettingsRow(Transform parent, string name, string label,
        Vector2 aMin, Vector2 aMax)
    {
        GameObject go = CreatePanel(name, parent, Hex("#0A1520"), aMin, aMax);
        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        // Alt çizgi
        GameObject line = CreatePanel("Line", go.transform, Hex("#FFFFFF11"),
            new Vector2(0f, 0f), new Vector2(1f, 0.04f));

        TextMeshProUGUI lbl = CreateText(go.transform, "Label", label, 24, textGray,
            TextAlignmentOptions.Left,
            new Vector2(0.03f, 0.1f), new Vector2(0.9f, 0.9f), FontStyles.Normal);

        CreateText(go.transform, "Arrow", "›", 30, Hex("#334455"),
            TextAlignmentOptions.Right,
            new Vector2(0.9f, 0.1f), new Vector2(0.99f, 0.9f), FontStyles.Normal);

        NeonRippleFeedback ripple = go.AddComponent<NeonRippleFeedback>();
        ripple.rippleColor = guitarCyan;
        ripple.expandRadius = 0.8f;

        return btn;
    }

    // ─────────────────────────────────────────
    // TEMEL HELPERS
    // ─────────────────────────────────────────
    private void DeleteOldCanvas()
    {
        GameObject old = GameObject.Find(CanvasName);
        if (old != null) DestroyImmediate(old);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private GameObject CreateCanvas()
    {
        GameObject go = new GameObject(CanvasName);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color,
        Vector2 aMin, Vector2 aMax)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions align,
        Vector2 aMin, Vector2 aMax, FontStyles style)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    // ─────────────────────────────────────────
    // DAIRE RİPPLE — TÜM BUTONLARA OTOMATIK UYGULA
    // ─────────────────────────────────────────
    [ContextMenu("Apply Circle Ripple to All Buttons")]
    public void ApplyCircleRippleToAllButtons()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas bulunamadı!");
            return;
        }

        Button[] allButtons = canvas.GetComponentsInChildren<Button>();
        int count = 0;

        foreach (Button btn in allButtons)
        {
            // Mevcut ripple componentlerini sil
            NeonRippleFeedback oldRipple = btn.GetComponent<NeonRippleFeedback>();
            if (oldRipple != null) DestroyImmediate(oldRipple);

            EnhancedRippleEffect oldEnhanced = btn.GetComponent<EnhancedRippleEffect>();
            if (oldEnhanced != null) DestroyImmediate(oldEnhanced);

            WaveRippleEffect oldWave = btn.GetComponent<WaveRippleEffect>();
            if (oldWave != null) DestroyImmediate(oldWave);

            // Yeni dalga ripple efekti ekle
            WaveRippleEffect waveRipple = btn.gameObject.AddComponent<WaveRippleEffect>();
            
            // Rengi butona göre ayarla
            Color waveColor = GetWaveColorForButton(btn.name);
            waveRipple.settings.colorPreset = WaveRippleEffect.ColorPreset.Custom;
            waveRipple.settings.customColor = waveColor;
            waveRipple.settings.expandRadius = 1.8f;
            waveRipple.settings.duration = 0.6f;
            waveRipple.settings.maxAlpha = 0.85f;
            waveRipple.settings.waveCount = 3;
            waveRipple.settings.waveDelay = 0.12f;
            waveRipple.buttonPulseScale = 1.06f;

            count++;
        }

        Debug.Log($"✅ {count} butona dalga ripple efekti uygulandı!");

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }

    /// <summary>
    /// Butona göre uygun dalga rengini belirle
    /// </summary>
    private Color GetWaveColorForButton(string buttonName)
    {
        if (buttonName.Contains("Guitar") || buttonName.Contains("guitar"))
            return guitarCyan;
        if (buttonName.Contains("Piano") || buttonName.Contains("piano"))
            return pianoOrange;
        if (buttonName.Contains("Drum") || buttonName.Contains("drum"))
            return drumMagenta;
        
        // Varsayılan
        return guitarCyan;
    }

    [ContextMenu("Apply Wave Ripple to All Buttons (Optimized)")]
    public void ApplyWaveRippleOptimized()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas bulunamadı!");
            return;
        }

        Button[] allButtons = canvas.GetComponentsInChildren<Button>();
        int count = 0;

        foreach (Button btn in allButtons)
        {
            // Mevcut tüm ripple componentlerini temizle
            foreach (var component in btn.GetComponents<MonoBehaviour>())
            {
                if (component is NeonRippleFeedback or EnhancedRippleEffect or WaveRippleEffect)
                {
                    DestroyImmediate(component);
                }
            }

            // Dalga efekti ekle
            WaveRippleEffect wave = btn.gameObject.AddComponent<WaveRippleEffect>();
            
            // Bağlamsal renkler
            Color buttonColor = GetWaveColorForButton(btn.name);
            
            wave.settings.colorPreset = WaveRippleEffect.ColorPreset.Custom;
            wave.settings.customColor = buttonColor;
            wave.settings.expandRadius = 1.6f;
            wave.settings.duration = 0.65f;
            wave.settings.maxAlpha = 0.8f;
            wave.settings.waveCount = 2; // Daha az CPU kullanımı
            wave.settings.waveDelay = 0.15f;
            wave.buttonPulseScale = 1.05f;
            wave.buttonPulseDuration = 0.3f;

            count++;
        }

        Debug.Log($"✅ {count} butona optimize dalga efekti uygulandı!");

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("Reset All Button Effects")]
    public void ResetAllButtonEffects()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("⚠️ Canvas bulunamadı!");
            return;
        }

        Button[] allButtons = canvas.GetComponentsInChildren<Button>();
        
        foreach (Button btn in allButtons)
        {
            // Tüm ripple bileşenlerini kaldır
            foreach (var component in btn.GetComponents<MonoBehaviour>())
            {
                if (component is NeonRippleFeedback or EnhancedRippleEffect or WaveRippleEffect)
                {
                    DestroyImmediate(component);
                }
            }
        }

        Debug.Log($"✅ {allButtons.Length} butondan ripple efektleri kaldırıldı!");

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }
}