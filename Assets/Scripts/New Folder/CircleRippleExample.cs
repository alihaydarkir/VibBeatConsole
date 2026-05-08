using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daire Ripple Efektleri için örnek kullaným
/// Bu dosya, tüm butonlara nasýl daire ripple ekleyeceðini gösterir
/// </summary>
public class CircleRippleExample : MonoBehaviour
{
    // --- GUITAR BUTTON ÖRNEÐI ---
    public void SetupGuitarButton(Button guitarButton)
    {
        EnhancedRippleEffect ripple = guitarButton.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // Daire ripple ayarlarý
        ripple.useCircleShape = true;           // Daire þekli etkinleþtir
        ripple.useShader = false;               // Hýzlý texture-based kullan
        
        // Ripple görünümü
        ripple.settings.rippleColor = Color.cyan;       // #00F0FF
        ripple.settings.expandRadius = 1.3f;            // Geniþleme miktarý
        ripple.settings.duration = 0.45f;               // Animasyon süresi
        ripple.settings.maxAlpha = 0.85f;               // Baþlangýç saydamlýðý
        
        // Button pulse
        ripple.buttonPulseScale = 1.04f;                // Hafif pulse
        ripple.buttonPulseDuration = 0.25f;             // Pulse süresi
    }

    // --- PIANO KEY ÖRNEÐI ---
    public void SetupPianoKey(Button pianoKey)
    {
        EnhancedRippleEffect ripple = pianoKey.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // Daire ripple ayarlarý
        ripple.useCircleShape = true;
        ripple.useShader = false;
        
        // Ripple görünümü
        ripple.settings.rippleColor = new Color(1f, 0.65f, 0f);  // #FFA500 (Orange)
        ripple.settings.expandRadius = 1.5f;
        ripple.settings.duration = 0.4f;
        ripple.settings.maxAlpha = 0.8f;
        
        // Button pulse
        ripple.buttonPulseScale = 1.08f;                // Daha belirgin
        ripple.buttonPulseDuration = 0.3f;
    }

    // --- DRUM PAD ÖRNEÐI ---
    public void SetupDrumPad(Button drumPad)
    {
        EnhancedRippleEffect ripple = drumPad.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // Daire ripple ayarlarý
        ripple.useCircleShape = true;
        ripple.useShader = false;
        
        // Ripple görünümü
        ripple.settings.rippleColor = new Color(1f, 0.1f, 0.68f);  // #FF1AAD (Magenta)
        ripple.settings.expandRadius = 2.0f;            // Daha geniþ ripple
        ripple.settings.duration = 0.5f;
        ripple.settings.maxAlpha = 0.75f;
        
        // Button pulse
        ripple.buttonPulseScale = 1.06f;
        ripple.buttonPulseDuration = 0.35f;
    }

    // --- GLOW BUTTON ÖRNEÐI ---
    public void SetupGlowButton(Button button, Color accentColor)
    {
        EnhancedRippleEffect ripple = button.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // Daire ripple ayarlarý
        ripple.useCircleShape = true;
        ripple.useShader = false;
        
        // Ripple görünümü
        ripple.settings.rippleColor = accentColor;
        ripple.settings.expandRadius = 1.3f;
        ripple.settings.duration = 0.45f;
        ripple.settings.maxAlpha = 0.8f;
        
        // Button pulse
        ripple.buttonPulseScale = 1.05f;
        ripple.buttonPulseDuration = 0.3f;
    }

    // --- TOP BAR BUTTON ÖRNEÐI ---
    public void SetupTopBarButton(Button button)
    {
        EnhancedRippleEffect ripple = button.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // Daire ripple ayarlarý
        ripple.useCircleShape = true;
        ripple.useShader = false;
        
        // Ripple görünümü (daha küçük)
        ripple.settings.rippleColor = Color.cyan;
        ripple.settings.expandRadius = 1.0f;            // Daha küçük
        ripple.settings.duration = 0.35f;               // Daha hýzlý
        ripple.settings.maxAlpha = 0.75f;
        
        // Button pulse
        ripple.buttonPulseScale = 1.03f;
        ripple.buttonPulseDuration = 0.2f;
    }

