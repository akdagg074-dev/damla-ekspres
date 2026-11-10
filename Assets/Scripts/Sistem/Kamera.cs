using UnityEngine;

// Tankeri iki eksende yumusakca takip eder; hizlaninca ileriye bakar ve biraz uzaklasir. Sarsinti destekli.
public class Kamera : MonoBehaviour
{
    public static Kamera I;
    public Transform hedef;
    public float minX = -1000f, maxX = 1000f;
    [Tooltip("Hedefe gore kamera kaydirmasi")] public float xOfset = 0f, yOfset = 1.6f;
    [Tooltip("Hizlanirken ileriye bakma mesafesi")] public float onGorus = 3f;
    public float boy = 6.5f, hizliBoy = 8f;

    Camera cam;
    Vector3 hiz;
    float bakis, sarsinti;

    void Awake() { I = this; cam = GetComponent<Camera>(); }

    public static void Salla(float m) { if (I != null) I.sarsinti = Mathf.Max(I.sarsinti, m); }

    void LateUpdate()
    {
        if (hedef == null) return;
        float dt = Time.deltaTime;
        var rb = hedef.GetComponent<Rigidbody2D>();
        Vector2 v = rb != null ? rb.linearVelocity : Vector2.zero;
        bakis = Mathf.Lerp(bakis, Mathf.Clamp(v.x / 10f, -1f, 1f) * onGorus, dt * 1.5f);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, Mathf.Lerp(boy, hizliBoy, Mathf.Clamp01(v.magnitude / 12f)), dt);

        float yarim = cam.orthographicSize * cam.aspect;
        Vector3 h = new Vector3(hedef.position.x + xOfset + bakis, hedef.position.y + yOfset, -10f);
        h.x = Mathf.Clamp(h.x, minX + yarim, Mathf.Max(minX + yarim, maxX - yarim));
        Vector3 p = Vector3.SmoothDamp(transform.position, h, ref hiz, 0.18f, Mathf.Infinity, dt);
        if (sarsinti > 0f && dt > 0f)
        {
            p += (Vector3)(Random.insideUnitCircle * sarsinti * 0.4f);
            sarsinti = Mathf.Max(0f, sarsinti - dt * 2f);
        }
        p.z = -10f;
        transform.position = p;
    }
}
