using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Ekran geçişlerini yönetir.
/// DOTween ile fade + slide animasyonu.
/// </summary>
public class VibeBeatScreenManager : MonoBehaviour
{
    [HideInInspector] public GameObject splashScreen;
    [HideInInspector] public GameObject onboardingScreen;
    [HideInInspector] public GameObject calibrationScreen;
    [HideInInspector] public GameObject mainConsoleScreen;
    [HideInInspector] public GameObject settingsScreen;

    [Header("Geçiş Ayarları")]
    [SerializeField] private float fadeDuration  = 0.3f;
    [SerializeField] private float slideDistance = 350f;

    private Image   overlayImage;
    private bool    initialized   = false;
    private bool    transitioning = false;
    private bool    overlayReady  = false;

    // ─────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────
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

        // Overlay'i burada yarat — Init'ten sonra Canvas hazır
        if (!overlayReady)
            CreateOverlay();

        Debug.Log("[SCREEN_MGR] [OK] Init tamamlandı.");
    }

    private void CreateOverlay()
    {
        var go  = new GameObject("TransitionOverlay");
        go.transform.SetParent(transform, false);

        overlayImage              = go.AddComponent<Image>();
        overlayImage.color        = Color.clear;  // başlangıçta tamamen şeffaf
        overlayImage.raycastTarget = false;

        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.transform.SetAsLastSibling();
        overlayReady = true;

        Debug.Log("[SCREEN_MGR] Overlay yaratıldı.");
    }

    // ─────────────────────────────────────────
    // GEÇİŞ METODLARI
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        if (!CheckInit()) return;
        DoFade(splashScreen, "VibBeat Console. Başlamak için ekrana dokunun.");
    }

    public void ShowOnboarding()
    {
        if (!CheckInit()) return;
        DoSlide(onboardingScreen, fromRight: true,
            "Nasıl kullanılır? Üç bölge var: Gitar, Piyano ve Davul.");
    }

    public void ShowCalibration()
    {
        if (!CheckInit()) return;
        DoFade(calibrationScreen,
            "Kalibrasyon. Sol elinizi telefon ışık sensörünün üzerine kapatın.");
    }

    public void ShowMainConsole()
    {
        if (!CheckInit()) return;
        bool fromSettings = settingsScreen != null && settingsScreen.activeSelf;
        if (fromSettings)
            DoSlide(mainConsoleScreen, fromRight: false,
                "Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
        else
            DoFade(mainConsoleScreen,
                "Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
    }

    public void ShowSettings()
    {
        if (!CheckInit()) return;
        DoSlide(settingsScreen, fromRight: true, "Ayarlar.");
    }

    // ─────────────────────────────────────────
    // FADE — siyah overlay ile
    // ─────────────────────────────────────────
    private void DoFade(GameObject target, string speech)
    {
        if (target == null) return;

        // Geçiş sırasında tekrar basılırsa anında geç
        if (transitioning)
        {
            Instant(target, speech);
            return;
        }

        // Overlay yoksa (olmamalı ama güvenlik) anında geç
        if (overlayImage == null)
        {
            Instant(target, speech);
            return;
        }

        transitioning = true;
        overlayImage.raycastTarget = true;
        overlayImage.color = Color.clear;

        Debug.Log($"[SCREEN_MGR] → {target.name} (fade)");

        overlayImage.DOFade(1f, fadeDuration * 0.4f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                ShowOnly(target);

                overlayImage.DOFade(0f, fadeDuration * 0.6f)
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
    // SLIDE — sağdan veya soldan kayma
    // ─────────────────────────────────────────
    private void DoSlide(GameObject target, bool fromRight, string speech)
    {
        if (target == null) return;

        if (transitioning)
        {
            Instant(target, speech);
            return;
        }

        var targetRT = target.GetComponent<RectTransform>();
        if (targetRT == null)
        {
            DoFade(target, speech);
            return;
        }

        transitioning = true;
        Debug.Log($"[SCREEN_MGR] → {target.name} (slide {(fromRight ? "sağ" : "sol")})");

        // Önce ekranı göster, sonra pozisyonla
        ShowOnly(target);
        float startX = fromRight ? slideDistance : -slideDistance;
        targetRT.anchoredPosition = new Vector2(startX, 0f);

        // CanvasGroup ile fade de ekle
        var cg = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        if (overlayImage != null)
            overlayImage.raycastTarget = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(targetRT.DOAnchorPosX(0f, fadeDuration).SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(1f, fadeDuration * 0.8f).SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            if (overlayImage != null)
                overlayImage.raycastTarget = false;
            transitioning = false;
            AccessibilityManager.Instance?.AnnounceScreenChange(speech);
        });
    }

    // ─────────────────────────────────────────
    // ANINDA GEÇİŞ (güvenlik fallback)
    // ─────────────────────────────────────────
    private void Instant(GameObject target, string speech)
    {
        if (overlayImage != null)
        {
            DOTween.Kill(overlayImage);
            overlayImage.color = Color.clear;
            overlayImage.raycastTarget = false;
        }
        ShowOnly(target);
        transitioning = false;
        Debug.Log($"[SCREEN_MGR] → {target?.name} (aninda)");
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
