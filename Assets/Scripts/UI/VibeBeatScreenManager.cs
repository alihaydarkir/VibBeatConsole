using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Ekran geçişleri — siyah overlay fade ile.
/// Sade ve garantili çalışır.
/// </summary>
public class VibeBeatScreenManager : MonoBehaviour
{
    [HideInInspector] public GameObject splashScreen;
    [HideInInspector] public GameObject onboardingScreen;
    [HideInInspector] public GameObject calibrationScreen;
    [HideInInspector] public GameObject mainConsoleScreen;
    [HideInInspector] public GameObject settingsScreen;

    [Header("Geçiş Süresi")]
    [SerializeField] private float fadeDuration = 0.4f;

    private Image overlayImage;
    private bool  initialized  = false;
    private bool  transitioning = false;

    // ─────────────────────────────────────────
    public void Init(GameObject splash, GameObject onboard, GameObject calib,
                     GameObject main, GameObject settings)
    {
        splashScreen      = splash;
        onboardingScreen  = onboard;
        calibrationScreen = calib;
        mainConsoleScreen = main;
        settingsScreen    = settings;
        initialized       = true;

        CreateOverlay();
        Debug.Log("[SCREEN_MGR] [OK] Init tamamlandı.");
    }

    private void CreateOverlay()
    {
        var go = new GameObject("TransitionOverlay");
        go.transform.SetParent(transform, false);

        overlayImage = go.AddComponent<Image>();
        overlayImage.color = Color.clear;
        overlayImage.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.transform.SetAsLastSibling();
    }

    // ─────────────────────────────────────────
    // GEÇİŞLER — hepsi aynı fade
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        // İlk acilis — animasyon yok, direkt goster
        if (!CheckInit()) return;
        ShowOnly(splashScreen);
        Debug.Log("[SCREEN_MGR] → SplashScreen (direkt)");
    }
    public void ShowOnboarding()   => Fade(onboardingScreen,  "Nasıl kullanılır? Üç bölge var: Gitar, Piyano ve Davul.");
    public void ShowCalibration()  => Fade(calibrationScreen, "Kalibrasyon. Sol elinizi telefon ışık sensörünün üzerine kapatın.");
    public void ShowMainConsole()  => Fade(mainConsoleScreen, "Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
    public void ShowSettings()     => Fade(settingsScreen,    "Ayarlar.");

    // ─────────────────────────────────────────
    // FADE CORE
    // ─────────────────────────────────────────
    private void Fade(GameObject target, string speech)
    {
        if (!CheckInit() || target == null) return;

        // Geçiş varsa anında geç
        if (transitioning)
        {
            DOTween.Kill(overlayImage);
            overlayImage.color = Color.clear;
            overlayImage.raycastTarget = false;
            ShowOnly(target);
            transitioning = false;
            return;
        }

        transitioning = true;
        overlayImage.raycastTarget = true;
        overlayImage.color = Color.clear;

        Debug.Log($"[SCREEN_MGR] → {target.name}");

        // Karart → ekran değiştir → aç
        overlayImage.DOFade(1f, fadeDuration * 0.45f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                ShowOnly(target);
                overlayImage.DOFade(0f, fadeDuration * 0.55f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        overlayImage.raycastTarget = false;
                        transitioning = false;
                        AccessibilityManager.Instance?.AnnounceScreenChange(speech);
                    });
            });
    }

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
    { if (go != null) go.SetActive(active); }

    private bool CheckInit()
    {
        if (!initialized)
            Debug.LogError("[SCREEN_MGR] [HATA] Init() çağrılmadı!");
        return initialized;
    }
}
