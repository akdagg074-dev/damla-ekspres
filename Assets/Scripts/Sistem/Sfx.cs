using System.Collections.Generic;
using UnityEngine;

// Sesler kodla uretilir (dosya gerekmez). Sfx.Cal("anahtar").
public static class Sfx
{
    const int SR = 22050;
    static AudioSource[] havuz;
    static int sira;
    static AudioSource muzik, motor;
    static readonly Dictionary<string, AudioClip> kliplar = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> son = new Dictionary<string, float>();
    static readonly System.Random rnd = new System.Random(3);

    // Ses nesnesi ilk kullanimda kurulur ve sahne gecislerinde yasamaya devam eder
    static void Hazirla()
    {
        if (havuz != null) return;
        var go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        Kur(go);
    }

    static void Kur(GameObject host)
    {
        havuz = new AudioSource[12];
        for (int i = 0; i < havuz.Length; i++) { havuz[i] = host.AddComponent<AudioSource>(); havuz[i].playOnAwake = false; }
        muzik = host.AddComponent<AudioSource>();
        muzik.loop = true; muzik.volume = 0.2f; muzik.playOnAwake = false;
        motor = host.AddComponent<AudioSource>();
        motor.loop = true; motor.volume = 0f; motor.playOnAwake = false;

        Yap("yukle", 0.08f, t => Sin(t, Mathf.Lerp(600f, 900f, t / 0.08f)) * Env(t, 0.002f, 0.08f) * 0.35f);
        Yap("teslim", 0.25f, t => (Sin(t, t < 0.08f ? 988f : 1319f) * 0.5f + Sq(t, t < 0.08f ? 988f : 1319f) * 0.12f) * Env(t, 0.002f, 0.25f) * 0.5f);
        Yap("firlat", 0.12f, t => Gurultu() * Mathf.Sin(t / 0.12f * Mathf.PI) * 0.25f, true);
        Yap("istek", 0.18f, t => Sin(t, t < 0.09f ? 660f : 880f) * Env(t, 0.003f, 0.18f) * 0.35f);
        Yap("hata", 0.45f, t => (Saw(t, Mathf.Lerp(300f, 110f, t / 0.45f)) * 0.5f) * Env(t, 0.005f, 0.45f) * 0.55f);
        Yap("kasis", 0.25f, t => (Sin(t, Mathf.Lerp(120f, 50f, t / 0.25f)) * 0.8f + Gurultu() * 0.3f) * Env(t, 0.002f, 0.25f) * 0.8f, true);
        Yap("cam", 0.5f, t => (Gurultu() * 0.5f * Env(t, 0.001f, 0.15f) + (Sin(t, 3100f) + Sin(t, 4200f) + Sin(t, 5300f)) * 0.12f * Env(t, 0.001f, 0.5f)) * 0.7f);
        Yap("korna", 0.45f, t => (Sq(t, 392f) + Sq(t, 494f)) * 0.18f * (t < 0.42f ? 1f : 0f) * Mathf.Clamp01(t / 0.01f));
        Yap("tik", 0.04f, t => Sin(t, 1200f) * Env(t, 0.001f, 0.04f) * 0.3f);
        Yap("basari", 0.5f, t =>
        {
            float[] n = { 523f, 659f, 784f, 1046f };
            int i = Mathf.Min(3, (int)(t / 0.1f));
            return (Sq(t, n[i]) * 0.2f + Sin(t, n[i] * 2f) * 0.2f) * Env(t - i * 0.1f, 0.003f, i == 3 ? 0.2f : 0.1f) * 0.6f;
        });
        Yap("bitti", 1.0f, t =>
        {
            float[] n = { 392f, 330f, 262f, 196f };
            int i = Mathf.Min(3, (int)(t / 0.2f));
            return (Saw(t, n[i]) * 0.25f + Sin(t, n[i]) * 0.3f) * Env(t - i * 0.2f, 0.004f, i == 3 ? 0.4f : 0.2f) * 0.6f;
        });

        Yap("su", 0.4f, t => Gurultu() * Env(t, 0.01f, 0.4f) * 0.55f + Sin(t, Mathf.Lerp(900f, 300f, t / 0.4f)) * Env(t, 0.005f, 0.12f) * 0.15f, true);
        Yap("damla", 0.16f, t => Sin(t, Mathf.Lerp(500f, 1500f, t / 0.16f)) * Env(t, 0.003f, 0.16f) * 0.45f);
        Yap("yildiz", 0.45f, t => (Sin(t, 1568f) * 0.4f + Sin(t, 2093f) * 0.25f) * Env(t, 0.002f, 0.45f) * 0.5f);

        // arayuz tiklamasi: Kenney UI Pack sesi (yoksa uretilen "tik" kalir)
        var klik = Resources.Load<AudioClip>("UI/Ses/click-a");
        if (klik != null) kliplar["tik"] = klik;

        // motor dongusu
        var md = new float[SR];
        for (int i = 0; i < md.Length; i++)
        {
            float t = i / (float)SR;
            md[i] = (Saw(t, 55f) * 0.5f + Sq(t, 27.5f) * 0.25f + Sin(t, 110f) * 0.2f) * (0.8f + 0.2f * Sin(t, 9f));
        }
        motor.clip = AudioClip.Create("motor", md.Length, 1, SR, false);
        motor.clip.SetData(md, 0);
        muzik.clip = Muzik();
    }