    // --- SETTINGS ROW ÖRNEÐI ---
    public void SetupSettingsRow(Button row)
    {
        EnhancedRippleEffect ripple = row.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // Daire ripple ayarlarý
        ripple.useCircleShape = true;
        ripple.useShader = false;
        
        // Ripple görünümü
        ripple.settings.rippleColor = Color.cyan;
        ripple.settings.expandRadius = 0.8f;            // Çok küçük
        ripple.settings.duration = 0.4f;
        ripple.settings.maxAlpha = 0.7f;
        
        // Button pulse
        ripple.buttonPulseScale = 1.02f;
        ripple.buttonPulseDuration = 0.15f;
    }

    // --- SHADER TABANI ÖRNEÐI (Profesyonel) ---
    public void SetupWithShader(Button button, Color rippleColor)
    {
        EnhancedRippleEffect ripple = button.gameObject.AddComponent<EnhancedRippleEffect>();
        
        // SHADER-BASED kullan (daha güzel kenarlar)
        ripple.useCircleShape = true;
        ripple.useShader = true;                // ? SHADER KULLAN
        
        // Ripple görünümü
        ripple.settings.rippleColor = rippleColor;
        ripple.settings.expandRadius = 1.5f;
        ripple.settings.duration = 0.5f;
        ripple.settings.maxAlpha = 0.8f;
        
        // Button pulse
        ripple.buttonPulseScale = 1.05f;
        ripple.buttonPulseDuration = 0.3f;
    }

    // --- ÖZEL YAPILANDIRMA ---
    public void SetupCustomRipple(Button button, 
                                  Color color,
                                  float expandRadius,
                                  float duration,
                                  float pulseScale)
    {
        EnhancedRippleEffect ripple = button.gameObject.AddComponent<EnhancedRippleEffect>();
        
        ripple.useCircleShape = true;
        ripple.useShader = false;
        
        ripple.settings.rippleColor = color;
        ripple.settings.expandRadius = expandRadius;
        ripple.settings.duration = duration;
        ripple.settings.maxAlpha = 0.8f;
        
        ripple.buttonPulseScale = pulseScale;
        ripple.buttonPulseDuration = duration * 0.6f;
    }

    // --- BATCH SETUP ÖRNEÐI ---
    public void SetupAllButtons(Button[] buttons, Color rippleColor)
    {
        foreach (Button button in buttons)
        {
            EnhancedRippleEffect ripple = button.gameObject.AddComponent<EnhancedRippleEffect>();
            ripple.useCircleShape = true;
            ripple.useShader = false;
            ripple.settings.rippleColor = rippleColor;
            ripple.settings.expandRadius = 1.3f;
            ripple.settings.duration = 0.45f;
            ripple.buttonPulseScale = 1.05f;
        }
    }

    // --- MANUAL TEST ÖRNEÐI ---
    public void TestCircleRipple(Button testButton)
    {
        // Ripple var mý kontrol et
        EnhancedRippleEffect ripple = testButton.GetComponent<EnhancedRippleEffect>();
        
        if (ripple == null)
        {
            Debug.LogWarning("? Ripple component bulunamadý!");
            return;
        }

        // Daire aktif mý kontrol et
        if (!ripple.useCircleShape)
        {
            Debug.LogWarning("? Daire þekli devre dýþý!");
            ripple.useCircleShape = true;
        }

        // Ripple efektini oynat
        ripple.PlayRippleEffect();
        Debug.Log("? Daire Ripple Testi Yapýldý");
    }
}

/*
 * HIZLI BAÞLANGAÇ:
 * 
 * 1. Bir button'a ripple ekle:
 *    CircleRippleExample example = gameObject.AddComponent<CircleRippleExample>();
 *    example.SetupGuitarButton(myButton);
 * 
 * 2. Tüm butonlarý configure et:
 *    example.SetupAllButtons(allButtons, guitarCyan);
 * 
 * 3. Test et:
 *    example.TestCircleRipple(myButton);
 * 
 * ÖNEMLÝ: useCircleShape = true yapýnýz!
 */
