# CoopGame
Unity 6000.6.7f1, URP 17.6, Photon Fusion 2.1.4 (Host) + Voice. Test cihazı: Android CPH2113 (eski/düşük seviye, "en kötü durum" cihazı). Hedef 60 FPS orta seviye cihazda.
Konsept: We Were Here tarzı 2 kişilik co-op bulmaca, birinci şahıs (3. şahıs kamera düğmesi test için kalacak). Şato: X çan kulesi (dişliler, kollar), O zindan/sarnıç (semboller, su). Oyun Lobi Holü'nde başlar; X yukarı, O aşağı gider; bulmacalar kolaydan zora. Final büyük salonda buluşma, kaçış 2. oyunun konusu. Görsel kalite öncelikli.
## Kurallar
- Bana Türkçe yaz. Raporlar çok kısa (en fazla ~10 satır). Gereksiz soru sorma.
- Push sadece ben söyleyince. Commit anlamlı küçük parçalar halinde.
- Token'a cimri ol: uzun açıklama, tekrar ve dosya dökümü yok.
- Unity editörü açık olabilir; batchmode gerekiyorsa önce bana söyle.
- Setup All (CoopGame menüsü) sahneyi baştan üretir; kalıcı ayarlar CoopSetup.cs'te tutulur.
## Durum
Biten: lobi+ses, hareket, 1. şahıs kamera+nişangâh+ışınla etkileşim, ağ etkileşimi, URP, P3 bulmaca altyapısı (Assets/Scripts/Puzzle), Bulmaca 1 (4 kol → sembol sırası → B'de kasa kapısı; telefon+PC test edildi). Görsel geçiş v1: temalı menü + HUD, A çan kulesi, B zindan (PC ekran görüntüsüyle doğrulandı, cihazda denenmedi).
Bulmaca 2 (kibrit mahzeni) test edildi. Bulmaca 3 (sarnıç) kodlandı, test edilmedi. İlerleme: her bulmaca yeni oda (X: kule → Saat Odası → Hazne Odası; O: zindan → kasa → mahzen → sarnıç).
Akış (Faz 3, cihazda/iki oyuncuyla DENENMEDİ): menü Oda Kur / Odaya Katıl → herkes Lobi Holü'nde doğar (Layout.Hall, x=-250; CoopSetup.Hall.cs) → biri X (merdiven sahanlığı), diğeri O (doğu nişi) plakasında 1.5 sn durunca HallGate rolleri atar ve ışınlar: X → çan kulesi, O → zindan. Rol artık giriş sırası değil: PlayerAvatar.RoleIndex (Roles.X/O/None), giriş sırası = Slot. Eski odalar (kule, zindan, mahzen, sarnıç) henüz kitle yenilenmedi; bake dışı (ExcludeFromBake).
Sıradakiler: görsel geçişin cihaz testi + FPS (gerekirse CoopSetup.Art.cs PostFx=false), FrameRateConfig.Target=60 (şu an 240), bulmaca 2-5 + final, sarnıç suyu, Mixamo karakter (Humanoid, animasyon hızdan türetilir).
Not: 3. şahıs/uzak oyuncu gövdesi hareket yönüne dönüyor, bakış yönüne değil; final salonunda düzelt.
Tasarım belgesi: Docs/PuzzleDesign.md
## Görsel altyapı
- CoopSetup üç dosya (partial): CoopSetup.cs (prefab/URP/build), CoopSetup.Art.cs (malzeme, ağ, odalar, menü arka planı), CoopSetup.UI.cs (tema, Lobby/Game sahneleri, HUD). Oyun adı: Echoes of the Keep (CoopSetup.UI.cs GameTitle/GameTitleSub, PlayerSettings.productName).
- Varlıklar: Assets/ThirdParty (Poly Haven CC0 doku/model/gökyüzü, OFL fontlar; LICENSES.md). Üretilenler: Assets/Generated (ağ, sprite), Assets/Materials.
- Görsel doğrulama: Unity kapalıyken batch `-executeMethod CoopGame.EditorTools.CoopSetup.SetupAndShots -shotDir <klasör>` (ya da yalnızca Shots) PNG üretir.
- Art kit (CoopSetup.Kit.cs → Assets/Art/Kit): malzemeler `K.*`, palet `Pal.*`, dekor `Kit("Ad", parent, pos, yaw)` (74 prefab; liste Assets/Art/Kit/Prefabs), ışık prefabları L_Candle/L_Candelabra/L_Chandelier (baked), L_Lantern (realtime), pencereler WindowLancet/WindowRose (ışıyan vitray + baked ay spotu, duvarda delik gerekmez). Mimari: Wainscot (lambri), BeamCeiling, KitProbes, KitReflection, KitBake. Post-fx: Art/Kit/Lighting/KitPostFx.asset.
- Kit doğrulama: batch `-executeMethod CoopGame.EditorTools.CoopSetup.KitShots -shotDir <klasör>` (sergi odası Assets/Art/Kit/KitShowroom.unity, bake ~20 sn). Model dökümü: Logs/kit_dump.txt.
- Baked ışık: duvar/zemin lightmap, dekor light probe. Probe ızgarası odayı zeminden tavana kaplamalı; gövde dışında kalan nesne simsiyah olur (prefablardaki ProbeAnchor bunu zemin/duvar dekoru için çözer).
- Nesne başına 4 ek ışık sınırı var: büyük yüzeyler Deck() ile parçalı; oda başına ~4-5 noktasal ışığı geçme.
## Art direction (kalıcı)
- Referanslar: Docs/Reference/ → lobi_hol.png, mekanizma.jpg, mahzen.png, genel_ic.jpg. Oda tasarlamadan önce hepsini incele. Her yeni oda bu detay seviyesinde, aynı şato stilinde.
- Yeni oda ASLA boş taş kutu + bulmaca değil; komple yaşanmış mekan: masa, üstünde kağıt/mektup/mum, raf, şişe, sandık, tablo, alet, örümcek ağı. Oda başına min 8-10 dekor objesi, environmental storytelling.
- Şato iç: ahşap lambri, sıva, kiriş, parke, kumaş. Yer altı (mahzen/sarnıç): taş.
- Sadece Docs/StyleGuide.md paleti ve Art kit (Assets/Art/Kit) kullanılır. Pembe/neon/parlak renk yasak.
- Referans görseli olmayan yeni oda için önce kullanıcıya Gemini prompt'u yaz (sonuna "no text"), görsel gelince tasarla.
- Asset: Poly Haven/ambientCG (CC0) kendin indir → Assets/ThirdParty/<kaynak>/; diğerleri "manuel indir" listesi (link + hedef klasör). Lisanslar Docs/CREDITS.md. Plan: Docs/AssetPlan.md.
- Mobil bütçe: URP mobile, baked light + light probes, oda başına max 2 realtime light, gerçek zamanlı yansıma yok (reflection probe ile taklit), LOD, occlusion culling, ASTC. Hedef orta Android 60 fps (min 30).
- Roller: X (yukarı/mekanizma) ve O (aşağı/mahzen); eski adları A/B.
