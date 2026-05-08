#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class VibeBeatBuilderWindow : EditorWindow
{
    [MenuItem("VibeBeat/Build UI")]
    public static void BuildUI()
    {
        VibeBeatAutoUIBuilder builder = FindFirstObjectByType<VibeBeatAutoUIBuilder>();

        if (builder == null)
        {
            GameObject go = new GameObject("VibeBeatUIBootstrap");
            builder = go.AddComponent<VibeBeatAutoUIBuilder>();
            Debug.Log("[VibeBeat] Bootstrap objesi oluşturuldu.");
        }

        builder.BuildVibeBeatUI();
        Debug.Log("[VibeBeat] ✅ UI build edildi!");
    }
}
#endif