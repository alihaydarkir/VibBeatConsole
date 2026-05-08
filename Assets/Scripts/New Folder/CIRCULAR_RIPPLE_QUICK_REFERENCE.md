# ?? Daire Ripple - Hýzlý Referans Kartý

## 1?? Temel Setup (30 saniye)

```csharp
Button myButton = GetComponent<Button>();

// Ripple ekle
EnhancedRippleEffect ripple = myButton.gameObject.AddComponent<EnhancedRippleEffect>();

// Daire yap - ÝÞTE BU!
ripple.useCircleShape = true;

// ? Daire ripple hazýr!
```

---

## 2?? Renk Seçenekleri

```csharp
ripple.settings.rippleColor = guitarCyan;     // #00F0FF
ripple.settings.rippleColor = pianoOrange;    // #FFA500
ripple.settings.rippleColor = drumMagenta;    // #FF1AAD
```

---

## 3?? Süreler

```csharp
ripple.settings.duration = 0.4f;              // Hýzlý
ripple.settings.duration = 0.45f;             // Standart ?
ripple.settings.duration = 0.5f;              // Normal
ripple.settings.duration = 0.7f;              // Yavaþ
```

---

## 4?? Geniþlik

```csharp
ripple.settings.expandRadius = 1.0f;          // Küçük (sadece button)
ripple.settings.expandRadius = 1.3f;          // Standart ?
ripple.settings.expandRadius = 1.5f;          // Normal
ripple.settings.expandRadius = 2.0f;          // Büyük (drum pad)
ripple.settings.expandRadius = 3.5f;          // Çok büyük
```

---

## 5?? Shader Seçimi

```csharp
// Option 1: Hýzlý (Önerilen)
ripple.useShader = false;

// Option 2: Güzel (Profesyonel)
ripple.useShader = true;
```

---

## ?? Tam Yapýlandýrma Örneði

```csharp
// Guitar Button
EnhancedRippleEffect ripple = guitarBtn.AddComponent<EnhancedRippleEffect>();
ripple.useCircleShape = true;
ripple.useShader = false;
ripple.settings.rippleColor = guitarCyan;
ripple.settings.expandRadius = 1.3f;
ripple.settings.duration = 0.45f;
ripple.settings.maxAlpha = 0.85f;
ripple.buttonPulseScale = 1.04f;
ripple.buttonPulseDuration = 0.25f;
```

---

## ?? Preset Konfigürasyonlar

### Guitar
```csharp
ripple.useCircleShape = true;
ripple.settings.rippleColor = guitarCyan;
ripple.settings.expandRadius = 1.3f;
ripple.settings.duration = 0.45f;
```

### Piano
```csharp
ripple.useCircleShape = true;
ripple.settings.rippleColor = pianoOrange;
ripple.settings.expandRadius = 1.5f;
ripple.settings.duration = 0.4f;
```

### Drum
```csharp
ripple.useCircleShape = true;
ripple.settings.rippleColor = drumMagenta;
ripple.settings.expandRadius = 2.0f;
ripple.settings.duration = 0.5f;
```

### Top Bar
```csharp
ripple.useCircleShape = true;
ripple.settings.rippleColor = guitarCyan;
ripple.settings.expandRadius = 1.0f;
ripple.settings.duration = 0.35f;
```

---

## ? Eðer Çalýþmýyorsa

### ? Hala Kare Görünüyor?
```csharp
ripple.useCircleShape = true;  // Kontrol et
```

### ? Görülmüyor?
```csharp
ripple.settings.maxAlpha = 0.8f;          // Alpha arttýr
ripple.settings.expandRadius = 1.5f;      // Geniþlik arttýr
```

### ? Çok Hýzlý?
```csharp
ripple.settings.duration = 0.5f;  // Daha uzun yap
```

### ? Kenarlar Pixelated?
```csharp
ripple.useShader = true;  // Shader kullan
```

---

## ?? Tüm Butonlara Uygulamak

```csharp
// Batch setup
Button[] allButtons = GetComponentsInChildren<Button>();

foreach (Button btn in allButtons)
{
    EnhancedRippleEffect ripple = btn.gameObject.AddComponent<EnhancedRippleEffect>();
    ripple.useCircleShape = true;
    ripple.settings.rippleColor = guitarCyan;
    ripple.settings.expandRadius = 1.3f;
    ripple.settings.duration = 0.45f;
}
```

---

## ?? Test Etmek

```csharp
void TestCircleRipple()
{
    Button btn = GetComponent<Button>();
    EnhancedRippleEffect ripple = btn.GetComponent<EnhancedRippleEffect>();
    
    if (ripple.useCircleShape)
    {
        Debug.Log("? Daire Ripple Çalýþýyor!");
        ripple.PlayRippleEffect();  // Test oynat
    }
}
```

---

## ?? Hýzlý Referans Tablosu

| Ayar | Guitar | Piano | Drum | Top Bar |
|------|:------:|:-----:|:----:|:-------:|
| **Shape** | Daire | Daire | Daire | Daire |
| **Color** | Cyan | Orange | Magenta | Cyan |
| **Radius** | 1.3 | 1.5 | 2.0 | 1.0 |
| **Duration** | 0.45s | 0.4s | 0.5s | 0.35s |
| **Shader** | No | No | No | No |

---

## ?? Dosyalar

- **EnhancedRippleEffect.cs** - Ana component
- **CircleRippleUtility.cs** - Daire helper
- **CircleRippleExample.cs** - Örnek kodlar
- **CIRCULAR_RIPPLE_GUIDE.md** - Tam kýlavuz
- **CIRCULAR_RIPPLE_SUMMARY.md** - Öz bilgi

---

## ?? 3 Adýmda Özet

```
1. ripple = button.AddComponent<EnhancedRippleEffect>();
2. ripple.useCircleShape = true;
3. ? Daire ripple hazýr!
```

---

**Yeterli bilgi! Þimdi uygulamaya baþla!** ??
