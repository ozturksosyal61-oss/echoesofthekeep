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
Biten: lobi+ses, hareket, 1. şahıs kamera+nişangâh+ışınla etkileşim, iki oda, ağ etkileşimi, URP, P3 bulmaca altyapısı (Assets/Scripts/Puzzle). Test sahnesinde Bulmaca 1 (4 kol → sembol sırası → B'de kasa kapısı), telefon+PC test edildi.
Sıradakiler: perf sonucuna göre URP ayarı ve FrameRateConfig.Target=60 (şu an 240), bulmaca 2-5 + final, çan kulesi odasının görsel geçişi, Mixamo karakter (Humanoid, animasyon hızdan türetilir), CC0 şato kitleri (Quaternius, Kenney).
Not: 3. şahıs/uzak oyuncu gövdesi hareket yönüne dönüyor, bakış yönüne değil; final salonunda düzelt.
Tasarım belgesi: Docs/PuzzleDesign.md
