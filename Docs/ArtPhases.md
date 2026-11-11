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
- Mahzen: KODLANDI, kullanıcı ONAYI bekliyor (CoopSetup.Cellar.cs, ekran görüntüleri Docs/Shots/faz5/o_cellar*.png). Lahitler kalktı: bulmaca 2'nin 6 sembol + rakamı batı duvarındaki panelde (CryptVisuals alanları aynı). Oda bulmaca gereği karanlık → bake DIŞI; alevler + 2 realtime ışık + ağlar "CellarLit" altında, çözülünce açılır (Shots bu grubu açıp örnek sembol çizer). OYNANARAK TEST EDİLMEDİ (kibritle panel okunabilirliği).
- Sarnıç: KODLANDI, kullanıcı ONAYI bekliyor (CoopSetup.Cistern.cs, ref Docs/Reference/sarnic.jpg, ekran görüntüleri Docs/Shots/faz5/o_cistern*.png). Baked (KeepBaked) + 2 realtime fener; 3 tuğla tonoz, sütunlar, taş yürüme yolu (0.1 m basamak, çarpışmalı), parmaklıklı kapılar (BarGate), vana = çark + renk panosu + sembol plakası. CisternPuzzle.Colors palete çekildi. OYNANARAK TEST EDİLMEDİ. Açık: su düz koyu yüzey (yansıma/dalga yok → "sarnıç suyu" işi), semboller plakada küçük kalabilir.
- O rolü şu an Layout.Spawn(Roles.O) = zindan başlangıcına ışınlanıyor; kullanıcı "O → aşağı mahzen" dedi, mahzen yenilenince başlangıcı gözden geçir.

## Sonra / açık konular
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
