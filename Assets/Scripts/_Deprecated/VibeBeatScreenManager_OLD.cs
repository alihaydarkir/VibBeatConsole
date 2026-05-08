using UnityEngine;

public class VibeBeatScreenManager : MonoBehaviour
{
    public GameObject splashScreen;
    public GameObject onboardingScreen;
    public GameObject calibrationScreen;
    public GameObject mainConsoleScreen;
    public GameObject settingsScreen;

    public void ShowSplash() { ShowOnly(splashScreen); }
    public void ShowOnboarding() { ShowOnly(onboardingScreen); }
    public void ShowCalibration() { ShowOnly(calibrationScreen); }
    public void ShowMainConsole() { ShowOnly(mainConsoleScreen); }
    public void ShowSettings() { ShowOnly(settingsScreen); }

    public void ShowOnly(GameObject target)
    {
        SetScreenActive(splashScreen, target);
        SetScreenActive(onboardingScreen, target);
        SetScreenActive(calibrationScreen, target);
        SetScreenActive(mainConsoleScreen, target);
        SetScreenActive(settingsScreen, target);
    }

    private void SetScreenActive(GameObject screen, GameObject target)
    {
        if (screen == null) return;
        screen.SetActive(screen == target);
    }
}