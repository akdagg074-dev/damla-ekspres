using UnityEngine;

// Oyuncu ayarlari (PlayerPrefs'te saklanir): ses seviyesi, muzik, tam ekran. M tusu anlik sessize alir.
public static class Ayarlar
{
    public static float Ses { get; private set; } = 0.8f;
    public static bool Muzik { get; private set; } = true;
    public static bool Sessiz { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Yukle()
    {
        Ses = PlayerPrefs.GetFloat("ayar_ses", 0.8f);
        Muzik = PlayerPrefs.GetInt("ayar_muzik", 1) == 1;
        Application.targetFrameRate = 60;
        Uygula();
    }

    static void Uygula()
    {
        AudioListener.volume = Sessiz ? 0f : Ses;
        Sfx.MuzikCal(Muzik);
    }

    public static void SesAyarla(float v)
    {
        Ses = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("ayar_ses", Ses);
        PlayerPrefs.Save();
        Uygula();
    }

    public static void MuzikAyarla(bool acik)
    {
        Muzik = acik;
        PlayerPrefs.SetInt("ayar_muzik", acik ? 1 : 0);
        PlayerPrefs.Save();
        Uygula();
    }

    public static void SessizDegistir() { Sessiz = !Sessiz; Uygula(); }

    public static bool TamEkran
    {
        get { return Screen.fullScreen; }
        set { Screen.fullScreen = value; }
    }
}
