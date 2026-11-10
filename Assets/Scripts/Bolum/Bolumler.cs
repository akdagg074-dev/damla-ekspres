using System.Collections.Generic;
using UnityEngine;

// Bir bolumun tasarim verisi. Arazi, sahne kurucuda ve bolum secim onizlemesinde bu veriden uretilir.
[System.Serializable]
public class BolumTanim
{
    public string ad;            // gorunen ad
    public string musteri;       // suyu bekleyen musteri
    public string aciklama;      // bolum secim ekrani metni
    public int zorluk;           // 1-5
    public float uzunluk;        // depodan teslimat bayragina (m)
    public float hedef;          // basari icin teslim edilmesi gereken asgari su orani
    public float parSure;        // ekspres teslimat suresi (sn)
    public float azamiEgim;      // arazinin en dik yeri (derece)
    public float tirmanis;       // baslangic ile bitis arasi rakim farki (m)
    public Vector3[] dalgalar;   // tepeler: (genlik, dalga boyu, faz)
    public Vector3[] tumsekler;  // tumsek / kasis / kaya: (x, genislik, yukseklik)
    public Vector2 kopru;        // asma kopru (baslangic x, bitis x); yoksa (0,0)
    public int jeton;            // damla jetonu sayisi
    public Color cim, toprak, toprakKoyu, gokUst, gokAlt, tepe;

    public bool KopruVar { get { return kopru.y > kopru.x; } }
}

// Bes bolumun tanimi ve arazi profili uretimi.
// Zorluk kademesi: her bolumde tepe genligi, azami egim, tumsek sayisi/boyu, yol uzunlugu ve su hedefi artar.
public static class Bolumler
{
    public const float Adim = 0.5f;          // profil ornekleme araligi (m)
    public const float Bas = -18f;           // arazinin sol ucu (depo bolgesi)
    public const float BasDuz = 18f;         // depodan sonraki duz yol
    public const float SonDuz = 30f;         // teslimat bayragindan sonraki duz alan (mahalle)
    public const float KopruDerinlik = 14f;  // kanyon derinligi

    static Color C(float r, float g, float b) { return new Color(r, g, b); }

