using UnityEngine.EventSystems;

/// <summary>
/// Android'de StandaloneInputModule'ün touch'tan simüle ettiği
/// mouse event'lerini devre dışı bırakır.
/// Tek dokunuş → tek onClick. Çift tetiklenme sorunu çözülür.
/// </summary>
public class MobileInputModule : StandaloneInputModule
{
    public override void Process()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android'de sadece touch event'lerini işle,
        // simüle edilmiş mouse event'lerini atla
        if (!ProcessTouchEvents() && input.mousePresent)
            ProcessMouseEvent();
#else
        base.Process();
#endif
    }
}
