using UnityEngine;

// Ana menu: Oyna, Ayarlar, Rozetler, Nasil Oynanir, Cikis.
// Arka planda depo onunde bekleyen tanker ve suzulen bulutlar vardir.
public class AnaMenu : MonoBehaviour
{
    enum Pencere { Yok, Ayarlar, Rozetler, Nasil }

    Pencere pencere;
    bool sifirlaOnay;
    float cikisMesajT;

    void Start()
    {
        Time.timeScale = 1f;
        Sfx.MuzikCal(Ayarlar.Muzik);
    }

    void Update()
    {
        if (Tus.Ses()) Ayarlar.SessizDegistir();
        if (pencere != Pencere.Yok && Tus.Duraklat()) { pencere = Pencere.Yok; sifirlaOnay = false; }
        else if (pencere == Pencere.Yok && Tus.Onay()) SahneGecis.Yukle(SahneGecis.BolumSecim);
        if (cikisMesajT > 0f) cikisMesajT -= Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        Arayuz.Basla();
        float vw = Arayuz.VW, vh = Arayuz.VH;

        // sol tarafta okunabilirlik icin koyu gecis
        Arayuz.Kutu(new Rect(0, 0, 120, vh), new Color(0.03f, 0.1f, 0.22f, 0.55f));
        Arayuz.YatayGradyan(new Rect(120, 0, 640, vh), new Color(0.03f, 0.1f, 0.22f, 0.55f));

        // logo
        float sal = Mathf.Sin(Time.unscaledTime * 2f) * 6f;
        Arayuz.Damla(new Rect(96, 70 + sal, 96, 120), Arayuz.AcikMavi);
        Arayuz.Damla(new Rect(118, 108 + sal, 52, 66), Color.white);
        Arayuz.Yaz(new Rect(212, 58, 900, 110), "DAMLA", new GUIStyle(Arayuz.Baslik) { alignment = TextAnchor.MiddleLeft, fontSize = 110 }, Color.white, 5f);
        Arayuz.Yaz(new Rect(212, 150, 900, 80), "EKSPRES", new GUIStyle(Arayuz.Buyuk) { alignment = TextAnchor.MiddleLeft, fontSize = 70 }, Arayuz.Sari, 4f);
        Arayuz.Yaz(new Rect(100, 236, 900, 44), "Damla Su Dağıtım A.Ş. · Su dökülmeden teslim et!", Arayuz.KucukSol, new Color(0.85f, 0.95f, 1f), 2f);

        float x = 100, y = 320, w = 470;
        bool serbest = pencere == Pencere.Yok;
        if (Arayuz.Dugme(new Rect(x, y, w, 96), "OYNA", Arayuz.Yesil, serbest, Arayuz.Buyuk)) SahneGecis.Yukle(SahneGecis.BolumSecim);
        if (Arayuz.Dugme(new Rect(x, y + 116, w, 78), "Ayarlar", Arayuz.Mavi, serbest)) pencere = Pencere.Ayarlar;
        if (Arayuz.Dugme(new Rect(x, y + 212, w, 78), "Rozetler  (" + Kayit.RozetSayisi + "/" + Kayit.Rozetler.Length + ")", Arayuz.Mavi, serbest)) pencere = Pencere.Rozetler;
        if (Arayuz.Dugme(new Rect(x, y + 308, w, 78), "Nasıl Oynanır?", Arayuz.Mavi, serbest)) pencere = Pencere.Nasil;
        if (Arayuz.Dugme(new Rect(x, y + 404, w, 78), "Çıkış", Arayuz.Kirmizi, serbest)) Cikis();

        // surucu karti
        Rect k = new Rect(x, vh - 170, w, 120);
        Arayuz.PanelCiz(k);
        Arayuz.Yaz(new Rect(k.x + 26, k.y + 14, w - 40, 34), "SÜRÜCÜ ÜNVANI", Arayuz.KucukSol, new Color(1, 1, 1, 0.6f), 1f);
        Arayuz.Yaz(new Rect(k.x + 26, k.y + 50, w - 40, 54), Kayit.Unvan, Arayuz.OrtaSol, Arayuz.Sari, 2f);
        Arayuz.Yildiz(new Rect(k.xMax - 150, k.y + 50, 48, 48), true);
        Arayuz.Yaz(new Rect(k.xMax - 100, k.y + 50, 90, 50), Kayit.ToplamYildiz + "/15", Arayuz.KucukSol, Color.white, 2f);

        if (cikisMesajT > 0f)
            Arayuz.Yaz(new Rect(x, y + 490, w, 40), "Tarayıcıda çıkmak için sekmeyi kapatabilirsin.", Arayuz.Minik, Arayuz.AcikMavi, 1f);

        switch (pencere)
        {
            case Pencere.Ayarlar: AyarlarCiz(vw, vh); break;
            case Pencere.Rozetler: RozetlerCiz(vw, vh); break;
            case Pencere.Nasil: NasilCiz(vw, vh); break;
        }
        Arayuz.Yaz(new Rect(vw - 330, vh - 30, 320, 28), Ayarlar.Sessiz ? "Ses kapalı (M)" : "M: ses", Arayuz.Minik, new Color(1, 1, 1, 0.45f), 1f);
        Arayuz.Yaz(new Rect(vw - 330, vh - 58, 320, 28), "Görseller: Kenney.nl (CC0)", Arayuz.Minik, new Color(1, 1, 1, 0.45f), 1f);
    }

