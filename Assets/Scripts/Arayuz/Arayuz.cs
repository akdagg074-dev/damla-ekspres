using UnityEngine;

// Damla Su kurumsal arayuz temasi: renk paleti, yazi stilleri ve OnGUI cizim yardimcilari.
// Paneller, butonlar, kaydirici, onay kutusu ve yildizlar Kenney UI Pack (CC0) parcalaridir;
// 9 dilimli (9-slice) cizilir, boylece her boyutta koseleri bozulmaz. Font: Paytone One (OFL).
// Tum ekranlar 1080 piksel yukseklige gore tasarlanir ve ekrana olceklenir.
public static class Arayuz
{
    public const float VH = 1080f;
    public static float S { get; private set; } = 1f;
    public static float VW { get { return Screen.width / S; } }

    public static readonly Color Lacivert = new Color(0.04f, 0.13f, 0.28f);
    public static readonly Color Panel = new Color(0.16f, 0.27f, 0.47f, 0.95f);
    // Kenney gri paneli Damla Su laciverti ile boyanir
    public static readonly Color PanelTonu = new Color(0.26f, 0.4f, 0.66f, 0.97f);
    public static readonly Color KoyuYazi = new Color(0.2f, 0.22f, 0.3f);
    public static readonly Color Mavi = new Color(0.13f, 0.53f, 0.9f);
    public static readonly Color Turkuaz = new Color(0.25f, 0.8f, 0.9f);
    public static readonly Color AcikMavi = new Color(0.55f, 0.85f, 1f);
    public static readonly Color Sari = new Color(1f, 0.84f, 0.25f);
    public static readonly Color Turuncu = new Color(1f, 0.55f, 0.15f);
    public static readonly Color Yesil = new Color(0.2f, 0.68f, 0.4f);
    public static readonly Color Kirmizi = new Color(0.86f, 0.3f, 0.27f);
    public static readonly Color Gri = new Color(0.36f, 0.42f, 0.52f);

    public static GUIStyle Dev, Baslik, Buyuk, Orta, Kucuk, Minik, OrtaSol, KucukSol, MinikSol, OrtaSag;
    static Texture2D damlaTex, yildizTex;

    // Her OnGUI basinda cagrilir: stiller + olcek matrisi
    public static void Basla()
    {
        Stiller();
        S = Screen.height / VH;
        GUI.matrix = Matrix4x4.Scale(new Vector3(S, S, 1f));
    }

    public static float Olcek { get { return Screen.height / VH; } }
    // Sanal koordinatlari ekran pikseline cevirir (pedallar icin)
    public static Rect Ekran(Rect r) { float s = Olcek; return new Rect(r.x * s, r.y * s, r.width * s, r.height * s); }

    static Font font;

    static void Stiller()
    {
        if (Baslik != null) return;
        font = Resources.Load<Font>("UI/Font/PaytoneOne-Regular");
        Dev = St(136, TextAnchor.MiddleCenter);
        Baslik = St(88, TextAnchor.MiddleCenter);
        Buyuk = St(52, TextAnchor.MiddleCenter);
        Orta = St(33, TextAnchor.MiddleCenter);
        Kucuk = St(26, TextAnchor.MiddleCenter);
        Minik = St(21, TextAnchor.MiddleCenter);
        OrtaSol = St(33, TextAnchor.MiddleLeft);
        OrtaSag = St(33, TextAnchor.MiddleRight);
        KucukSol = St(26, TextAnchor.MiddleLeft);
        MinikSol = St(21, TextAnchor.UpperLeft);
    }

