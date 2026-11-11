# Görsel yenileme fazları (devam notu)

Kurallar: her "ONAY" noktasında dur, kısa rapor ver, onay bekle. Push yok. Raporlar kısa. Kullanıcı tüm fazlar bitince Unity'de deneyecek.
Token tasarrufu: oda başına 1–2 ekran görüntüsü oku; her faz ayrı oturum.

## Biten
- Faz 0: CLAUDE.md "Art direction", Docs/StyleGuide.md
- Faz 1: asset'ler (Docs/AssetPlan.md, Docs/CREDITS.md)
- Faz 2: Art kit (CoopSetup.Kit.cs, Assets/Art/Kit, sergi odası) — commit c13cee3
- Faz 3: menü Oda Kur/Odaya Katıl, Lobi Holü, X/O plakaları, A/B → X/O — commit 55c14f8. İki oyunculu akış OYNANARAK TEST EDİLMEDİ.

## Faz 4 – Mekanizma odası (sıradaki)
Ref: Docs/Reference/mekanizma.jpg. Bulmaca mantığına DOKUNMA, yalnızca görsel yenile (CoopSetup.Art.cs BuildTower + BuildPuzzle1'in kule tarafı).
- Lambri + sıva + kirişli tavan + parke (Wainscot, BeamCeiling, K.*), vitray pencere, tezgah + aletler + fener, notlar, örümcek ağı, aplik mumlar.
- Arka duvarda dişli yığını düşük maliyetli (statik, tek malzeme); öndeki birkaç dişli bulmaca çözülünce dönsün (Spin bileşeni var: Assets/Scripts/Fx/Spin.cs).
- Çan: kit "Bell" prefabı (manuel model) kirişten asılı.
- Kol plakaları çerçeveli; renkler StyleGuide bulmaca renkleri (Pal.Puzzle), parlak renk yok.
- Odayı bake'e dahil et (ExcludeFromBake'ten çıkar), KitProbes + KitReflection ekle, en fazla 2 realtime ışık.
- Ekran görüntüsü → Docs/Shots/faz4/. ONAY.

## Faz 5 – Mahzen ve sarnıç
- Mahzen: ref Docs/Reference/mahzen.png; sembol paneli = bulmaca 2 duvarı. Tonoz tuğla (K.cellarBrick), ıslak zemin (K.cellarFloor), fıçı istifleri, şarap rafı + şişeler, fenerler, masa + bordo örtü + mum + kadeh, amfora, ızgara. Ekran görüntüsü + ONAY.
- Sarnıç: referans görsel YOK → önce kullanıcıya Gemini prompt'u yaz (sonuna "no text"), görsel gelene kadar bekle; sonra tasarla. Ekran görüntüsü + ONAY.
- O rolü şu an Layout.Spawn(Roles.O) = zindan başlangıcına ışınlanıyor; kullanıcı "O → aşağı mahzen" dedi, mahzen yenilenince başlangıcı gözden geçir.

## Sonra / açık konular
- Occlusion culling (StaticOcclusionCulling.Compute) henüz yok.
- Oyun içi credits ekranı (CC-BY modeller: Docs/CREDITS.md).
- treasure_chest 95k üçgen; mesh LOD'un devrede olduğu doğrulanmadı.
- Ay huzmesi kartında bir açıdan ince çizgi kalıyor (LightShaft.shader _Fade artırılabilir).
- Menü arka planındaki kule penceresi (lanternGlass) ACES ile beyaza patlıyor.
- Cihaz testi + FPS hiç yapılmadı.

## Çalıştırma
Unity kapalıyken (kullanıcıya sor):
`"C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod CoopGame.EditorTools.CoopSetup.SetupAndShots -shotDir <klasör> -logFile <log>`
(~3-4 dk; Setup All + bake + tüm ekran görüntüleri). Yalnızca kit: `...CoopSetup.KitShots`. Hata ara: `error CS|Exception|\[CoopSetup\]`.

## Öğrenilenler
- Light probe gövdesinin dışında kalan nesne simsiyah olur: KitProbes odayı zeminden tavana kaplamalı; dolu hacim içindeki probe'lar `skip` Bounds ile atlanır.
- Unity fake-null: `GetComponent<T>() is T x` kullanma, `!= null` kullan.
- Model malzeme adında "glass/flame/canvas" eşlemesi KitPropMat'te.
- Bash heredoc içinde uzun Python yaması bozulabiliyor: yamayı dosyaya yazıp çalıştır.