    Rect PencereAlani(float vw, float vh, float w, float h)
    {
        float cx = Mathf.Max(vw * 0.5f + 200f, 640f + w * 0.5f);
        cx = Mathf.Min(cx, vw - w * 0.5f - 30f);
        return new Rect(cx - w * 0.5f, vh * 0.5f - h * 0.5f, w, h);
    }

    void AyarlarCiz(float vw, float vh)
    {
        Rect p = PencereAlani(vw, vh, 760, 600);
        Arayuz.PanelCiz(p, "AYARLAR");
        float x = p.x + 50, y = p.y + 110;

        Arayuz.Yaz(new Rect(x, y, 260, 50), "Ses seviyesi", Arayuz.KucukSol, Color.white, 1f);
        float s = Arayuz.Kaydirici(new Rect(x + 270, y + 18, 300, 14), Ayarlar.Ses);
        if (Mathf.Abs(s - Ayarlar.Ses) > 0.001f) Ayarlar.SesAyarla(s);
        Arayuz.Yaz(new Rect(x + 590, y, 100, 50), "%" + Mathf.RoundToInt(Ayarlar.Ses * 100), Arayuz.KucukSol, Color.white, 1f);

        y += 80;
        Arayuz.Yaz(new Rect(x, y, 260, 50), "Müzik", Arayuz.KucukSol, Color.white, 1f);
        bool m = Arayuz.Anahtar(new Rect(x + 270, y + 5, 96, 42), Ayarlar.Muzik);
        if (m != Ayarlar.Muzik) Ayarlar.MuzikAyarla(m);

        y += 80;
        Arayuz.Yaz(new Rect(x, y, 260, 50), "Tam ekran", Arayuz.KucukSol, Color.white, 1f);
        bool t = Arayuz.Anahtar(new Rect(x + 270, y + 5, 96, 42), Ayarlar.TamEkran);
        if (t != Ayarlar.TamEkran) Ayarlar.TamEkran = t;

        y += 90;
        if (!sifirlaOnay)
        {
            if (Arayuz.Dugme(new Rect(x, y, 420, 70), "İlerlemeyi Sıfırla", Arayuz.Gri, true, Arayuz.Kucuk)) sifirlaOnay = true;
        }
        else
        {
            Arayuz.Yaz(new Rect(x, y - 6, 660, 40), "Yıldızlar, skorlar ve rozetler silinsin mi?", Arayuz.KucukSol, new Color(1f, 0.7f, 0.6f), 1f);
            if (Arayuz.Dugme(new Rect(x, y + 36, 200, 64), "Evet, sil", Arayuz.Kirmizi, true, Arayuz.Kucuk)) { Kayit.Sifirla(); sifirlaOnay = false; }
            if (Arayuz.Dugme(new Rect(x + 220, y + 36, 200, 64), "Vazgeç", Arayuz.Gri, true, Arayuz.Kucuk)) sifirlaOnay = false;
        }

        if (Arayuz.Dugme(new Rect(p.center.x - 150, p.yMax - 100, 300, 74), "Kapat", Arayuz.Mavi)) { pencere = Pencere.Yok; sifirlaOnay = false; }
    }

