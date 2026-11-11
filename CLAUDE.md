# CoopGame
Unity 6000.6.7f1, URP 17.6, Photon Fusion 2.1.4 (Host) + Voice. Test cihazı: Android CPH2113 (eski/düşük seviye, "en kötü durum" cihazı). Hedef 60 FPS orta seviye cihazda.
Konsept: We Were Here tarzı 2 kişilik co-op bulmaca, birinci şahıs (3. şahıs kamera düğmesi test için kalacak). Şato: A çan kulesi (dişliler, kollar), B zindan/sarnıç (semboller, su). A en tepede, B en dipte başlar; bulmacalar kolaydan zora. Final büyük salonda buluşma, kaçış 2. oyunun konusu. Görsel kalite öncelikli.
## Kurallar
- Bana Türkçe yaz. Raporlar çok kısa (en fazla ~10 satır). Gereksiz soru sorma.
- Push sadece ben söyleyince. Commit anlamlı küçük parçalar halinde.
- Token'a cimri ol: uzun açıklama, tekrar ve dosya dökümü yok.
- Unity editörü açık olabilir; batchmode gerekiyorsa önce bana söyle.
- Setup All (CoopGame menüsü) sahneyi baştan üretir; kalıcı ayarlar CoopSetup.cs'te tutulur.
## Durum
Biten: lobi+ses, hareket, 1. şahıs kamera+nişangâh+ışınla etkileşim, ağ etkileşimi, URP, P3 bulmaca altyapısı (Assets/Scripts/Puzzle), Bulmaca 1 (4 kol → sembol sırası → B'de kasa kapısı; telefon+PC test edildi). Görsel geçiş v1: temalı menü + HUD, A çan kulesi, B zindan (PC ekran görüntüsüyle doğrulandı, cihazda denenmedi).
Bulmaca 2 (kibrit mahzeni) test edildi. Bulmaca 3 (sarnıç) kodlandı, test edilmedi. İlerleme: her bulmaca yeni oda (A: kule → Saat Odası → Hazne Odası; B: zindan → kasa → mahzen → sarnıç).
Sıradakiler: görsel geçişin cihaz testi + FPS (gerekirse CoopSetup.Art.cs PostFx=false), FrameRateConfig.Target=60 (şu an 240), bulmaca 2-5 + final, sarnıç suyu, Mixamo karakter (Humanoid, animasyon hızdan türetilir).
Not: 3. şahıs/uzak oyuncu gövdesi hareket yönüne dönüyor, bakış yönüne değil; final salonunda düzelt.
Tasarım belgesi: Docs/PuzzleDesign.md
## Görsel altyapı
- CoopSetup üç dosya (partial): CoopSetup.cs (prefab/URP/build), CoopSetup.Art.cs (malzeme, ağ, odalar, menü arka planı), CoopSetup.UI.cs (tema, Lobby/Game sahneleri, HUD). Oyun adı: Echoes of the Keep (CoopSetup.UI.cs GameTitle/GameTitleSub, PlayerSettings.productName).
- Varlıklar: Assets/ThirdParty (Poly Haven CC0 doku/model/gökyüzü, OFL fontlar; LICENSES.md). Üretilenler: Assets/Generated (ağ, sprite), Assets/Materials.
- Görsel doğrulama: Unity kapalıyken batch `-executeMethod CoopGame.EditorTools.CoopSetup.SetupAndShots -shotDir <klasör>` (ya da yalnızca Shots) PNG üretir.
- Nesne başına 4 ek ışık sınırı var: büyük yüzeyler Deck() ile parçalı; oda başına ~4-5 noktasal ışığı geçme.
