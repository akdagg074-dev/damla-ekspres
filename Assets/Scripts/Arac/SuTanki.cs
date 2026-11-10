using UnityEngine;

// Tanker kasasindaki su (ustunde havalandirma kapagi olan kapali tank).
// 1) Calkalanma: su yuzeyi, hissettigi etkin yercekimine (g - a) dik olmak ister. Yuzeyin dunyadaki acisi
//    bu hedef aciya sonumlu bir yay gibi yaklasir: ani gaz/fren, tumsek ve inislerde salinir.
//    Tank egilince su dunyaya gore duz kalir; tanka gore yuzey acisi = yuzey acisi - kasa egimi.
// 2) Kutle ve agirlik merkezi: suyun kutlesi ve egik yuzeyli su kutlesinin agirlik merkezi
//    her fizik adiminda kasanin Rigidbody2D'sine (mass + centerOfMass) yazilir.
//    Su geriye yigilinca on teker hafifler; su azaldikca arac hafifler ve daha cok ziplar.
// 3) Dokulme: calkalanma genligi esigi asarsa su kapaktan tasar (dolu tank daha kolay tasar);
//    tank cok yatarsa / ters donerse bosalir; sert inislerde (yuksek g) su sicrar.
public class SuTanki : MonoBehaviour
{
    [Header("Tank olculeri (yerel, metre)")]
    public float genislik = 1.5f;
    public float yukseklik = 0.75f;
    [Tooltip("Dolu tankta su yuksekligi / tank yuksekligi")]
    [Range(0.3f, 0.95f)] public float dolulukOrani = 0.62f;

    [Header("Kutle")]
    public float kapasiteLitre = 1000f;
    [Tooltip("Dolu tanktaki suyun kutlesi (Rigidbody2D birimi)")] public float suKutlesi = 1f;
    [Tooltip("Bos kasanin kutlesi")] public float bosKutle = 1.2f;
    [Tooltip("Bos kasanin agirlik merkezi (kasa yerel)")] public Vector2 bosAgirlikMerkezi = new Vector2(0.6f, -0.35f);

    [Header("Calkalanma")]
    [Tooltip("Suyun dogal salinim frekansi (Hz)")] public float frekans = 1.1f;
    [Tooltip("Sonum orani: tankin icindeki dalgakiran levhalarin etkisi")]
    [Range(0f, 1f)] public float sonum = 0.45f;

    [Header("Dokulme")]
    [Tooltip("Calkalanma genligi (radyan) bu esigi asinca su kapaktan tasar. Tank bosaldikca esik buyur")] public float calkalanmaEsigi = 0.18f;
    [Tooltip("Esigi asan her radyan icin saniyede dokulen kapasite orani")] public float tasmaKatsayisi = 0.5f;
    [Tooltip("Tank buna gore bu aciyi (derece) asinca kapaktan bosalir")] public float yatmaSiniri = 56f;
    [Tooltip("Bu g degerinin uzerindeki darbeler su sicratir")] public float darbeEsigi = 3.8f;
    [Tooltip("Esigi asan her g icin kalan suyun dokulen orani")] public float darbeKaybi = 0.015f;

    [Header("Gorsel")]
    public MeshFilter suMesh;
    public Color ustRenk = new Color(0.45f, 0.8f, 1f, 0.92f);
    public Color altRenk = new Color(0.08f, 0.38f, 0.85f, 0.95f);

    public float Litre { get; private set; }
    public float Oran { get { return Litre / kapasiteLitre; } }
    public float DarbeKaybi { get; private set; }     // sert inislerde toplam kayip (L)
    public float DokulmeHizi { get; private set; }    // L/sn (HUD uyarisi)
    public float YuzeyAcisi { get { return aci * Mathf.Rad2Deg; } }   // tanka gore
    public float Calkalanma { get { return Mathf.Abs(psi - ortalama); } } // genlik (radyan)
    public event System.Action<float> Darbe;           // sert iniste dokulen litre

