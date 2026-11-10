using UnityEngine;

// Yol uzerindeki Damla Su jetonu: tankerle dokununca toplanir (+puan, "Damla Avcisi" rozeti).
public class DamlaJeton : MonoBehaviour
{
    Vector3 bas, olcek;
    float faz;
    bool alindi;

    void Start()
    {
        bas = transform.position;
        olcek = transform.localScale;
        faz = bas.x * 0.7f;
    }

    void Update()
    {
        float t = Time.time + faz;
        transform.position = bas + Vector3.up * Mathf.Sin(t * 3f) * 0.12f;
        transform.localScale = new Vector3(olcek.x * Mathf.Max(0.25f, Mathf.Abs(Mathf.Cos(t * 2.2f))), olcek.y, 1f);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (alindi || c.transform.root.GetComponentInChildren<AracKontrol>() == null) return;
        alindi = true;
        if (BolumYonetici.I != null) BolumYonetici.I.JetonAl(transform.position);
        Efekt.Pirilti(transform.position);
        Destroy(gameObject);
    }
}