    static float lp;
    static float Gurultu() { return (float)rnd.NextDouble() * 2f - 1f; }
    static float Sin(float t, float f) { return Mathf.Sin(t * f * 2f * Mathf.PI); }
    static float Sq(float t, float f) { return Mathf.Repeat(t * f, 1f) < 0.5f ? 1f : -1f; }
    static float Saw(float t, float f) { return Mathf.Repeat(t * f, 1f) * 2f - 1f; }
    static float Tri(float t, float f) { float p = Mathf.Repeat(t * f, 1f); return p < 0.5f ? p * 4f - 1f : 3f - p * 4f; }
    static float Env(float t, float a, float d)
    {
        if (t < 0f) return 0f;
        if (t < a) return t / a;
        float k = Mathf.Clamp01(1f - (t - a) / Mathf.Max(0.0001f, d - a));
        return k * k;
    }

    static void Yap(string ad, float sure, System.Func<float, float> f, bool alcak = false)
    {
        int n = Mathf.CeilToInt(sure * SR);
        var d = new float[n];
        lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float v = f(i / (float)SR);
            if (alcak) { lp += (v - lp) * 0.3f; v = lp; }
            d[i] = Mathf.Clamp(v, -1f, 1f);
        }
        var c = AudioClip.Create(ad, n, 1, SR, false);
        c.SetData(d, 0);
        kliplar[ad] = c;
    }

    // Neseli kucuk dongu: C - Am - F - G
    static AudioClip Muzik()
    {
        float sekizlik = 60f / 128f / 2f;
        int adim = 32;
        int n = Mathf.CeilToInt(adim * sekizlik * SR);
        var d = new float[n];
        float[] kok = { 130.81f, 110f, 87.31f, 98f };
        float[][] akor =
        {
            new[] { 261.63f, 329.63f, 392f, 523.25f },
            new[] { 220f, 261.63f, 329.63f, 440f },
            new[] { 174.61f, 220f, 261.63f, 349.23f },
            new[] { 196f, 246.94f, 293.66f, 392f },
        };
        int[] melodi = { 2, -1, 1, 2, 3, -1, 2, 0 };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            int s = (int)(t / sekizlik);
            float st = t - s * sekizlik;
            int bar = (s / 8) % 4;
            float v = 0f;
            float bf = (s % 2 == 0) ? kok[bar] : kok[bar] * 2f;
            v += Tri(t, bf) * Env(st, 0.004f, sekizlik * 0.9f) * 0.35f;
            float on6 = sekizlik * 0.5f;
            int s16 = (int)(t / on6);
            v += Tri(t, akor[bar][s16 % 4] * 2f) * Env(t - s16 * on6, 0.002f, on6 * 0.9f) * 0.07f;
            int m = melodi[s % 8];
            if (m >= 0) v += (Sq(t, akor[bar][m] * 2f) * 0.35f + Sin(t, akor[bar][m] * 2f) * 0.5f) * Env(st, 0.005f, sekizlik * 0.9f) * 0.07f;
            if (s % 4 == 0) v += Sin(st, Mathf.Lerp(130f, 50f, Mathf.Clamp01(st / 0.1f))) * Env(st, 0.002f, 0.14f) * 0.45f;
            if (s % 2 == 1) v += Gurultu() * Env(st, 0.001f, 0.03f) * 0.06f;
            d[i] = Mathf.Clamp(v, -1f, 1f) * 0.8f;
        }
        var c = AudioClip.Create("muzik", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    public static void Cal(string ad, float ses = 1f, float perde = 0f, float aralik = 0.04f)
    {
        Hazirla();
        AudioClip c;
        if (!kliplar.TryGetValue(ad, out c)) return;
        float now = Time.unscaledTime, l;
        if (son.TryGetValue(ad, out l) && now - l < aralik) return;
        son[ad] = now;
        var src = havuz[sira];
        sira = (sira + 1) % havuz.Length;
        src.clip = c;
        src.volume = ses;
        src.pitch = 1f + perde + Random.Range(-0.04f, 0.04f);
        src.Play();
    }

    public static void MuzikCal(bool acik)
    {
        Hazirla();
        if (acik) { if (!muzik.isPlaying) muzik.Play(); }
        else muzik.Stop();
    }

    // hiz: 0..1
    public static void Motor(bool acik, float hiz)
    {
        Hazirla();
        if (acik)
        {
            if (!motor.isPlaying) motor.Play();
            motor.pitch = 0.8f + hiz * 1.1f;
            motor.volume = 0.06f + hiz * 0.08f;
        }
        else if (motor.isPlaying) motor.Stop();
    }
}
