using System.IO;
using UnityEngine;

// Kenney 3D modellerini (FBX) ortografik kamerayla render edip seffaf arka planli, koyu dis hatli
// 2D sprite'a (PNG) cevirir. Sahne kurucu, tanker ve binalarin gorsellerini boyle uretir.
public static class ModelRender
{
    public const string Klasor = "Assets/Sprites/Render/";
    public const float PikselBirim = 256f;   // render cozunurlugu (1 model birimi = 256 px); import PPU ile ayni
    const int Katman = 31;

    public class Sonuc
    {
        public string yol;
        public Vector3 merkez;      // goruntunun merkezi (dunya)
        public Vector3 sag, yukari; // kamera eksenleri
        public Vector2 boyut;       // model birimi
        public Vector3 kok;         // render sirasinda modelin kok konumu (dunya)

        // Modeldeki bir dunya noktasinin sprite merkezine gore konumu (model birimi)
        public Vector2 Nokta(Vector3 dunya)
        {
            Vector3 d = dunya - merkez;
            return new Vector2(Vector3.Dot(d, sag), Vector3.Dot(d, yukari));
        }
    }

    // bakis: kameranin yonu (dunya). gizle: true donen cocuk nesneler render edilmez.
    public static Sonuc Render(GameObject model, string ad, Quaternion bakis, System.Func<Transform, bool> gizle = null,
                               float dishat = 5f, Color? hatRengi = null, float isikYaw = -35f)
    {
        Directory.CreateDirectory(Klasor);
        var kok = Object.Instantiate(model);
        kok.transform.position = new Vector3(0f, -500f, 0f);
        foreach (var t in kok.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = Katman;
            if (gizle != null && t != kok.transform && gizle(t)) t.gameObject.SetActive(false);
        }

        // kamera eksenlerinde sinirlar
        Vector3 sag = bakis * Vector3.right, yukari = bakis * Vector3.up, ileri = bakis * Vector3.forward;
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        Vector3 ref0 = kok.transform.position;
        foreach (var r in kok.GetComponentsInChildren<Renderer>())
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 k = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float x = Vector3.Dot(k - ref0, sag), y = Vector3.Dot(k - ref0, yukari);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        }
        int pay = Mathf.CeilToInt(dishat) + 3;
        float payBirim = pay / PikselBirim;
        minX -= payBirim; maxX += payBirim; minY -= payBirim; maxY += payBirim;
        int w = Mathf.CeilToInt((maxX - minX) * PikselBirim), h = Mathf.CeilToInt((maxY - minY) * PikselBirim);
        Vector3 merkez = ref0 + sag * (minX + maxX) * 0.5f + yukari * (minY + maxY) * 0.5f;

        // kamera ve isik
        var camGo = new GameObject("RenderKamera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = (maxY - minY) * 0.5f;
        cam.aspect = (float)w / h;
        cam.transform.rotation = bakis;
        cam.transform.position = merkez - ileri * 50f;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = 1 << Katman;
        var isikGo = new GameObject("RenderIsik");
        var isik = isikGo.AddComponent<Light>();
        isik.type = LightType.Directional;
        isik.intensity = 0.75f;
        isik.shadows = LightShadows.None;
        isik.cullingMask = 1 << Katman;
        isik.transform.rotation = bakis * Quaternion.Euler(30f, isikYaw, 0f);
        var eskiMod = RenderSettings.ambientMode; var eskiRenk = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.66f);

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt;
        cam.Render();
        var onceki = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = onceki;

        RenderSettings.ambientMode = eskiMod; RenderSettings.ambientLight = eskiRenk;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(isikGo);
        Object.DestroyImmediate(kok);

        DisHat(tex, dishat, hatRengi ?? new Color(0.13f, 0.1f, 0.17f));
        string yol = Klasor + ad + ".png";
        File.WriteAllBytes(yol, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        return new Sonuc { yol = yol, merkez = merkez, sag = sag, yukari = yukari, boyut = new Vector2(maxX - minX, maxY - minY), kok = ref0 };
    }

    // Kenney 2D stiline uyan kalin koyu dis hat (render, siyah zemine "premultiplied" gelir)
    static void DisHat(Texture2D tex, float r, Color hat)
    {
        int w = tex.width, h = tex.height;
        var px = tex.GetPixels();
        var a = new float[w * h];
        for (int i = 0; i < px.Length; i++) a[i] = px[i].a;
        int ri = Mathf.CeilToInt(r);
        var cikti = new Color[px.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // hat kapsami: yaricap icindeki en yakin dolu piksele uzaklik
                float kapsam = 0f;
                for (int dy = -ri; dy <= ri && kapsam < 1f; dy++)
                {
                    int yy = y + dy;
                    if (yy < 0 || yy >= h) continue;
                    for (int dx = -ri; dx <= ri; dx++)
                    {
                        int xx = x + dx;
                        if (xx < 0 || xx >= w) continue;
                        float al = a[yy * w + xx];
                        if (al < 0.4f) continue;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        kapsam = Mathf.Max(kapsam, Mathf.Clamp01(r + 0.5f - d));
                        if (kapsam >= 1f) break;
                    }
                }
                Color s = px[y * w + x];             // premultiplied
                float sa = s.a;
                float oa = kapsam * (1f - sa);
                float fa = sa + oa;
                Color c = new Color(s.r + hat.r * oa, s.g + hat.g * oa, s.b + hat.b * oa, fa);
                if (fa > 0.0001f) { c.r /= fa; c.g /= fa; c.b /= fa; }
                cikti[y * w + x] = c;
            }
        tex.SetPixels(cikti);
        tex.Apply();
    }
}
