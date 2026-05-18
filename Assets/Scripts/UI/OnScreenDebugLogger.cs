using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tüm Unity loglarını ekranda gösterir.
/// Sahneye boş bir GameObject ekle, bu scripti attach et — otomatik UI kurar.
/// RELEASE BUILD'DE KALDIRILACAK.
/// </summary>
public class OnScreenDebugLogger : MonoBehaviour
{
    private string logText      = "";
    private Vector2 scrollPos   = Vector2.zero;
    private bool    visible     = true;
    private GUIStyle boxStyle;
    private GUIStyle textStyle;
    private GUIStyle btnStyle;
    private bool stylesReady    = false;

    private const int MAX_CHARS = 6000;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += HandleLog;
        Log("=== DEBUG LOG BAŞLADI ===");
        Log($"Platform: {Application.platform}");
        Log($"Unity: {Application.unityVersion}");
        Log($"OS: {SystemInfo.operatingSystem}");
        Log($"GPU: {SystemInfo.graphicsDeviceName}");
        Log($"GPU API: {SystemInfo.graphicsDeviceType}");
        Log("========================");
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        string prefix;
        if      (type == LogType.Error)     prefix = "[ERR] ";
        else if (type == LogType.Exception) prefix = "[EXC] ";
        else if (type == LogType.Warning)   prefix = "[WRN] ";
        else                                prefix = "[LOG] ";

        string entry = prefix + message;
        if (type == LogType.Exception || type == LogType.Error)
            entry += "\n" + stackTrace;

        Log(entry);
    }

    private void Log(string entry)
    {
        logText += entry + "\n";
        if (logText.Length > MAX_CHARS)
            logText = logText.Substring(logText.Length - MAX_CHARS);
        scrollPos = new Vector2(0, float.MaxValue);
    }

    private void InitStyles()
    {
        if (stylesReady) return;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize  = Mathf.Max(22, Screen.height / 40),
            alignment = TextAnchor.UpperLeft,
            wordWrap  = true,
            normal    = { background = MakeTex(2, 2, new Color(0, 0, 0, 0.88f)) }
        };

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = Mathf.Max(22, Screen.height / 40),
            alignment = TextAnchor.UpperLeft,
            wordWrap  = true,
            richText  = true,
            normal    = { textColor = Color.white }
        };

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = Mathf.Max(24, Screen.height / 38),
            fontStyle = FontStyle.Bold
        };

        stylesReady = true;
    }

    private void OnGUI()
    {
        InitStyles();

        float btnW = Screen.width  * 0.22f;
        float btnH = Screen.height * 0.07f;

        if (GUI.Button(new Rect(0, 0, btnW, btnH), visible ? "GİZLE" : "LOG", btnStyle))
            visible = !visible;

        if (!visible) return;

        float panelH = Screen.height * 0.75f;
        float panelY = Screen.height - panelH;

        GUI.Box(new Rect(0, panelY, Screen.width, panelH), "", boxStyle);

        Rect viewRect    = new Rect(0, panelY + 4, Screen.width, panelH - 8);
        Rect contentRect = new Rect(0, 0, Screen.width - 20, textStyle.CalcHeight(
            new GUIContent(logText), Screen.width - 20));

        scrollPos = GUI.BeginScrollView(viewRect, scrollPos, contentRect);
        GUI.Label(contentRect, logText, textStyle);
        GUI.EndScrollView();
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h);
        t.SetPixels(pix);
        t.Apply();
        return t;
    }
}