    Rigidbody2D rb;
    float psi, psiHiz, ortalama;   // yuzeyin dunyadaki acisi, acisal hizi ve ortalama (denge) acisi
    float aci, darbeBekle, damlaBirikim;
    Vector2 oncekiHiz, ivme;
    bool ilk = true;
    Mesh mesh;
    const int N = 14;
    Vector3[] kose;
    Color[] renk;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        rb.useAutoMass = false;
        Litre = kapasiteLitre;
        MeshKur();
        Kutle();
    }

    float Duz { get { return yukseklik * dolulukOrani * Oran; } }

    // Egik su yuzeyi y = merkez + t*x. Az suda yuzey tabani kesince su ucgen olur (alan korunur).
    void Yuzey(out float merkez, out float t, out float yuksek)
    {
        float h = Duz, yarim = genislik * 0.5f;
        t = Mathf.Tan(aci);
        float e = Mathf.Abs(t) * yarim;
        if (e <= h) { merkez = h; yuksek = h + e; }
        else { yuksek = Mathf.Sqrt(2f * h * genislik * Mathf.Abs(t)); merkez = yuksek - e; }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // tankin ivmesi (tank tabanindaki noktanin hiz degisimi, hafif suzgecli)
        Vector2 hiz = rb.GetPointVelocity(transform.position);
        if (ilk) { oncekiHiz = hiz; ilk = false; }
        ivme = Vector2.Lerp(ivme, (hiz - oncekiHiz) / dt, 1f - Mathf.Exp(-dt / 0.1f));
        oncekiHiz = hiz;

        // etkin yercekimi (dunya ekseninde) ve yuzeyin olmak istedigi aci
        Vector2 gEtkin = Physics2D.gravity * rb.gravityScale - ivme;
        bool ters = transform.up.y < -0.2f;   // tank gercekten ters donmus
        float hedef = Mathf.Clamp(Mathf.Atan2(gEtkin.x, -gEtkin.y), -0.8f, 0.8f);
        // yay sertligi etkin yercekimiyle orantili (sarkac gibi): serbest dususte su "agirliksiz" kalir
        float gOran = Mathf.Clamp(gEtkin.magnitude / 9.81f, 0f, 2.5f);
        ortalama = Mathf.Lerp(ortalama, hedef, (1f - Mathf.Exp(-dt / 0.5f)) * Mathf.Min(1f, gOran));

        // calkalanma: sonumlu yay
        float w = 2f * Mathf.PI * frekans;
        psiHiz += (w * w * gOran * (hedef - psi) - 2f * sonum * w * psiHiz) * dt;
        psi = Mathf.Clamp(psi + psiHiz * dt, -0.9f, 0.9f);
        aci = Mathf.Clamp(Mathf.DeltaAngle(rb.rotation, psi * Mathf.Rad2Deg) * Mathf.Deg2Rad, -1.2f, 1.2f);

        // dokulme
        float kayip = 0f;
        if (Litre > 0f)
        {
            float merkez, t, yuksek;
            Yuzey(out merkez, out t, out yuksek);
            float esik = calkalanmaEsigi + (1f - dolulukOrani * Oran) * 0.4f;
            float tasma = Calkalanma - esik;
            float yatma = Mathf.Abs(aci) * Mathf.Rad2Deg - yatmaSiniri;
            if (ters) kayip += 0.5f * kapasiteLitre * dt;
            else
            {
                if (tasma > 0f) kayip += tasmaKatsayisi * tasma * kapasiteLitre * dt;
                if (yatma > 0f) kayip += yatma * 0.01f * kapasiteLitre * dt;
            }

            darbeBekle -= dt;
            float g = gEtkin.magnitude / 9.81f;
            if (g > darbeEsigi && darbeBekle <= 0f)
            {
                float d = Mathf.Min(Litre - kayip, (g - darbeEsigi) * darbeKaybi * Litre);
                kayip += d;
                DarbeKaybi += d;
                darbeBekle = 0.3f;
                Sfx.Cal("su", Mathf.Clamp01(0.4f + d * 0.02f));
                Kamera.Salla(Mathf.Clamp(g * 0.05f, 0.1f, 0.5f));
                if (Darbe != null) Darbe(d);
                for (int i = 0; i < 6 + (int)(d * 0.4f) && i < 30; i++) DamlaAt(Random.Range(-1f, 1f), 2.5f);
            }
            kayip = Mathf.Min(kayip, Litre);
            Litre -= kayip;
            if (Litre < 0.5f) Litre = 0f;

            // tasan sudan damlalar (yuksek kenardan)
            damlaBirikim += kayip;
            while (damlaBirikim > 2.5f)
            {
                damlaBirikim -= 2.5f;
                DamlaAt(ters ? Random.Range(-1f, 1f) : Mathf.Sign(t), 1.2f);
            }
        }
        DokulmeHizi = Mathf.Lerp(DokulmeHizi, kayip / dt, 0.15f);

        Kutle();
    }

    // Disaridan su sicratma (ornegin kasise hizli girmek): kalan suyun bu orani dokulur
    public void Sicrat(float oran)
    {
        float d = Litre * Mathf.Clamp01(oran);
        if (d <= 0f) return;
        Litre -= d;
        DarbeKaybi += d;
        psiHiz += Random.Range(-2f, 2f);
        Sfx.Cal("su", Mathf.Clamp01(0.4f + d * 0.02f));
        if (Darbe != null) Darbe(d);
        for (int i = 0; i < 8 + (int)(d * 0.3f) && i < 30; i++) DamlaAt(Random.Range(-1f, 1f), 2.5f);
        Kutle();
    }

    // Suyun kutlesini ve agirlik merkezini kasaya yansit
    void Kutle()
    {
        float mw = suKutlesi * Oran;
        float merkez, t, yuksek;
        Yuzey(out merkez, out t, out yuksek);
        float h = Mathf.Max(Duz, 0.001f);
        // egik yuzeyli dikdortgen kesitin agirlik merkezi kaymasi: t * W^2 / (12 h)
        float dx = Mathf.Clamp(t * genislik * genislik / (12f * h), -genislik / 3f, genislik / 3f);
        Vector2 suYerel = (Vector2)transform.localPosition + new Vector2(dx, h * 0.5f);
        rb.mass = bosKutle + mw;
        rb.centerOfMass = (bosAgirlikMerkezi * bosKutle + suYerel * mw) / rb.mass;
    }

    void DamlaAt(float taraf, float guc)
    {
        Vector3 yerel = new Vector3(taraf * genislik * 0.5f, yukseklik, 0f);
        Vector3 p = transform.TransformPoint(yerel);
        Vector2 v = rb.GetPointVelocity(p) * 0.8f
                  + (Vector2)transform.up * Random.Range(1f, 2.5f) * guc
                  + (Vector2)transform.right * taraf * Random.Range(0.5f, 1.5f);
        Efekt.Damla(p, v);
    }

    // ---------------------------------------------------------------- gorsel (su kesiti)
    void MeshKur()
    {
        if (suMesh == null) return;
        mesh = new Mesh { name = "su" };
        mesh.MarkDynamic();
        kose = new Vector3[(N + 1) * 2];
        renk = new Color[kose.Length];
        var ucgen = new int[N * 6];
        for (int i = 0; i < N; i++)
        {
            int a = i * 2;
            ucgen[i * 6] = a; ucgen[i * 6 + 1] = a + 1; ucgen[i * 6 + 2] = a + 3;
            ucgen[i * 6 + 3] = a; ucgen[i * 6 + 4] = a + 3; ucgen[i * 6 + 5] = a + 2;
        }
        LateUpdate();
        mesh.vertices = kose;
        mesh.triangles = ucgen;
        suMesh.sharedMesh = mesh;
    }

    void LateUpdate()
    {
        if (mesh == null || kose == null) return;
        float merkez, t, yuksek;
        Yuzey(out merkez, out t, out yuksek);
        float dalga = 0.008f + Mathf.Min(0.03f, Mathf.Abs(psiHiz) * 0.01f);
        for (int i = 0; i <= N; i++)
        {
            float x = -genislik * 0.5f + genislik * i / N;
            float y = Litre <= 0f ? 0f : merkez + t * x + Mathf.Sin(Time.time * 7f + x * 8f) * dalga;
            y = Mathf.Clamp(y, 0f, yukseklik);
            kose[i * 2] = new Vector3(x, 0f, 0f);
            kose[i * 2 + 1] = new Vector3(x, y, 0f);
            renk[i * 2] = altRenk;
            renk[i * 2 + 1] = ustRenk;
        }
        mesh.vertices = kose;
        mesh.colors = renk;
        mesh.RecalculateBounds();
    }
}
