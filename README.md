# Damla Ekspres

**Oyunlaştırma dersi vize projesi** · Gökmen Akdağ · 2506127005

Damla Su Dağıtım A.Ş.'nin su tankerini depodan müşteriye sür, tanktaki suyu **dökmeden** teslim et.
Unity 6 (6000.3.22f1) ile geliştirilmiş 2D, fizik tabanlı bir sürüş oyunu.

- **Tarayıcıda oyna (WebGL):** https://akdagg074-dev.github.io/damla-ekspres/
- **Oyun Tasarım Dokümanı:** [Docs/DamlaEkspres_GDD.pdf](Docs/DamlaEkspres_GDD.pdf)
- **Windows sürümü:** `DamlaEkspres.exe` (USB teslimindeki `Build_Windows` klasörü)

## Kontroller

| Tuş | İşlev |
|---|---|
| D / Sağ ok | Gaz (havadayken burnu kaldırır) |
| A / Sol ok | Fren, durunca geri vites (havadayken burnu indirir) |
| Esc / P | Duraklat |
| R | Bölümü yeniden başlat |
| H | Korna |
| M | Sesi aç/kapat |
| F9 | (Bölüm seçim ekranında) tüm bölümlerin kilidini aç — değerlendirme için |

Ekrandaki FREN / GAZ pedalları fare ve dokunmatikle de çalışır.

## Öne çıkanlar

- **WheelJoint2D araç:** iki teker, yaylı süspansiyon ve motor; hedef hızı sınırlı ivmeyle izleyen motor kontrolü.
- **Su fiziği:** tanktaki su yüzeyi etkin yerçekimine (g − a) dik olmak ister, sönümlü yay gibi çalkalanır.
  Suyun kütlesi ve ağırlık merkezi her fizik adımında kasanın `Rigidbody2D`'sine yazılır. Çalkalanma, aşırı yatma,
  sert iniş ve hızlı kasis geçişi suyu döker.
- **5 bölüm, kademeli zorluk:** Mahalle Yokuşu → Bağ Yolu → Çam Ormanı → Kanyon Köprüsü (HingeJoint2D asma köprü) → Zirve Köyü.
  Yol uzar, en dik eğim 20°'den 44°'ye çıkar, su hedefi %50'den %60'a yükselir.
- **Oyunlaştırma:** puan, damla jetonları, ekspres süre bonusu, uçuş/takla akrobasisi, müşteri memnuniyeti,
  3 yıldız, 9 rozet, sürücü ünvanı, bölüm kilitleri, kalıcı kayıt.

## Proje yapısı

```
Assets/
  Scripts/Arac      AracKontrol (WheelJoint2D sürüş), SuTanki (su fiziği)
  Scripts/Bolum     Bolumler (5 bölüm verisi + arazi üretimi), Kasis, Kopru, DamlaJeton
  Scripts/Oyun      BolumYonetici (akış, HUD, duraklatma, sonuç), Kayit (kayıt + rozetler)
  Scripts/Arayuz    Arayuz (tema), AnaMenu, BolumSecim
  Scripts/Sistem    Sfx, Tus, Efekt, Kamera, Parallax, SahneGecis, Ayarlar
  Editor            SahneKurucu (tüm sahneleri kurar), ModelRender (3D → 2D sprite)
  Scenes            AnaMenu, BolumSecim, Bolum1–5
  Prefabs           SuTankeri
  Kenney            Kenney CC0 paketleri (modeller, sprite'lar) + lisanslar
  Resources/UI      Kenney UI Pack parçaları, Paytone One fontu
```

Sahneler `Damla Ekspres > Tum Sahneleri Kur` menüsüyle yeniden üretilebilir.

## Kaynaklar ve lisanslar

- Görseller ve 3D modeller: [Kenney](https://kenney.nl) — Car Kit, City Kit Commercial, City Kit Suburban,
  Background Elements Remastered, New Platformer Pack, UI Pack (CC0).
- Font: Paytone One (Google Fonts, SIL Open Font License 1.1).
- Ses efektleri ve müzik kodla üretilir (`Sfx.cs`); arayüz tık sesi Kenney UI Pack'ten.

Lisans dosyaları: `Assets/Kenney/Lisanslar/`.
