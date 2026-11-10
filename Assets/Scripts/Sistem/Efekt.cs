using UnityEngine;

// Ucan yazilar ve su damlasi efektleri
public static class Efekt
{
    static Font font;
    static Sprite damla;

    public static void Yazi(Vector3 pos, string text, Color c, float boy = 1f)
    {
        if (font == null) font = Resources.Load<Font>("UI/Font/PaytoneOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("yazi");
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.06f * boy;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Normal;
        tm.color = c;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 100;
        var gol = new GameObject("golge");
        gol.transform.SetParent(go.transform, false);
        gol.transform.localPosition = new Vector3(0.04f, -0.04f, 0);
        var tm2 = gol.AddComponent<TextMesh>();
        tm2.font = font; tm2.text = text; tm2.fontSize = 64; tm2.characterSize = tm.characterSize;
        tm2.anchor = TextAnchor.MiddleCenter; tm2.alignment = TextAlignment.Center; tm2.fontStyle = FontStyle.Normal;
        tm2.color = new Color(0, 0, 0, 0.6f);
        var mr2 = gol.GetComponent<MeshRenderer>();
        mr2.sharedMaterial = font.material;
        mr2.sortingOrder = 99;
        go.AddComponent<UcanYazi>();
    }

    // Tanktan sicrayan / dokulen su damlasi
    public static void Damla(Vector3 pos, Vector2 hiz, float boy = 0.14f)
    {
        if (damla == null)
        {
            var t = Arayuz.DamlaDoku();
            damla = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.35f), t.height);
        }
        var go = new GameObject("damla");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * boy * Random.Range(0.7f, 1.3f);
        var s = go.AddComponent<SpriteRenderer>();
        s.sprite = damla;
        s.color = Random.value < 0.3f ? new Color(0.8f, 0.95f, 1f, 0.95f) : new Color(0.35f, 0.7f, 1f, 0.9f);
        s.sortingOrder = 17;
        go.AddComponent<SuDamlasi>().v = hiz;
    }

    // Jeton toplaninca etrafa sacilan parlak damlalar
    public static void Pirilti(Vector3 pos)
    {
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            Damla(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.5f, 4f) + Vector2.up * 2f, 0.12f);
        }
    }
}

public class UcanYazi : MonoBehaviour
{
    float t;
    TextMesh[] tms;
    Color[] renk;

    void Start()
    {
        tms = GetComponentsInChildren<TextMesh>();
        renk = new Color[tms.Length];
        for (int i = 0; i < tms.Length; i++) renk[i] = tms[i].color;
    }

    void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * Time.deltaTime * 1.2f;
        float pop = 1f + Mathf.Max(0f, 0.25f - t) * 2f;
        transform.localScale = Vector3.one * pop;
        float a = Mathf.Clamp01((1.4f - t) / 0.4f);
        for (int i = 0; i < tms.Length; i++) tms[i].color = new Color(renk[i].r, renk[i].g, renk[i].b, renk[i].a * a);
        if (t > 1.4f) Destroy(gameObject);
    }
}

// Yercekimiyle dusen, hareket yonune donen ve solan su damlasi
public class SuDamlasi : MonoBehaviour
{
    public Vector2 v;
    float t, omur;
    SpriteRenderer s;
    Color c;

    void Start() { s = GetComponent<SpriteRenderer>(); c = s.color; omur = Random.Range(0.5f, 0.9f); }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        v += Physics2D.gravity * dt;
        transform.position += (Vector3)(v * dt);
        if (v.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-v.y, -v.x) * Mathf.Rad2Deg - 90f);
        s.color = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01((omur - t) / 0.25f));
        if (t > omur) Destroy(gameObject);
    }
}
