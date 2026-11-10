using UnityEngine;

public class Rozet
{
    public string id, ad, aciklama;
    public Rozet(string id, string ad, string aciklama) { this.id = id; this.ad = ad; this.aciklama = aciklama; }
}

// Oyuncu ilerlemesi (PlayerPrefs): acik bolumler, yildizlar, en iyi skorlar, rozetler ve surucu unvani.
public static class Kayit
{
    public const int BolumSayisi = 5;

    public static readonly Rozet[] Rozetler =
    {
        new Rozet("ilk_teslimat", "İlk Teslimat", "Herhangi bir teslimatı başarıyla tamamla."),
        new Rozet("damlasi_damlasina", "Damlası Damlasına", "Suyun en az %95'ini teslim et."),
        new Rozet("ekspres", "Ekspres Kurye", "Bir bölümü ekspres sürenin altında bitir."),
        new Rozet("ucan_tanker", "Uçan Tanker", "Tek sıçrayışta 2 saniye havada kal."),
        new Rozet("takla", "Takla Ustası", "Tankerle havada tam tur at."),
        new Rozet("damla_avcisi", "Damla Avcısı", "Bir bölümdeki tüm damla jetonlarını topla."),
        new Rozet("puruzsuz", "Pürüzsüz Sürüş", "Sert inişte hiç su dökmeden bir bölüm bitir."),
        new Rozet("zirve", "Zirvenin Su Kahramanı", "Zirve Köyü'ne suyu ulaştır."),
        new Rozet("yildiz_bayi", "Yıldız Bayi", "Tüm bölümlerde 3 yıldız topla."),
    };

    public static int AcikBolum { get { return Mathf.Clamp(PlayerPrefs.GetInt("acik_bolum", 1), 1, BolumSayisi); } }
    public static bool Acik(int no) { return no <= AcikBolum; }
    public static int Yildiz(int no) { return PlayerPrefs.GetInt("b" + no + "_yildiz", 0); }
    public static int EnIyiSkor(int no) { return PlayerPrefs.GetInt("b" + no + "_skor", 0); }

    public static int ToplamYildiz
    {
        get { int t = 0; for (int i = 1; i <= BolumSayisi; i++) t += Yildiz(i); return t; }
    }

    // Bolum sonucunu kaydeder; yeni rekor ise true
    public static bool BolumSonucu(int no, bool basari, int yildiz, int skor)
    {
        if (!basari) return false;
        bool rekor = skor > EnIyiSkor(no);
        if (rekor) PlayerPrefs.SetInt("b" + no + "_skor", skor);
        if (yildiz > Yildiz(no)) PlayerPrefs.SetInt("b" + no + "_yildiz", yildiz);
        if (no + 1 > AcikBolum && no < BolumSayisi) PlayerPrefs.SetInt("acik_bolum", no + 1);
        PlayerPrefs.Save();
        return rekor;
    }

    public static bool RozetVar(string id) { return PlayerPrefs.GetInt("rozet_" + id, 0) == 1; }

    // Yeni kazanildiysa true
    public static bool RozetVer(string id)
    {
        if (RozetVar(id)) return false;
        PlayerPrefs.SetInt("rozet_" + id, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static int RozetSayisi
    {
        get { int n = 0; foreach (var r in Rozetler) if (RozetVar(r.id)) n++; return n; }
    }

    public static Rozet RozetBul(string id)
    {
        foreach (var r in Rozetler) if (r.id == id) return r;
        return null;
    }

    // Toplam yildiza gore Damla Su surucu unvani
    public static string Unvan
    {
        get
        {
            int y = ToplamYildiz;
            if (y >= 15) return "Damla Su Efsanesi";
            if (y >= 12) return "Usta Kaptan";
            if (y >= 8) return "Kıdemli Şoför";
            if (y >= 4) return "Dağıtım Şoförü";
            return "Stajyer Şoför";
        }
    }

    // Degerlendirme / tanitim videosu icin: tum bolumlerin kilidini acar (bolum secimde F9)
    public static void TumunuAc()
    {
        PlayerPrefs.SetInt("acik_bolum", BolumSayisi);
        PlayerPrefs.Save();
    }

    public static void Sifirla()
    {
        float ses = PlayerPrefs.GetFloat("ayar_ses", 0.8f);
        int muzik = PlayerPrefs.GetInt("ayar_muzik", 1);
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetFloat("ayar_ses", ses);
        PlayerPrefs.SetInt("ayar_muzik", muzik);
        PlayerPrefs.Save();
    }
}
