using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sahneler arasi yumusak gecis: lacivert perde + ziplayan damla. Ornek: SahneGecis.Yukle(SahneGecis.BolumSecim)
public class SahneGecis : MonoBehaviour
{
    public const string AnaMenu = "AnaMenu";
    public const string BolumSecim = "BolumSecim";
    public static string Bolum(int no) { return "Bolum" + no; }

    static SahneGecis I;
    float perde;
    bool yukleniyor;

    public static void Yukle(string sahne)
    {
        if (I == null)
        {
            var go = new GameObject("SahneGecis");
            DontDestroyOnLoad(go);
            I = go.AddComponent<SahneGecis>();
        }
        if (I.yukleniyor) return;
        I.StartCoroutine(I.Gecis(sahne));
    }

    IEnumerator Gecis(string sahne)
    {
        yukleniyor = true;
        while (perde < 1f) { perde = Mathf.Min(1f, perde + Time.unscaledDeltaTime * 3.5f); yield return null; }
        Time.timeScale = 1f;
        Tus.PedalGaz = Tus.PedalFren = false;
        Sfx.Motor(false, 0f);
        var op = SceneManager.LoadSceneAsync(sahne);
        while (!op.isDone) yield return null;
        yield return null;
        while (perde > 0f) { perde = Mathf.Max(0f, perde - Time.unscaledDeltaTime * 2.5f); yield return null; }
        yukleniyor = false;
    }

    void OnGUI()
    {
        if (perde <= 0f) return;
        GUI.depth = -1000;
        Arayuz.Basla();
        float vw = Arayuz.VW, vh = Arayuz.VH;
        Color c = Arayuz.Lacivert;
        c.a = perde;
        Arayuz.Kutu(new Rect(0, 0, vw, vh), c);
        float z = Time.unscaledTime;
        Arayuz.Damla(new Rect(vw * 0.5f - 40, vh * 0.5f - 110 - Mathf.Abs(Mathf.Sin(z * 5f)) * 30f, 80, 100), new Color(0.4f, 0.8f, 1f, perde));
        Arayuz.Yaz(new Rect(vw * 0.5f - 300, vh * 0.5f + 20, 600, 60), "Yükleniyor...", Arayuz.Orta, new Color(1, 1, 1, perde));
    }
}
