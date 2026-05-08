using UnityEngine;
using System.Collections.Generic;

public enum TouchZoneType
{
    GuitarMute,
    PianoKeys,
    DrumPad,
    None
}

public class TouchZoneController : MonoBehaviour
{
    // --- Bölge Sınırları (Normalized 0-1) ---
    // Zone 1: Sol %30 → Gitar Mute
    private Rect guitarMuteZone = new Rect(0.00f, 0.00f, 0.30f, 1.00f);
    // Zone 2: Sağ Üst → Piyano
    private Rect pianoZone = new Rect(0.30f, 0.50f, 0.70f, 0.50f);
    // Zone 3: Sağ Alt → Davul
    private Rect drumZone = new Rect(0.30f, 0.00f, 0.70f, 0.50f);

    // --- State ---
    private bool isGuitarMuted = false;
    private Dictionary<int, TouchZoneType> activeTouches = new Dictionary<int, TouchZoneType>();

    // --- Inspector Debug ---
    [SerializeField] private bool debugShowZones = true;
    [SerializeField] private TouchZoneType debugActiveZone = TouchZoneType.None;
    [SerializeField] private bool debugIsGuitarMuted = false;
    [SerializeField] private int debugPianoKeyIndex = -1;

    // --- Events ---
    public delegate void OnZonePressedDelegate(TouchZoneType zone, Vector2 normalizedPos);
    public event OnZonePressedDelegate OnZonePressed;

    public delegate void OnZoneReleasedDelegate(TouchZoneType zone);
    public event OnZoneReleasedDelegate OnZoneReleased;

    public delegate void OnGuitarMuteChangedDelegate(bool isMuted);
    public event OnGuitarMuteChangedDelegate OnGuitarMuteChanged;

    public delegate void OnPianoKeyDelegate(int keyIndex);
    public event OnPianoKeyDelegate OnPianoKeyPressed;

    public delegate void OnDrumHitDelegate();
    public event OnDrumHitDelegate OnDrumHit;

    private void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    // --- Mobil Dokunuş ---
    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount && i < 5; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 pos = GetNormalizedPosition(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    ProcessPress(touch.fingerId, pos);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    ProcessRelease(touch.fingerId);
                    break;
            }
        }
    }

    // --- Editor Mouse Simülasyonu ---
    private void HandleMouseInput()
    {
        Vector2 pos = GetNormalizedPosition(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
            ProcessPress(0, pos);

        if (Input.GetMouseButtonUp(0))
            ProcessRelease(0);
    }

    // --- Basma İşlemi ---
    private void ProcessPress(int fingerId, Vector2 normalizedPos)
    {
        TouchZoneType zone = DetectZone(normalizedPos);
        activeTouches[fingerId] = zone;

        debugActiveZone = zone;
        OnZonePressed?.Invoke(zone, normalizedPos);

        switch (zone)
        {
            case TouchZoneType.GuitarMute:
                isGuitarMuted = true;
                debugIsGuitarMuted = true;
                OnGuitarMuteChanged?.Invoke(true);
                Debug.Log("[TOUCH] 🎸 Gitar MUTE açıldı");
                break;

            case TouchZoneType.PianoKeys:
                int keyIndex = GetPianoKeyIndex(normalizedPos);
                debugPianoKeyIndex = keyIndex;
                OnPianoKeyPressed?.Invoke(keyIndex);
                Debug.Log($"[TOUCH] 🎹 Piyano tuş: {keyIndex}");
                break;

            case TouchZoneType.DrumPad:
                OnDrumHit?.Invoke();
                Debug.Log("[TOUCH] 🥁 Davul vuruldu!");
                break;
        }
    }

    // --- Bırakma İşlemi ---
    private void ProcessRelease(int fingerId)
    {
        if (!activeTouches.ContainsKey(fingerId)) return;

        TouchZoneType zone = activeTouches[fingerId];
        activeTouches.Remove(fingerId);

        OnZoneReleased?.Invoke(zone);

        if (zone == TouchZoneType.GuitarMute)
        {
            isGuitarMuted = false;
            debugIsGuitarMuted = false;
            OnGuitarMuteChanged?.Invoke(false);
            Debug.Log("[TOUCH] 🎸 Gitar MUTE kapandı");
        }

        debugActiveZone = TouchZoneType.None;
    }

    // --- Bölge Tespiti ---
    private TouchZoneType DetectZone(Vector2 pos)
    {
        if (guitarMuteZone.Contains(pos)) return TouchZoneType.GuitarMute;
        if (pianoZone.Contains(pos)) return TouchZoneType.PianoKeys;
        if (drumZone.Contains(pos)) return TouchZoneType.DrumPad;
        return TouchZoneType.None;
    }

    // --- Piano Tuş Index (0-3) ---
    public int GetPianoKeyIndex(Vector2 normalizedPos)
    {
        // Piyano bölgesi X eksenini 4'e böl
        float relativeX = (normalizedPos.x - pianoZone.x) / pianoZone.width;
        relativeX = Mathf.Clamp01(relativeX);
        int index = Mathf.FloorToInt(relativeX * 4f);
        return Mathf.Clamp(index, 0, 3);
    }

    // --- Pozisyon Normalize ---
    private Vector2 GetNormalizedPosition(Vector2 screenPos)
    {
        return new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );
    }

    // --- Debug Görsel (Editor) ---
    private void OnGUI()
    {
        if (!debugShowZones) return;

#if UNITY_EDITOR
        DrawZone(guitarMuteZone, "🎸 MUTE", new Color(1, 0, 0, 0.3f));
        DrawZone(pianoZone, "🎹 PIANO", new Color(0, 0, 1, 0.3f));
        DrawZone(drumZone, "🥁 DRUM", new Color(0, 1, 0, 0.3f));
#endif
    }

    private void DrawZone(Rect normalizedRect, string label, Color color)
    {
        Rect screenRect = new Rect(
            normalizedRect.x * Screen.width,
            (1f - normalizedRect.y - normalizedRect.height) * Screen.height,
            normalizedRect.width * Screen.width,
            normalizedRect.height * Screen.height
        );

        GUI.color = color;
        GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(screenRect, label, style);
    }

    // --- Getters ---
    public bool IsGuitarMuted => isGuitarMuted;
}