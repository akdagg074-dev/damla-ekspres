using UnityEngine;

// Bolum secim ekrani: 5 bolum karti (arazi onizlemesi, zorluk, hedef, yildiz, en iyi skor, kilit).
public class BolumSecim : MonoBehaviour
{
    float acilis, kilitMesajT;

    void Start()
    {
        Time.timeScale = 1f;
        Sfx.MuzikCal(Ayarlar.Muzik);
    }

    void Update()
    {
        acilis += Time.unscaledDeltaTime;
        if (Tus.Ses()) Ayarlar.SessizDegistir();
        if (kilitMesajT > 0f) kilitMesajT -= Time.unscaledDeltaTime;
        if (Tus.KilitAc()) { Kayit.TumunuAc(); kilitMesajT = 2.5f; Sfx.Cal("basari", 0.6f); }
        if (Tus.Duraklat()) SahneGecis.Yukle(SahneGecis.AnaMenu);
        else if (Tus.Onay()) SahneGecis.Yukle(SahneGecis.Bolum(Kayit.AcikBolum));
    }

    void OnGUI()
    {
        Arayuz.Basla();
        float ekranW = Arayuz.VW, vh = Arayuz.VH, cx = ekranW * 0.5f;
        Arayuz.Kutu(new Rect(0, 0, ekranW, vh), new Color(0.03f, 0.1f, 0.22f, 0.45f));

        // dar ekranlarda 1800 genislikte tasarlanan sahne orantili kuculur
        const float Tasarim = 1800f;
        var eski = Arayuz.Sigdir(Tasarim);
        float vw = Mathf.Max(ekranW, Tasarim);
        float sol = cx - vw * 0.5f;

        if (Arayuz.Dugme(new Rect(sol + 40, 36, 250, 74), "< Ana Menü", Arayuz.Gri, true, Arayuz.Kucuk)) SahneGecis.Yukle(SahneGecis.AnaMenu);
        Arayuz.Yaz(new Rect(cx - 600, 40, 1200, 100), "BÖLÜM SEÇ", Arayuz.Baslik, Color.white, 5f);
        Arayuz.Yaz(new Rect(cx - 600, 136, 1200, 44), "Her bölümde yol uzar, yokuşlar dikleşir, müşteri daha çok su ister.", Arayuz.Kucuk, new Color(0.85f, 0.95f, 1f), 2f);

        Rect yk = new Rect(sol + vw - 330, 40, 290, 70);
        Arayuz.PanelCiz(yk);
        Arayuz.Yildiz(new Rect(yk.x + 20, yk.y + 11, 48, 48), true);
        Arayuz.Yaz(new Rect(yk.x + 80, yk.y, 200, 70), Kayit.ToplamYildiz + " / 15", Arayuz.OrtaSol, Color.white, 2f);

        int n = Kayit.BolumSayisi;
        float gap = 26f;
        float cw = Mathf.Min(320f, (vw - 80f - gap * (n - 1)) / n);
        float ch = 650f;
        float x0 = cx - (cw * n + gap * (n - 1)) * 0.5f;
        for (int i = 0; i < n; i++)
        {
            float k = Mathf.Clamp01((acilis - i * 0.08f) * 4f);
            float yy = 230f + (1f - k) * 80f;
            Kart(new Rect(x0 + i * (cw + gap), yy, cw, ch), i + 1, k);
        }
        Arayuz.Yaz(new Rect(cx - 600, vh - 70, 1200, 40), "Sonraki bölüm, öncekini tamamlayınca açılır.  ·  Enter: son açılan bölüm", Arayuz.Minik, new Color(1, 1, 1, 0.55f), 1f);
        if (kilitMesajT > 0f)
        {
            Rect m = new Rect(cx - 300, vh - 170, 600, 70);
            Arayuz.PanelCiz(m);
            Arayuz.Yaz(m, "Tüm bölümlerin kilidi açıldı", Arayuz.Orta, Arayuz.Sari, 2f);
        }
        GUI.matrix = eski;
    }

