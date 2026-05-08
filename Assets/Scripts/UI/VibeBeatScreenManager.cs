using UnityEngine;

/// <summary>
/// Ekran geçişlerini yönetir.
/// Yalnızca UI görünürlüğünden sorumludur — ses ve sensör mantığına dokunmaz.
/// Her geçişte AccessibilityManager üzerinden sesli duyuru yapar.
/// </summary>
public class VibeBeatScreenManager : MonoBehaviour
{
    [Header("Ekranlar")]
    public GameObject splashScreen;
    public GameObject onboardingScreen;
    public GameObject calibrationScreen;
    public GameObject mainConsoleScreen;
    public GameObject settingsScreen;

    // ─────────────────────────────────────────
    // GEÇIŞ METODLARI
    // ─────────────────────────────────────────
    public void ShowSplash()
    {
        ShowOnly(splashScreen);
        AccessibilityManager.Instance?.AnnounceScreenChange("VibBeat Console. Başlamak için ekrana dokunun.");
    }

    public void ShowOnboarding()
    {
        ShowOnly(onboardingScreen);
        AccessibilityManager.Instance?.AnnounceScreenChange(
            "Nasıl kullanılır? Üç bölge var: Gitar, Piyano ve Davul.");
    }

    public void ShowCalibration()
    {
        ShowOnly(calibrationScreen);
        AccessibilityManager.Instance?.AnnounceScreenChange(
            "Kalibrasyon. Sol elinizi telefon ışık sensörünün üzerine kapatın.");
    }

    public void ShowMainConsole()
    {
        ShowOnly(mainConsoleScreen);
        AccessibilityManager.Instance?.AnnounceScreenChange(
            "Müzik konsolu. Sol bölge gitar, sağ üst piyano, sağ alt davul.");
    }

    public void ShowSettings()
    {
        ShowOnly(settingsScreen);
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
}
