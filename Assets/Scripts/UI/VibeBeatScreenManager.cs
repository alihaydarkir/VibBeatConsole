using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Ekran geçişlerini yönetir.
/// DOTween ile fade + hafif slide animasyonu kullanır.
/// MainConsole → Settings: sağdan kayar
/// Settings → MainConsole: soldan kayar
/// Diğer geçişler: fade (karartıp açılır)
/// </summary>
public class VibeBeatScreenManager : MonoBehaviour
{
    [HideInInspector] public GameObject splashScreen;
    [HideInInspector] public GameObject onboardingScreen;
    [HideInInspector] public GameObject calibrationScreen;
    [HideInInspector] public GameObject mainConsoleScreen;
    [HideInInspector] public GameObject settingsScreen;

    [Header("Geçiş Ayarları")]
    [Tooltip("Fade süresi (saniye)")]
    [SerializeField] private float fadeDuration  = 0.35f;
    [Tooltip("Slide mesafesi (piksel) — sağ/sol kayma")]
    [SerializeField] private float slideDistance = 400f;
    [Tooltip("Geçiş sırasında tüm ekranı karartacak overlay")]
    private Image overlayImage;

    private bool initialized  = false;
    private bool transitioning = false;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Awake()
    {
        CreateOverlay();
    }

    private void CreateOverlay()
    {
        // Canvas'ın en üstüne siyah overlay ekle — geçişlerde kullanılır
        var go = new GameObject("TransitionOverlay");
        go.transform.SetParent(transform, false);

        overlayImage = go.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.transform.SetAsLastSibling(); // her zaman en üstte
    }

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

        // Overlay en üstte kalsın
        if (overlayImage != null)
            overlayImage.transform.SetAsLastSibling();

        Debug.Log($"[SCREEN_MGR] [OK] Init tamamlandı.");
    }

    // ─────────────────────────────────────────
    // GEÇİŞ METODLARI
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        if (!CheckInit()) return;
        FadeTransition(splashScreen, "VibBeat Console. Başlamak için ekrana dokunun.");
    }

    public void ShowOnboarding()
    {
        if (!CheckInit()) return;
        SlideTransition(onboardingScreen, fromRight: true,
            "Nasıl kullanılır? Üç bölge var: Gitar, Piyano ve Davul.");
    }

    public void ShowCalibration()
    {
        if (!CheckInit()) return;
        FadeTransition(calibrationScreen,
            "Kalibrasyon. Sol elinizi telefon ışık sensörünün üzerine kapatın.");
    }

    public void ShowMainConsole()
    {
        if (!CheckInit()) return;
        // Settings'ten ana ekrana dönüş → soldan kayar
        bool fromSettings = settingsScreen != null && settingsScreen.activeSelf;
        if (fromSettings)
            SlideTransition(mainConsoleScreen, fromRight: false,
                "Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
        else
            FadeTransition(mainConsoleScreen,
                "Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
    }

    public void ShowSettings()
    {
        if (!CheckInit()) return;
        // Ana ekrandan settings → sağdan kayar
        SlideTransition(settingsScreen, fromRight: true, "Ayarlar.");
    }

    // ─────────────────────────────────────────
    // ANİMASYON METODLARI
    // ─────────────────────────────────────────

    /// <summary>
    /// Siyah overlay fade out → yeni ekran → overlay fade in.
    /// Genel geçiş için kullanılır.
    /// </summary>
    private void FadeTransition(GameObject target, string announcement)
    {
        if (transitioning) { QuickSwitch(target, announcement); return; }
        transitioning = true;

        Debug.Log($"[SCREEN_MGR] → {target?.name} (fade)");

        // Overlay karart
        overlayImage.raycastTarget = true;
        overlayImage.DOFade(1f, fadeDuration * 0.5f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                // Ekranı değiştir
                ShowOnly(target);

                // Overlay aç
                overlayImage.DOFade(0f, fadeDuration * 0.7f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        overlayImage.raycastTarget = false;
                        transitioning = false;
                        AccessibilityManager.Instance?.AnnounceScreenChange(announcement);
                    });
            });
    }

    /// <summary>
    /// Yeni ekran sağdan veya soldan kayarak girer.
    /// Settings↔MainConsole geçişleri için kullanılır.
    /// </summary>
    private void SlideTransition(GameObject target, bool fromRight, string announcement)
    {
        if (transitioning) { QuickSwitch(target, announcement); return; }
        transitioning = true;

        Debug.Log($"[SCREEN_MGR] → {target?.name} (slide {(fromRight ? "sağdan" : "soldan")})");

        // Hedef ekranı pozisyonla
        var targetRT = target?.GetComponent<RectTransform>();
        if (targetRT == null) { FadeTransition(target, announcement); return; }

        float screenW   = Screen.width;
        float startX    = fromRight ? slideDistance : -slideDistance;

        ShowOnly(target);
        targetRT.anchoredPosition = new Vector2(startX, 0f);

        // Hafif fade ile birlikte slide
        var canvasGroup = target.GetComponent<CanvasGroup>()
                       ?? target.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.3f;

        overlayImage.raycastTarget = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(targetRT.DOAnchorPosX(0f, fadeDuration)
                            .SetEase(Ease.OutCubic));
        seq.Join(canvasGroup.DOFade(1f, fadeDuration)
                             .SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            overlayImage.raycastTarget = false;
            transitioning = false;
            AccessibilityManager.Instance?.AnnounceScreenChange(announcement);
        });
    }

    /// <summary>Geçiş devam ederken çağrılırsa anında değiş.</summary>
    private void QuickSwitch(GameObject target, string announcement)
    {
        DOTween.Kill(overlayImage);
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false;
        ShowOnly(target);
        transitioning = false;
        Debug.Log($"[SCREEN_MGR] → {target?.name} (hızlı)");
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
            Debug.LogError("[SCREEN_MGR] [HATA] Init() henüz çağrılmadı!");
        return initialized;
    }
}