    public static readonly BolumTanim[] Liste =
    {
        new BolumTanim
        {
            ad = "Mahalle Yokuşu", musteri = "Lale Apartmanı",
            aciklama = "Hafif yokuşlar ve üç kasis. Kasiste yavaşlamayı öğren.",
            zorluk = 1, uzunluk = 140f, hedef = 0.50f, parSure = 24f, azamiEgim = 14f, tirmanis = 4f,
            dalgalar = new[] { new Vector3(1.6f, 26f, 0f), new Vector3(0.6f, 11f, 1.3f) },
            tumsekler = new[] { new Vector3(52f, 2f, 0.45f), new Vector3(96f, 2f, 0.45f), new Vector3(118f, 2f, 0.45f) },
            jeton = 8,
            cim = C(0.42f, 0.74f, 0.3f), toprak = C(0.62f, 0.46f, 0.3f), toprakKoyu = C(0.36f, 0.25f, 0.16f),
            gokUst = C(0.3f, 0.6f, 0.95f), gokAlt = C(0.78f, 0.92f, 1f), tepe = C(0.55f, 0.78f, 0.5f),
        },
        new BolumTanim
        {
            ad = "Bağ Yolu", musteri = "Bağ Evi Ailesi",
            aciklama = "Dalgalı bağ yolu ve art arda kasisler. Çalkalanmaya dikkat!",
            zorluk = 2, uzunluk = 170f, hedef = 0.55f, parSure = 31f, azamiEgim = 20f, tirmanis = 2f,
            dalgalar = new[] { new Vector3(2.8f, 30f, 0.7f), new Vector3(1f, 13f, 2.1f), new Vector3(0.35f, 5.5f, 0.4f) },
            tumsekler = new[] { new Vector3(70f, 2.5f, 0.4f), new Vector3(74f, 2.5f, 0.4f), new Vector3(78f, 2.5f, 0.4f), new Vector3(122f, 6f, 1.2f) },
            jeton = 10,
            cim = C(0.62f, 0.74f, 0.26f), toprak = C(0.68f, 0.5f, 0.32f), toprakKoyu = C(0.4f, 0.27f, 0.16f),
            gokUst = C(0.42f, 0.66f, 0.95f), gokAlt = C(0.98f, 0.93f, 0.8f), tepe = C(0.62f, 0.7f, 0.4f),
        },
        new BolumTanim
        {
            ad = "Çam Ormanı", musteri = "Orman Kampı",
            aciklama = "Dik tepeler, kayalar ve sıçratan bir rampa. Havada dengeyi koru.",
            zorluk = 3, uzunluk = 200f, hedef = 0.55f, parSure = 40f, azamiEgim = 26f, tirmanis = 6f,
            dalgalar = new[] { new Vector3(4f, 34f, 1.1f), new Vector3(1.6f, 15f, 0.2f), new Vector3(0.5f, 6f, 2.5f) },
            tumsekler = new[] { new Vector3(60f, 3f, 0.6f), new Vector3(96f, 8f, 2.2f), new Vector3(140f, 3f, 0.6f), new Vector3(146f, 3f, 0.6f) },
            jeton = 12,
            cim = C(0.24f, 0.55f, 0.26f), toprak = C(0.44f, 0.33f, 0.22f), toprakKoyu = C(0.22f, 0.16f, 0.11f),
            gokUst = C(0.36f, 0.58f, 0.85f), gokAlt = C(0.82f, 0.92f, 0.95f), tepe = C(0.22f, 0.42f, 0.3f),
        },
        new BolumTanim
        {
            ad = "Kanyon Köprüsü", musteri = "Kanyon Çiftliği",
            aciklama = "Derin vadiler ve sallanan asma köprü. Köprüde yavaş ve dengeli geç.",
            zorluk = 4, uzunluk = 230f, hedef = 0.60f, parSure = 49f, azamiEgim = 32f, tirmanis = -3f,
            dalgalar = new[] { new Vector3(5.5f, 38f, 2f), new Vector3(2f, 16f, 0.9f), new Vector3(0.6f, 7f, 1.7f) },
            tumsekler = new[] { new Vector3(60f, 5f, 1.5f), new Vector3(178f, 3f, 0.8f) },
            kopru = new Vector2(110f, 124f),
            jeton = 12,
            cim = C(0.86f, 0.64f, 0.36f), toprak = C(0.74f, 0.42f, 0.24f), toprakKoyu = C(0.42f, 0.2f, 0.12f),
            gokUst = C(0.95f, 0.6f, 0.38f), gokAlt = C(1f, 0.88f, 0.68f), tepe = C(0.78f, 0.46f, 0.3f),
        },
        new BolumTanim
        {
            ad = "Zirve Köyü", musteri = "Zirve Köyü Muhtarı",
            aciklama = "Karlı dağ yolu: çok dik tırmanışlar ve sert inişler. Son sınav!",
            zorluk = 5, uzunluk = 260f, hedef = 0.60f, parSure = 58f, azamiEgim = 38f, tirmanis = 22f,
            dalgalar = new[] { new Vector3(6f, 40f, 0.4f), new Vector3(2.6f, 17f, 1.9f), new Vector3(0.8f, 7.5f, 0.3f) },
            tumsekler = new[] { new Vector3(80f, 2f, 0.8f), new Vector3(150f, 10f, 3f), new Vector3(200f, 2f, 0.9f), new Vector3(205f, 2f, 0.9f) },
            jeton = 15,
            cim = C(0.93f, 0.96f, 1f), toprak = C(0.5f, 0.5f, 0.56f), toprakKoyu = C(0.26f, 0.26f, 0.32f),
            gokUst = C(0.2f, 0.34f, 0.7f), gokAlt = C(0.72f, 0.82f, 0.96f), tepe = C(0.55f, 0.6f, 0.74f),
        },
    };

    static readonly Dictionary<int, float[]> onbellek = new Dictionary<int, float[]>();

    public static BolumTanim Tanim(int no) { return Liste[no - 1]; }
    public static float Son(int no) { return Liste[no - 1].uzunluk + SonDuz; }

