using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BolumDurumu { Oyun, Duraklat, Bitti }

// Bir bolumun akisi ve arayuzu:
// surus -> teslimat / basarisizlik kontrolu -> puan, yildiz, musteri memnuniyeti, rozetler.
// Oyun ici gosterge (hiz, kalan su, skor, ilerleme), duraklatma menusu ve sonuc ekrani burada cizilir.
public class BolumYonetici : MonoBehaviour
{
    public static BolumYonetici I;

    [Range(1, 5)] public int bolumNo = 1;

    [Header("Puanlama")]
    public int jetonPuani = 50;
    [Tooltip("Ekspres sureden erken kalan her saniye")] public int saniyeBonusu = 10;
    public int taklaPuani = 300;
    [Tooltip("Havada kalinan her saniye")] public int ucusPuani = 100;
    public int yildizBonusu = 100;

    [Header("Kurallar")]
    [Tooltip("Ters donen tanker bu kadar saniye sonra kaybeder")] public float devrilmeSuresi = 3f;
    [Tooltip("Hiz gostergesinin azami degeri (km/sa)")] public float azamiGosterge = 50f;

    public BolumDurumu Durum { get; private set; }
    public BolumTanim Tanim { get; private set; }
    public float Zaman { get; private set; }
    public int Jeton { get; private set; }
    public int ToplamJeton { get; private set; }
    public int Akrobasi { get; private set; }
    public int CanliSkor { get { return Jeton * jetonPuani + Akrobasi; } }

    AracKontrol arac;
    SuTanki tank;
    float girisT = 4f, devrikT, bitisT, sonucT, skorPop, rozetT;
    bool bitiyor, basari, ayarAcik;
    string neden;
    readonly List<Rozet> yeniRozetler = new List<Rozet>();
    readonly Queue<Rozet> rozetKuyruk = new Queue<Rozet>();
    Rozet gosterilenRozet;

    // sonuc
    int sSu, sSure, sJeton, sAkro, sYildiz, sToplam, yildiz, memnuniyet;
    float teslimOrani;
    bool yeniRekor;

    void Awake()
    {
        I = this;
        Tanim = Bolumler.Tanim(bolumNo);
        Time.timeScale = 1f;
    }

    void Start()
    {
        arac = FindAnyObjectByType<AracKontrol>();
        tank = arac.GetComponentInChildren<SuTanki>();
        arac.Indi += Indi;
        arac.Takla += Takla;
        tank.Darbe += SertInis;
        ToplamJeton = FindObjectsByType<DamlaJeton>(FindObjectsSortMode.None).Length;
        Sfx.MuzikCal(Ayarlar.Muzik);
    }

    void OnDestroy()
    {
        Tus.PedalGaz = Tus.PedalFren = false;
        if (I == this) I = null;
    }

    // ================================================================ akis
    void Update()
    {
        if (Tus.Ses()) Ayarlar.SessizDegistir();
        if (skorPop > 0f) skorPop = Mathf.Max(0f, skorPop - Time.unscaledDeltaTime * 3f);
        Pedallar();

        switch (Durum)
        {
            case BolumDurumu.Oyun:
                if (!bitiyor && Tus.Duraklat()) { Duraklat(); break; }
                if (!bitiyor && Tus.Yeniden()) { YenidenBasla(); break; }
                if (Tus.Korna()) Sfx.Cal("korna", 0.6f, 0f, 0.4f);
                Akis();
                break;
            case BolumDurumu.Duraklat:
                if (Tus.Duraklat()) Devam();
                else if (Tus.Yeniden()) YenidenBasla();
                break;
            case BolumDurumu.Bitti:
                sonucT += Time.unscaledDeltaTime;
                if (sonucT > 1f && Tus.Onay()) { if (basari && bolumNo < Kayit.BolumSayisi) Sonraki(); else YenidenBasla(); }
                else if (Tus.Yeniden()) YenidenBasla();
                break;
        }
    }

    void Akis()
    {
        float dt = Time.deltaTime;
        if (girisT > 0f) girisT -= dt;
        RozetToast(Time.unscaledDeltaTime);

        if (bitiyor)
        {
            bitisT -= Time.unscaledDeltaTime;
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, 0.2f, Time.unscaledDeltaTime * 1.5f);
            Sfx.Motor(false, 0f);
            if (bitisT <= 0f)
            {
                Time.timeScale = 0f;
                Durum = BolumDurumu.Bitti;
                sonucT = 0f;
                if (basari && yildiz > 0) Sfx.Cal("yildiz", 0.7f);
            }
            return;
        }

        Zaman += dt;
        Sfx.Motor(true, Mathf.Clamp01(Mathf.Abs(arac.Giris) * 0.45f + arac.Rb.linearVelocity.magnitude / 14f));

