using UnityEngine;

// Arka plan katmani: kamerayla birlikte daha yavas kayar (derinlik hissi). Bulutlar ayrica suzulur.
public class Parallax : MonoBehaviour
{
    [Range(0f, 1f)] public float carpan = 0.5f;
    [Tooltip("Dikey takip orani (yokus/inislerde arka plan da kaysin)")]
    [Range(0f, 1f)] public float carpanY = 0f;
    public float suzulme = 0f;
    Vector3 baslangic;
    Transform cam;
    float kayma, camY0;

    void Start()
    {
        baslangic = transform.position;
        if (Camera.main != null) { cam = Camera.main.transform; camY0 = cam.position.y; }
    }

    void LateUpdate()
    {
        if (cam == null) return;
        kayma += suzulme * Time.deltaTime;
        float x = baslangic.x + cam.position.x * carpan + kayma;
        // bulutlar ekrandan cikinca diger taraftan gelsin
        if (suzulme != 0f)
        {
            float d = x - cam.position.x;
            if (d > 25f) kayma -= 50f;
            else if (d < -25f) kayma += 50f;
            x = baslangic.x + cam.position.x * carpan + kayma;
        }
        // dikeyde kameranin baslangic yuksekligine gore kayar (katman kuruldugu yerde baslar)
        transform.position = new Vector3(x, baslangic.y + (cam.position.y - camY0) * carpanY, baslangic.z);
    }
}
