using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Sağ tık → "Build VibeBeat UI" ile Canvas'ı baştan oluşturur.
/// Efekt / animasyon YOK — bunları kendin ekle.
/// </summary>
public class VibeBeatAutoUIBuilder : MonoBehaviour
{
    private const string CanvasName = "VibeBeatCanvas";

    // ─── Renk Paleti ──────────────────────────────────────────────────────────
    private static readonly Color BgDeep      = Hex("#080C14");  // referans: cok koyu lacivert
    private static readonly Color BgSurface   = Hex("#0D1520");  // panel arkaplan
    private static readonly Color BgCard      = Hex("#111C2A");  // kart arkaplan
    private static readonly Color BgBorder    = Hex("#1A2C40");  // ince sinir
    private static readonly Color GuitarCyan  = Hex("#00E5FF");  // parlak cyan
    private static readonly Color PianoOrange = Hex("#FF9500");  // sicak turuncu
    private static readonly Color DrumMagenta = Hex("#FF0099");  // canli magenta
    private static readonly Color TextWhite   = Hex("#EEEEFF");  // hafif mavi-beyaz
    private static readonly Color TextGray    = Hex("#7A9AB5");  // soguk gri
    private static readonly Color Passive     = Hex("#162030");  // buton pasif
    private static readonly Color Dim         = Hex("#2A3F55");  // cok soluk

    private VibeBeatScreenManager sm; // screen manager kısaltması