    static GUIStyle St(int size, TextAnchor a)
    {
        // Paytone One tek agirliktir (kalin); sahte kalinlastirma yapilmasin diye Normal
        var st = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = a, wordWrap = true, richText = true, fontStyle = FontStyle.Normal };
        if (font != null) st.font = font;
        st.normal.textColor = Color.white;
        st.padding = new RectOffset(0, 0, 0, 0);
        return st;
    }

    // ---------------------------------------------------------------- Kenney UI parcalari (9 dilim)
    public const string DugmeTur = "button_rectangle_depth_gradient";
    public const string KareTur = "button_square_depth_gradient";
    public const string PanelTur = "button_square_depth_flat";
    static readonly System.Collections.Generic.Dictionary<string, GUIStyle> dilimler = new System.Collections.Generic.Dictionary<string, GUIStyle>();
    static readonly System.Collections.Generic.Dictionary<string, Texture2D> dokular = new System.Collections.Generic.Dictionary<string, Texture2D>();

    public static Texture2D UI(string yol)
    {
        Texture2D t;
        if (!dokular.TryGetValue(yol, out t)) { t = Resources.Load<Texture2D>("UI/" + yol); dokular[yol] = t; }
        return t;
    }

    static GUIStyle DilimStil(string yol, RectOffset kenar)
    {
        GUIStyle st;
        if (dilimler.TryGetValue(yol, out st)) return st;
        st = new GUIStyle { border = kenar };
        st.normal.background = UI(yol);
        dilimler[yol] = st;
        return st;
    }

    // renk klasoru: Blue, Green, Red, Yellow, Grey
    public static void Dilim(Rect r, string renk, string tur, Color ton)
    {
        if (Event.current.type != EventType.Repaint) return;
        var st = DilimStil(renk + "/" + tur, new RectOffset(26, 26, 24, 32));
        if (st.normal.background == null) { Kutu(r, ton, 16f); return; }
        var old = GUI.color;
        GUI.color = ton;
        st.Draw(r, false, false, false, false);
        GUI.color = old;
    }

    static string RenkKlasoru(Color c)
    {
        if (c == Yesil) return "Green";
        if (c == Kirmizi) return "Red";
        if (c == Gri) return "Grey";
        if (c == Sari || c == Turuncu) return "Yellow";
        return "Blue";
    }

    // ---------------------------------------------------------------- temel cizim
    public static void Kutu(Rect r, Color c, float rad = 0f) { GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, rad); }
    public static void Cerceve(Rect r, Color c, float w, float rad = 0f) { GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, w, rad); }

    public static void Yaz(Rect r, string t, GUIStyle st, Color c, float g = 3f)
    {
        var old = st.normal.textColor;
        if (g > 0f)
        {
            st.normal.textColor = new Color(0, 0, 0, 0.55f * c.a);
            GUI.Label(new Rect(r.x + g, r.y + g, r.width, r.height), t, st);
        }
        st.normal.textColor = c;
        GUI.Label(r, t, st);
        st.normal.textColor = old;
    }

    public static void Doku(Rect r, Texture t, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
        GUI.color = old;
    }

    // Kurumsal panel: laciverte boyanmis Kenney paneli, istege bagli mavi baslik seridi
    public static void PanelCiz(Rect r, string baslik = null)
    {
        Kutu(new Rect(r.x + 5, r.y + 8, r.width, r.height), new Color(0, 0, 0, 0.22f), 22f);
        Dilim(r, "Grey", PanelTur, PanelTonu);
        if (baslik != null)
        {
            Dilim(new Rect(r.x, r.y, r.width, 74), "Blue", PanelTur, Color.white);
            Yaz(new Rect(r.x, r.y + 4, r.width, 60), baslik, Orta, Color.white, 2f);
        }
    }

    public static bool Uzerinde(Rect r) { return r.Contains(Event.current.mousePosition); }

    // Kenney renkli butonu: Yesil / Mavi / Kirmizi / Gri / Sari
    public static bool Dugme(Rect r, string yazi, Color renk, bool aktif = true, GUIStyle st = null)
    {
        Event e = Event.current;
        bool ust = aktif && r.Contains(e.mousePosition);
        string klasor = aktif ? RenkKlasoru(renk) : "Grey";
        Rect rr = new Rect(r.x, r.y + (ust ? -3f : 0f), r.width, r.height);
        Dilim(rr, klasor, DugmeTur, aktif ? (ust ? Color.white : new Color(0.93f, 0.93f, 0.93f)) : new Color(1, 1, 1, 0.6f));
        bool koyu = klasor == "Grey" || klasor == "Yellow";
        Rect yaziAlani = new Rect(rr.x, rr.y - 3f, rr.width, rr.height);   // alt derinlik payi
        Color yr = koyu ? KoyuYazi : Color.white;
        if (!aktif) yr.a = 0.5f;
        Yaz(yaziAlani, yazi, st ?? Orta, yr, koyu ? 0f : 2f);
        if (aktif && e.type == EventType.MouseDown && e.button == 0 && ust)
        {
            e.Use();
            Sfx.Cal("tik");
            return true;
        }
        return false;
    }

    // Yatay ilerleme cubugu
    public static void Bar(Rect r, float oran, Color dolgu, Color arka)
    {
        Kutu(r, arka, r.height * 0.5f);
        oran = Mathf.Clamp01(oran);
        if (oran > 0.001f) Kutu(new Rect(r.x, r.y, Mathf.Max(r.height, r.width * oran), r.height), dolgu, r.height * 0.5f);
    }

    // Surukle-birak kaydirici (0..1): Kenney gri ray + mavi dolgu + tutamac
    public static float Kaydirici(Rect r, float v)
    {
        Event e = Event.current;
        Rect alan = new Rect(r.x - 20, r.y - 24, r.width + 40, r.height + 48);
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && alan.Contains(e.mousePosition))
        {
            v = Mathf.Clamp01((e.mousePosition.x - r.x) / r.width);
            e.Use();
        }
        // ray dokusunun uclari yuvarlak: yatayda 9 dilimle cizilir, uclar bozulmaz
        Rect ray = new Rect(r.x - 12, r.center.y - 16, r.width + 24, 32);
        if (e.type == EventType.Repaint)
        {
            DilimStil("Grey/slide_horizontal_grey", new RectOffset(16, 16, 0, 0)).Draw(ray, false, false, false, false);
            if (v > 0.01f)
                DilimStil("Blue/slide_horizontal_color", new RectOffset(16, 16, 0, 0)).Draw(new Rect(ray.x, ray.y, Mathf.Max(32f, 24f + r.width * v), ray.height), false, false, false, false);
        }
        float kx = r.x + r.width * v;
        Doku(new Rect(kx - 17, r.center.y - 26, 34, 44), UI("Blue/slide_hangle"), Color.white);
        return v;
    }

    // Acik/kapali onay kutusu (Kenney)
    public static bool Anahtar(Rect r, bool acik)
    {
        Event e = Event.current;
        Rect kutu = new Rect(r.x, r.center.y - 24, 48, 48);
        bool ust = new Rect(r.x - 8, r.y - 8, Mathf.Max(r.width, 64), r.height + 16).Contains(e.mousePosition);
        Doku(kutu, UI(acik ? "Blue/check_square_color_checkmark" : "Grey/check_square_grey"), ust ? Color.white : new Color(0.94f, 0.94f, 0.94f));
        Yaz(new Rect(kutu.xMax + 12, r.y, 160, r.height), acik ? "Açık" : "Kapalı", KucukSol, new Color(1, 1, 1, 0.85f), 1f);
        if (e.type == EventType.MouseDown && e.button == 0 && ust) { e.Use(); Sfx.Cal("tik"); return !acik; }
        return acik;
    }

    // ---------------------------------------------------------------- ikonlar (kodla uretilen dokular)
    public static void Damla(Rect r, Color c) { Doku(r, DamlaDoku(), c); }

    // Soldan saga solan yumusak golge (cizgi/bant olusmadan)
    static Texture2D gradyanTex;
    public static void YatayGradyan(Rect r, Color c)
    {
        if (gradyanTex == null)
            gradyanTex = Uret(256, 1, (x, y) => 1f - Mathf.SmoothStep(0f, 1f, x / 256f));
        Doku(r, gradyanTex, c);
    }

    // Kenney yildizi (dolu: sari, bos: soluk gri)
    public static void Yildiz(Rect r, bool dolu)
    {
        var t = UI(dolu ? "Yellow/star" : "Grey/star");
        if (t == null) { Doku(r, YildizDoku(), dolu ? Sari : new Color(1, 1, 1, 0.16f)); return; }
        Doku(r, t, dolu ? Color.white : new Color(1, 1, 1, 0.45f));
    }

    // Damla seklinde doku (ucu yukarida)
    public static Texture2D DamlaDoku()
    {
        if (damlaTex != null) return damlaTex;
        const float cx = 32f, cy = 28f, r = 24f, uc = 78f;
        damlaTex = Uret(64, 80, (x, y) =>
        {
            float daire = r - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            float koni = -1f;
            if (y > cy && y < uc)
            {
                float k = (uc - y) / (uc - cy);
                float yarim = r * Mathf.Sqrt(k) * k * 1.05f;
                koni = yarim - Mathf.Abs(x - cx);
            }
            return Mathf.Max(daire, koni) + 0.5f;
        });
        return damlaTex;
    }

    // Bes koseli yildiz dokusu
    public static Texture2D YildizDoku()
    {
        if (yildizTex != null) return yildizTex;
        yildizTex = Uret(64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x - 32f, y - 31f);
            float a = Mathf.Atan2(p.x, p.y);
            float seg = Mathf.PI * 2f / 5f;
            float u = Mathf.Abs(Mathf.Repeat(a + seg * 0.5f, seg) - seg * 0.5f) / (seg * 0.5f);
            float sinir = Mathf.Lerp(30f, 13f, u);
            return sinir - p.magnitude + 0.5f;
        });
        return yildizTex;
    }

    static Texture2D Uret(int w, int h, System.Func<float, float, float> alfa)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(alfa(x + 0.5f, y + 0.5f)));
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // Genis tasarlanmis bir ekran dar pencereye sigmazsa yatay ortadan orantili kucultur.
    // Onceki matrisi dondurur (cizimden sonra GUI.matrix'e geri verilir).
    public static Matrix4x4 Sigdir(float tasarimGenisligi)
    {
        var old = GUI.matrix;
        float k = Mathf.Min(1f, VW / tasarimGenisligi);
        if (k < 1f)
            GUI.matrix = old * Matrix4x4.TRS(new Vector3(VW * 0.5f * (1f - k), VH * 0.5f * (1f - k), 0f), Quaternion.identity, new Vector3(k, k, 1f));
        return old;
    }

    // ---------------------------------------------------------------- donmus cizim
    // Cizimi sanal koordinatta bir pivot etrafinda dondurur
    public static void Dondur(float derece, Vector2 pivot, System.Action ciz)
    {
        var old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(pivot, Quaternion.Euler(0, 0, derece), Vector3.one) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
        ciz();
        GUI.matrix = old;
    }

    public static string Sure(float sn)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(sn));
        return (s / 60) + ":" + (s % 60).ToString("00");
    }
}
