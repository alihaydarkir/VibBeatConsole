using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daire þekli ripple efektleri için utility sýnýfý
/// Circle sprite oluþturur ve Image component'ine uygular
/// </summary>
public class CircleRippleUtility : MonoBehaviour
{
    private static Texture2D circleTexture;
    private static Sprite circleSprite;

    /// <summary>
    /// Daire þekli Image component'ini hazýrla
    /// </summary>
    public static void MakeCircleImage(Image image, int textureSize = 256)
    {
        if (image == null) return;

        // Daire texture'ý oluþtur
        Texture2D texture = CreateCircleTexture(textureSize);
        
        // Sprite'a dönüþtür
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), 
                                     new Vector2(0.5f, 0.5f), 100f);
        
        // Image'e ata
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }

    /// <summary>
    /// Daire þekli texture oluþtur
    /// </summary>
    private static Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        
        float radius = size / 2f;
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Her pixel için merkez uzaklýðýný hesapla
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Daire dýþý = þeffaf, içi = opak
                // Kenarlar için smooth edge (antialiasing)
                float smoothRadius = radius - 2f; // Edge smoothing
                float alpha = 1f - Mathf.Clamp01((distance - smoothRadius) / 2f);

                int index = y * size + x;
                colors[index] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.name = "CircleTexture";
        
        return texture;
    }

    /// <summary>
    /// Ripple Image'i daire þekline ayarla
    /// </summary>
    public static void ConfigureRippleImage(Image rippleImage)
    {
        if (rippleImage == null) return;

        rippleImage.type = Image.Type.Simple;
        rippleImage.preserveAspect = true;

        // Veya sprite kullanmadan, layout element ile kontrol et
        LayoutElement layoutElement = rippleImage.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rippleImage.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = 30f;
        layoutElement.preferredHeight = 30f;
        layoutElement.preferredWidth = layoutElement.preferredHeight; // Kare yap
    }

    /// <summary>
    /// Shader-based daire render
    /// </summary>
    public static void ApplyCircleShader(Image image)
    {
        if (image == null) return;

        Shader circleShader = Shader.Find("UI/CircleRipple");
        if (circleShader != null)
        {
            Material mat = new Material(circleShader);
            image.material = mat;
        }
    }
}