    // ═════════════════════════════════════════════════════════════════════════
    [ContextMenu("Build VibeBeat UI")]
    public void BuildVibeBeatUI()
    {
        var old = GameObject.Find(CanvasName);
        if (old) DestroyImmediate(old);
        EnsureEventSystem();

        GameObject canvas = MakeCanvas();
        sm = canvas.AddComponent<VibeBeatScreenManager>();
        if (!canvas.GetComponent<VibeBeatBootstrap>())
            canvas.AddComponent<VibeBeatBootstrap>();

        var splash  = SplashScreen(canvas.transform);
        var onboard = OnboardingScreen(canvas.transform);
        var calib   = CalibrationScreen(canvas.transform);
        var main    = MainConsoleScreen(canvas.transform);
        var sett    = SettingsScreen(canvas.transform);

        sm.splashScreen      = splash;
        sm.onboardingScreen  = onboard;
        sm.calibrationScreen = calib;
        sm.mainConsoleScreen = main;
        sm.settingsScreen    = sett;

        splash.SetActive(true);
        onboard.SetActive(false);
        calib.SetActive(false);
        main.SetActive(false);
        sett.SetActive(false);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[VibeBeat] ✅ UI hazır — Ctrl+S ile kaydet.");
#endif
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SPLASH
    // ═════════════════════════════════════════════════════════════════════════
    private GameObject SplashScreen(Transform parent)
    {
        var root = FullScreen("SplashScreen", parent, BgDeep);

        // Başlık
        Txt(root, "TitleA", "VIBE", 92, TextWhite, Anchor.Right,
            0.05f, 0.50f, 0.69f, 0.83f, bold: true);
        Txt(root, "TitleB", "BEAT", 92, GuitarCyan, Anchor.Left,
            0.50f, 0.95f, 0.69f, 0.83f, bold: true);
        Txt(root, "TitleC", "CONSOLE", 44, new Color(0.7f, 0.5f, 1f, 0.9f), Anchor.Center,
            0.25f, 0.75f, 0.63f, 0.70f, bold: true);

        // Alt başlık
        Txt(root, "Subtitle", "Sensör Tabanlı Müzik Deneyimi", 28, TextGray,
            Anchor.Center, 0.15f, 0.85f, 0.57f, 0.63f);

        // Başla butonu
        var startBtn = GlowBtn(root.transform, "StartButton", "BASLA  >",
            GuitarCyan, 0.37f, 0.63f, 0.24f, 0.36f);
        startBtn.onClick.AddListener(sm.ShowOnboarding);

        // Alt yazı
        Txt(root, "Footer", "Samsung S20 FE icin optimize edildi",
            20, Dim, Anchor.Center, 0.20f, 0.80f, 0.07f, 0.13f);

        return root;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ONBOARDING
    // ═════════════════════════════════════════════════════════════════════════
    private GameObject OnboardingScreen(Transform parent)
    {
        var root = FullScreen("OnboardingScreen", parent, BgDeep);
        TopBar(root.transform, settings: false, back: false, dots: false);

        Txt(root, "Title", "NASIL KULLANILIR?", 58, TextWhite,
            Anchor.Center, 0.10f, 0.90f, 0.80f, 0.91f, bold: true);
        Txt(root, "Sub", "VibeBeat Console'u keşfetmeye başlamak çok kolay.",
            24, TextGray, Anchor.Center, 0.10f, 0.90f, 0.73f, 0.80f);

        OnboardCard(root.transform, "GuitarCard", "1", "GİTAR",
            "Elini sensöre yaklaştırarak sesi kontrol et",
            GuitarCyan, 0.03f, 0.33f, 0.26f, 0.70f);

        OnboardCard(root.transform, "PianoCard", "2", "PİYANO",
            "Sağ üstteki tuşlara dokun",
            PianoOrange, 0.36f, 0.64f, 0.26f, 0.70f);

        OnboardCard(root.transform, "DrumCard", "3", "DAVUL",
            "Sağ alttaki pad ile ritim vur",
            DrumMagenta, 0.67f, 0.97f, 0.26f, 0.70f);

        // Sayfa noktaları
        Txt(root, "Dots",    "*  o  o", 20, GuitarCyan,
            Anchor.Center, 0.36f, 0.64f, 0.13f, 0.19f);
        Txt(root, "PageNum", "1 / 3",   18, TextGray,
            Anchor.Center, 0.36f, 0.64f, 0.07f, 0.13f);

        var btn = GlowBtn(root.transform, "ContinueButton", "DEVAM ET  >",
            GuitarCyan, 0.72f, 0.97f, 0.07f, 0.20f);
        btn.onClick.AddListener(sm.ShowCalibration);

        return root;
    }

    private void OnboardCard(Transform parent, string name,
        string num, string title, string desc, Color accent,
        float x0, float x1, float y0, float y1)
    {
        var card = Panel(name, parent, BgCard, x0, x1, y0, y1);

        // Sol renkli kenar çizgisi
        Panel("Edge", card.transform, accent, 0f, 0.022f, 0f, 1f);

        // Büyük arka plan rakamı (şeffaf)
        Txt(card, "BigNum", num, 110,
            new Color(accent.r, accent.g, accent.b, 0.06f),
            Anchor.Right, 0.45f, 0.96f, 0.40f, 0.95f, bold: true);

        Txt(card, "Num",   num,   30, accent,  Anchor.Left,   0.08f, 0.30f, 0.83f, 0.96f, bold: true);
        Txt(card, "Title", title, 38, accent,  Anchor.Center, 0.05f, 0.95f, 0.55f, 0.72f, bold: true);
        Txt(card, "Desc",  desc,  22, TextGray,Anchor.Center, 0.07f, 0.93f, 0.08f, 0.55f);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CALİBRASYON
    // ═════════════════════════════════════════════════════════════════════════
    private GameObject CalibrationScreen(Transform parent)
    {
        var root = FullScreen("CalibrationScreen", parent, BgDeep);
        TopBar(root.transform, settings: true, back: false, dots: true);

        var card = Panel("CalibrationCard", root.transform, BgSurface,
            0.26f, 0.74f, 0.07f, 0.90f);

        Txt(card, "Icon",      "+",                           38, GuitarCyan, Anchor.Center, 0.30f, 0.70f, 0.87f, 0.97f);
        Txt(card, "CardTitle", "Sensor Kalibrasyonu",          30, TextWhite,  Anchor.Center, 0.05f, 0.95f, 0.79f, 0.88f, bold: true);

        var stepText = Txt(card, "StepText",
            "1/2  Elini sensorun ustune kapat", 21, TextGray,
            Anchor.Center, 0.05f, 0.95f, 0.72f, 0.79f);

        // ── İlerleme halkası (sabit kare) ─────────────────────────────────
        var ringCont = new GameObject("RingContainer");
        ringCont.transform.SetParent(card.transform, false);
        var rcRT = ringCont.AddComponent<RectTransform>();
        rcRT.anchorMin = rcRT.anchorMax = new Vector2(0.50f, 0.57f);
        rcRT.sizeDelta = new Vector2(220f, 220f);

        // Arka plan dolu daire
        RingLayer(ringCont.transform, "RingBG",
            CircleSprite(256), Hex("#0B1825"), 0f, 1f, 0f, 1f);

        // İlerleme halkası (doldurulan)
        var ringGO = RingLayer(ringCont.transform, "ProgressRing",
            RingSprite(256, 0.93f, 0.67f), GuitarCyan, 0f, 1f, 0f, 1f);
        var ringImg = ringGO.GetComponent<Image>();
        ringImg.type       = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillOrigin = (int)Image.Origin360.Top;
        ringImg.fillAmount = 0f;

        // İç kapak (donut efekti)
        RingLayer(ringCont.transform, "RingCenter",
            CircleSprite(256), BgSurface, 0.17f, 0.83f, 0.17f, 0.83f);

        // Yüzde ve etiket — card üzerinde, ring'in üstüne
        var percentText = Txt(card, "PercentText", "0%",    60, GuitarCyan,
            Anchor.Center, 0.20f, 0.80f, 0.46f, 0.68f, bold: true);
        Txt(card, "ProgressLabel", "İLERLİYOR", 17, Dim,
            Anchor.Center, 0.20f, 0.80f, 0.41f, 0.46f);

        // Bilgi çubuğu
        var infoBar = Panel("InfoBar", card.transform, BgCard,
            0.06f, 0.94f, 0.27f, 0.37f);
        var luxText = Txt(infoBar, "LuxText",    "Lux: --",           19, TextGray, Anchor.Left,  0.04f, 0.48f, 0.10f, 0.90f);
        var statTxt = Txt(infoBar, "StatusText", "Durum: Bekliyor",   19, TextGray, Anchor.Right, 0.52f, 0.96f, 0.10f, 0.90f);

        // Butonlar
        var retryGO = Panel("RetryButton", card.transform, Passive, 0.06f, 0.45f, 0.08f, 0.22f);
        var retryBtn = retryGO.AddComponent<Button>();
        retryBtn.transition = Selectable.Transition.None;
        Txt(retryGO, "Label", "TEKRAR DENE",    19, TextGray, Anchor.Center, 0f, 1f, 0f, 1f, bold: true);

        var contBtn = GlowBtn(card.transform, "ContinueButton", "DEVAM  >",
            GuitarCyan, 0.55f, 0.94f, 0.08f, 0.22f);
        contBtn.onClick.AddListener(sm.ShowMainConsole);

        // Bootstrap'in CalibrationRoutine'i bu alanları isim ile bulur
        // stepText → "CalibrationCard/StepText"
        // percentText → "CalibrationCard/PercentText"
        // luxText → "CalibrationCard/InfoBar/LuxText"
        // statTxt → "CalibrationCard/InfoBar/StatusText"
        // ringImg → "CalibrationCard/RingContainer/ProgressRing"
        retryBtn.onClick.AddListener(sm.ShowCalibration); // yeniden başlat

        return root;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MAIN CONSOLE  — panel sınırı yok, elemanlar serbest yüzer
    // ═════════════════════════════════════════════════════════════════════════
    private GameObject MainConsoleScreen(Transform parent)
    {
        var root = FullScreen("MainConsoleScreen", parent, BgDeep);

        // TopBar ince ve kompakt — ekrani yemiyor
        var topBar = Panel("TopBar", root.transform, Hex("#060C16"),
            0f, 1f, 0.905f, 1f);
        Txt(topBar, "TitleA", "VIBE", 30, TextWhite, Anchor.Left,   0.05f, 0.28f, 0.1f, 0.9f, bold:true);
        Txt(topBar, "TitleB", "BEAT", 30, GuitarCyan, Anchor.Left,  0.26f, 0.50f, 0.1f, 0.9f, bold:true);
        Txt(topBar, "TitleC", "CONSOLE",24,Hex("#8855FF"),Anchor.Left,0.50f,0.75f,0.1f,0.9f, bold:true);

        // Settings butonu
        var settBtn = Panel("SettingsButton", topBar.transform, Color.clear,
            0.88f, 0.98f, 0.05f, 0.95f);
        settBtn.AddComponent<Button>().transition = Selectable.Transition.None;
        Txt(settBtn, "Icon", "[S]", 28, TextGray, Anchor.Center, 0f,1f,0f,1f);

        // Aktif dot
        Panel("ActiveDot", topBar.transform, Hex("#00FF88"),
            0.955f, 0.975f, 0.35f, 0.65f)
            .GetComponent<Image>().sprite = CircleSprite(16);

        // ── GUTAR PANEL (sol) ────────────────────────────────────────────
        var gPanel = Panel("GuitarPanel", root.transform, BgSurface,
            0.008f, 0.300f, 0.025f, 0.898f);

        // Baslik
        Txt(gPanel, "TitleText", "GUITAR", 36, GuitarCyan,
            Anchor.Center, 0.05f, 0.95f, 0.88f, 0.97f, bold:true);

        // Sensor label + deger
        Txt(gPanel, "SensorLabel",    "SENSOR SEVIYESI", 17, TextGray,
            Anchor.Center, 0.05f, 0.95f, 0.81f, 0.88f);
        Txt(gPanel, "SensorValueText","0.00", 56, TextWhite,
            Anchor.Center, 0.05f, 0.95f, 0.68f, 0.81f, bold:true);

        // Skala cizgileri — solda
        string[] scaleLabels = {"- 1.0", "", "- 0.5", "", "- 0.0"};
        for (int s = 0; s < 5; s++)
        {
            float sy = 0.63f - s * 0.088f;
            // Cizgi
            Panel("ScaleLine_"+s, gPanel.transform, Hex("#1A3348"),
                0.02f, 0.14f, sy, sy + 0.003f);
            if (scaleLabels[s] != "")
                Txt(gPanel, "ScaleLbl_"+s, scaleLabels[s], 15, Hex("#4A7090"),
                    Anchor.Left, 0.02f, 0.38f, sy-0.025f, sy+0.035f);
        }

        // Dalga alani
        Panel("WaveformArea", gPanel.transform, Hex("#07101A"),
            0.15f, 0.95f, 0.27f, 0.65f);

        // Butonlar
        var calBtn = GlowBtn(gPanel.transform, "CalibrateButton", "KALIBRE ET",
            GuitarCyan, 0.07f, 0.93f, 0.13f, 0.24f);
        calBtn.onClick.AddListener(sm.ShowCalibration);

        var muteGO = Panel("MuteButton", gPanel.transform, Passive,
            0.07f, 0.93f, 0.01f, 0.12f);
        muteGO.AddComponent<Button>().transition = Selectable.Transition.None;
        Txt(muteGO, "Label", "MUTE", 22, TextGray, Anchor.Center, 0f,1f,0f,1f, bold:true);

        // Cyan glow separator
        Panel("VSep1", root.transform, Hex("#00E5FF30"), 0.298f, 0.302f, 0.025f, 0.898f);
        Panel("VSep2", root.transform, Hex("#00E5FF0A"), 0.294f, 0.308f, 0.025f, 0.898f);

        // ── SAG PANEL ────────────────────────────────────────────────────
        var right = EmptyRect("RightPanel", root.transform,
            0.308f, 0.992f, 0.025f, 0.898f);

        // ── PIANO (ust %47) ───────────────────────────────────────────────
        var pPanel = Panel("PianoPanel", right.transform, BgSurface,
            0f, 1f, 0.535f, 1f);

        // Baslik satiri
        Txt(pPanel, "TitleText", "PIANO", 32, PianoOrange,
            Anchor.Left, 0.03f, 0.55f, 0.84f, 0.98f, bold:true);

        string[] notes      = {"C4","D4","E4","F4"};
        string[] noteLabels = {"Do","Re","Mi","Fa"};
        Color[]  noteColors = {
            Hex("#FF9500"), Hex("#FFD700"), Hex("#00BFFF"), Hex("#C060FF")
        };

        for (int i = 0; i < 4; i++)
        {
            float kx0 = 0.012f + i * 0.247f;
            float kx1 = kx0 + 0.232f;
            Color nc  = noteColors[i];

            var key = Panel("PianoKey_"+notes[i], pPanel.transform,
                BgCard, kx0, kx1, 0.06f, 0.81f);
            key.AddComponent<Button>().transition = Selectable.Transition.None;

            // Renkli ust sinir
            Panel("TopBorder", key.transform,
                new Color(nc.r, nc.g, nc.b, 0.8f), 0f, 1f, 0.955f, 1f);

            // BgImage slotu
            var bgi = EmptyRect("BgImage", key.transform, 0f,1f,0f,1f);
            var bgiC = bgi.AddComponent<Image>();
            bgiC.color = new Color(1f,1f,1f,0.12f);
            bgiC.preserveAspect = true;
            bgiC.raycastTarget  = false;

            // Ince orta cizgi
            Panel("Line", key.transform,
                new Color(nc.r, nc.g, nc.b, 0.15f), 0.44f, 0.56f, 0.15f, 0.85f);

            // Not ismi — altta, renkli
            Txt(key, "NoteLabel", noteLabels[i], 24,
                new Color(nc.r, nc.g, nc.b, 1f),
                Anchor.Center, 0f,1f, 0.02f, 0.22f, bold:true);
        }

        // Ayirici
        Panel("HSep", right.transform, Hex("#FFFFFF08"), 0f,1f, 0.528f, 0.534f);

        // ── DRUM (alt %52) ─────────────────────────────────────────────────
        var dPanel = Panel("DrumPanel", right.transform, BgSurface,
            0f, 1f, 0f, 0.522f);

        // Baslik — kompakt
        Txt(dPanel, "TitleText", "DRUM", 32, DrumMagenta,
            Anchor.Left, 0.03f, 0.45f, 0.84f, 0.97f, bold:true);

        // DrumPad — alt %83 (baslik icin yer birak)
        var padGO = Panel("DrumPad", dPanel.transform, BgCard,
            0.01f, 0.99f, 0.01f, 0.82f);
        padGO.AddComponent<Button>().transition = Selectable.Transition.None;

        // Magenta ust sinir
        Panel("TopBorder", padGO.transform,
            new Color(DrumMagenta.r, DrumMagenta.g, DrumMagenta.b, 0.7f),
            0f, 1f, 0.97f, 1f);

        // BgImage slotu
        var dbi = EmptyRect("BgImage", padGO.transform, 0f,1f,0f,1f);
        var dbiC = dbi.AddComponent<Image>();
        dbiC.color = new Color(1f,1f,1f,0.10f);
        dbiC.preserveAspect = true;
        dbiC.raycastTarget  = false;

        // Elliptik halka dekorasyonu — referanstaki gibi yatay oval
        var rh = EmptyRect("RingHolder", padGO.transform, 0.5f,0.5f,0.5f,0.5f);
        var rhRT = rh.GetComponent<RectTransform>();
        rhRT.sizeDelta = new Vector2(300f, 130f);

        float[] ringScales = {1.00f, 0.72f, 0.48f, 0.28f, 0.14f};
        float[] ringAlphas = {0.05f, 0.10f, 0.20f, 0.38f, 0.70f};
        for (int i = 0; i < ringScales.Length; i++)
        {
            float h = ringScales[i] * 0.5f;
            var ring  = new GameObject("Ring_"+i);
            ring.transform.SetParent(rh.transform, false);
            var rRT = ring.AddComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0.5f-h, 0.5f-h);
            rRT.anchorMax = new Vector2(0.5f+h, 0.5f+h);
            rRT.offsetMin = rRT.offsetMax = Vector2.zero;
            var ri = ring.AddComponent<Image>();
            ri.sprite = CircleSprite(128);
            ri.color  = new Color(DrumMagenta.r, DrumMagenta.g, DrumMagenta.b, ringAlphas[i]);
            ri.raycastTarget = false;
        }

        // Parlak merkez nokta
        var dot = Panel("CenterDot", padGO.transform, Color.white,
            0.490f, 0.510f, 0.430f, 0.570f);
        dot.GetComponent<Image>().sprite = CircleSprite(32);
        dot.GetComponent<Image>().color  = new Color(1f, 0.3f, 0.7f, 1f);

        return root;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // AYARLAR
    // ═════════════════════════════════════════════════════════════════════════
    private GameObject SettingsScreen(Transform parent)
    {
        var root = FullScreen("SettingsScreen", parent, BgDeep);
        TopBar(root.transform, settings: false, back: true, dots: false);

        Txt(root, "SettingsTitle", "AYARLAR", 56, TextWhite,
            Anchor.Left, 0.06f, 0.70f, 0.80f, 0.92f, bold: true);

        var panel = Panel("SettingsPanel", root.transform, BgSurface,
            0.05f, 0.95f, 0.15f, 0.78f);

        // Row 1: Tekrar kalibre
        SettingsNav(panel.transform, "RecalibrateRow", "+", "TEKRAR KALIBRE ET", 0.83f, 0.96f)
            .onClick.AddListener(sm.ShowCalibration);

        // Row 2: Haptic toggle
        HapticRow(panel.transform, 0.64f, 0.78f);

        // Row 3: Efekt yoğunluğu
        EffectRow(panel.transform, 0.44f, 0.58f);

        // Row 4: Ses seviyesi
        VolumeRow(panel.transform, 0.24f, 0.38f);

        // Row 5: Hakkında
        SettingsNav(panel.transform, "AboutRow", "i", "HAKKINDA", 0.04f, 0.18f);

        var backBtn = GlowBtn(root.transform, "BackToMainButton",
            "ANA EKRANA DON", GuitarCyan, 0.30f, 0.70f, 0.03f, 0.13f);
        backBtn.onClick.AddListener(sm.ShowMainConsole);

        return root;
    }

    private Button SettingsNav(Transform parent, string name,
        string icon, string label, float y0, float y1)
    {
        var row = Panel(name, parent, BgCard, 0.012f, 0.988f, y0, y1);
        var btn = row.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        Txt(row, "Icon",  icon,  26, GuitarCyan, Anchor.Center, 0.02f, 0.09f, 0.10f, 0.90f);
        Txt(row, "Label", label, 22, TextGray,   Anchor.Left,   0.11f, 0.87f, 0.10f, 0.90f, bold: true);
        Txt(row, "Arrow", ">",   30, Dim,         Anchor.Right,  0.90f, 0.99f, 0.10f, 0.90f);
        Panel("Sep", row.transform, Hex("#FFFFFF08"), 0f, 1f, 0f, 0.03f);
        return btn;
    }

    private void HapticRow(Transform parent, float y0, float y1)
    {
        var row = Panel("HapticRow", parent, BgCard, 0.012f, 0.988f, y0, y1);
        var btn = row.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        Txt(row, "Icon",  ">",              26, GuitarCyan, Anchor.Center, 0.02f, 0.09f, 0.10f, 0.90f);
        Txt(row, "Label", "HAPTIC FEEDBACK", 22, TextGray,  Anchor.Left,   0.11f, 0.65f, 0.10f, 0.90f, bold: true);

        // Toggle görsel (Bootstrap bunları isme göre bulur)
        var track = Panel("ToggleTrack", row.transform, GuitarCyan,
            0.71f, 0.87f, 0.22f, 0.78f);
        Panel("ToggleHandle", track.transform, Color.white,
            0.52f, 0.96f, 0.08f, 0.92f);

        Txt(row, "HapticStatusText", "ACIK", 20, GuitarCyan,
            Anchor.Right, 0.87f, 0.99f, 0.20f, 0.80f, bold: true);
        Panel("Sep", row.transform, Hex("#FFFFFF08"), 0f, 1f, 0f, 0.03f);
    }

    private void EffectRow(Transform parent, float y0, float y1)
    {
        var row = Panel("EffectIntensityRow", parent, BgCard, 0.012f, 0.988f, y0, y1);
        Txt(row, "Icon",  "#",               26, GuitarCyan, Anchor.Center, 0.02f, 0.09f, 0.10f, 0.90f);
        Txt(row, "Label", "EFEKT YOGUNLUGU", 22, TextGray,  Anchor.Left,   0.11f, 0.46f, 0.10f, 0.90f, bold: true);

        string[] names  = { "EffectBtn_Low", "EffectBtn_Mid", "EffectBtn_High" };
        string[] labels = { "DUSUK",          "ORTA",          "YUKSEK" };
        float[]  xs     = { 0.47f, 0.63f, 0.79f };
        for (int i = 0; i < 3; i++)
        {
            bool active = i == 1; // başlangıçta ORTA aktif
            var bg = active
                ? new Color(GuitarCyan.r, GuitarCyan.g, GuitarCyan.b, 0.18f)
                : Passive;
            var b = Panel(names[i], row.transform, bg, xs[i], xs[i] + 0.13f, 0.15f, 0.85f);
            var bt = b.AddComponent<Button>();
            bt.transition = Selectable.Transition.None;
            Txt(b, "Label", labels[i], 18, active ? GuitarCyan : TextGray,
                Anchor.Center, 0f, 1f, 0f, 1f, bold: true);
        }
        Panel("Sep", row.transform, Hex("#FFFFFF08"), 0f, 1f, 0f, 0.03f);
    }

    private void VolumeRow(Transform parent, float y0, float y1)
    {
        var row = Panel("VolumeRow", parent, BgCard, 0.012f, 0.988f, y0, y1);
        Txt(row, "Icon",  "VOL",         26, GuitarCyan, Anchor.Center, 0.02f, 0.09f, 0.10f, 0.90f);
        Txt(row, "Label", "SES SEVIYESI",22, TextGray,  Anchor.Left,   0.11f, 0.39f, 0.10f, 0.90f, bold: true);

        // Slider
        var slGO = new GameObject("VolumeSlider");
        slGO.transform.SetParent(row.transform, false);
        var slRT = slGO.AddComponent<RectTransform>();
        slRT.anchorMin = new Vector2(0.39f, 0.20f);
        slRT.anchorMax = new Vector2(0.88f, 0.80f);
        slRT.offsetMin = slRT.offsetMax = Vector2.zero;
        var sl = slGO.AddComponent<Slider>();
        sl.minValue = 0f; sl.maxValue = 1f; sl.value = 0.7f;
        sl.direction = Slider.Direction.LeftToRight;

        var bgGO  = Child("Background", slGO.transform, Vector2.zero, Vector2.one);
        bgGO.AddComponent<Image>().color = Passive;

        var faGO  = Child("Fill Area", slGO.transform,
            new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
        faGO.GetComponent<RectTransform>().offsetMin = new Vector2(4, 0);
        faGO.GetComponent<RectTransform>().offsetMax = new Vector2(-4, 0);
        var fillGO = Child("Fill", faGO.transform, Vector2.zero, Vector2.one);
        fillGO.AddComponent<Image>().color = GuitarCyan;

        var hsGO   = Child("Handle Slide Area", slGO.transform, Vector2.zero, Vector2.one);
        var hGO    = new GameObject("Handle");
        hGO.transform.SetParent(hsGO.transform, false);
        var hRT    = hGO.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.7f, 0f);
        hRT.anchorMax = new Vector2(0.7f, 1f);
        hRT.sizeDelta = new Vector2(16f, 0f);
        var hImg   = hGO.AddComponent<Image>();
        hImg.color = Color.white;

        sl.fillRect     = fillGO.GetComponent<RectTransform>();
        sl.handleRect   = hGO.GetComponent<RectTransform>();
        sl.targetGraphic = hImg;

        Txt(row, "VolumeValueText", "70%", 22, TextGray,
            Anchor.Right, 0.88f, 0.98f, 0.10f, 0.90f);
        Panel("Sep", row.transform, Hex("#FFFFFF08"), 0f, 1f, 0f, 0.03f);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TOP BAR
    // ═════════════════════════════════════════════════════════════════════════
    private void TopBar(Transform parent, bool settings, bool back, bool dots)
    {
        var bar = Panel("TopBar", parent, BgSurface, 0f, 1f, 0.91f, 1f);

        Txt(bar, "MenuIcon", "MENU", 32, GuitarCyan, Anchor.Center,
            0.01f, 0.06f, 0.10f, 0.90f);

        // Logo: VIBE8EAT CONSOLE
        Txt(bar, "LogoVIBE",    "VIBE",    26, TextWhite,   Anchor.Right,  0.07f, 0.175f, 0.15f, 0.85f, bold: true);
        Txt(bar, "Logo8",       "8",       26, GuitarCyan,  Anchor.Center, 0.172f,0.210f, 0.15f, 0.85f, bold: true);
        Txt(bar, "LogoEAT",     "EAT",     26, TextWhite,   Anchor.Left,   0.207f,0.275f, 0.15f, 0.85f, bold: true);
        Txt(bar, "LogoCONSOLE", "CONSOLE", 26, GuitarCyan,  Anchor.Left,   0.282f,0.455f, 0.15f, 0.85f, bold: true);

        if (dots)
        {
            Txt(bar, "GDot",   "*", 14, GuitarCyan,  Anchor.Center, 0.50f, 0.53f, 0.20f, 0.80f);
            Txt(bar, "GLabel", "GUITAR", 14, TextGray,Anchor.Left,  0.53f, 0.61f, 0.20f, 0.80f);
            Txt(bar, "PDot",   "*", 14, PianoOrange, Anchor.Center, 0.62f, 0.65f, 0.20f, 0.80f);
            Txt(bar, "PLabel", "PIANO",  14, TextGray,Anchor.Left,  0.65f, 0.73f, 0.20f, 0.80f);
            Txt(bar, "DDot",   "*", 14, DrumMagenta, Anchor.Center, 0.74f, 0.77f, 0.20f, 0.80f);
            Txt(bar, "DLabel", "DRUM",   14, TextGray,Anchor.Left,  0.77f, 0.84f, 0.20f, 0.80f);
        }

        if (settings)
        {
            var sg = Panel("SettingsButton", bar.transform, Passive, 0.90f, 0.975f, 0.10f, 0.90f);
            var sb = sg.AddComponent<Button>();
            sb.transition = Selectable.Transition.None;
            Txt(sg, "Label", "SET", 24, TextGray, Anchor.Center, 0f, 1f, 0f, 1f);
            sb.onClick.AddListener(sm.ShowSettings);
            Panel("StatusDot", bar.transform, Hex("#00FF88"), 0.978f, 0.997f, 0.45f, 0.75f);
        }

        if (back)
        {
            var bg = Panel("BackButton", bar.transform, Passive, 0.90f, 0.975f, 0.10f, 0.90f);
            var bb = bg.AddComponent<Button>();
            bb.transition = Selectable.Transition.None;
            Txt(bg, "Label", "<", 26, GuitarCyan, Anchor.Center, 0f, 1f, 0f, 1f);
            bb.onClick.AddListener(sm.ShowMainConsole);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // YARDIMCI — Sprite üretimi
    // ═════════════════════════════════════════════════════════════════════════
    private static Sprite CircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px  = new Color[size * size];
        float c = size * 0.5f, r = c - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x+.5f-c)*(x+.5f-c)+(y+.5f-c)*(y+.5f-c));
                px[y*size+x] = new Color(1,1,1, Mathf.Clamp01((r-d)/1.5f));
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(.5f,.5f));
    }

    private static Sprite RingSprite(int size, float outerR, float innerR)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px  = new Color[size * size];
        float c = size*.5f, o = outerR*c, i = innerR*c;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x+.5f-c)*(x+.5f-c)+(y+.5f-c)*(y+.5f-c));
                float a = (d>=i && d<=o)
                    ? Mathf.Min(Mathf.Clamp01((o-d)/1.8f), Mathf.Clamp01((d-i)/1.8f))
                    : 0f;
                px[y*size+x] = new Color(1,1,1,a);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(.5f,.5f));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // YARDIMCI — UI Primitifleri
    // ═════════════════════════════════════════════════════════════════════════
    private enum Anchor { Left, Center, Right }

