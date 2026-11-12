# Görsel yenileme fazları (devam notu)

Kurallar: her "ONAY" noktasında dur, kısa rapor ver, onay bekle. Push yok. Raporlar kısa. Kullanıcı tüm fazlar bitince Unity'de deneyecek.
Token tasarrufu: oda başına 1–2 ekran görüntüsü oku; her faz ayrı oturum.

## Biten
- Faz 0: CLAUDE.md "Art direction", Docs/StyleGuide.md
- Faz 1: asset'ler (Docs/AssetPlan.md, Docs/CREDITS.md)
- Faz 2: Art kit (CoopSetup.Kit.cs, Assets/Art/Kit, sergi odası) — commit c13cee3
- Faz 3: menü Oda Kur/Odaya Katıl, Lobi Holü, X/O plakaları, A/B → X/O — commit 55c14f8. İki oyunculu akış OYNANARAK TEST EDİLMEDİ.

- Faz 4: mekanizma odası (CoopSetup.Tower.cs; kol görseli MakeLever). Baked, 2 fener realtime. Ekran görüntüleri Docs/Shots/faz4. OYNANARAK TEST EDİLMEDİ: çözülünce dönen dişliler (PuzzleSpin), O odasındaki freskte yassı semboller (çalışma anında kopyalanır, ekran görüntüsünde yok).

## Faz 5 – Mahzen ve sarnıç (sıradaki)
- Mahzen: ONAYLANDI (CoopSetup.Cellar.cs, ekran görüntüleri Docs/Shots/faz5/o_cellar*.png). Lahitler kalktı: bulmaca 2'nin 6 sembol + rakamı batı duvarındaki panelde (CryptVisuals alanları aynı). Oda bulmaca gereği karanlık → bake DIŞI; alevler + 2 realtime ışık + ağlar "CellarLit" altında, çözülünce açılır (Shots bu grubu açıp örnek sembol çizer). OYNANARAK TEST EDİLMEDİ (kibritle panel okunabilirliği).
- Sarnıç: KODLANDI, kullanıcı ONAYI bekliyor (CoopSetup.Cistern.cs, ref Docs/Reference/sarnic.jpg, ekran görüntüleri Docs/Shots/faz5/o_cistern*.png). Baked (KeepBaked) + 2 realtime fener; 3 tuğla tonoz, sütunlar, taş yürüme yolu (0.1 m basamak, çarpışmalı), parmaklıklı kapılar (BarGate), vana = çark + renk panosu + sembol plakası. CisternPuzzle.Colors palete çekildi. OYNANARAK TEST EDİLMEDİ. Açık: su düz koyu yüzey (yansıma/dalga yok → "sarnıç suyu" işi), semboller plakada küçük kalabilir.
- Saat Odası + Hazne Odası (X): KODLANDI, ekran görüntüsü/ONAY bekliyor (CoopSetup.Clock.cs, ref saat.jpg/hazne.jpg). Baked (KeepBaked(clock)); Saat: dişli duvarı, asılı çan, vitray, tezgah, portre; Hazne: gösterge önünde sarnıç kuyusu (bordürlü), borular, 2 asılı fener (realtime). Kullanıcı gotik/hafif korku eklentilerine serbestlik verdi; lobi tarzı esas.
- Zindan (O, bulmaca 1) + sarnıç v2: KODLANDI, kullanıcı kontrol edecek (CoopSetup.Dungeon.cs ref zindan.png: 2 tuğla tonoz + sütun dizisi, boyalı fresk, yuvarlak perçinli kasa kapısı, baked; Cistern.cs ref sarnic2.png: yüksek tonoz, tavan açıklıkları + ay huzmesi, renkli boru dirsekleri, suyla yükselen kayık/döküntü). Mahzen: girişten 3 sn sonra tüm ışıklar söner (CryptHud). Hazne kuyusu ızgaralı.
- O rolü şu an Layout.Spawn(Roles.O) = zindan başlangıcına ışınlanıyor; kullanıcı "O → aşağı mahzen" dedi, mahzen yenilenince başlangıcı gözden geçir.

