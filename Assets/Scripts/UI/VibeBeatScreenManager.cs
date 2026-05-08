using UnityEngine;

/// <summary>
/// Ekran geçişlerini yönetir.
/// Ekran referansları Bootstrap tarafından Init() ile beslenir —
/// Inspector'dan elle atama gerekmez.
/// </summary>
public class VibeBeatScreenManager : MonoBehaviour
{
    // Ekranlar Bootstrap.Awake() içinde Init() ile set edilir
    [HideInInspector] public GameObject splashScreen;
    [HideInInspector] public GameObject onboardingScreen;
    [HideInInspector] public GameObject calibrationScreen;
    [HideInInspector] public GameObject mainConsoleScreen;
    [HideInInspector] public GameObject settingsScreen;

    private bool initialized = false;

    /// <summary>Bootstrap tarafından çağrılır — ekranları bağlar.</summary>
    public void Init(
        GameObject splash, GameObject onboard, GameObject calib,
        GameObject main,   GameObject settings)
    {
        splashScreen      = splash;
        onboardingScreen  = onboard;
        calibrationScreen = calib;
        mainConsoleScreen = main;
        settingsScreen    = settings;
        initialized       = true;

        Debug.Log($"[SCREEN_MGR] [OK] Init tamamlandı. " +
            $"Splash:{splash != null} Main:{main != null} Settings:{settings != null}");
    }

    // ─────────────────────────────────────────
    // GEÇİŞ METODLARI
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        if (!CheckInit()) return;
        ShowOnly(splashScreen);
        Debug.Log("[SCREEN_MGR] → SplashScreen");
        AccessibilityManager.Instance?.AnnounceScreenChange("VibBeat Console. Başlamak için ekrana dokunun.");
    }

    public void ShowOnboarding()
    {
        if (!CheckInit()) return;
        ShowOnly(onboardingScreen);
        Debug.Log("[SCREEN_MGR] → OnboardingScreen");
        AccessibilityManager.Instance?.AnnounceScreenChange("Nasıl kullanılır? Üç bölge var: Gitar, Piyano ve Davul.");
    }

    public void ShowCalibration()
    {
        if (!CheckInit()) return;
        ShowOnly(calibrationScreen);
        Debug.Log("[SCREEN_MGR] → CalibrationScreen");
        AccessibilityManager.Instance?.AnnounceScreenChange("Kalibrasyon. Sol elinizi telefon ışık sensörünün üzerine kapatın.");
    }

    public void ShowMainConsole()
    {
        if (!CheckInit()) return;
        ShowOnly(mainConsoleScreen);
        Debug.Log("[SCREEN_MGR] → MainConsoleScreen");
        AccessibilityManager.Instance?.AnnounceScreenChange("Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
    }

    public void ShowSettings()
    {
        if (!CheckInit()) return;
        ShowOnly(settingsScreen);
        Debug.Log("[SCREEN_MGR] → SettingsScreen");
        AccessibilityManager.Instance?.AnnounceScreenChange("Ayarlar.");
    }

    // ─────────────────────────────────────────
    // YARDIMCI
    // ─────────────────────────────────────────
    public void ShowOnly(GameObject target)
    {
        SetActive(splashScreen,      splashScreen      == target);
        SetActive(onboardingScreen,  onboardingScreen  == target);
        SetActive(calibrationScreen, calibrationScreen == target);
        SetActive(mainConsoleScreen, mainConsoleScreen == target);
        SetActive(settingsScreen,    settingsScreen    == target);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private bool CheckInit()
    {
        if (!initialized)
            Debug.LogError("[SCREEN_MGR] [HATA] Init() henüz çağrılmadı! Bootstrap bağlantısı eksik.");
        return initialized;
    }
}
