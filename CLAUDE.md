# CoopGame
Unity 6000.6.7f1, URP 17.6, Photon Fusion 2.1.4 (Host) + Voice. Test cihazı: Android CPH2113 (eski/düşük seviye, "en kötü durum" cihazı). Hedef 60 FPS orta seviye cihazda.
Konsept: We Were Here tarzı 2 kişilik co-op bulmaca, birinci şahıs (3. şahıs kamera düğmesi test için kalacak). Şato: X çan kulesi (dişliler, kollar), O zindan/sarnıç (semboller, su). Oyun Lobi Holü'nde başlar; X yukarı, O aşağı gider; bulmacalar kolaydan zora. Final büyük salonda buluşma, kaçış 2. oyunun konusu. Görsel kalite öncelikli.
## EN ÖNEMLİ: Oda kalitesi (her oda, her rol)
- Her oda dekoratif olarak BOL ve görünüş olarak GÜZEL olmalı: geniş + yüksek tavan (lobi ölçeği), odalar birbirinden TAMAMEN farklı (plan, tavan, malzeme, hero prop).
- Oda başına 110-140 dekor: mobilya, aksesuar, masa üstü (kitap, not, mum, şişe), duvar (tablo, sancak, aplik), zemin (halı, sandık, kâğıt), bol ışık kaynağı (mum/fener/avize; performans sınırı içinde).
- Kitte model yoksa: Poly Haven'dan indir; bulunamazsa kullanıcıya link + hedef klasör ver, o indirir.
- O rolünün tüm odaları (zindan, kasa, mahzen, sarnıç, kütüphane, katakomb, kuyu) bu standarda getirilecek (sıradaki büyük iş).
## Kurallar
- Bana Türkçe yaz. Raporlar çok kısa (en fazla ~10 satır). Gereksiz soru sorma.
- Push sadece ben söyleyince. Commit anlamlı küçük parçalar halinde.
- Token'a cimri ol: uzun açıklama, tekrar ve dosya dökümü yok.
- Unity editörü açık olabilir; batchmode gerekiyorsa önce bana söyle.
- Setup All (CoopGame menüsü) sahneyi baştan üretir; kalıcı ayarlar CoopSetup.cs'te tutulur.
## Durum
Biten: lobi+ses, hareket, 1. şahıs kamera+nişangâh+ışınla etkileşim, ağ etkileşimi, URP, P3 bulmaca altyapısı (Assets/Scripts/Puzzle), Bulmaca 1 (4 kol → sembol sırası → B'de kasa kapısı; telefon+PC test edildi). Görsel geçiş v1: temalı menü + HUD, A çan kulesi, B zindan (PC ekran görüntüsüyle doğrulandı, cihazda denenmedi).
Bulmaca 2 (kibrit mahzeni) test edildi. Bulmaca 3 (sarnıç) test edildi. Bulmaca 4 (şifreli mektup; Arşiv + Mezar kütüphanesi, lobi ölçeği, CoopSetup.Puzzle4.cs) kodlandı, test edilmedi; geçiş Passage (ışınlama). Bulmaca 5 (katakomb labirenti; Harita odası + Katakomb, CoopSetup.Puzzle5.cs, iç duvarlar çalışma anında) kodlandı, test edilmedi. Bulmaca 6 (ay aynaları; Çatı terası + Kuyu odası, CoopSetup.Puzzle6.cs, MoonPuzzle/MoonMirror/MoonView/MoonHud; geçiş: harita kürsüsündeki çatı merdiveni / katakomb çıkış kapısı) kodlandı, test edildi. Bulmaca 7 (çan ve kemik org; Çan Dairesi + Kemik Org Kriptası, CoopSetup.Puzzle7.cs, BellPuzzle/BellNode/BellView, simge seti Notes) ve Final (Büyük Salon, Layout.Final x=-500, AreaFinal, CoopSetup.Final.cs, FinalHall + kapanış ekranı) test edildi, kullanıcı beğendi. İlerleme: her bulmaca yeni oda (X: kule → Saat Odası → Hazne Odası; O: zindan → kasa → mahzen → sarnıç).
Akış (Faz 3, cihazda/iki oyuncuyla DENENMEDİ): menü Oda Kur / Odaya Katıl → herkes Lobi Holü'nde doğar (Layout.Hall, x=-250; CoopSetup.Hall.cs) → biri X (merdiven sahanlığı), diğeri O (doğu nişi) plakasında 1.5 sn durunca HallGate rolleri atar ve ışınlar: X → çan kulesi, O → zindan. Rol artık giriş sırası değil: PlayerAvatar.RoleIndex (Roles.X/O/None), giriş sırası = Slot. Mekanizma odası (kule) kitle yenilendi, baked (CoopSetup.Tower.cs). Eski odalar (saat odası, zindan, mahzen, sarnıç) henüz yenilenmedi; bake dışı (ExcludeFromBake).
Sıradakiler: (1) B5 harita odası onaylandı (kullanıcı beğendi; ~120 dekor, kâşif koleksiyonu); labirent 2. zorlaştırma (rastgele kuzey çıkışı, tüm sapaklar tuzak, yağ 170 sn) test bekliyor, (2) B7 + final test edildi (tamam), (3) SON BULMACADAN SONRA TOPLU DÜZENLEME: B1-B4'ün X ve O tarafındaki TÜM odaları (kule, saat, hazne, zindan, kasa, mahzen, sarnıç, arşiv, kütüphane) harita odası kalitesine getir (O tarafı öne alındı): lobi ölçeği, 110-140 dekor, gerekirse yeni CC0 model, tekrar yok, "bulmaca girişte görünmesin", (4) okunabilir lore mektupları (okuma ekranı), (5) sarnıç suyu shader, Mixamo karakter, FrameRateConfig.Target=60 (şu an 240), cihaz FPS testi, (6) bulmacalar bitince Mac'te TestFlight (Tools/ios_testflight.sh; TEAM_ID/BUNDLE_ID kullanıcıdan).
Test kolaylığı: CoopGame → Setup Hızlı (bake yok) + editörde Play; menüde Başlangıç düğmesi (Kaldığın yer / Baştan / Bulmaca N / Tüm odalar açık); editörde tek kişi plakada rol alır. İlerleme kaydı: Progress.cs (yeni bulmacada LastPuzzle++ ve Checkpoint ekle).
Repo: github.com/ozturksosyal61-oss/echoesofthekeep (dal main).
Düğüm (nodeId) aralıkları: B1-B3 0-30, B4 35-43, B5 44-49, B6 50-58, B7 59-80, final 81-85, lobi 90-92. Yeni bulmacada çakışma kontrol et (PuzzleNode OnEnable hatası basar).
Not: 3. şahıs/uzak oyuncu gövdesi hareket yönüne dönüyor, bakış yönüne değil; final salonunda düzelt.
Tasarım belgesi: Docs/PuzzleDesign.md
Görsel yenileme fazları ve devam notu: Docs/ArtPhases.md (Faz 5: mahzen onaylı, sarnıç kodlandı → onay bekliyor).
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
- Yeni oda ASLA boş taş kutu + bulmaca değil; komple yaşanmış mekan: masa, üstünde kağıt/mektup/mum, raf, şişe, sandık, tablo, alet, örümcek ağı. Lobi ölçeğinde oda DOLU görünmeli: 30+ dekor (heykel, büst, silah, ayna, portre, kitaplık, avize, sancak, halı, mum, kağıt, kemik vb.), environmental storytelling. Gotik/gizemli/hafif korku dokunuşları serbest (kullanıcı izinli).
- Ölçek: yeni odalar lobi gibi geniş ve yüksek (lobi 18x22, tavan 9 m; ref CoopSetup.Hall.cs + lobi_hol.png): galeri/sahanlık, niş, heykel, büst, sancak, tablo. Basık 10x10 oda yok (bilinçli dar: mahzen). Eski odalar sonra büyütülecek.
- Bulmaca girişte göze batmaz: oyuncu odayı keşfedip aramalı (galeri, yan nef, niş; gerekirse sahte ipucu). Odalar arası tekrar yok: her odaya yeni dekor (Assets/ThirdParty, gerekirse yeni CC0 indir).
- Odalar birbirini TEKRAR ETMEZ (kalıcı kural): her yeni oda önceki odalardan farklı plan, tavan tipi, galeri/merdiven biçimi, duvar malzemesi ve ana dekor (hero prop) kullanır; yazmadan önce mevcut odalarla karşılaştır. Merdivenler duvara yapışık olmaz (en az ~1 m boşluk ya da ortada serbest merdiven).
- Simgeler TEKRAR ETMEZ (kalıcı kural): her bulmaca kendi simge/rakam setini kullanır. Kullanılan: B1-B4 Glyphs (kare/elmas/artı/çarpı/üçgen/ters üçgen + Roma rakamı), B5 Runes (rün işaretleri + zar noktaları), B6 ay evreleri (Assets/Generated/Moon), B7 nota işaretleri (Notes.cs). Yeni bulmacada yeni set (ör. hayvan, gezegen, renk, nota).
- Kitaplıklar daima dolu (BookshelfFull*; boş raf/yüzen kitap yok). Mekan boş görünmemeli: zemin, duvar, raf ve masa yüzeylerinin hepsinde dekor.
- Şato iç: ahşap lambri, sıva, kiriş, parke, kumaş. Yer altı (mahzen/sarnıç): taş.
- Sadece Docs/StyleGuide.md paleti ve Art kit (Assets/Art/Kit) kullanılır. Pembe/neon/parlak renk yasak.
- Referans görseli olmayan yeni oda için önce kullanıcıya Gemini prompt'u yaz (sonuna "no text"), görsel gelince tasarla.
- Asset: kitte uygun model yoksa sorma, kendin indir: Poly Haven API (api.polyhaven.com/files/<id> → fbx 1k + _diff_/_nor_gl_ dokular) → Assets/ThirdParty/PolyHaven/Models/<id>/, sonra CoopSetup.Kit.cs BuildKitPrefabs'a PhProp ekle (boy/yön: api.polyhaven.com/info/<id> dimensions, Blender Z yukarı, -Y ön). CC0 bulunamayan (ör. kafatası, zırh) için kullanıcıya link + hedef klasör ver, o indirir. Lisanslar Docs/CREDITS.md. Plan: Docs/AssetPlan.md.
- Mobil bütçe: URP mobile, baked light + light probes, oda başına max 2 realtime light, gerçek zamanlı yansıma yok (reflection probe ile taklit), LOD, occlusion culling, ASTC. Hedef orta Android 60 fps (min 30).
- Roller: X (yukarı/mekanizma) ve O (aşağı/mahzen); eski adları A/B.