## Faz 7 – Tüm odaların yenilenmesi (DEVAM EDİYOR, 2026-11-12)
Sıra: O (zindan → kasa → mahzen → sarnıç → kütüphane), sonra X (kule → saat → hazne → arşiv). Her oda: lobi ölçeği, 110-140 dekor, 5 özgün hikâye detayı, sonda oyuncu gözüyle eleştiri + en zayıf 3 noktayı düzelt. Oda başına ayrı commit, push yok.
- 35 yeni Poly Haven CC0 model indirildi (Models/: old_bed_frame, street_rat, mousetrap, IronGate vb.), Kit.cs "Faz 7 ekleri" PhProp'ları; mesh LOD kapalı (NoMeshLod).
- Zindan v3 YAZILDI, DOĞRULANMADI (CoopSetup.Dungeon.cs): 16x26, tonoz 7.2, kuzeyde 3 hücre, gözcü platformu + serbest merdiven, gibbet kafes, gardiyan masası, sorgu köşesi; fresk doğu Kayıt Şapeli'ne taşındı (Art.cs x 12.9). O doğma noktası Layout.Spawn → (1.8, 0, -11).
- Bake çökmesi ÇÖZÜLDÜ: reflection probe'lar bake sırasında Custom, sonra RenderProbesManually ile kamerayla çekilir (Unity 6.6 ReplaceIndicesForSingleLod hatası).
- Hızlı deneme: batch `-draft -bakeOnly RoomB_Sarnic` (yalnız o kök pişer, düşük kalite; ~30 sn bake, ~3 dk toplam). Sahne taslak kalır → build/commit öncesi tam Setup All.
- Zindan v3 doğrulandı (ekran görüntüsü): zemin cobblestone_floor_08, saman dirt_floor, gardiyan köşesi dolduruldu. Kullanıcı onayı bekliyor.
- Shots: -shotFilter önek listesi eklendi (o_dg*, o_fresco, o_vault).
- Batch bake ~8-9 GB RAM kullanıyor; makinede bellek dar.

## Sonra / açık konular
- Eski odaları lobi ölçeğine büyüt (saat, hazne, zindan, sarnıç; mahzen dar kalabilir). Kullanıcı istedi, bulmaca 4-7 sonrası.
- Occlusion culling (StaticOcclusionCulling.Compute) henüz yok.
- Oyun içi credits ekranı (CC-BY modeller: Docs/CREDITS.md).
- treasure_chest 95k üçgen; mesh LOD'un devrede olduğu doğrulanmadı.
- Ay huzmesi kartında bir açıdan ince çizgi kalıyor (LightShaft.shader _Fade artırılabilir).
- Menü arka planındaki kule penceresi (lanternGlass) ACES ile beyaza patlıyor.
- Cihaz testi + FPS hiç yapılmadı.
- ChandelierIron askı çubuğu kalın/kırmızımsı görünüyor (kit prefabı).
- Kit "Door" duvar çapalı değil: duvara yapışık kullanınca probe çapasını elle odaya al (Tower'daki DoorProbeAnchor).

## Çalıştırma
Unity kapalıyken (kullanıcıya sor):
`"C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod CoopGame.EditorTools.CoopSetup.SetupAndShots -shotDir <klasör> -logFile <log>`
(~3-4 dk; Setup All + bake + tüm ekran görüntüleri). Yalnızca kit: `...CoopSetup.KitShots`. Hata ara: `error CS|Exception|\[CoopSetup\]`.

## Öğrenilenler
- Light probe gövdesinin dışında kalan nesne simsiyah olur: KitProbes odayı zeminden tavana kaplamalı; dolu hacim içindeki probe'lar `skip` Bounds ile atlanır.
- Unity fake-null: `GetComponent<T>() is T x` kullanma, `!= null` kullan.
- Model malzeme adında "glass/flame/canvas" eşlemesi KitPropMat'te.
- Eski odalarla aynı sahnede baked oda: KeepBaked(root) ile ExcludeFromBake'ten muaf tut; ince parçalı tavanın üstüne tek parça "Roof" koy (ay ışığı derzlerden sızıyor).
- -shotDir olarak proje Temp/ klasörünü verme (Unity çıkışta siler).
- Bash heredoc içinde uzun Python yaması bozulabiliyor: yamayı dosyaya yazıp çalıştır.
- Kit şişe modeli 6-8 bin üçgen: toplu kullanımda (raf) torna ağ kullan (Cellar "kit_rackbottle"). Küçük kit dekoru LODGroup ile ~3 m ötede kırpılır.
- Işıksız (unlit) malzemeler (K.cobweb, K.flame, K.lanternGlass) karanlık odada parlar: karanlık bulmaca odasında çözülünce açılan gruba koy.