    static float Yumusak(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

    // Arazi yukseklikleri: x = Bas + i * Adim
    public static float[] Profil(int no)
    {
        float[] y;
        if (onbellek.TryGetValue(no, out y)) return y;
        var t = Liste[no - 1];
        int n = Mathf.CeilToInt((Son(no) - Bas) / Adim) + 1;
        y = new float[n];
        float o0 = BasDuz, o1 = t.uzunluk - 8f;

        // tepeler + genel tirmanis
        for (int i = 0; i < n; i++)
        {
            float x = Bas + i * Adim;
            float zarf = Yumusak((x - o0) / 14f) * Yumusak((o1 - x) / 14f);
            float h = 0f;
            foreach (var d in t.dalgalar) h += d.x * Mathf.Sin(2f * Mathf.PI * x / d.y + d.z);
            y[i] = t.tirmanis * Yumusak((x - o0) / (o1 - o0)) + zarf * h;
        }

        // kopru icin iki yakayi ayni yukseklikte duzle
        if (t.KopruVar)
        {
            float a = t.kopru.x, b = t.kopru.y;
            float hk = y[Indeks(a - 10f)];
            for (int i = 0; i < n; i++)
            {
                float x = Bas + i * Adim;
                if (x >= a - 10f && x <= b + 10f) y[i] = hk;
                else if (x > a - 24f && x < a - 10f) y[i] = Mathf.Lerp(y[i], hk, Yumusak((x - (a - 24f)) / 14f));
                else if (x > b + 10f && x < b + 24f) y[i] = Mathf.Lerp(y[i], hk, Yumusak((b + 24f - x) / 14f));
            }
        }

        // azami egim siniri (zorluk kademesi); vadi diplerini ve tepe uclarini yuvarlat (gercek yol gibi)
        EgimSiniri(y, t.azamiEgim);
        for (int k = 0; k < 2; k++) Ortala(y, 4);

        // tumsek / kasis / kaya / rampa (sekli korunsun diye yumusatmadan sonra)
        for (int i = 0; i < n; i++)
        {
            float x = Bas + i * Adim;
            foreach (var b in t.tumsekler)
            {
                float u = (x - b.x) / (b.y * 0.5f);
                if (Mathf.Abs(u) < 1f) y[i] += b.z * 0.5f * (1f + Mathf.Cos(Mathf.PI * u));
            }
        }
        EgimSiniri(y, t.azamiEgim + 6f);   // yokus ustundeki tumsek tirmanilamaz duvara donmesin
        Ortala(y, 1);

        // kanyon
        if (t.KopruVar)
        {
            int ia = Indeks(t.kopru.x), ib = Indeks(t.kopru.y);
            float dip = Mathf.Min(y[ia], y[ib]) - KopruDerinlik;
            for (int i = ia + 1; i < ib; i++) y[i] = dip;
        }

        onbellek[no] = y;
        return y;
    }

    static void EgimSiniri(float[] y, float derece)
    {
        float dmax = Mathf.Tan(derece * Mathf.Deg2Rad) * Adim;
        for (int i = 1; i < y.Length; i++) y[i] = Mathf.Clamp(y[i], y[i - 1] - dmax, y[i - 1] + dmax);
    }

    // Kayan ortalama (yaricap r ornek)
    static void Ortala(float[] y, int r)
    {
        var z = (float[])y.Clone();
        for (int i = 0; i < y.Length; i++)
        {
            float top = 0f;
            int say = 0;
            for (int k = -r; k <= r; k++)
            {
                int j = i + k;
                if (j < 0 || j >= y.Length) continue;
                top += z[j];
                say++;
            }
            y[i] = top / say;
        }
    }

    public static int Indeks(float x) { return Mathf.RoundToInt((x - Bas) / Adim); }

    public static float Yukseklik(int no, float x)
    {
        var p = Profil(no);
        float f = (x - Bas) / Adim;
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, p.Length - 2);
        return Mathf.Lerp(p[i], p[i + 1], Mathf.Clamp01(f - i));
    }

    // Yol yuzeyi (koprude kopru seviyesi)
    public static float Yol(int no, float x)
    {
        var t = Liste[no - 1];
        if (t.KopruVar && x > t.kopru.x && x < t.kopru.y)
            return Mathf.Lerp(Yukseklik(no, t.kopru.x), Yukseklik(no, t.kopru.y), (x - t.kopru.x) / (t.kopru.y - t.kopru.x));
        return Yukseklik(no, x);
    }

    // Arac bu yuksekligin altina duserse bolum biter
    public static float DusmeSiniri(int no)
    {
        var t = Liste[no - 1];
        var p = Profil(no);
        float min = float.MaxValue;
        foreach (var v in p) min = Mathf.Min(min, v);
        return t.KopruVar ? min + KopruDerinlik * 0.5f : min - 10f;
    }
}
