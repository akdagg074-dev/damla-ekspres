using UnityEngine;
using UnityEngine.InputSystem;

// Klavye / gamepad / ekran pedallari: surus girisi ve tek seferlik tus basimlari.
public static class Tus
{
    // Ekrandaki gaz/fren pedallari (BolumYonetici her kare doldurur)
    public static bool PedalGaz, PedalFren;
    // Test ve tanitim videosu icin otomatik surus (-1..1). 0 = kapali
    public static float OtoSurus;

    static bool K(Key key) { var k = Keyboard.current; return k != null && k[key].wasPressedThisFrame; }
    static bool B(Key key) { var k = Keyboard.current; return k != null && k[key].isPressed; }

    // -1 (fren / geri) .. +1 (gaz). Havada aracin burnunu kaldirir / indirir.
    public static float Surus()
    {
        if (OtoSurus != 0f) return OtoSurus;
        float v = 0f;
        if (B(Key.D) || B(Key.RightArrow) || B(Key.W) || B(Key.UpArrow) || PedalGaz) v += 1f;
        if (B(Key.A) || B(Key.LeftArrow) || B(Key.S) || B(Key.DownArrow) || PedalFren) v -= 1f;
        var g = Gamepad.current;
        if (g != null)
        {
            v += g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue();
            float x = g.leftStick.ReadValue().x;
            if (Mathf.Abs(x) > 0.2f) v += x;
        }
        return Mathf.Clamp(v, -1f, 1f);
    }

    public static bool Duraklat()
    {
        var g = Gamepad.current;
        return K(Key.Escape) || K(Key.P) || (g != null && g.startButton.wasPressedThisFrame);
    }

    public static bool Onay()
    {
        var g = Gamepad.current;
        return K(Key.Enter) || K(Key.Space) || (g != null && g.buttonSouth.wasPressedThisFrame);
    }

    public static bool Yeniden()
    {
        var g = Gamepad.current;
        return K(Key.R) || (g != null && g.selectButton.wasPressedThisFrame);
    }

    public static bool Korna()
    {
        var g = Gamepad.current;
        return K(Key.H) || (g != null && g.buttonNorth.wasPressedThisFrame);
    }

    public static bool Ses() { return K(Key.M); }

    // Bolum secimde tum bolumlerin kilidini acar (degerlendirme ve tanitim videosu icin)
    public static bool KilitAc() { return K(Key.F9); }

    // Ekranda bir alan (piksel, sol-ust orijin) fare ya da parmakla basili tutuluyor mu?
    public static bool AlanBasili(Rect r)
    {
        var m = Mouse.current;
        if (m != null && m.leftButton.isPressed && r.Contains(Cevir(m.position.ReadValue()))) return true;
        var ts = Touchscreen.current;
        if (ts != null)
            foreach (var t in ts.touches)
                if (t.press.isPressed && r.Contains(Cevir(t.position.ReadValue()))) return true;
        return false;
    }

    static Vector2 Cevir(Vector2 p) { return new Vector2(p.x, Screen.height - p.y); }
}
