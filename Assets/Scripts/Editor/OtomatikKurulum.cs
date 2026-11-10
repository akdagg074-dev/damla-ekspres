using System.IO;
using UnityEditor;

// Proje kokunde "sahne_kur.flag" dosyasi varsa, editor acilinca sahneyi bir kez otomatik kurar.
[InitializeOnLoad]
public static class OtomatikKurulum
{
    const string Isaret = "sahne_kur.flag";

    static OtomatikKurulum()
    {
        if (!File.Exists(Isaret)) return;
        EditorApplication.delayCall += Calistir;
    }

    static void Calistir()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Calistir;
            return;
        }
        if (!File.Exists(Isaret)) return;
        File.Delete(Isaret);
        SahneKurucu.Kur();
    }
}