    void RozetlerCiz(float vw, float vh)
    {
        Rect p = PencereAlani(vw, vh, 1000, 820);
        Arayuz.PanelCiz(p, "ROZETLER  " + Kayit.RozetSayisi + "/" + Kayit.Rozetler.Length);
        float cw = (p.width - 80) / 3f, ch = 190;
        for (int i = 0; i < Kayit.Rozetler.Length; i++)
        {
            var r = Kayit.Rozetler[i];
            bool kazanildi = Kayit.RozetVar(r.id);
            Rect c = new Rect(p.x + 40 + (i % 3) * cw, p.y + 96 + (i / 3) * (ch + 10), cw - 14, ch);
            Arayuz.Kutu(c, kazanildi ? new Color(1f, 0.84f, 0.25f, 0.13f) : new Color(1, 1, 1, 0.05f), 18f);
            BolumYonetici.Madalya(new Rect(c.center.x - 40, c.y + 12, 80, 80), kazanildi, 1f);
            Arayuz.Yaz(new Rect(c.x + 8, c.y + 96, c.width - 16, 36), r.ad, Arayuz.Minik, kazanildi ? Arayuz.Sari : new Color(1, 1, 1, 0.6f), 1f);
            Arayuz.Yaz(new Rect(c.x + 12, c.y + 130, c.width - 24, 56), r.aciklama, new GUIStyle(Arayuz.Minik) { fontSize = 19, fontStyle = FontStyle.Normal }, new Color(1, 1, 1, kazanildi ? 0.85f : 0.5f), 0f);
        }
        if (Arayuz.Dugme(new Rect(p.center.x - 150, p.yMax - 96, 300, 72), "Kapat", Arayuz.Mavi)) pencere = Pencere.Yok;
    }

    void NasilCiz(float vw, float vh)
    {
        Rect p = PencereAlani(vw, vh, 1000, 800);
        Arayuz.PanelCiz(p, "NASIL OYNANIR?");
        string m =
            "<b>Görev:</b> Damla Su'nun tankerini depodan müşteriye sür, tanktaki suyu <b>dökmeden</b> teslim et.\n\n" +
            "<b>D / sağ ok</b>  gaz          <b>A / sol ok</b>  fren ve geri vites   (ekrandaki pedallar da olur)\n" +
            "Havadayken gaz burnu kaldırır, fren indirir: inişe hazırlan!\n" +
            "<b>Esc</b> duraklat   ·   <b>R</b> yeniden başla   ·   <b>H</b> korna   ·   <b>M</b> ses\n\n" +
            "<b>Su fiziği:</b> Ani gaz/fren, kasis ve tümseklerde su çalkalanır; dalga büyürse tankın kapağından taşar. Dolu tank daha kolay taşar. " +
            "Sert inişler suyu sıçratır. Su arkaya yığılınca ön teker hafifler, su azaldıkça tanker hafifleyip daha çok zıplar.\n\n" +
            "<b>Puan:</b> teslim edilen her litre, ekspres süre bonusu, damla jetonları, uçuş ve takla.\n" +
            "<b>Yıldız:</b> 1 = hedefi tuttur · 2 = suyun %80'i · 3 = %92 + ekspres süre.\n" +
            "Yıldız topladıkça <b>sürücü ünvanın</b> yükselir, rozetler yeni hedefler sunar.";
        Arayuz.Yaz(new Rect(p.x + 50, p.y + 96, p.width - 100, 580), m, new GUIStyle(Arayuz.MinikSol) { fontSize = 22, richText = true }, Color.white, 0f);
        if (Arayuz.Dugme(new Rect(p.center.x - 150, p.yMax - 96, 300, 72), "Tamam", Arayuz.Mavi)) pencere = Pencere.Yok;
    }

    void Cikis()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        cikisMesajT = 3f;
#else
        Application.Quit();
#endif
    }
}