    private Button GlowBtn(Transform parent, string name, string label,
        Color glow, float x0, float x1, float y0, float y1)
    {
        var go = R(name, parent, x0, x1, y0, y1);
        go.AddComponent<Image>().color = BgSurface;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        var border = Child("Border", go.transform, Vector2.zero, Vector2.one);
        border.AddComponent<Image>().color =
            new Color(glow.r, glow.g, glow.b, 0.65f);
        border.GetComponent<Image>().raycastTarget = false;

        var inner = new GameObject("Inner");
        inner.transform.SetParent(go.transform, false);
        var iRT = inner.AddComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
        iRT.offsetMin = new Vector2(1.5f,1.5f);
        iRT.offsetMax = new Vector2(-1.5f,-1.5f);
        inner.AddComponent<Image>().color = BgSurface;
        inner.GetComponent<Image>().raycastTarget = false;

        var t = Txt(go, "Label", label, 24, glow, Anchor.Center, 0f, 1f, 0f, 1f, bold: true);
        t.raycastTarget = false;
        return btn;
    }

    private GameObject RingLayer(Transform parent, string name,
        Sprite spr, Color col, float x0, float x1, float y0, float y1)
    {
        var go = Child(name, parent, new Vector2(x0,y0), new Vector2(x1,y1));
        var img = go.AddComponent<Image>();
        img.sprite = spr; img.color = col; img.raycastTarget = false;
        return go;
    }

