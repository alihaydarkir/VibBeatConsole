using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// VibBeat Ripple (Halka Yayilma) Efekti
/// Herhangi bir UI elemaninin pozisyonundan tum ekrana yayilan halka uretir.
/// Canvas Overlay modunda da dogru calisir.
/// </summary>
public class RippleEffect : MonoBehaviour
{
    public static RippleEffect Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[RIPPLE] Awake - Instance set.");
    }

    [Header("Ripple Ayarlari")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private int   poolSize     = 12;

    [Header("Gorsel Parametreler")]
    [Tooltip("Halkalar ekrani kac saniyede kaplar. Dusuk = hizli, Yuksek = yavas")]
    [SerializeField] [Range(0.2f, 2.0f)] private float expansionSpeed = 0.6f;

    [Tooltip("Halka kalinligi orani (0=cizgi, 1=dolu daire). 0.15 = ince halka")]
    [SerializeField] [Range(0.05f, 0.5f)] private float ringWidth     = 0.15f;

    [Tooltip("Baslangic alpha degeri (0-1). Dusuk = soluk, Yuksek = canli)")]
    [SerializeField] [Range(0.1f, 1.0f)] private float startAlpha    = 0.75f;

    [Tooltip("Halka bitis alpha degeri. 0 = tamamen kaybolur")]
    [SerializeField] [Range(0.0f, 0.3f)] private float endAlpha      = 0f;

    // Renk paleti
    public static readonly Color ColorPianoDo = new Color(1.00f, 0.65f, 0.00f, 1f);
    public static readonly Color ColorPianoRe = new Color(0.80f, 0.90f, 0.00f, 1f);
    public static readonly Color ColorPianoMi = new Color(0.00f, 0.85f, 0.80f, 1f);
    public static readonly Color ColorPianoFa = new Color(0.85f, 0.30f, 1.00f, 1f);
    public static readonly Color ColorDrum    = new Color(1.00f, 0.10f, 0.68f, 1f);
    public static readonly Color ColorGuitar  = new Color(0.00f, 0.94f, 1.00f, 1f);

    private RectTransform[] poolRects;
    private CircleImage[]   poolImages;
    private GameObject[]    poolObjects;
    private int poolIndex = 0;
    private RectTransform canvasRect;

    private void Start()
    {
        // Canvas otomatik bul
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas == null)
        {
            Debug.LogError("[RIPPLE] Canvas bulunamadi!");
            return;
        }

        canvasRect = targetCanvas.GetComponent<RectTransform>();
        BuildPool();
        Debug.Log($"[RIPPLE] [OK] {poolSize} halka havuzu hazir. Canvas: {targetCanvas.name}");
    }

    private void BuildPool()
    {
        poolObjects = new GameObject[poolSize];
        poolImages  = new CircleImage[poolSize];
        poolRects   = new RectTransform[poolSize];

        // Halka nesnelerini DOGRUDAN ANA CANVAS altina koy
        // Bu sayede hicbir parent clip tarafindan kirpilmaz
        Transform canvasTransform = targetCanvas.transform;

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"Ripple_{i}");
            go.transform.SetParent(canvasTransform, false);
            go.transform.SetAsLastSibling(); // her zaman en uste cizilsin

            // CircleImage: ici bos halka (ring) — OnPopulateMesh override
            var img = go.AddComponent<CircleImage>();
            img.color        = Color.clear;
            img.raycastTarget = false;
            // innerRadius SpawnSingle'da dinamik set edilir

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            go.SetActive(false);

            poolObjects[i] = go;
            poolImages[i]  = img;
            poolRects[i]   = rt;
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────

    /// <summary>
    /// Verilen EKRAN koordinatindan (Input.mousePosition gibi) ripple baslatir.
    /// UI elemanin Screen pozisyonunu kullanir.
    /// </summary>
    public void SpawnFromScreenPos(Vector2 screenPos, Color color,
                                   float duration = 0.7f, int ringCount = 2)
    {
        if (canvasRect == null) return;

        Vector2 localPos;
        Camera cam = (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : Camera.main;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, cam, out localPos);

        for (int r = 0; r < ringCount; r++)
            SpawnSingle(localPos, color, duration, r * duration * 0.35f);
    }

    /// <summary>
    /// Verilen WORLD pozisyonundan ripple baslatir (3D world coords).
    /// </summary>
    public void SpawnFromWorld(Vector3 worldPos, Color color,
                               float duration = 0.7f, int ringCount = 2)
    {
        if (canvasRect == null) return;

        Camera cam = (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : Camera.main;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        SpawnFromScreenPos(screenPos, color, duration, ringCount);
    }

    // Enstruman kisayollari
    public void SpawnPiano(Vector3 worldPos, int keyIndex)
    {
        Color[] c = { ColorPianoDo, ColorPianoRe, ColorPianoMi, ColorPianoFa };
        Color col = (keyIndex >= 0 && keyIndex < c.Length) ? c[keyIndex] : ColorPianoDo;
        SpawnFromWorld(worldPos, col, expansionSpeed, 2);
    }

    public void SpawnDrum(Vector3 worldPos)
    {
        SpawnFromWorld(worldPos, ColorDrum, expansionSpeed * 0.85f, 3);
    }

    public void SpawnGuitar(Vector3 worldPos)
    {
        SpawnFromWorld(worldPos, ColorGuitar, expansionSpeed * 1.8f, 1);
    }

    // ─────────────────────────────────────────
    // CORE
    // ─────────────────────────────────────────
    private void SpawnSingle(Vector2 localPos, Color color, float duration, float delay)
    {
        if (poolObjects == null) return;

        int idx = poolIndex;
        poolIndex = (poolIndex + 1) % poolSize;

        var go  = poolObjects[idx];
        var img = poolImages[idx];
        var rt  = poolRects[idx];

        // Onceki tween temizle
        DOTween.Kill(rt);
        DOTween.Kill(img);

        // Baslangic pozisyonu ve boyutu ayarla
        rt.anchoredPosition = localPos;
        rt.sizeDelta = Vector2.zero;
        img.color = Color.clear;
        go.SetActive(true);
        go.transform.SetAsLastSibling(); // her zaman en uste

        // Ekranin kosegeni — halkaning ulasacagi maksimum boyut
        float screenDiag = Mathf.Sqrt(
            Screen.width  * (float)Screen.width +
            Screen.height * (float)Screen.height
        ) / targetCanvas.scaleFactor;
        float maxSize = screenDiag * 2.5f; // ekrandan kesinlikle tasin

        // innerRadius: dis yaricapin ne kadari ic bos (1 - ringWidth)
        img.innerRadius = 1f - ringWidth;
        img.SetVerticesDirty();  // yeni innerRadius ile mesh yeniden ciz

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.AppendCallback(() =>
        {
            float startSize = maxSize * 0.04f;  // baslangic boyutu: maksimumun %4u
            rt.sizeDelta = new Vector2(startSize, startSize);
            img.color    = new Color(color.r, color.g, color.b, startAlpha);
            img.SetVerticesDirty();
        });
        seq.Append(
            rt.DOSizeDelta(new Vector2(maxSize, maxSize), expansionSpeed)
              .SetEase(Ease.OutQuart)
        );
        seq.Join(
            img.DOFade(endAlpha, expansionSpeed)
               .SetEase(Ease.OutQuad)
        );
        seq.AppendCallback(() =>
        {
            go.SetActive(false);
            rt.sizeDelta = Vector2.zero;
        });
    }
}
