using UnityEngine;

// Hiz kasisi kurali: tanker kasisi guvenli hizdan hizli gecerse tanktaki su sicrar ve "YAVAS!" uyarisi cikar.
// Fazla hiz arttikca dokulen su da artar (en fazla kalan suyun %25'i).
public class Kasis : MonoBehaviour
{
    [Tooltip("Bu hizin (km/sa) altinda gecis cezasizdir")] public float guvenliHiz = 15f;
    [Tooltip("Guvenli hizi asan her km/sa icin kalan suyun dokulen orani")] public float kayipKatsayisi = 0.012f;

    float oncekiX;
    bool hazir;

    void FixedUpdate()
    {
        var a = AracKontrol.I;
        if (a == null) return;
        float x = a.transform.position.x, kx = transform.position.x;
        if (hazir && oncekiX < kx && x >= kx)
        {
            float fazla = a.HizKmSa - guvenliHiz;
            if (fazla > 0f)
            {
                a.GetComponentInChildren<SuTanki>().Sicrat(Mathf.Min(0.25f, fazla * kayipKatsayisi));
                Efekt.Yazi(transform.position + Vector3.up * 2.8f, "YAVAŞ! Kasis", Arayuz.Sari, 1.3f);
                Sfx.Cal("kasis", 0.9f);
                Kamera.Salla(0.35f);
            }
            else Sfx.Cal("kasis", 0.3f);
        }
        oncekiX = x;
        hazir = true;
    }
}
