using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Damla Ekspres > Tum Sahneleri Kur
// 1) Kenney 3D modellerinden (pikap, dukkan, apartman, ev) dis hatli 2D sprite'lar render eder.
// 2) Fizik malzemelerini, su tankeri prefab'ini (WheelJoint2D) ve 7 sahneyi
//    (AnaMenu, BolumSecim, Bolum1-5) Kenney gorselleriyle kurar; Build Settings'i ayarlar.
public static class SahneKurucu
{
    const string Uret = "Assets/Sprites/Uretilen/";
    const string KS = "Assets/Sprites/Kenney/";
    const string PrefabYolu = "Assets/Prefabs/SuTankeri.prefab";
    const string FizikKlasor = "Assets/Materyaller/Fizik/";
    const string AraziKlasor = "Assets/Modeller/Arazi/";
    const float TankerUzunluk = 4f;   // oyun dunyasinda pikabin boyu (m)
    // Binalar yukari bakis acisi olmadan (yalnizca 25 derece yandan) render edilir: taban duz bir cizgidir.
    // Dis hat payi kadar zemine gomulur ki taban cimle birlessin (zemin onde cizilir).
    const float Gomme = 0.025f;

    static Sprite kare, daire, yumusak, gradyan, kasis, tankArka, tankOn, damla, tankerGovde, tankerTeker;
    static Material spriteMat;
    static PhysicsMaterial2D lastik, zemin, govdeMat;
    static Font font;

    // tanker render bilgisi (model birimi, sprite merkezine gore)
    static float olcek, tekerR;
    static Vector2 onTekerP, arkaTekerP;
    static float TekerAlt { get { return -(arkaTekerP.y - tekerR) * olcek; } }

    // ---------------------------------------------------------------- bolum temalari
    class Tema
    {
        public string zemin;
        public Color ton = Color.white;
        public string[] agac, cali, kaya, musteri;
        public Vector2 agacBoy = new Vector2(3.2f, 5f);
        public float musteriBoy = 5.5f;
        public bool daglar;
        public Color uzak, yakin;
    }

    static Tema TemaAl(int no)
    {
        switch (no)
        {
            case 1: return new Tema
            {
                zemin = "grass", agac = new[] { "tree", "treeSmall_green1", "treeSmall_green2", "treeSmall_green3", "treeLong" },
                cali = new[] { "bush1", "bush2", "bush3", "bush4" }, kaya = new string[0], musteri = new[] { "bina_i", "bina_l" }, musteriBoy = 8.5f,
                uzak = new Color(0.62f, 0.84f, 0.72f), yakin = new Color(0.45f, 0.78f, 0.5f),
            };
            case 2: return new Tema
            {
                zemin = "grass", ton = new Color(1f, 0.93f, 0.72f),
                agac = new[] { "treeOrange", "treeLongOrange", "treeSmall_orange1", "treeSmall_orange2", "treeSmall_orange3", "tree" },
                cali = new[] { "bushOrange1", "bushOrange2", "bushOrange3", "bushOrange4" }, kaya = new string[0], musteri = new[] { "ev_c", "ev_h" },
                uzak = new Color(0.86f, 0.82f, 0.64f), yakin = new Color(0.76f, 0.72f, 0.42f),
            };
            case 3: return new Tema
            {
                zemin = "grass", ton = new Color(0.8f, 0.93f, 0.82f), agac = new[] { "treePine", "treeLong", "treePine", "treePineOrange" },
                agacBoy = new Vector2(4f, 6.5f), cali = new[] { "bushAlt1", "bushAlt2", "bushAlt3", "bushAlt4" }, kaya = new[] { "rock", "mushroom_red" },
                musteri = new[] { "ev_k", "ev_f" }, daglar = true,
                uzak = new Color(0.56f, 0.72f, 0.76f), yakin = new Color(0.32f, 0.56f, 0.42f),
            };
            case 4: return new Tema
            {
                zemin = "sand", agac = new[] { "cactus1", "cactus2", "cactus3", "treeDead", "treePalm" }, agacBoy = new Vector2(2.2f, 4.2f),
                cali = new string[0], kaya = new[] { "rock" }, musteri = new[] { "ev_r", "ev_n" }, daglar = true,
                uzak = new Color(0.95f, 0.72f, 0.52f), yakin = new Color(0.86f, 0.56f, 0.38f),
            };
            default: return new Tema
            {
                zemin = "snow", agac = new[] { "treePineSnow", "treeSnow", "treePineFrozen", "treeLongSnow", "treeFrozen" }, agacBoy = new Vector2(3.5f, 6f),
                cali = new string[0], kaya = new string[0], musteri = new[] { "ev_f", "ev_c" }, daglar = true,
                uzak = new Color(0.86f, 0.9f, 0.98f), yakin = new Color(0.76f, 0.83f, 0.93f),
            };
        }
    }

    [MenuItem("Damla Ekspres/Tum Sahneleri Kur")]
    static void Menu()
    {
        if (!EditorUtility.DisplayDialog("Damla Ekspres", "Tum sahneler ve tanker prefab'i bastan kurulsun mu? (Sahnelerde elle yaptigin degisiklikler kaybolur)", "Kur", "Vazgec"))
            return;
        Kur();
    }

    public static void Kur()
    {
        foreach (var k in new[] { "Assets/Prefabs", FizikKlasor.TrimEnd('/'), "Assets/Modeller", AraziKlasor.TrimEnd('/') })
            Directory.CreateDirectory(k);
        Uretilenler();
        Renderler();
        AssetDatabase.Refresh();
        foreach (var klasor in new[] { "Assets/Sprites" })
            foreach (var f in Directory.GetFiles(klasor, "*.png", SearchOption.AllDirectories))
                AssetDatabase.ImportAsset(f.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
        Yukle();
        Malzemeler();
        var prefab = TankerPrefab();

        Physics2D.velocityIterations = 10;
        Physics2D.positionIterations = 6;
        PlayerSettings.productName = "Damla Ekspres";

        AnaMenuSahnesi(prefab);
        BolumSecimSahnesi();
        for (int i = 1; i <= Kayit.BolumSayisi; i++) BolumSahnesi(i, prefab);

        var liste = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/AnaMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/BolumSecim.unity", true),
        };
        for (int i = 1; i <= Kayit.BolumSayisi; i++) liste.Add(new EditorBuildSettingsScene("Assets/Scenes/Bolum" + i + ".unity", true));
        EditorBuildSettings.scenes = liste.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/AnaMenu.unity");
        Debug.Log("Damla Ekspres: 7 sahne ve SuTankeri prefab'i kuruldu.");
    }