    void Kart(Rect r, int no, float a)
    {
        var t = Bolumler.Tanim(no);
        bool acik = Kayit.Acik(no);
        bool ust = acik && Arayuz.Uzerinde(r);
        Rect rr = ust ? new Rect(r.x, r.y - 8, r.width, r.height) : r;

        Arayuz.Kutu(new Rect(rr.x + 6, rr.y + 10, rr.width, rr.height), new Color(0, 0, 0, 0.25f * a), 22f);
        Color ton = Arayuz.PanelTonu; ton.a *= a;
        Arayuz.Dilim(rr, "Grey", Arayuz.PanelTur, ton);
        if (ust) Arayuz.Cerceve(new Rect(rr.x - 3, rr.y - 3, rr.width + 6, rr.height + 3), new Color(1f, 0.84f, 0.25f, a), 4f, 24f);

        // ust serit: bolum renginde Kenney paneli
        Color serit = Color.Lerp(t.gokUst, Color.white, 0.15f); serit.a = a;
        Arayuz.Dilim(new Rect(rr.x, rr.y, rr.width, 100), "Grey", Arayuz.PanelTur, serit);
        Arayuz.Yaz(new Rect(rr.x + 20, rr.y + 8, 80, 80), no.ToString(), new GUIStyle(Arayuz.Buyuk) { fontSize = 70 }, new Color(1, 1, 1, a), 3f);
        Arayuz.Yaz(new Rect(rr.x + 96, rr.y + 10, rr.width - 108, 76), t.ad, new GUIStyle(Arayuz.Orta) { alignment = TextAnchor.MiddleLeft, fontSize = 30 }, new Color(1, 1, 1, a), 2f);

        // arazi onizlemesi
        Rect on = new Rect(rr.x + 16, rr.y + 110, rr.width - 32, 130);
        Arayuz.Kutu(on, new Color(t.gokAlt.r, t.gokAlt.g, t.gokAlt.b, 0.9f * a), 12f);
        Onizleme(on, no, t, a);

        float y = on.yMax + 14;
        Arayuz.Yaz(new Rect(rr.x + 20, y, rr.width - 40, 70), t.aciklama, new GUIStyle(Arayuz.MinikSol) { fontSize = 17 }, new Color(1, 1, 1, 0.8f * a), 0f);
        y += 78;
        Arayuz.Yaz(new Rect(rr.x + 20, y, 120, 34), "Zorluk", Arayuz.KucukSol, new Color(1, 1, 1, 0.7f * a), 1f);
        for (int i = 0; i < 5; i++)
            Arayuz.Damla(new Rect(rr.x + 130 + i * 30, y + 2, 24, 30), i < t.zorluk ? new Color(0.4f, 0.8f, 1f, a) : new Color(1, 1, 1, 0.15f * a));
        y += 42;
        Arayuz.Yaz(new Rect(rr.x + 20, y, rr.width - 40, 34), "Hedef: suyun %" + Mathf.RoundToInt(t.hedef * 100) + "'i", Arayuz.KucukSol, new Color(1, 1, 1, 0.85f * a), 1f);
        y += 36;
        Arayuz.Yaz(new Rect(rr.x + 20, y, rr.width - 40, 34), Mathf.RoundToInt(t.uzunluk) + " m yol  ·  ekspres " + Arayuz.Sure(t.parSure), new GUIStyle(Arayuz.KucukSol) { fontSize = 23, wordWrap = false }, new Color(0.7f, 0.9f, 1f, 0.9f * a), 1f);
        y += 44;

        int yildiz = Kayit.Yildiz(no);
        for (int i = 0; i < 3; i++) Arayuz.Yildiz(new Rect(rr.center.x - 84 + i * 58, y, 52, 52), i < yildiz);
        y += 58;
        int en = Kayit.EnIyiSkor(no);
        Arayuz.Yaz(new Rect(rr.x, y, rr.width, 30), en > 0 ? "En iyi: " + en.ToString("N0") : "Henüz oynanmadı", Arayuz.Minik, new Color(1, 1, 1, 0.6f * a), 1f);

        Rect d = new Rect(rr.x + 24, rr.yMax - 84, rr.width - 48, 66);
        if (acik)
        {
            if (Arayuz.Dugme(d, yildiz > 0 ? "Tekrar Oyna" : "OYNA", yildiz > 0 ? Arayuz.Mavi : Arayuz.Yesil)) SahneGecis.Yukle(SahneGecis.Bolum(no));
        }
        else
        {
            Arayuz.Kutu(rr, new Color(0.02f, 0.05f, 0.1f, 0.6f * a), 24f);
            Kilit(new Rect(rr.center.x - 40, rr.y + 300, 80, 96), a);
            Arayuz.Yaz(new Rect(rr.x + 10, rr.y + 410, rr.width - 20, 60), "Kilitli\n<size=20>Bölüm " + (no - 1) + "'i tamamla</size>", Arayuz.Kucuk, new Color(1, 1, 1, 0.85f * a), 2f);
        }
    }

    // Arazi profilinin kucuk silueti
    static void Onizleme(Rect r, int no, BolumTanim t, float a)
    {
        var p = Bolumler.Profil(no);
        float min = float.MaxValue, max = float.MinValue;
        foreach (var v in p) { min = Mathf.Min(min, v); max = Mathf.Max(max, v); }
        if (t.KopruVar) min = Mathf.Max(min, max - 30f);
        float aralik = Mathf.Max(12f, max - min);
        const int N = 64;
        float w = (r.width - 16) / N;
        for (int i = 0; i < N; i++)
        {
            float h = p[Mathf.Clamp(Mathf.RoundToInt(i / (N - 1f) * (p.Length - 1)), 0, p.Length - 1)];
            float k = Mathf.Clamp01((h - min) / aralik);
            float hh = 14f + k * (r.height - 40f);
            Rect s = new Rect(r.x + 8 + i * w, r.yMax - 8 - hh, w + 0.5f, hh);
            Arayuz.Kutu(s, new Color(t.toprak.r, t.toprak.g, t.toprak.b, a));
            Arayuz.Kutu(new Rect(s.x, s.y, s.width, 5), new Color(t.cim.r, t.cim.g, t.cim.b, a));
        }
    }

    static void Kilit(Rect r, float a)
    {
        Color c = new Color(0.85f, 0.88f, 0.95f, a);
        Arayuz.Cerceve(new Rect(r.x + r.width * 0.2f, r.y, r.width * 0.6f, r.height * 0.6f), c, 10f, r.width * 0.3f);
        Arayuz.Kutu(new Rect(r.x, r.y + r.height * 0.4f, r.width, r.height * 0.6f), c, 10f);
        Arayuz.Kutu(new Rect(r.center.x - 6, r.y + r.height * 0.58f, 12, 22), new Color(0.04f, 0.12f, 0.25f, a), 6f);
    }
}
