using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// VibBeat Ripple (Halka Yayılma) Efekti
///
/// İBE — Feedback + Multimodality:
///   Kullanıcının dokunduğu noktadan tüm ekrana yayılan görsel dalga.
///   Her enstrüman farklı renk ve hız kullanır — kullanıcı hangisine
///   bastığını görmeden de anlayabilir (renk körü / görme engelli desteği
///   için haptik ile birleşir).
///
/// Kullanım:
///   RippleEffect.Instance.Spawn(worldPosition, color, duration);
///
/// Sahnede bir Canvas child'ı olarak çalışır.
/// </summary>
public class RippleEffect : MonoBehaviour
{
    // ─────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────
    public static RippleEffect Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────
    [Header("Ripple Ayarlari")]
    [Tooltip("Halkalar bu Canvas altinda olusturulur — en ust Canvas olmali")]
    [SerializeField] private Canvas targetCanvas;

    [Tooltip("Kac halka ayni anda aktif olabilir (pool boyutu)")]
    [SerializeField] private int poolSize = 8;

    [Tooltip("Halka kalinligi (px)")]
    [SerializeField] private float ringThickness = 6f;

    // ─────────────────────────────────────────
    // RENK PALETİ — her enstrüman için
    // ─────────────────────────────────────────
    public static readonly Color ColorPianoDo  = new Color(1.00f, 0.65f, 0.00f, 1f); // Turuncu
    public static readonly Color ColorPianoRe  = new Color(0.80f, 0.90f, 0.00f, 1f); // Sarı
    public static readonly Color ColorPianoMi  = new Color(0.00f, 0.85f, 0.80f, 1f); // Turkuaz
    public static readonly Color ColorPianoFa  = new Color(0.85f, 0.30f, 1.00f, 1f); // Mor
    public static readonly Color ColorDrum     = new Color(1.00f, 0.10f, 0.68f, 1f); // Magenta
    public static readonly Color ColorGuitar   = new Color(0.00f, 0.94f, 1.00f, 1f); // Cyan
    public static readonly Color ColorCalib    = new Color(1.00f, 1.00f, 1.00f, 1f); // Beyaz

    // ─────────────────────────────────────────
    // POOL
    // ─────────────────────────────────────────
    private GameObject[] pool;
    private Image[]      poolImages;
    private RectTransform[] poolRects;
    private int poolIndex = 0;

    // ─────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────
    private void Start()
    {
        // Canvas otomatik bul (atanmamışsa)
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        BuildPool();
        Debug.Log($"[RIPPLE] [OK] {poolSize} halka havuzu hazir.");
    }

    private void BuildPool()
    {
        pool       = new GameObject[poolSize];
        poolImages = new Image[poolSize];
        poolRects  = new RectTransform[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"Ripple_{i}");
            go.transform.SetParent(targetCanvas.transform, false);

            // Image bileşeni — sprite olmadan sadece renk
            var img = go.AddComponent<Image>();
            img.color = Color.clear;

            // RectTransform başlangıçta sıfır boyut
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = Vector2.zero;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            go.SetActive(false);

            pool[i]       = go;
            poolImages[i] = img;
            poolRects[i]  = rt;
        }
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────

    /// <summary>
    /// Verilen dünya pozisyonundan tüm ekrana yayılan halka spawn eder.
    /// </summary>
    /// <param name="worldPos">Butonun dünya koordinatı (transform.position)</param>
    /// <param name="color">Halka rengi</param>
    /// <param name="duration">Yayılma süresi (saniye)</param>
    /// <param name="ringCount">Kaç halka art arda gelsin</param>
    public void Spawn(Vector3 worldPos, Color color,
                      float duration = 0.7f, int ringCount = 2)
    {
        for (int r = 0; r < ringCount; r++)
        {
            float delay = r * (duration * 0.35f);
            SpawnSingle(worldPos, color, duration, delay);
        }
    }

    // ─────────────────────────────────────────
    // ENSTRÜMAN KISAYOLLARI
    // ─────────────────────────────────────────

    public void SpawnPiano(Vector3 pos, int keyIndex)
    {
        Color[] colors = { ColorPianoDo, ColorPianoRe, ColorPianoMi, ColorPianoFa };
        Color c = (keyIndex >= 0 && keyIndex < colors.Length)
            ? colors[keyIndex] : ColorPianoDo;

        // Piyano: 2 halka, hızlı
        Spawn(pos, c, duration: 0.65f, ringCount: 2);
    }

    public void SpawnDrum(Vector3 pos)
    {
        // Davul: 3 halka, hızlı ve büyük
        Spawn(pos, ColorDrum, duration: 0.55f, ringCount: 3);
    }

    public void SpawnGuitar(Vector3 pos)
    {
        // Gitar: tek halka, yavaş ve geniş
        Spawn(pos, ColorGuitar, duration: 1.2f, ringCount: 1);
    }

    public void SpawnCalibration(Vector3 pos)
    {
        Spawn(pos, ColorCalib, duration: 0.9f, ringCount: 2);
    }

    // ─────────────────────────────────────────
    // SPAWN CORE
    // ─────────────────────────────────────────
    private void SpawnSingle(Vector3 worldPos, Color color,
                             float duration, float delay)
    {
        // Bir sonraki pool slotunu al (döngüsel)
        int idx = poolIndex;
        poolIndex = (poolIndex + 1) % poolSize;

        var go  = pool[idx];
        var img = poolImages[idx];
        var rt  = poolRects[idx];

        // Ekran pozisyonunu Canvas local koordinatına çevir
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetCanvas.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos),
            targetCanvas.worldCamera,
            out localPos
        );
        rt.anchoredPosition = localPos;

        // Ekranın köşegeni = halkanın ulaşması gereken maksimum çap
        float screenDiag = Mathf.Sqrt(
            Screen.width  * Screen.width +
            Screen.height * Screen.height
        );
        // Canvas scale'ini hesaba kat
        float canvasScale = targetCanvas.GetComponent<RectTransform>().localScale.x;
        float maxSize = (screenDiag / canvasScale) * 2.2f;  // ekrandan taşsın

        // Halka efekti: ince kenarlı daire — OutlineWidth trick
        // Unity'de halka = tam opak circle + içi boş değil
        // DOTween ile: başlangıçta küçük tam dolu → büyürken alpha → 0
        // Daha iyi halka görünümü için: scale'i kullan, outline image yok
        // En temiz yol: Image alpha'sını başta tam aç, büyürken kapat
        rt.sizeDelta = new Vector2(ringThickness * 4f, ringThickness * 4f);
        img.color    = Color.clear;
        go.SetActive(true);

        // Eski tween'leri temizle
        DOTween.Kill(rt);
        DOTween.Kill(img);

        // Sequence: delay → büyü + soluk
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.AppendCallback(() =>
        {
            rt.sizeDelta = new Vector2(ringThickness * 4f, ringThickness * 4f);
            img.color    = new Color(color.r, color.g, color.b, 0.85f);
        });
        seq.Append(
            rt.DOSizeDelta(new Vector2(maxSize, maxSize), duration)
              .SetEase(Ease.OutCubic)
        );
        seq.Join(
            img.DOFade(0f, duration)
               .SetEase(Ease.InQuad)
        );
        seq.AppendCallback(() =>
        {
            go.SetActive(false);
            rt.sizeDelta = Vector2.zero;
        });
    }
}