    // ================================================================ 3D -> 2D render
    static GameObject Model(string yol) { return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modeller/Kenney/" + yol + ".fbx"); }

    static void Renderler()
    {
        var truck = Model("CarKit/truck");
        var yan = Quaternion.LookRotation(Vector3.left, Vector3.up);   // pikabin burnu ekranin sagina bakar
        var govde = ModelRender.Render(truck, "tanker_govde", yan, t => t.name.StartsWith("wheel"));
        var teker = ModelRender.Render(truck, "tanker_teker", yan, t => t.name != "wheel-front-right");
        BijonEkle(teker.yol);

        var on = truck.transform.Find("wheel-front-right");
        var ark = truck.transform.Find("wheel-back-right");
        onTekerP = govde.Nokta(govde.kok + on.localPosition);
        arkaTekerP = govde.Nokta(govde.kok + ark.localPosition);
        tekerR = on.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;
        var body = truck.transform.Find("body").GetComponent<MeshFilter>().sharedMesh.bounds;
        olcek = TankerUzunluk / (body.size.z);

        // binalar (yandan 25 derece acili: 2.5B gorunum, taban duz). Var olanlar yeniden render edilmez.
        var onCephe = Quaternion.Euler(0f, 25f, 0f);
        var evCephe = Quaternion.Euler(0f, 205f, 0f);
        foreach (var b in new[] { "k", "i", "l" })
            if (!File.Exists(ModelRender.Klasor + "bina_" + b + ".png")) ModelRender.Render(Model("CityCommercial/building-" + b), "bina_" + b, onCephe);
        foreach (var e in new[] { "c", "f", "h", "k", "n", "r" })
            if (!File.Exists(ModelRender.Klasor + "ev_" + e + ".png")) ModelRender.Render(Model("CitySuburban/building-type-" + e), "ev_" + e, evCephe);
    }

    // Teker donerken gorunsun diye jantin uzerine bijon noktalari
    static void BijonEkle(string yol)
    {
        var t = new Texture2D(2, 2);
        t.LoadImage(File.ReadAllBytes(yol));
        int w = t.width, h = t.height;
        Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
        float halka = w * 0.17f, r = w * 0.045f;
        var px = t.GetPixels();
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f;
            Vector2 m = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * halka;
            for (int y = (int)(m.y - r - 2); y <= m.y + r + 2; y++)
                for (int x = (int)(m.x - r - 2); x <= m.x + r + 2; x++)
                {
                    float k = Mathf.Clamp01(r + 0.5f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), m));
                    int idx = y * w + x;
                    if (k <= 0f || idx < 0 || idx >= px.Length) continue;
                    var o = px[idx];
                    px[idx] = new Color(Mathf.Lerp(o.r, 0.2f, k), Mathf.Lerp(o.g, 0.21f, k), Mathf.Lerp(o.b, 0.27f, k), o.a);
                }
        }
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(yol, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static void Yukle()
    {
        kare = S(Uret + "kare.png"); daire = S(Uret + "daire.png"); yumusak = S(Uret + "yumusak.png");
        gradyan = S(Uret + "gradyan.png"); kasis = S(Uret + "kasis.png");
        tankArka = S(Uret + "tank_arka.png"); tankOn = S(Uret + "tank_on.png"); damla = S(Uret + "damla.png");
        tankerGovde = S(ModelRender.Klasor + "tanker_govde.png"); tankerTeker = S(ModelRender.Klasor + "tanker_teker.png");
        spriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fontlar/Resources/UI/Font/PaytoneOne-Regular.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Sprite K(string yol) { return S(KS + yol + ".png"); }
    static Sprite R(string ad) { return S(ModelRender.Klasor + ad + ".png"); }

    static void Malzemeler()
    {
        lastik = FizikMalzeme("Lastik", 1.4f, 0.05f);
        zemin = FizikMalzeme("Zemin", 0.9f, 0f);
        govdeMat = FizikMalzeme("Govde", 0.3f, 0.05f);
    }

    static PhysicsMaterial2D FizikMalzeme(string ad, float surtunme, float sekme)
    {
        string yol = FizikKlasor + ad + ".physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(yol);
        if (m == null) { m = new PhysicsMaterial2D(ad); AssetDatabase.CreateAsset(m, yol); }
        m.friction = surtunme;
        m.bounciness = sekme;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ================================================================ tanker prefab'i
    // Kok (bos) altinda: Govde (Rigidbody2D + PolygonCollider2D + 2x WheelJoint2D + AracKontrol),
    // OnTeker / ArkaTeker (Rigidbody2D + CircleCollider2D). Su tanki govdenin cocugudur.
    // Gorseller Kenney Car Kit pikabindan render edilir; olculer modelden hesaplanir.
    static GameObject TankerPrefab()
    {
        float s = olcek;
        var kok = new GameObject("SuTankeri");
        var govde = new GameObject("Govde");
        govde.transform.SetParent(kok.transform, false);

        var rb = govde.AddComponent<Rigidbody2D>();
        rb.useAutoMass = false;
        rb.mass = 2.2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.angularDamping = 0.1f;

        Spr("Kasa", tankerGovde, govde.transform, Vector3.zero, Vector3.one * s, 10, Color.white);
        // carpisma: kasa sekli (sprite merkezine gore model birimi); alt kenar tekerlerin ustunde
        var pc = govde.AddComponent<PolygonCollider2D>();
        var noktalar = new[]
        {
            new Vector2(-1.43f, -0.2f), new Vector2(-1.43f, 0.09f), new Vector2(-0.23f, 0.09f), new Vector2(-0.23f, 0.58f),
            new Vector2(0.39f, 0.58f), new Vector2(0.78f, 0.09f), new Vector2(1.39f, -0.02f), new Vector2(1.45f, -0.2f),
        };
        for (int i = 0; i < noktalar.Length; i++) noktalar[i] *= s;
        pc.points = noktalar;
        pc.sharedMaterial = govdeMat;

        // kapidaki firma logosu
        Spr("Logo Damla", damla, govde.transform, new Vector3(0.02f * s, -0.04f * s, 0), Vector3.one * 0.34f, 11, new Color(0.85f, 0.95f, 1f));
        Yazi3D(govde.transform, "Damla Su", new Vector3(0.3f * s, -0.03f * s, 0), 0.022f, Color.white, 11);

        // su tanki (kasanin ustunde)
        var tankGo = new GameObject("SuTanki");
        tankGo.transform.SetParent(govde.transform, false);
        tankGo.transform.localPosition = new Vector3(-0.78f * s, 0.05f * s, 0);
        var tank = tankGo.AddComponent<SuTanki>();
        float W = tank.genislik, H = tank.yukseklik;
        Spr("Tank Arka", tankArka, tankGo.transform, new Vector3(0, H * 0.5f, 0), Vector3.one, 11, Color.white);
        var suGo = new GameObject("Su");
        suGo.transform.SetParent(tankGo.transform, false);
        tank.suMesh = suGo.AddComponent<MeshFilter>();
        var mr = suGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = spriteMat;
        mr.sortingOrder = 12;
        Spr("Tank On", tankOn, tankGo.transform, new Vector3(0, H * 0.5f, 0), Vector3.one, 13, Color.white);
        Spr("Taban", kare, tankGo.transform, new Vector3(0, -0.03f, 0), new Vector3(W + 0.1f, 0.1f, 1), 13, new Color(0.13f, 0.1f, 0.17f));
        Spr("Etiket", kare, tankGo.transform, new Vector3(0, 0.16f, 0), new Vector3(0.95f, 0.17f, 1), 14, new Color(0.05f, 0.2f, 0.45f, 0.95f));
        Yazi3D(tankGo.transform, "DAMLA SU", new Vector3(0, 0.16f, 0), 0.018f, Color.white, 15);

        // tekerlekler: WheelJoint2D (suspansiyon + motor)
        var ak = govde.AddComponent<AracKontrol>();
        ak.onTeker = Teker(kok.transform, govde, "OnTeker", onTekerP * s);
        ak.arkaTeker = Teker(kok.transform, govde, "ArkaTeker", arkaTekerP * s);

        var p = PrefabUtility.SaveAsPrefabAsset(kok, PrefabYolu);
        Object.DestroyImmediate(kok);
        return p;
    }

    static WheelJoint2D Teker(Transform kok, GameObject govde, string ad, Vector2 yerel)
    {
        var t = new GameObject(ad);
        t.transform.SetParent(kok, false);
        t.transform.localPosition = yerel;
        t.transform.localScale = Vector3.one * olcek;
        var sr = t.AddComponent<SpriteRenderer>();
        sr.sprite = tankerTeker;
        sr.sortingOrder = 16;
        var trb = t.AddComponent<Rigidbody2D>();
        trb.mass = 0.15f;
        trb.interpolation = RigidbodyInterpolation2D.Interpolate;
        trb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cc = t.AddComponent<CircleCollider2D>();
        cc.radius = tekerR;   // yerel; olcekle birlikte tekerR * olcek olur
        cc.sharedMaterial = lastik;

        var j = govde.AddComponent<WheelJoint2D>();
        j.connectedBody = trb;
        j.autoConfigureConnectedAnchor = false;
        j.anchor = yerel;
        j.connectedAnchor = Vector2.zero;
        j.enableCollision = false;
        j.suspension = new JointSuspension2D { dampingRatio = 0.7f, frequency = 4.5f, angle = 90f };
        j.useMotor = false;
        return j;
    }

    // ================================================================ sahneler
    static void AnaMenuSahnesi(GameObject prefab)
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var t = Bolumler.Tanim(1);
        var cam = KameraKur(t, new Vector3(0, 2.5f, -10));
        Manzara(t, 1, 0f, 80f, 1.5f);
        var p = Bolumler.Profil(1);
        AraziKur(1, "AnaMenu", p, Bolumler.Indeks(60f));
        // menude solda butonlar, sagda dukkanin onunde bekleyen tanker gorunur
        Depo(15f, 0f);
        var dekor = new GameObject("Dekor").transform;
        Boy("Agac", K("Cevre/tree"), dekor, new Vector3(1f, Bolumler.Yukseklik(1, 1f) - 0.35f, 0), 4.5f, -30);
        Boy("Agac", K("Cevre/treeSmall_green2"), dekor, new Vector3(-2.5f, Bolumler.Yukseklik(1, -2.5f) - 0.35f, 0), 2.6f, -30);
        Bitki(dekor, TemaAl(1), 1, 3.5f, 0f, 0);
        Bitki(dekor, TemaAl(1), 1, 10.5f, 0f, 2);

        var arac = Tanker(prefab, new Vector3(7f, Bolumler.Yukseklik(1, 7f) + TekerAlt + 0.05f, 0));
        var ak = arac.GetComponentInChildren<AracKontrol>();
        ak.kontrolAktif = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ak);
        var kam = cam.gameObject.AddComponent<Kamera>();
        kam.hedef = ak.transform;
        kam.xOfset = -1.5f; kam.yOfset = 1.6f; kam.onGorus = 0f;
        kam.boy = kam.hizliBoy = 6f;
        cam.transform.position = new Vector3(5.5f, 2.2f, -10);
        new GameObject("AnaMenu").AddComponent<AnaMenu>();
        Kaydet(sc, "AnaMenu");
    }

    static void BolumSecimSahnesi()
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var t = Bolumler.Tanim(2);
        var cam = KameraKur(t, new Vector3(42f, Bolumler.Yukseklik(2, 42f) + 3.5f, -10));
        cam.orthographicSize = 7f;
        Manzara(t, 2, 0f, 100f, 1.5f);
        AraziKur(2, "BolumSecim", Bolumler.Profil(2), Bolumler.Indeks(90f));
        Agaclar(2, t, new GameObject("Dekor").transform, 20f, 80f);
        new GameObject("BolumSecim").AddComponent<BolumSecim>();
        Kaydet(sc, "BolumSecim");
    }

    static void BolumSahnesi(int no, GameObject prefab)
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var t = Bolumler.Tanim(no);
        var p = Bolumler.Profil(no);
        float son = Bolumler.Son(no);
        float y0 = Bolumler.Yukseklik(no, 1.5f);

        var cam = KameraKur(t, new Vector3(3f, y0 + 2.5f, -10));
        Manzara(t, no, 0f, son, 1f);
        var araziCol = AraziKur(no, "Bolum" + no, p, p.Length);
        var dekor = new GameObject("Dekor").transform;

        Depo(-10f, 0f);
        Agaclar(no, t, dekor, Bolumler.Bas + 3f, Bolumler.Son(no) - 2f);
        KmTabelalari(no, dekor);
        Kasisler(no, t, dekor);
        Bitis(no, t, dekor);
        if (t.KopruVar) KopruKur(no, t, araziCol);
        Jetonlar(no, t);

        // gorunmez duvarlar
        Duvar("Sol Duvar", new Vector2(Bolumler.Bas + 0.5f, 0f));
        Duvar("Sag Duvar", new Vector2(son - 0.5f, Bolumler.Yukseklik(no, son)));

        var arac = Tanker(prefab, new Vector3(1.5f, y0 + TekerAlt + 0.05f, 0));
        var kam = cam.gameObject.AddComponent<Kamera>();
        kam.hedef = arac.GetComponentInChildren<AracKontrol>().transform;
        kam.minX = Bolumler.Bas + 0.5f;
        kam.maxX = son - 0.5f;

        var y = new GameObject("BolumYonetici").AddComponent<BolumYonetici>();
        y.bolumNo = no;
        Kaydet(sc, "Bolum" + no);
    }

    static void Kaydet(UnityEngine.SceneManagement.Scene sc, string ad)
    {
        EditorSceneManager.SaveScene(sc, "Assets/Scenes/" + ad + ".unity");
    }

    static GameObject Tanker(GameObject prefab, Vector3 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        return go;
    }

    static Camera KameraKur(BolumTanim t, Vector3 pos)
    {
        var cg = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = cg.AddComponent<Camera>();
        cg.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = t.gokAlt;
        cg.transform.position = pos;
        // gokyuzu gecisi (kameraya bagli)
        Spr("Gokyuzu", gradyan, cg.transform, new Vector3(0, 3f, 20f), new Vector3(80f / gradyan.bounds.size.x, 22f / gradyan.bounds.size.y, 1f), -100, t.gokUst);
        return cam;
    }

    // ---------------------------------------------------------------- arka plan (paralaks katmanlari)
    // Kenney siluet katmanlari acik mavi cizilmis; seviyenin rengine boyamak icin carpan rengi hesaplanir.
    static Color Boya(Color hedef)
    {
        Color taban = new Color(0.73f, 0.9f, 0.98f);
        return new Color(Mathf.Min(1f, hedef.r / taban.r), Mathf.Min(1f, hedef.g / taban.g), Mathf.Min(1f, hedef.b / taban.b), 1f);
    }

    static void Manzara(BolumTanim t, int no, float x0, float x1, float carpanY)
    {
        var tema = TemaAl(no);
        var arka = new GameObject("Arka Plan").transform;
        float y0 = Bolumler.Yukseklik(no, 0f);
        float py = Mathf.Min(carpanY, 0.97f);

        var gunes = Spr("Gunes", K("Cevre/sun"), arka, new Vector3(x0 + 9f, y0 + 9.5f, 0), Vector3.one * 0.9f, -92, no == 4 ? new Color(1f, 0.85f, 0.7f) : Color.white);
        Px(gunes, 0.97f, 0.97f);

        Katman(arka, "Uzak Bulutlar", K("ArkaPlan/cloudLayerB1"), x0, x1, y0 + 3f, 2.2f, -95, new Color(1, 1, 1, 0.85f), 0.93f, py);
        Katman(arka, tema.daglar ? "Daglar" : "Uzak Tepeler", K(tema.daglar ? "ArkaPlan/mountains" : "ArkaPlan/hillsLarge"), x0, x1, y0 - 2.5f, 2.6f, -90, Boya(tema.uzak), 0.82f, py * 0.97f);
        Katman(arka, "Bulutlar", K("ArkaPlan/cloudLayer1"), x0, x1, y0 + 1.5f, 2f, -88, new Color(1, 1, 1, 0.9f), 0.75f, py * 0.95f);
        Katman(arka, "Tepeler", K("ArkaPlan/hills"), x0, x1, y0 - 4f, 2.2f, -85, Boya(tema.yakin), 0.62f, py * 0.92f);
        // tepelerin alti: kamera vadilere inince gokyuzu gorunmesin
        float L = x1 - x0;
        var dolgu = Spr("Tepeler Alti", kare, arka, new Vector3(x0 + 0.38f * L * 0.5f, y0 - 4f - 14.9f, 0), new Vector3(0.38f * L + 140f, 30f, 1), -85, tema.yakin * 0.97f);
        Px(dolgu, 0.62f, py * 0.92f);

        // suzulen bulutlar
        for (int i = 0; i < 8; i++)
        {
            var b = Spr("Bulut", K("Cevre/cloud" + (i + 1)), arka, new Vector3(x0 - 20f + i * 7f, y0 + 6.5f + (i % 3) * 1.1f, 0), Vector3.one * (0.55f + (i % 3) * 0.15f), -84, no == 4 ? new Color(1f, 0.95f, 0.9f) : Color.white);
            var px = Px(b, 0.85f, py * 0.96f);
            px.suzulme = 0.25f + (i % 3) * 0.1f;
        }
    }

    // Yatayda tekrar eden (Tiled) paralaks katmani; kamera tum bolumu gezse de bosluk kalmaz
    static void Katman(Transform p, string ad, Sprite sp, float x0, float x1, float y, float sc, int order, Color c, float carpan, float carpanY)
    {
        float L = x1 - x0;
        float genislik = (1f - carpan) * L + 140f;
        var go = Spr(ad, sp, p, new Vector3(x0 + (1f - carpan) * L * 0.5f, y, 0), Vector3.one * sc, order, c);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(genislik / sc, sp.bounds.size.y);
        Px(go, carpan, carpanY);
    }

    static Parallax Px(GameObject go, float carpan, float carpanY)
    {
        var p = go.AddComponent<Parallax>();
        p.carpan = carpan;
        p.carpanY = carpanY;
        return p;
    }

    // ---------------------------------------------------------------- arazi
    // Kenney zemin karolari arazi yuzeyi boyunca kivrilir: ust serit (cim/kum/kar) + altta tekrar eden toprak.
    // Mesh'ler asset olarak kaydedilir; EdgeCollider2D yuzeyi takip eder.
    const float Karo = 1.6f;

    static EdgeCollider2D AraziKur(int no, string ad, float[] h, int n)
    {
        var tema = TemaAl(no);
        n = Mathf.Min(n, h.Length);
        float min = float.MaxValue;
        for (int i = 0; i < n; i++) min = Mathf.Min(min, h[i]);
        float alt = min - 30f;

        var u = new float[n];
        var noktalar = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            noktalar[i] = new Vector2(Bolumler.Bas + i * Bolumler.Adim, h[i]);
            u[i] = i == 0 ? 0f : u[i - 1] + Vector2.Distance(noktalar[i], noktalar[i - 1]) / Karo;
        }

        var ust = new Mesh { name = ad + "_Yuzey" };
        var dolgu = new Mesh { name = ad + "_Toprak" };
        var uv = new Vector3[n * 2]; var uuv = new Vector2[n * 2];
        var dv = new Vector3[n * 2]; var duv = new Vector2[n * 2];
        var renk = new Color[n * 2];
        var tr = new List<int>();
        for (int i = 0; i < n; i++)
        {
            float x = noktalar[i].x, y = noktalar[i].y;
            uv[i * 2] = new Vector3(x, y); uuv[i * 2] = new Vector2(u[i], 1f);
            uv[i * 2 + 1] = new Vector3(x, y - Karo); uuv[i * 2 + 1] = new Vector2(u[i], 0f);
            dv[i * 2] = new Vector3(x, y - Karo); duv[i * 2] = new Vector2(u[i], 0f);
            dv[i * 2 + 1] = new Vector3(x, alt); duv[i * 2 + 1] = new Vector2(u[i], -(y - Karo - alt) / Karo);
            renk[i * 2] = renk[i * 2 + 1] = tema.ton;
            if (i == n - 1) continue;
            int a = i * 2, b = (i + 1) * 2;
            tr.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        ust.vertices = uv; ust.uv = uuv; ust.colors = renk; ust.triangles = tr.ToArray(); ust.RecalculateBounds();
        dolgu.vertices = dv; dolgu.uv = duv; dolgu.colors = renk; dolgu.triangles = tr.ToArray(); dolgu.RecalculateBounds();
        ust = MeshKaydet(ust);
        dolgu = MeshKaydet(dolgu);

        var go = new GameObject("Arazi");
        go.AddComponent<MeshFilter>().sharedMesh = dolgu;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = ZeminMalzeme(tema.zemin, "center");
        mr.sortingOrder = -10;
        var ec = go.AddComponent<EdgeCollider2D>();
        ec.points = noktalar;
        ec.sharedMaterial = zemin;

        var cg = new GameObject("Yuzey");
        cg.transform.SetParent(go.transform, false);
        cg.AddComponent<MeshFilter>().sharedMesh = ust;
        var cmr = cg.AddComponent<MeshRenderer>();
        cmr.sharedMaterial = ZeminMalzeme(tema.zemin, "top");
        cmr.sortingOrder = -9;
        return ec;
    }

    static Material ZeminMalzeme(string tur, string parca)
    {
        string yol = AraziKlasor + "Zemin_" + tur + "_" + parca + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
        if (m == null) { m = new Material(spriteMat); AssetDatabase.CreateAsset(m, yol); }
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(KS + "Zemin/terrain_" + tur + "_block_" + parca + ".png");
        EditorUtility.SetDirty(m);
        return m;
    }

    static Mesh MeshKaydet(Mesh m)
    {
        string yol = AraziKlasor + m.name + ".asset";
        AssetDatabase.DeleteAsset(yol);
        AssetDatabase.CreateAsset(m, yol);
        return m;
    }

    static void Duvar(string ad, Vector2 p)
    {
        var d = new GameObject(ad);
        d.transform.position = new Vector3(p.x, p.y + 10f, 0);
        d.AddComponent<BoxCollider2D>().size = new Vector2(1f, 40f);
    }

    // ---------------------------------------------------------------- dekor
    // Sprite'i istenen yukseklige olcekleyerek yerlestirir (Kenney sprite'larinin pivotu altta)
    static GameObject Boy(string ad, Sprite sp, Transform parent, Vector3 pos, float yukseklik, int order, Color? c = null, bool aynala = false)
    {
        float k = yukseklik / sp.bounds.size.y;
        var go = Spr(ad, sp, parent, pos, new Vector3(aynala ? -k : k, k, 1f), order, c ?? Color.white);
        return go;
    }

    // Damla Su dolum deposu: Kenney dukkanli bina + cati tabelasi
    static void Depo(float x, float y)
    {
        var depo = new GameObject("Damla Su Deposu").transform;
        var bina = Boy("Dolum Dukkani", R("bina_k"), depo, new Vector3(x, y - Gomme * 7f, 0), 7f, -20);
        float ust = y - Gomme * 7f + 7f - 0.75f;  // cati kenari (sprite ustundeki baca/dis hat payi kadar asagida)
        float gen = bina.GetComponent<SpriteRenderer>().bounds.size.x;
        Spr("Tabela Direk", kare, depo, new Vector3(x - 1.2f, ust + 0.2f, 0), new Vector3(0.15f, 0.9f, 1), -21, new Color(0.25f, 0.27f, 0.33f));
        Spr("Tabela Direk", kare, depo, new Vector3(x + 1.2f, ust + 0.2f, 0), new Vector3(0.15f, 0.9f, 1), -21, new Color(0.25f, 0.27f, 0.33f));
        Spr("Tabela Hat", kare, depo, new Vector3(x, ust + 1.05f, 0), new Vector3(Mathf.Min(gen, 5.4f) + 0.16f, 1.21f, 1), -20, new Color(0.13f, 0.1f, 0.17f));
        Spr("Tabela", kare, depo, new Vector3(x, ust + 1.05f, 0), new Vector3(Mathf.Min(gen, 5.4f), 1.05f, 1), -19, new Color(0.08f, 0.32f, 0.68f));
        Spr("Tabela Logo", damla, depo, new Vector3(x - 1.9f, ust + 1.05f, 0), Vector3.one * 1.15f, -18, new Color(0.55f, 0.88f, 1f));
        Yazi3D(depo, "DAMLA SU", new Vector3(x + 0.3f, ust + 1.2f, 0), 0.075f, Color.white, -18);
        Yazi3D(depo, "dolum tesisi", new Vector3(x + 0.3f, ust + 0.78f, 0), 0.038f, new Color(0.75f, 0.9f, 1f), -18);
        Boy("Cali", K("Cevre/bush1"), depo, new Vector3(x + gen * 0.5f + 0.6f, y - 0.15f, 0), 0.9f, -19);
        Boy("Cali", K("Cevre/bush3"), depo, new Vector3(x - gen * 0.5f - 0.4f, y - 0.15f, 0), 1f, -19);
    }

    static void Agaclar(int no, BolumTanim t, Transform p, float bas, float son)
    {
        var tema = TemaAl(no);
        var rnd = new System.Random(no * 97);
        float x = bas;
        int i = 0;
        while (x < son)
        {
            x += 4f + (float)rnd.NextDouble() * 6f;
            bool depo = x < -3f;
            bool bitis = x > t.uzunluk - 3f;
            bool kopru = t.KopruVar && x > t.kopru.x - 1f && x < t.kopru.y + 1f;
            if (depo || bitis || kopru || x >= son) continue;
            float y = Bolumler.Yukseklik(no, x);
            string ad = tema.agac[rnd.Next(tema.agac.Length)];
            float boy = Mathf.Lerp(tema.agacBoy.x, tema.agacBoy.y, (float)rnd.NextDouble());
            if (ad.StartsWith("treeSmall") || ad.StartsWith("cactus")) boy *= 0.6f;
            Boy("Agac " + ad, K("Cevre/" + ad), p, new Vector3(x, y - 0.35f, 0), boy, -30 - (i % 3), null, rnd.NextDouble() < 0.5);
            Bitki(p, tema, no, x + 1.5f + (float)rnd.NextDouble() * 1.5f, son, i);
            i++;
        }
    }

    // Agac aralarina cali / kaya / mantar (aracin arkasinda, zeminin onunde)
    static void Bitki(Transform p, Tema tema, int no, float x, float son, int i)
    {
        if (son > 0f && x >= son) return;
        var t = Bolumler.Tanim(no);
        if (t.KopruVar && x > t.kopru.x - 1f && x < t.kopru.y + 1f) return;
        if (x > t.uzunluk - 3f && no > 0 && son > 0f) return;
        float y = Bolumler.Yukseklik(no, x);
        if (tema.cali.Length > 0 && i % 2 == 0)
            Boy("Cali", K("Cevre/" + tema.cali[i % tema.cali.Length]), p, new Vector3(x, y - 0.2f, 0), 0.7f + (i % 3) * 0.15f, -8);
        else if (tema.kaya.Length > 0 && i % 3 == 1)
            Boy("Kaya", K("Dekor/" + tema.kaya[i % tema.kaya.Length]), p, new Vector3(x, y - 0.25f, 0), 0.75f, -8);
    }

    static void KmTabelalari(int no, Transform p)
    {
        var t = Bolumler.Tanim(no);
        for (float x = 50f; x < t.uzunluk - 10f; x += 50f)
        {
            if (t.KopruVar && x > t.kopru.x - 2f && x < t.kopru.y + 2f) continue;
            float y = Bolumler.Yukseklik(no, x);
            Boy("Km Tabelasi", K("Dekor/sign"), p, new Vector3(x, y - 0.15f, 0), 1.8f, -25);
            Yazi3D(p, Mathf.RoundToInt(x) + " m", new Vector3(x, y - 0.15f + 1.8f * 0.6f, 0), 0.055f, new Color(0.3f, 0.14f, 0.05f), -24);
        }
    }

    static void Kasisler(int no, BolumTanim t, Transform p)
    {
        foreach (var b in t.tumsekler)
        {
            if (b.z > 0.45f || b.y > 3.5f) continue; // sadece kucuk tumsekler kasis gorunur
            // kasis gercek zemin yuzeyine oturur ve yokusun egimine gore doner
            float yuk = Bolumler.Yukseklik(no, b.x);
            float egim = Mathf.Atan2(Bolumler.Yukseklik(no, b.x + 1f) - Bolumler.Yukseklik(no, b.x - 1f), 2f) * Mathf.Rad2Deg;
            float boy = 0.42f;
            var k = Spr("Kasis", kasis, p, new Vector3(b.x, yuk + boy * 0.3f, 0), new Vector3(b.y / 1.5f, boy / 0.44f, 1), -7, Color.white);
            k.transform.rotation = Quaternion.Euler(0, 0, egim);
            k.AddComponent<Kasis>();
            Spr("Kasis Hat", kasis, k.transform, new Vector3(0, -0.01f, 0), new Vector3(1.08f, 1.15f, 1), -8, new Color(0.13f, 0.1f, 0.17f));
        }
    }

    static void Bitis(int no, BolumTanim t, Transform p)
    {
        var tema = TemaAl(no);
        float x = t.uzunluk;
        float y = Bolumler.Yukseklik(no, x);
        Boy("Teslimat Bayragi", K("Dekor/flag_green_a"), p, new Vector3(x, y - 0.15f, 0), 2.6f, -15);
        Boy("Teslimat Tabelasi", K("Dekor/sign"), p, new Vector3(x - 2.4f, y - 0.15f, 0), 2.4f, -16);
        Yazi3D(p, "TESLİMAT", new Vector3(x - 2.4f, y - 0.15f + 2.4f * 0.6f, 0), 0.045f, new Color(0.3f, 0.14f, 0.05f), -15);

        for (int i = 0; i < tema.musteri.Length; i++)
        {
            float ax = x + 9f + i * 10f;
            float ay = Bolumler.Yukseklik(no, ax);
            float mb = tema.musteriBoy * (i == 0 ? 1f : 0.92f);
            Boy(t.musteri, R(tema.musteri[i]), p, new Vector3(ax, ay - Gomme * mb, 0), mb, -20 - i);
        }
        // musteri tabelasi (Damla Su lacivert tabela)
        float tx = x + 4.2f, ty = Bolumler.Yukseklik(no, tx);
        float tg = t.musteri.Length * 0.2f + 0.5f;
        Color hat = new Color(0.13f, 0.1f, 0.17f);
        Spr("Musteri Direk", kare, p, new Vector3(tx - tg * 0.3f, ty + 0.9f, 0), new Vector3(0.14f, 2f, 1), -18, hat);
        Spr("Musteri Direk", kare, p, new Vector3(tx + tg * 0.3f, ty + 0.9f, 0), new Vector3(0.14f, 2f, 1), -18, hat);
        Spr("Musteri Tabela Hat", kare, p, new Vector3(tx, ty + 2.3f, 0), new Vector3(tg + 0.14f, 0.84f, 1), -17, hat);
        Spr("Musteri Tabela", kare, p, new Vector3(tx, ty + 2.3f, 0), new Vector3(tg, 0.7f, 1), -16, new Color(0.08f, 0.32f, 0.68f));
        Yazi3D(p, t.musteri, new Vector3(tx, ty + 2.3f, 0), 0.05f, Color.white, -15);
        if (tema.cali.Length > 0)
            for (int i = 0; i < 3; i++) Boy("Cali", K("Cevre/" + tema.cali[i % tema.cali.Length]), p, new Vector3(x + 5f + i * 6f, Bolumler.Yukseklik(no, x + 5f + i * 6f) - 0.2f, 0), 0.9f, -8);
    }

    // HingeJoint2D zinciriyle asma kopru (Kenney kopru tahtalari)
    static void KopruKur(int no, BolumTanim t, EdgeCollider2D arazi)
    {
        var k = new GameObject("Asma Kopru");
        k.AddComponent<Kopru>().arazi = arazi;
        float a = t.kopru.x, b = t.kopru.y;
        float ha = Bolumler.Yukseklik(no, a), hb = Bolumler.Yukseklik(no, b);
        int n = Mathf.RoundToInt(b - a);
        float w = (b - a) / n;
        var tahtaSp = K("Dekor/bridge");
        Rigidbody2D onceki = null;
        for (int i = 0; i < n; i++)
        {
            float u = (i + 0.5f) / n;
            var go = new GameObject("Tahta " + (i + 1));
            go.transform.SetParent(k.transform, false);
            go.transform.position = new Vector3(a + w * (i + 0.5f), Mathf.Lerp(ha, hb, u) - 0.12f, 0);
            go.transform.localScale = new Vector3(w, 1f, 1f);
            // tahta gorseli: karonun ust yarisinda; carpisma kutusuna hizalanir
            Spr("Gorsel", tahtaSp, go.transform, new Vector3(0, -0.13f, 0), new Vector3(1.04f, 0.55f, 1f), -2, Color.white);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.35f;
            rb.angularDamping = 0.5f;
            rb.linearDamping = 0.1f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(1f, 0.24f);
            bc.sharedMaterial = zemin;
            var j = go.AddComponent<HingeJoint2D>();
            j.autoConfigureConnectedAnchor = false;
            j.anchor = new Vector2(-0.5f, 0.12f);
            if (onceki == null) j.connectedAnchor = new Vector2(a, ha);
            else { j.connectedBody = onceki; j.connectedAnchor = new Vector2(0.5f, 0.12f); }
            if (i == n - 1)
            {
                var j2 = go.AddComponent<HingeJoint2D>();
                j2.autoConfigureConnectedAnchor = false;
                j2.anchor = new Vector2(0.5f, 0.12f);
                j2.connectedAnchor = new Vector2(b, hb);
            }
            onceki = rb;
        }
        Color hat = new Color(0.13f, 0.1f, 0.17f), ahsap = new Color(0.62f, 0.38f, 0.2f);
        foreach (var x in new[] { a, b })
        {
            float y = x == a ? ha : hb;
            Spr("Kazik Hat", kare, k.transform, new Vector3(x, y + 0.45f, 0), new Vector3(0.36f, 1.86f, 1), -4, hat);
            Spr("Kazik", kare, k.transform, new Vector3(x, y + 0.45f, 0), new Vector3(0.24f, 1.74f, 1), -3, ahsap);
        }
        float dip = Mathf.Min(ha, hb) - Bolumler.KopruDerinlik;
        var dere = Spr("Dere", K("Dekor/water_top"), k.transform, new Vector3((a + b) * 0.5f, dip - 0.2f, 0), Vector3.one * 1.4f, -8, Color.white);
        var dsr = dere.GetComponent<SpriteRenderer>();
        dsr.drawMode = SpriteDrawMode.Tiled;
        dsr.size = new Vector2((b - a + 1f) / 1.4f, 1f);
        float ux = a - 7f;
        float uy = Bolumler.Yukseklik(no, ux);
        Boy("Uyari Tabelasi", K("Dekor/sign"), k.transform, new Vector3(ux, uy - 0.15f, 0), 2.2f, -16);
        Yazi3D(k.transform, "DİKKAT!\nASMA KÖPRÜ", new Vector3(ux, uy - 0.15f + 2.2f * 0.6f, 0), 0.034f, new Color(0.3f, 0.14f, 0.05f), -15);

        // kanyonun arka duvari (bosluktan gokyuzu gorunmesin): koyulastirilmis zemin dokusu
        float tepe = Mathf.Max(ha, hb) - 0.2f, taban = dip - 2f;
        var m = new Mesh { name = "Bolum" + no + "_KanyonDuvar" };
        m.vertices = new[] { new Vector3(a, taban), new Vector3(a, tepe), new Vector3(b, tepe), new Vector3(b, taban) };
        m.uv = new[] { new Vector2(a / Karo, taban / Karo), new Vector2(a / Karo, tepe / Karo), new Vector2(b / Karo, tepe / Karo), new Vector2(b / Karo, taban / Karo) };
        var koyu = new Color(0.55f, 0.5f, 0.55f);
        m.colors = new[] { koyu, koyu, koyu, koyu };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateBounds();
        m = MeshKaydet(m);
        var duvar = new GameObject("Kanyon Duvari");
        duvar.transform.SetParent(k.transform, false);
        duvar.AddComponent<MeshFilter>().sharedMesh = m;
        var dmr = duvar.AddComponent<MeshRenderer>();
        dmr.sharedMaterial = ZeminMalzeme(TemaAl(no).zemin, "center");
        dmr.sortingOrder = -12;
    }

    static void Jetonlar(int no, BolumTanim t)
    {
        var kok = new GameObject("Damla Jetonlari").transform;
        var rnd = new System.Random(no * 31);
        float x0 = Bolumler.BasDuz + 6f, x1 = t.uzunluk - 10f;
        for (int i = 0; i < t.jeton; i++)
        {
            float x = Mathf.Lerp(x0, x1, (i + 0.5f) / t.jeton) + ((float)rnd.NextDouble() - 0.5f) * 4f;
            // tankerin govdesi/tanki hizasinda (yoldan ~1.1-1.6 m): kasa ya da tank degerek her zaman alinabilir.
            // Cevredeki en yuksek zemine gore yerlestirilir ki tepe ustunde yokusun icine gomulmesin.
            float zemin = Mathf.Max(Bolumler.Yol(no, x - 0.6f), Mathf.Max(Bolumler.Yol(no, x), Bolumler.Yol(no, x + 0.6f)));
            float y = zemin + (i % 3 == 2 ? 1.4f : 1.0f);
            var j = Spr("Damla Jetonu", damla, kok, new Vector3(x, y, 0), Vector3.one, 5, new Color(0.45f, 0.82f, 1f));
            j.layer = 2;
            var cc = j.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.5f;
            j.AddComponent<DamlaJeton>();
            Spr("Hat", damla, j.transform, new Vector3(0, -0.01f, 0), Vector3.one * 1.22f, 4, new Color(0.13f, 0.1f, 0.17f));
            Spr("Parilti", yumusak, j.transform, Vector3.zero, Vector3.one * 1.5f, 3, new Color(0.6f, 0.9f, 1f, 0.45f));
        }
    }

    // ================================================================ yardimcilar
    static Sprite S(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }

    static GameObject Spr(string ad, Sprite sp, Transform parent, Vector3 pos, Vector3 scale, int order, Color c)
    {
        var go = new GameObject(ad);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var s = go.AddComponent<SpriteRenderer>();
        s.sprite = sp;
        s.sortingOrder = order;
        s.color = c;
        return go;
    }

    static void Yazi3D(Transform parent, string yazi, Vector3 pos, float boy, Color c, int order)
    {
        var go = new GameObject("Yazi: " + yazi.Replace("\n", " "));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = yazi;
        tm.fontSize = 64;
        tm.characterSize = boy;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Normal;
        tm.color = c;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = order;
    }

    // ================================================================ uretilen basit gorseller
    static void Uretilenler()
    {
        Directory.CreateDirectory(Uret);
        Kaydet("kare", 4, 4, (x, y) => Color.white);
        Kaydet("daire", 128, 128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64));
            return new Color(1, 1, 1, Mathf.Clamp01(63.5f - d));
        });
        Kaydet("yumusak", 64, 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
            float a = Mathf.Clamp01(1f - d);
            return new Color(1, 1, 1, a * a);
        });
        Kaydet("gradyan", 4, 128, (x, y) => new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, y / 127f)));
        Kaydet("kasis", 96, 28, (x, y) =>
        {
            float u = (x + 0.5f) / 96f * 2f - 1f;
            float top = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * 26f;
            if (y + 0.5f > top) return new Color(0, 0, 0, 0);
            bool sari = ((x + y) / 12) % 2 == 0;
            return sari ? new Color(1f, 0.82f, 0.15f) : new Color(0.12f, 0.12f, 0.12f);
        });
        // tank: 1.5 x 0.75 m (192x96 px, 128 ppu)
        Kaydet("tank_arka", 192, 96, (x, y) =>
        {
            float d = YuvarlakKutu(x + 0.5f, y + 0.5f, 96f, 48f, 92f, 44f, 16f);
            return new Color(0.82f, 0.92f, 1f, 0.3f * Mathf.Clamp01(-d + 0.5f));
        });
        Kaydet("tank_on", 192, 96, (x, y) =>
        {
            float d = YuvarlakKutu(x + 0.5f, y + 0.5f, 96f, 48f, 92f, 44f, 16f);
            float kenar = Mathf.Clamp01(-d + 0.5f);
            if (d > -4.5f) return new Color(0.13f, 0.1f, 0.17f, kenar);   // Kenney tarzi koyu dis hat
            if (d > -8f) return new Color(0.92f, 0.96f, 1f, 1f);
            // olcu cizgileri (sag kenar) ve parlama seridi
            if (x > 170 && x < 180 && (y % 12) < 2) return new Color(1, 1, 1, 0.55f);
            if (x > 16 && x < 26) return new Color(1, 1, 1, 0.28f);
            if ((x == 64 || x == 128) && y > 6 && y < 90) return new Color(1, 1, 1, 0.18f);
            return new Color(0, 0, 0, 0);
        });
        Kaydet("damla", 64, 80, (x, y) =>
        {
            float px = x + 0.5f, py = y + 0.5f;
            float daire = 24f - Vector2.Distance(new Vector2(px, py), new Vector2(32f, 28f));
            float koni = -1f;
            if (py > 28f && py < 78f)
            {
                float k = (78f - py) / 50f;
                koni = 24f * Mathf.Sqrt(k) * k * 1.05f - Mathf.Abs(px - 32f);
            }
            float a = Mathf.Clamp01(Mathf.Max(daire, koni) + 0.5f);
            // parlak nokta
            float parla = Vector2.Distance(new Vector2(px, py), new Vector2(24f, 34f)) < 6f ? 1f : 0f;
            Color c = Color.Lerp(Color.white * 0.92f, Color.white, parla);
            c.a = a;
            return c;
        });
    }

    // Yuvarlak koseli dikdortgen isaretli mesafesi (icerisi negatif)
    static float YuvarlakKutu(float x, float y, float cx, float cy, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(x - cx) - (hw - r), qy = Mathf.Abs(y - cy) - (hh - r);
        return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    static void Kaydet(string ad, int w, int h, System.Func<int, int, Color> f)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, f(x, y));
        t.Apply();
        File.WriteAllBytes(Uret + ad + ".png", t.EncodeToPNG());
    }
}