        Vector2 p = arac.transform.position;
        if (p.x >= Tanim.uzunluk) { Bitir(true, null); return; }
        if (tank.Oran < Tanim.hedef) { Bitir(false, "Tankta yeterli su kalmadı!"); return; }
        if (p.y < Bolumler.DusmeSiniri(bolumNo)) { Bitir(false, "Tanker kanyona yuvarlandı!"); return; }
        devrikT = arac.transform.up.y < -0.1f ? devrikT + dt : 0f;
        if (devrikT > devrilmeSuresi) Bitir(false, "Tanker devrildi!");
    }

    void Pedallar()
    {
        bool aktif = Durum == BolumDurumu.Oyun && !bitiyor;
        Tus.PedalFren = aktif && Tus.AlanBasili(Arayuz.Ekran(FrenAlani));
        Tus.PedalGaz = aktif && Tus.AlanBasili(Arayuz.Ekran(GazAlani));
    }

    Rect FrenAlani { get { return new Rect(236, Arayuz.VH - 210, 150, 180); } }
    Rect GazAlani { get { return new Rect(Screen.width / Arayuz.Olcek - 480, Arayuz.VH - 210, 150, 180); } }

    // ================================================================ olaylar
    public void JetonAl(Vector3 pos)
    {
        if (bitiyor) return;
        Jeton++;
        skorPop = 1f;
        Sfx.Cal("damla", 0.7f, Mathf.Min(0.4f, Jeton * 0.02f));
        Efekt.Yazi(pos + Vector3.up * 0.8f, "+" + jetonPuani, Arayuz.AcikMavi, 1f);
    }

    void Indi(float sure)
    {
        if (bitiyor) return;
        if (sure >= 1f)
        {
            int b = Mathf.RoundToInt(sure * ucusPuani / 10f) * 10;
            Akrobasi += b;
            skorPop = 1f;
            Efekt.Yazi(arac.transform.position + Vector3.up * 2.4f, "Uçuş " + sure.ToString("0.0") + " sn! +" + b, Arayuz.Sari, 1.2f);
        }
        if (sure >= 2f) RozetVer("ucan_tanker");
    }

    void Takla()
    {
        if (bitiyor) return;
        Akrobasi += taklaPuani;
        skorPop = 1f;
        Sfx.Cal("basari", 0.6f, 0.2f);
        Efekt.Yazi(arac.transform.position + Vector3.up * 3f, "TAKLA! +" + taklaPuani, Arayuz.Turuncu, 1.5f);
        RozetVer("takla");
    }

    void SertInis(float litre)
    {
        if (bitiyor || litre < 4f) return;
        Efekt.Yazi(arac.transform.position + Vector3.up * 2.2f, "Sert iniş! -" + Mathf.RoundToInt(litre) + " L", new Color(1f, 0.5f, 0.45f), 1f);
    }

    void RozetVer(string id)
    {
        if (!Kayit.RozetVer(id)) return;
        var r = Kayit.RozetBul(id);
        yeniRozetler.Add(r);
        rozetKuyruk.Enqueue(r);
    }

    void RozetToast(float dt)
    {
        if (gosterilenRozet == null && rozetKuyruk.Count > 0)
        {
            gosterilenRozet = rozetKuyruk.Dequeue();
            rozetT = 3f;
            Sfx.Cal("basari", 0.7f, 0.1f);
        }
        if (gosterilenRozet != null) { rozetT -= dt; if (rozetT <= 0f) gosterilenRozet = null; }
    }

    // ================================================================ bitis
    void Bitir(bool ok, string sebep)
    {
        if (bitiyor) return;
        bitiyor = true;
        basari = ok;
        neden = sebep;
        bitisT = 1.8f;
        arac.kontrolAktif = false;
        Hesapla();
        Vector3 ust = arac.transform.position + Vector3.up * 2.6f;
        if (ok) { Sfx.Cal("basari", 0.8f); Efekt.Yazi(ust, "TESLİM EDİLDİ!", new Color(0.5f, 1f, 0.6f), 1.8f); }
        else { Sfx.Cal("bitti", 0.8f, 0f, 0f); Kamera.Salla(0.4f); Efekt.Yazi(ust, sebep, new Color(1f, 0.45f, 0.4f), 1.4f); }
    }

    void Hesapla()
    {
        teslimOrani = tank.Oran;
        float gec = Mathf.Max(0f, Zaman - Tanim.parSure);
        memnuniyet = basari ? Mathf.RoundToInt(Mathf.Clamp(teslimOrani * 100f - gec * 0.5f, 0f, 100f)) : 0;
        yildiz = !basari ? 0 : (teslimOrani >= 0.92f && Zaman <= Tanim.parSure) ? 3 : (teslimOrani >= 0.8f ? 2 : 1);
        sSu = basari ? Mathf.RoundToInt(tank.Litre) : 0;
        sSure = basari ? Mathf.RoundToInt(Mathf.Max(0f, Tanim.parSure - Zaman) * saniyeBonusu) : 0;
        sJeton = Jeton * jetonPuani;
        sAkro = Akrobasi;
        sYildiz = yildiz * yildizBonusu;
        sToplam = sSu + sSure + sJeton + sAkro + sYildiz;
        yeniRekor = Kayit.BolumSonucu(bolumNo, basari, yildiz, sToplam);

        if (!basari) return;
        RozetVer("ilk_teslimat");
        if (teslimOrani >= 0.95f) RozetVer("damlasi_damlasina");
        if (Zaman <= Tanim.parSure) RozetVer("ekspres");
        if (ToplamJeton > 0 && Jeton >= ToplamJeton) RozetVer("damla_avcisi");
        if (tank.DarbeKaybi < 1f) RozetVer("puruzsuz");
        if (bolumNo == Kayit.BolumSayisi) RozetVer("zirve");
        if (Kayit.ToplamYildiz >= Kayit.BolumSayisi * 3) RozetVer("yildiz_bayi");
    }

    // ================================================================ sahne gecisleri
    void Duraklat() { Durum = BolumDurumu.Duraklat; Time.timeScale = 0f; Sfx.Motor(false, 0f); ayarAcik = false; }
    void Devam() { Durum = BolumDurumu.Oyun; Time.timeScale = 1f; }
    void YenidenBasla() { SahneGecis.Yukle(SceneManager.GetActiveScene().name); }
    void Sonraki() { SahneGecis.Yukle(SahneGecis.Bolum(bolumNo + 1)); }

    // ================================================================ ARAYUZ
    void OnGUI()
    {
        Arayuz.Basla();
        switch (Durum)
        {
            case BolumDurumu.Oyun: Gosterge(); break;
            case BolumDurumu.Duraklat: Gosterge(); DuraklatCiz(); break;
            case BolumDurumu.Bitti: SonucCiz(); break;
        }
        Arayuz.Yaz(new Rect(Arayuz.VW - 330, Arayuz.VH - 30, 320, 28), Ayarlar.Sessiz ? "Ses kapalı (M)" : "M: ses  ·  R: yeniden", Arayuz.Minik, new Color(1, 1, 1, 0.45f), 1f);
    }

    void Gosterge()
    {
        float vw = Arayuz.VW, vh = Arayuz.VH;
        SkorPaneli();
        IlerlemePaneli(vw);
        SuGostergesi(vh);
        HizGostergesi(vw, vh);
        Pedal(FrenAlani, "FREN", Tus.Surus() < -0.05f, Arayuz.Kirmizi);
        Pedal(GazAlani, "GAZ", Tus.Surus() > 0.05f, Arayuz.Yesil);

        // duraklat dugmesi
        if (!bitiyor && Arayuz.Dugme(new Rect(vw - 112, 22, 90, 90), "II", Arayuz.Mavi, true, Arayuz.Buyuk)) Duraklat();

        // bolum basi afisi
        if (girisT > 0f)
        {
            float a = Mathf.Clamp01(girisT / 0.6f) * Mathf.Clamp01((4f - girisT) / 0.3f);
            Rect r = new Rect(vw * 0.5f - 520, vh * 0.27f, 1040, 230);
            Arayuz.Kutu(r, new Color(0.04f, 0.12f, 0.25f, 0.85f * a), 30f);
            Arayuz.Yaz(new Rect(r.x, r.y + 14, r.width, 50), "BÖLÜM " + bolumNo, Arayuz.Orta, new Color(Arayuz.Turkuaz.r, Arayuz.Turkuaz.g, Arayuz.Turkuaz.b, a));
            Arayuz.Yaz(new Rect(r.x, r.y + 58, r.width, 100), Tanim.ad, Arayuz.Baslik, new Color(1f, 0.92f, 0.4f, a), 4f);
            Arayuz.Yaz(new Rect(r.x, r.y + 160, r.width, 50), Tanim.musteri + " su bekliyor: suyun en az %" + Mathf.RoundToInt(Tanim.hedef * 100) + "'ini ulaştır!", Arayuz.Kucuk, new Color(1, 1, 1, a));
        }

        // devrilme uyarisi
        if (devrikT > 0.2f && !bitiyor)
        {
            float k = Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.5f ? 1f : 0.7f;
            Arayuz.Yaz(new Rect(vw * 0.5f - 500, vh * 0.42f, 1000, 90), "TANKER DEVRİLDİ! " + Mathf.CeilToInt(devrilmeSuresi - devrikT), Arayuz.Buyuk, new Color(1f, 0.4f, 0.35f, k), 4f);
            Arayuz.Yaz(new Rect(vw * 0.5f - 500, vh * 0.42f + 80, 1000, 50), "Gaz / fren ile kendini düzelt", Arayuz.Kucuk, Color.white);
        }

        // rozet bildirimi
        if (gosterilenRozet != null)
        {
            float a = Mathf.Clamp01(rozetT / 0.4f) * Mathf.Clamp01((3f - rozetT) / 0.25f);
            Rect r = new Rect(vw - 560, 130, 520, 110);
            Arayuz.Kutu(r, new Color(0.04f, 0.12f, 0.25f, 0.92f * a), 20f);
            Arayuz.Cerceve(r, new Color(1f, 0.84f, 0.25f, a), 3f, 20f);
            Madalya(new Rect(r.x + 16, r.y + 15, 80, 80), true, a);
            Arayuz.Yaz(new Rect(r.x + 112, r.y + 12, 400, 40), "YENİ ROZET!", Arayuz.KucukSol, new Color(1f, 0.84f, 0.25f, a), 1f);
            Arayuz.Yaz(new Rect(r.x + 112, r.y + 50, 400, 50), gosterilenRozet.ad, Arayuz.OrtaSol, new Color(1, 1, 1, a), 2f);
        }
    }

    void SkorPaneli()
    {
        Rect r = new Rect(22, 22, 400, 172);
        Arayuz.PanelCiz(r);
        Arayuz.Yaz(new Rect(r.x + 26, r.y + 12, 300, 34), "SKOR", Arayuz.KucukSol, new Color(1, 1, 1, 0.6f), 1f);
        var st = Arayuz.OrtaSol;
        int eski = st.fontSize;
        st.fontSize = Mathf.RoundToInt(58 * (1f + skorPop * 0.2f));
        Arayuz.Yaz(new Rect(r.x + 26, r.y + 44, 360, 66), CanliSkor.ToString("N0"), st, Arayuz.Sari);
        st.fontSize = eski;
        Arayuz.Damla(new Rect(r.x + 26, r.y + 114, 34, 44), Arayuz.AcikMavi);
        Arayuz.Yaz(new Rect(r.x + 68, r.y + 116, 100, 40), Jeton + "/" + ToplamJeton, Arayuz.KucukSol, Color.white, 2f);
        // sure / ekspres sure: ekspres gecilince kirmiziya doner
        bool ekspres = Zaman <= Tanim.parSure;
        Arayuz.Yaz(new Rect(r.x + 160, r.y + 116, 230, 40), "Süre " + Arayuz.Sure(Zaman) + " / " + Arayuz.Sure(Tanim.parSure), new GUIStyle(Arayuz.KucukSol) { wordWrap = false }, ekspres ? Arayuz.AcikMavi : new Color(1f, 0.6f, 0.5f), 2f);
    }

    void IlerlemePaneli(float vw)
    {
        // skor paneli (sol) ile duraklat dugmesi (sag) arasina sigar
        float x0 = Mathf.Max(vw * 0.5f - 410f, 442f), x1 = Mathf.Min(vw * 0.5f + 410f, vw - 132f);
        float w = Mathf.Max(300f, x1 - x0);
        Rect r = new Rect(x0, 22, w, 118);
        Arayuz.PanelCiz(r);
        Arayuz.Yaz(new Rect(r.x + 24, r.y + 10, w - 48, 40), "Bölüm " + bolumNo + " · " + Tanim.ad, new GUIStyle(Arayuz.KucukSol) { wordWrap = false, clipping = TextClipping.Clip }, Color.white, 2f);

        Rect bar = new Rect(r.x + 64, r.y + 70, w - 128, 16);
        float oran = Mathf.Clamp01(arac.transform.position.x / Tanim.uzunluk);
        Arayuz.Bar(bar, oran, Arayuz.Turkuaz, new Color(1, 1, 1, 0.15f));
        Arayuz.Damla(new Rect(r.x + 22, r.y + 58, 30, 38), Arayuz.AcikMavi);
        // teslimat bayragi
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
                Arayuz.Kutu(new Rect(r.xMax - 50 + i * 10, r.y + 62 + j * 10, 10, 10), (i + j) % 2 == 0 ? Color.white : Color.black);
        Arayuz.Kutu(new Rect(r.xMax - 52, r.y + 62, 3, 34), Color.white);
        // tanker isaretcisi
        float mx = bar.x + bar.width * oran;
        Arayuz.Kutu(new Rect(mx - 20, bar.y - 14, 40, 20), Arayuz.Kirmizi, 5f);
        Arayuz.Kutu(new Rect(mx - 18, bar.y - 22, 20, 12), new Color(0.75f, 0.9f, 1f), 4f);
        Arayuz.Kutu(new Rect(mx - 16, bar.y + 2, 11, 11), Color.black, 5.5f);
        Arayuz.Kutu(new Rect(mx + 6, bar.y + 2, 11, 11), Color.black, 5.5f);
    }

    void SuGostergesi(float vh)
    {
        Rect g = new Rect(22, vh - 392, 196, 366);
        Arayuz.PanelCiz(g);
        Arayuz.Yaz(new Rect(g.x, g.y + 10, g.width, 36), "KALAN SU", Arayuz.Kucuk, new Color(1, 1, 1, 0.75f), 1f);

        Rect t = new Rect(g.x + 48, g.y + 52, 100, 178);
        Arayuz.Kutu(t, new Color(1, 1, 1, 0.1f), 18f);
        Arayuz.Cerceve(t, new Color(0.7f, 0.9f, 1f, 0.6f), 3f, 18f);
        float oran = tank.Oran;
        bool kritik = oran < Tanim.hedef + 0.08f;
        bool dokuluyor = tank.DokulmeHizi > 15f;
        float yan = Mathf.Repeat(Time.unscaledTime * 4f, 1f) < 0.5f ? 1f : 0f;
        Color su = kritik ? Color.Lerp(Arayuz.Kirmizi, Arayuz.Mavi, yan * 0.6f) : Arayuz.Mavi;
        float ic = t.height - 12f;
        float sx = dokuluyor ? Random.Range(-2f, 2f) : 0f;
        if (oran > 0.005f)
        {
            Rect f = new Rect(t.x + 6 + sx, t.yMax - 6 - ic * oran, t.width - 12, ic * oran);
            Arayuz.Kutu(f, su, 12f);
            Arayuz.Kutu(new Rect(f.x, f.y, f.width, Mathf.Min(8f, f.height)), new Color(1, 1, 1, 0.3f), 4f);
        }
        // hedef cizgisi
        float hy = t.yMax - 6 - ic * Tanim.hedef;
        Arayuz.Kutu(new Rect(t.x - 14, hy - 2, t.width + 28, 4), Arayuz.Sari);
        Arayuz.Yaz(new Rect(g.x, t.yMax + 4, g.width, 60), "%" + Mathf.RoundToInt(oran * 100f), Arayuz.Buyuk, kritik ? new Color(1f, 0.55f, 0.5f) : Color.white);
        Arayuz.Yaz(new Rect(g.x, t.yMax + 62, g.width, 30), Mathf.RoundToInt(tank.Litre) + " / " + Mathf.RoundToInt(tank.kapasiteLitre) + " L", Arayuz.Minik, new Color(1, 1, 1, 0.75f), 1f);
        Arayuz.Yaz(new Rect(g.x, t.yMax + 86, g.width, 26), "Hedef %" + Mathf.RoundToInt(Tanim.hedef * 100f), Arayuz.Minik, Arayuz.Sari, 1f);
        if (dokuluyor && !bitiyor) Arayuz.Yaz(new Rect(g.xMax + 10, g.y + 120, 260, 40), "Su dökülüyor!", Arayuz.KucukSol, new Color(1f, 0.6f, 0.5f, 0.6f + yan * 0.4f), 2f);
    }

    void HizGostergesi(float vw, float vh)
    {
        Rect r = new Rect(vw - 302, vh - 302, 280, 280);
        Vector2 c = r.center;
        Arayuz.Kutu(new Rect(r.x + 6, r.y + 8, r.width, r.height), new Color(0, 0, 0, 0.3f), 140f);
        Arayuz.Kutu(r, Arayuz.Panel, 140f);
        Arayuz.Cerceve(r, new Color(0.55f, 0.85f, 1f, 0.35f), 3f, 140f);
        float hiz = arac.HizKmSa;
        float k = Mathf.Clamp01(hiz / azamiGosterge);
        const int N = 25;
        for (int i = 0; i < N; i++)
        {
            float u = i / (N - 1f);
            float aci = -135f + 270f * u;
            Color col = u <= k + 0.001f && k > 0.01f ? Color.Lerp(Arayuz.Turkuaz, Arayuz.Kirmizi, u) : new Color(1, 1, 1, 0.15f);
            float uzun = i % 6 == 0 ? 30f : 20f;
            Arayuz.Dondur(aci, c, () => Arayuz.Kutu(new Rect(c.x - 4, r.y + 18, 8, uzun), col, 3f));
        }
        Arayuz.Yaz(new Rect(r.x, c.y - 46, r.width, 80), Mathf.RoundToInt(hiz).ToString(), Arayuz.Buyuk, Color.white);
        Arayuz.Yaz(new Rect(r.x, c.y + 26, r.width, 36), "km/sa", Arayuz.Kucuk, new Color(1, 1, 1, 0.6f), 1f);
        if (arac.Havada && arac.HavaSuresi > 0.4f)
            Arayuz.Yaz(new Rect(r.x, c.y + 66, r.width, 36), "HAVADA " + arac.HavaSuresi.ToString("0.0"), Arayuz.Minik, Arayuz.Sari, 1f);
    }

    void Pedal(Rect r, string yazi, bool basili, Color renk)
    {
        // Kenney kare buton: basiliyken asagi iner ve opaklasir
        float a = basili ? 1f : 0.55f;
        Rect rr = basili ? new Rect(r.x, r.y + 8, r.width, r.height - 8) : r;
        Arayuz.Dilim(rr, renk == Arayuz.Kirmizi ? "Red" : "Green", Arayuz.KareTur, new Color(1, 1, 1, a));
        for (int i = 0; i < 4; i++) Arayuz.Kutu(new Rect(rr.x + 30, rr.y + 26 + i * 20, rr.width - 60, 8), new Color(0, 0, 0, 0.18f * a), 4f);
        Arayuz.Yaz(new Rect(rr.x, rr.yMax - 62, rr.width, 40), yazi, Arayuz.Kucuk, new Color(1, 1, 1, a), 2f);
    }

    // ---------------------------------------------------------------- duraklatma
    void DuraklatCiz()
    {
        float vw = Arayuz.VW, vh = Arayuz.VH, cx = vw * 0.5f;
        Arayuz.Kutu(new Rect(0, 0, vw, vh), new Color(0.02f, 0.05f, 0.1f, 0.6f));
        float ph = ayarAcik ? 630f : 520f;
        Rect p = new Rect(cx - 300, vh * 0.5f - ph * 0.5f, 600, ph);
        Arayuz.PanelCiz(p, "DURAKLATILDI");
        float y = p.y + 100;
        if (Arayuz.Dugme(new Rect(cx - 230, y, 460, 84), "Devam Et", Arayuz.Yesil)) Devam();
        if (Arayuz.Dugme(new Rect(cx - 230, y + 100, 460, 84), "Yeniden Başlat", Arayuz.Mavi)) YenidenBasla();
        if (Arayuz.Dugme(new Rect(cx - 230, y + 200, 460, 84), ayarAcik ? "Ses Ayarları  -" : "Ses Ayarları  +", Arayuz.Gri)) ayarAcik = !ayarAcik;
        float yy = y + 300;
        if (ayarAcik)
        {
            Arayuz.Yaz(new Rect(cx - 230, yy, 160, 50), "Ses", Arayuz.KucukSol, Color.white, 1f);
            float s = Arayuz.Kaydirici(new Rect(cx - 120, yy + 18, 260, 14), Ayarlar.Ses);
            if (Mathf.Abs(s - Ayarlar.Ses) > 0.001f) Ayarlar.SesAyarla(s);
            Arayuz.Yaz(new Rect(cx + 160, yy, 70, 50), "%" + Mathf.RoundToInt(Ayarlar.Ses * 100), Arayuz.KucukSol, Color.white, 1f);
            Arayuz.Yaz(new Rect(cx - 230, yy + 50, 200, 50), "Müzik", Arayuz.KucukSol, Color.white, 1f);
            bool m = Arayuz.Anahtar(new Rect(cx + 120, yy + 58, 90, 40), Ayarlar.Muzik);
            if (m != Ayarlar.Muzik) Ayarlar.MuzikAyarla(m);
            yy += 110;
        }
        if (Arayuz.Dugme(new Rect(cx - 230, yy, 220, 80), "Bölümler", Arayuz.Gri, true, Arayuz.Kucuk)) SahneGecis.Yukle(SahneGecis.BolumSecim);
        if (Arayuz.Dugme(new Rect(cx + 10, yy, 220, 80), "Ana Menü", Arayuz.Kirmizi, true, Arayuz.Kucuk)) SahneGecis.Yukle(SahneGecis.AnaMenu);
    }

    // ---------------------------------------------------------------- sonuc ekrani
    void SonucCiz()
    {
        float vw = Arayuz.VW, vh = Arayuz.VH, cx = vw * 0.5f;
        Arayuz.Kutu(new Rect(0, 0, vw, vh), new Color(0.02f, 0.06f, 0.13f, Mathf.Clamp01(sonucT * 3f) * 0.82f));
        var eskiMatris = Arayuz.Sigdir(1480f);
        SonucIcerik(vw, vh, cx);
        GUI.matrix = eskiMatris;
    }

    void SonucIcerik(float vw, float vh, float cx)
    {

        Color baslikRenk = basari ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 0.5f, 0.45f);
        Arayuz.Yaz(new Rect(cx - 800, 40, 1600, 110), basari ? "TESLİMAT TAMAMLANDI!" : "TESLİMAT BAŞARISIZ", Arayuz.Baslik, baslikRenk, 5f);
        Arayuz.Yaz(new Rect(cx - 800, 140, 1600, 46), "Bölüm " + bolumNo + " · " + Tanim.ad + (basari ? "" : "  —  " + neden), Arayuz.Orta, Color.white);

        // yildizlar (sirayla belirir)
        for (int i = 0; i < 3; i++)
        {
            float k = Mathf.Clamp01((sonucT - 0.3f - i * 0.25f) * 4f);
            bool dolu = i < yildiz && k > 0f;
            float boy = 120f * (dolu ? (1f + (1f - k) * 0.6f) : 1f);
            float bx = cx + (i - 1) * 150f;
            float by = i == 1 ? 196f : 216f;
            Arayuz.Yildiz(new Rect(bx - boy * 0.5f, by + 60 - boy * 0.5f, boy, boy), dolu);
        }

        // sol panel: su ve musteri
        Rect sp = new Rect(cx - 600, 370, 570, 400);
        Arayuz.PanelCiz(sp, "TESLİM EDİLEN SU");
        Arayuz.Yaz(new Rect(sp.x, sp.y + 76, sp.width, 120), "%" + Mathf.RoundToInt(teslimOrani * 100f), Arayuz.Dev, basari ? Color.white : new Color(1f, 0.6f, 0.55f), 4f);
        Arayuz.Yaz(new Rect(sp.x, sp.y + 190, sp.width, 36), Mathf.RoundToInt(teslimOrani * tank.kapasiteLitre) + " / " + Mathf.RoundToInt(tank.kapasiteLitre) + " litre  ·  hedef %" + Mathf.RoundToInt(Tanim.hedef * 100f), Arayuz.Kucuk, new Color(1, 1, 1, 0.75f), 1f);
        Rect bar = new Rect(sp.x + 40, sp.y + 236, sp.width - 80, 18);
        Arayuz.Bar(bar, teslimOrani, basari ? Arayuz.Turkuaz : Arayuz.Kirmizi, new Color(1, 1, 1, 0.12f));
        Arayuz.Kutu(new Rect(bar.x + bar.width * Tanim.hedef - 2, bar.y - 6, 4, 30), Arayuz.Sari);
        Arayuz.Yaz(new Rect(sp.x + 40, sp.y + 270, 330, 40), "Müşteri memnuniyeti", Arayuz.KucukSol, Color.white, 1f);
        Arayuz.Yaz(new Rect(sp.xMax - 160, sp.y + 270, 120, 40), "%" + memnuniyet, new GUIStyle(Arayuz.Orta) { alignment = TextAnchor.MiddleRight }, MemnuniyetRengi(), 2f);
        Arayuz.Yaz(new Rect(sp.x + 40, sp.y + 312, sp.width - 80, 80), "<i>\"" + Yorum() + "\"</i>\n— " + Tanim.musteri, new GUIStyle(Arayuz.MinikSol) { alignment = TextAnchor.UpperLeft, fontSize = 18 }, new Color(0.8f, 0.92f, 1f), 1f);

        // sag panel: puan dokumu
        Rect pp = new Rect(cx + 30, 370, 570, 400);
        Arayuz.PanelCiz(pp, "PUAN");
        Satir(pp, 0, "Teslim edilen su", sSu);
        Satir(pp, 1, "Ekspres bonusu", sSure);
        Satir(pp, 2, "Damla jetonu (" + Jeton + "/" + ToplamJeton + ")", sJeton);
        Satir(pp, 3, "Akrobasi", sAkro);
        Satir(pp, 4, "Yıldız bonusu", sYildiz);
        Arayuz.Kutu(new Rect(pp.x + 30, pp.y + 300, pp.width - 60, 3), new Color(1, 1, 1, 0.25f));
        Arayuz.Yaz(new Rect(pp.x + 40, pp.y + 310, 250, 60), "TOPLAM", Arayuz.OrtaSol, Color.white);
        Arayuz.Yaz(new Rect(pp.xMax - 290, pp.y + 306, 250, 66), sToplam.ToString("N0"), new GUIStyle(Arayuz.Buyuk) { alignment = TextAnchor.MiddleRight }, Arayuz.Sari, 3f);
        if (basari)
        {
            if (yeniRekor)
            {
                float n = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f;
                Rect yr = new Rect(pp.center.x - 120 * n, pp.yMax - 24, 240 * n, 44);
                Arayuz.Kutu(yr, Arayuz.Turuncu, 14f);
                Arayuz.Yaz(yr, "YENİ REKOR!", Arayuz.Kucuk, Color.white, 2f);
            }
            else Arayuz.Yaz(new Rect(pp.x, pp.yMax - 20, pp.width, 36), "En iyi: " + Kayit.EnIyiSkor(bolumNo).ToString("N0"), Arayuz.Minik, new Color(1, 1, 1, 0.6f), 1f);
        }

        // yeni rozetler
        float ry = 800;
        if (yeniRozetler.Count > 0)
        {
            float w = yeniRozetler.Count * 300f;
            float x = cx - w * 0.5f;
            foreach (var r in yeniRozetler)
            {
                Rect rr = new Rect(x + 8, ry, 284, 64);
                Arayuz.Kutu(rr, new Color(1f, 0.84f, 0.25f, 0.18f), 32f);
                Madalya(new Rect(rr.x + 6, rr.y + 6, 52, 52), true, 1f);
                Arayuz.Yaz(new Rect(rr.x + 66, rr.y, 214, rr.height), r.ad, new GUIStyle(Arayuz.Minik) { alignment = TextAnchor.MiddleLeft }, Color.white, 1f);
                x += 300f;
            }
        }

        // dugmeler
        float by2 = 900;
        var dugmeler = new List<KeyValuePair<string, System.Action>>();
        if (basari && bolumNo < Kayit.BolumSayisi) dugmeler.Add(new KeyValuePair<string, System.Action>("Sonraki Bölüm >", Sonraki));
        dugmeler.Add(new KeyValuePair<string, System.Action>(basari ? "Tekrar Oyna" : "Tekrar Dene", YenidenBasla));
        dugmeler.Add(new KeyValuePair<string, System.Action>("Bölüm Seçimi", () => SahneGecis.Yukle(SahneGecis.BolumSecim)));
        dugmeler.Add(new KeyValuePair<string, System.Action>("Ana Menü", () => SahneGecis.Yukle(SahneGecis.AnaMenu)));
        float dw = 330f, top = dugmeler.Count * dw + (dugmeler.Count - 1) * 24f;
        float dx = cx - top * 0.5f;
        for (int i = 0; i < dugmeler.Count; i++)
        {
            Color renk = i == 0 ? Arayuz.Yesil : (dugmeler[i].Key == "Ana Menü" ? Arayuz.Kirmizi : Arayuz.Mavi);
            if (Arayuz.Dugme(new Rect(dx + i * (dw + 24f), by2, dw, 86), dugmeler[i].Key, renk) && sonucT > 0.5f) dugmeler[i].Value();
        }
    }

    void Satir(Rect p, int i, string ad, int puan)
    {
        float y = p.y + 84 + i * 42;
        Arayuz.Yaz(new Rect(p.x + 40, y, 360, 40), ad, Arayuz.KucukSol, new Color(1, 1, 1, 0.8f), 1f);
        Arayuz.Yaz(new Rect(p.xMax - 240, y, 200, 40), puan > 0 ? "+" + puan.ToString("N0") : "0", new GUIStyle(Arayuz.Kucuk) { alignment = TextAnchor.MiddleRight }, Color.white, 1f);
    }

    Color MemnuniyetRengi()
    {
        return memnuniyet >= 85 ? new Color(0.5f, 1f, 0.6f) : memnuniyet >= 65 ? Arayuz.Sari : new Color(1f, 0.55f, 0.5f);
    }

    string Yorum()
    {
        if (!basari) return "Hâlâ su bekliyoruz... Bir daha deneyin lütfen!";
        if (memnuniyet >= 95) return "Bu ne hız, bu ne özen! Damla Su'dan şaşmayız.";
        if (memnuniyet >= 85) return "Tertemiz teslimat, ellerinize sağlık!";
        if (memnuniyet >= 70) return "Biraz dökülmüş ama yine de teşekkürler.";
        return "Su geldi ama tank yarı boş gibi...";
    }

    // Altin / gri rozet madalyasi
    public static void Madalya(Rect r, bool kazanildi, float a)
    {
        Color dis = kazanildi ? new Color(0.95f, 0.7f, 0.15f, a) : new Color(0.4f, 0.45f, 0.55f, a);
        Color ic = kazanildi ? new Color(1f, 0.86f, 0.35f, a) : new Color(0.28f, 0.32f, 0.4f, a);
        Arayuz.Kutu(r, dis, r.width * 0.5f);
        Rect i2 = new Rect(r.x + r.width * 0.12f, r.y + r.height * 0.12f, r.width * 0.76f, r.height * 0.76f);
        Arayuz.Kutu(i2, ic, i2.width * 0.5f);
        Rect d = new Rect(r.center.x - r.width * 0.2f, r.center.y - r.height * 0.27f, r.width * 0.4f, r.height * 0.5f);
        Arayuz.Damla(d, kazanildi ? new Color(0.13f, 0.53f, 0.9f, a) : new Color(1, 1, 1, 0.25f * a));
    }
}
