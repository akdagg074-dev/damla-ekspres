using UnityEngine;

// Su tankerinin surusu.
// - On ve arka tekerlek govdeye birer WheelJoint2D ile baglidir: yay (suspansiyon) + motor.
// - Gaz: tekerlek motorlari ileri doner. Fren/geri: once frenler, durunca geri vites.
// - Havadayken gaz/fren tusu govdeyi dondurur (inise hazirlik).
// - Havada kalma suresi ve takla takip edilir (oyunlastirma olaylari).
[RequireComponent(typeof(Rigidbody2D))]
public class AracKontrol : MonoBehaviour
{
    public static AracKontrol I;

    [Header("Tekerlekler (WheelJoint2D)")]
    public WheelJoint2D onTeker;
    public WheelJoint2D arkaTeker;

    [Header("Motor")]
    [Tooltip("Ileri azami hiz (m/sn)")] public float azamiHiz = 11f;
    [Tooltip("Geri vites azami hizi (m/sn)")] public float geriHiz = 4f;
    [Tooltip("Gazda hizlanma (m/sn^2). Ani hizlanma suyu calkalar, bu yuzden sinirli")] public float hizlanma = 2.6f;
    [Tooltip("Frende yavaslama (m/sn^2)")] public float yavaslama = 3.5f;
    [Tooltip("Her tekerlege uygulanabilen en fazla motor torku (dik yokuslar icin)")] public float tork = 8f;
    public float frenTorku = 16f;

    [Header("Havada denge")]
    public float havaTorku = 4.5f;

    [Tooltip("Kapaliyken arac el freninde bekler (menu, bolum sonu)")]
    public bool kontrolAktif = true;

    // Damla jetonlari "Ignore Raycast" katmanindadir; tekerlek temas kontrolu onlari saymaz
    public const int ZeminMaske = ~(1 << 2);

    public Rigidbody2D Rb { get; private set; }
    public float Giris { get; private set; }
    public bool Havada { get; private set; }
    public float HavaSuresi { get; private set; }
    public float HizKmSa { get { return Rb.linearVelocity.magnitude * 3.6f; } }

    public event System.Action<float> Indi;   // havada kalinan sure
    public event System.Action Takla;

    Collider2D onC, arkaC;
    float donus, oncekiAci, komut, tekerYaricap = 0.3f;

    void Awake()
    {
        I = this;
        Rb = GetComponent<Rigidbody2D>();
        onC = onTeker.connectedBody.GetComponent<Collider2D>();
        arkaC = arkaTeker.connectedBody.GetComponent<Collider2D>();
        var cc = onC as CircleCollider2D;
        if (cc != null) tekerYaricap = cc.radius * cc.transform.lossyScale.x;
        oncekiAci = Rb.rotation;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Giris = kontrolAktif ? Tus.Surus() : 0f;
        bool yerde = onC.IsTouchingLayers(ZeminMaske) || arkaC.IsTouchingLayers(ZeminMaske);

        // ---- motor / fren (WheelJoint2D motoru)
        // Motor, hedef bir tekerlek hizini izler; hedef hiz sinirli ivmeyle degisir.
        // Boylece yokusta yeterli tork varken duzde ani kalkis/fren suyu dokmez.
        float ileri = Vector2.Dot(Rb.linearVelocity, transform.right);
        float tork0 = tork;
        bool motorAcik = true;
        if (!kontrolAktif)
        {
            komut = Mathf.MoveTowards(Mathf.Min(komut, Mathf.Max(ileri, 0f)), 0f, yavaslama * dt);
            tork0 = frenTorku;
        }
        else if (Giris > 0.05f)
        {
            if (komut < ileri - 0.3f) komut = ileri; // yokus asagi: motor freni gibi hizi tutar
            komut = Mathf.MoveTowards(komut, azamiHiz * Giris, hizlanma * dt);
        }
        else if (Giris < -0.05f)
        {
            if (ileri > 0.3f) { komut = Mathf.MoveTowards(Mathf.Min(komut, ileri), 0f, yavaslama * dt); tork0 = frenTorku; }
            else komut = Mathf.MoveTowards(Mathf.Min(komut, ileri + 0.3f), -geriHiz * -Giris, hizlanma * dt);
        }
        else { motorAcik = false; komut = ileri; } // serbest yuvarlanma
        var m = new JointMotor2D { motorSpeed = -komut / tekerYaricap * Mathf.Rad2Deg, maxMotorTorque = tork0 };
        Motor(onTeker, motorAcik, m);
        Motor(arkaTeker, motorAcik, m);

        // ---- havada denge
        if (!yerde && kontrolAktif) Rb.AddTorque(Giris * havaTorku);

        // ---- havada kalma ve takla takibi
        float aci = Rb.rotation;
        float d = Mathf.DeltaAngle(oncekiAci, aci);
        oncekiAci = aci;
        if (!yerde)
        {
            HavaSuresi += dt;
            donus += d;
            if (Mathf.Abs(donus) >= 340f)
            {
                donus -= Mathf.Sign(donus) * 360f;
                if (Takla != null) Takla();
            }
        }
        else
        {
            if (Havada && HavaSuresi > 0.15f && Indi != null) Indi(HavaSuresi);
            HavaSuresi = 0f;
            donus = 0f;
        }
        Havada = !yerde;
    }

    static void Motor(WheelJoint2D j, bool acik, JointMotor2D m)
    {
        j.useMotor = acik;
        if (acik) j.motor = m;
    }
}