// Sprites klasorundeki resimler icin ice aktarma ayarlari (boyutlar oyun dunyasina gore)
public class SpriteAyarlari : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (p.StartsWith("Assets/Sprites/Kenney/") || p.StartsWith("Assets/Sprites/Render/")) { Kenney(p); return; }
        if (p.StartsWith("Assets/Sprites/Arayuz/"))
        {
            // OnGUI arayuz dokulari (9 dilimli panel/buton): mipmap yok, kenarlar keskin
            var ui = (TextureImporter)assetImporter;
            ui.textureType = TextureImporterType.GUI;
            ui.mipmapEnabled = false;
            ui.alphaIsTransparency = true;
            ui.textureCompression = TextureImporterCompression.Uncompressed;
            ui.filterMode = FilterMode.Bilinear;
            ui.wrapMode = TextureWrapMode.Clamp;
            return;
        }
        if (!p.StartsWith("Assets/Sprites/")) return;
        var imp = (TextureImporter)assetImporter;
        string ad = Path.GetFileNameWithoutExtension(p);
        float ppu = 100f;
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        switch (ad)
        {
            case "kamyon": ppu = 170f; break;      // ~4 m uzunluk
            case "bufe": ppu = 110f; break;        // ~5.4 m
            case "apartman": ppu = 26f; break;     // ~7.7 m yukseklik
            case "su_sisesi": ppu = 240f; break;
            case "kare": ppu = 4f; break;          // 1 m
            case "daire": ppu = 128f; break;       // 1 m
            case "yumusak": ppu = 64f; break;
            case "gradyan": ppu = 128f; break;     // 1 m yukseklik
            case "ucgen": ppu = 128f; break;       // 1 m
            case "kasis": ppu = 64f; break;        // 1.5 m
            case "teker": ppu = 128f / 0.6f; break; // cap 0.6 m
            case "tank_arka": case "tank_on": ppu = 128f; break; // 1.5 x 0.75 m
            case "damla": ppu = 160f; pivot = new Vector2(0.5f, 0.35f); break; // 0.5 m
            case "bayrak": ppu = 64f; pivot = new Vector2(0f, 0.5f); break;    // 1.5 x 1 m, direge bagli
            case "tahta": ppu = 64f; break;        // 1 x 0.25 m
        }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = ad == "bayrak" ? FilterMode.Point : FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        imp.SetTextureSettings(s);
    }

    // Kenney paketleri ve 3D modellerden render edilen sprite'lar
    void Kenney(string p)
    {
        var imp = (TextureImporter)assetImporter;
        string ad = Path.GetFileNameWithoutExtension(p);
        imp.mipmapEnabled = true;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;   // build boyutu icin (BC7), kalite kaybi yok denecek kadar az
        imp.filterMode = FilterMode.Trilinear;

        // zemin dokulari: mesh malzemesi olarak tekrarlanir
        if (p.Contains("/Zemin/"))
        {
            imp.textureType = TextureImporterType.Default;
            imp.wrapModeU = TextureWrapMode.Repeat;
            // ust serit dikeyde tekrar etmez (kenar sizmasi ince cizgi yapmasin)
            imp.wrapModeV = ad.EndsWith("_top") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            return;
        }

        float ppu = 100f;
        Vector2 pivot = new Vector2(0.5f, 0f);   // cogu nesne zemine oturur
        bool tekrar = false;
        if (p.Contains("/Render/")) { ppu = ModelRender.PikselBirim; if (ad.StartsWith("tanker")) pivot = new Vector2(0.5f, 0.5f); }
        else if (p.Contains("/Dekor/")) { ppu = 128f; if (ad.StartsWith("bridge")) pivot = new Vector2(0.5f, 0.5f); }
        else if (p.Contains("/ArkaPlan/")) tekrar = true;
        if (ad.StartsWith("cloud") && !p.Contains("/ArkaPlan/") || ad.StartsWith("sun") || ad.StartsWith("moon")) pivot = new Vector2(0.5f, 0.5f);

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.wrapModeU = tekrar ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.wrapModeV = TextureWrapMode.Clamp;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteGenerateFallbackPhysicsShape = true;
        imp.SetTextureSettings(s);
    }
}