    private TextMeshProUGUI Txt(GameObject parent, string name, string text,
        float size, Color col, Anchor align,
        float x0, float x1, float y0, float y1, bool bold = false)
    {
        var go  = R(name, parent.transform, x0, x1, y0, y1);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text    = text; tmp.fontSize = size; tmp.color = col;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = align == Anchor.Left   ? TextAlignmentOptions.Left
                      : align == Anchor.Right  ? TextAlignmentOptions.Right
                      :                          TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private GameObject Panel(string name, Transform parent, Color col,
        float x0, float x1, float y0, float y1)
    {
        var go = R(name, parent, x0, x1, y0, y1);
        go.AddComponent<Image>().color = col;
        return go;
    }

    private GameObject EmptyRect(string name, Transform parent,
        float x0, float x1, float y0, float y1)
        => R(name, parent, x0, x1, y0, y1);

    private GameObject FullScreen(string name, Transform parent, Color col)
    {
        var go = R(name, parent, 0f, 1f, 0f, 1f);
        go.AddComponent<Image>().color = col;
        return go;
    }

    private GameObject Child(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private GameObject R(string name, Transform parent,
        float x0, float x1, float y0, float y1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Canvas / EventSystem kurulum
    // ═════════════════════════════════════════════════════════════════════════
    private GameObject MakeCanvas()
    {
        var go = new GameObject(CanvasName);
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(2400f, 1080f);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private void EnsureEventSystem()
    {
        var es = FindFirstObjectByType<EventSystem>();
        if (es != null) { PatchInputModule(es.gameObject); return; }
        var g = new GameObject("EventSystem");
        g.AddComponent<EventSystem>();
        PatchInputModule(g);
    }

    private void PatchInputModule(GameObject g)
    {
#if ENABLE_INPUT_SYSTEM
        if (!g.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>())
            g.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var old = g.GetComponent<StandaloneInputModule>();
        if (old) DestroyImmediate(old);
#else
        if (!g.GetComponent<StandaloneInputModule>())
            g.AddComponent<StandaloneInputModule>();
#endif
    }

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c); return c;
    }
}
