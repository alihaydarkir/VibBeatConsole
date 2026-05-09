using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Su damlasi efekti — piyano ve davul icin.
/// Tusa basilinca merkez parlak nokta + disariya yayilan
/// eliptik halkalar olusturur. Su damlasinin suya dusme hissi.
/// 
/// Kullanim:
///   WaterDropEffect.Instance.SpawnDrop(worldPos, color, isElliptic);
/// </summary>
public class WaterDropEffect : MonoBehaviour
{
    public static WaterDropEffect Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Ayarlar")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private int    poolSize      = 16; // halka havuzu (her drop 3-4 halka)
    [SerializeField] private float  dropDuration  = 0.8f;
    [SerializeField] private int    ringPerDrop   = 3;  // her dokunusta kac halka
    [SerializeField] private float  ringDelay     = 0.12f; // halkalar arasi gecikme
    [SerializeField] private float  maxRingSize   = 280f;  // max halka capı (piksel)
    [SerializeField] private float  ellipseYScale = 0.5f;  // eliptik eziyet orani (1=daire)

    // Merkez parlak nokta ayarlari
    [SerializeField] private float  dotSize       = 18f;
    [SerializeField] private float  dotFadeDur    = 0.25f;

    private RectTransform[] ringRects;
    private Image[]         ringImages;
    private GameObject[]    ringObjects;
    private int             poolIdx = 0;
    private RectTransform   canvasRT;

    private void Start()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>()
                        ?? FindFirstObjectByType<Canvas>();

        canvasRT = targetCanvas.GetComponent<RectTransform>();
        BuildPool();
        Debug.Log($"[WATER_DROP] [OK] {poolSize} halka hazir. Canvas: {targetCanvas.name}");
    }

    private void BuildPool()
    {
        ringObjects = new GameObject[poolSize];
        ringImages  = new Image[poolSize];
        ringRects   = new RectTransform[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            var go  = new GameObject($"Drop_Ring_{i}");
            go.transform.SetParent(targetCanvas.transform, false);
            go.transform.SetAsLastSibling();

            var img = go.AddComponent<Image>();
            img.color         = Color.clear;
            img.raycastTarget = false;
            // Knob sprite — daire gibi gorunur ama eliptik scale ile oval olur
            img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            go.SetActive(false);

            ringObjects[i] = go;
            ringImages[i]  = img;
            ringRects[i]   = rt;
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────

    /// <summary>Piyano tus renkleri icin kisayol</summary>
    public void SpawnPianoDrop(Vector3 worldPos, int keyIndex)
    {
        Color[] colors = {
            new Color(1.00f, 0.58f, 0.00f, 1f), // Do — turuncu
            new Color(1.00f, 0.85f, 0.00f, 1f), // Re — altin
            new Color(0.00f, 0.75f, 1.00f, 1f), // Mi — cyan
            new Color(0.75f, 0.25f, 1.00f, 1f), // Fa — mor
        };
        Color c = (keyIndex >= 0 && keyIndex < colors.Length)
            ? colors[keyIndex] : colors[0];
        SpawnDrop(worldPos, c, elliptic: false);
    }

    /// <summary>Davul icin genis eliptik damla</summary>
    public void SpawnDrumDrop(Vector3 worldPos)
    {
        SpawnDrop(worldPos, new Color(1f, 0.1f, 0.6f, 1f), elliptic: true);
    }

    /// <summary>
    /// Ana spawn metodu.
    /// elliptic=true: yatay oval (davul yuzeyine dusan damla hissi)
    /// elliptic=false: daire (piyano tus hissi)
    /// </summary>
    public void SpawnDrop(Vector3 worldPos, Color color, bool elliptic = false)
    {
        // World pos → canvas local pos
        Camera cam = (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : Camera.main;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, cam, out localPos);

        // Merkez parlak nokta
        StartCoroutine(SpawnCenterDot(localPos, color));

        // Halkalar — birer birer gecikmeyle
        for (int r = 0; r < ringPerDrop; r++)
            StartCoroutine(SpawnRing(localPos, color, r * ringDelay, elliptic));
    }

    // ─────────────────────────────────────────
    // MERKEZ NOKTA
    // ─────────────────────────────────────────
    private IEnumerator SpawnCenterDot(Vector2 localPos, Color color)
    {
        // Gecici nokta objesi yarat
        var go  = new GameObject("DropCenter");
        go.transform.SetParent(targetCanvas.transform, false);
        go.transform.SetAsLastSibling();

        var img = go.AddComponent<Image>();
        img.sprite        = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(dotSize, dotSize);

        // Parlak beyaz → renk → seffaf
        img.color = Color.white;
        img.DOColor(color, dotFadeDur * 0.3f)
           .OnComplete(() =>
               img.DOFade(0f, dotFadeDur * 0.7f)
                  .OnComplete(() => Destroy(go)));

        // Nokta buyuyup kuculsun
        rt.DOSizeDelta(new Vector2(dotSize * 2.5f, dotSize * 2.5f), dotFadeDur)
          .SetEase(Ease.OutCubic);

        yield return null;
    }

    // ─────────────────────────────────────────
    // HALKA
    // ─────────────────────────────────────────
    private IEnumerator SpawnRing(Vector2 localPos, Color color,
                                   float delay, bool elliptic)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        int idx = poolIdx;
        poolIdx = (poolIdx + 1) % poolSize;

        var go  = ringObjects[idx];
        var img = ringImages[idx];
        var rt  = ringRects[idx];

        DOTween.Kill(rt);
        DOTween.Kill(img);

        rt.anchoredPosition = localPos;

        // Eliptik: y eksenini olcturerek oval yap
        float yScale = elliptic ? ellipseYScale : 1f;

        float startSize = dotSize * 1.5f;
        rt.sizeDelta = new Vector2(startSize, startSize * yScale);
        img.color    = new Color(color.r, color.g, color.b, 0.85f);
        go.SetActive(true);
        go.transform.SetAsLastSibling();

        // Disariya yayil + soluk
        Sequence seq = DOTween.Sequence();
        seq.Append(
            rt.DOSizeDelta(new Vector2(maxRingSize, maxRingSize * yScale), dropDuration)
              .SetEase(Ease.OutCubic)
        );
        seq.Join(
            img.DOFade(0f, dropDuration)
               .SetEase(Ease.InQuad)
        );
        seq.OnComplete(() =>
        {
            go.SetActive(false);
            rt.sizeDelta = Vector2.zero;
        });
    }
}
