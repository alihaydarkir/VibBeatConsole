using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Herhangi bir UI elementine eklenince o elementi tıklanan/dokunulan
/// TAM noktadan ripple ve water drop spawn eder.
/// DrumPad ve PianoKey'lere otomatik eklenir.
/// </summary>
public class TouchSpawnHandler : MonoBehaviour,
    IPointerDownHandler
{
    public enum SpawnType { Piano0, Piano1, Piano2, Piano3, Drum }

    [SerializeField] public SpawnType spawnType = SpawnType.Drum;

    private Canvas parentCanvas;
    private RectTransform canvasRT;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            canvasRT = parentCanvas.GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (canvasRT == null) return;

        // Tam dokunma noktasini canvas local koordinatina cevir
        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : Camera.main;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, eventData.position, cam, out localPos);

        Vector2 screenPos = eventData.position;

        switch (spawnType)
        {
            case SpawnType.Drum:
                // Davul: dokunulan noktadan eliptik water drop + ripple
                WaterDropEffect.Instance?.SpawnDropAtScreen(screenPos,
                    new Color(1f, 0f, 0.6f, 1f), elliptic: true);
                RippleEffect.Instance?.SpawnFromScreenPos(screenPos,
                    RippleEffect.ColorDrum);
                break;

            case SpawnType.Piano0:
            case SpawnType.Piano1:
            case SpawnType.Piano2:
            case SpawnType.Piano3:
                int idx = (int)spawnType;
                // Piyano: tıklanan noktadan daire water drop + ripple
                Color[] pianoColors = {
                    RippleEffect.ColorPianoDo,
                    RippleEffect.ColorPianoRe,
                    RippleEffect.ColorPianoMi,
                    RippleEffect.ColorPianoFa
                };
                Color pc = pianoColors[idx];
                WaterDropEffect.Instance?.SpawnDropAtScreen(screenPos, pc, elliptic: false);
                RippleEffect.Instance?.SpawnFromScreenPos(screenPos, pc);
                break;
        }
    }
}
