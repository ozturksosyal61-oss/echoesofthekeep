# Asset planı (Faz 1)

PH = Poly Haven (CC0, indirildi), aCG = ambientCG (CC0, indirildi), ÜRET = kod ile üretilir (Assets/Generated), MAN = manuel indir.
Poligon sayısı yüksek modeller (≥15k) kitte LOD/decimate ile kullanılır.

## Lobi Holü (ref: lobi_hol.png)
| Öğe | Kaynak |
|---|---|
| Duvar/sütun/kemer taşı | PH medieval_blocks_05 |
| Zemin | PH monastery_stone_floor + ÜRET koyu baklava dolgu |
| Halı | PH dirty_carpet (bordo) |
| Flamalar (zambak) | PH velour_velvet + ÜRET zambak deseni |
| Avize (demir çember + mumlar) | ÜRET (kit) — ya da MAN |
| Şamdanlar | PH brass_candleholders |
| Heykel + niş | PH gothic_statue, marble_bust_01 — MAN: kadın heykeli |
| Sandık | PH treasure_chest (103k → LOD) |
| Masa, konsol, komodin | PH WoodenTable_01, ClassicConsole_01, GothicCommode_01, GothicCabinet_01 |
| Koltuk | PH ArmChair_01 (bordo tint) |
| Tablolar | PH fancy_picture_frame_01, hanging_picture_frame_02 |
| Kitap yığını | ÜRET kutu + PH book_pattern / brown_leather |
| Vazo, kadeh | PH brass_vase_03, brass_goblets |
| Vitray pencere, gül pencere, ay huzmesi | ÜRET |
| Merdiven + korkuluk, şömine | ÜRET (kit modülleri) |
| Örümcek ağı, yaprak, sarmaşık | ÜRET alfa kart |
| Küre / halkalı küre | MAN (opsiyonel) |
| X / O basınç plakaları | ÜRET |

## Mekanizma odası (ref: mekanizma.jpg)
| Öğe | Kaynak |
|---|---|
| Lambri / sıva / kiriş / parke | PH wooden_panels, worn_plaster_wall, dark_wooden_planks, old_wood_floor |
| Dişliler, miller, kasnak, kayış | ÜRET (mevcut gear ağı) + aCG Metal008 |
| Çan + halat | ÜRET (bronz) — ya da MAN |
| Kol plakaları (çerçeveli) | ÜRET + palet bulmaca renkleri |
| Tezgah | PH WoodenTable_01, vintage_wooden_drawer_01 |
| Aletler | PH combination_wrench, cross_pein_hammer, wooden_hammer_01, bench_vice_01, hand_plane_no4 |
| Fener, aplik mum | PH wooden_lantern_01 |
| Notlar/kağıtlar | ÜRET kart + aCG Paper001 |
| Tabure, merdiven, kova | PH wooden_stool_01, wooden_ladder_02, wooden_bucket_01 |
| Büyük saat (Saat Odası) | PH vintage_grandfather_clock_01 |
| Vitray pencere, örümcek ağı | ÜRET |

## Mahzen (ref: mahzen.png)
| Öğe | Kaynak |
|---|---|
| Tonoz/duvar | PH castle_brick_01 |
| Islak zemin | PH slab_tiles |
| Fıçılar | PH wine_barrel_01 (yatık istif + dik) |
| Şarap rafı | ÜRET raf + PH wine_bottles_01 (LOD) |
| Sandık/kasa | PH wooden_crate_01, wooden_crate_02 |
| Kapı | PH large_castle_door |
| Sembol paneli (bulmaca 2 duvarı) | ÜRET kabartma + pirinç semboller |
| Fenerler (demir kollu) | PH wooden_lantern_01 + ÜRET kol |
| Masa, bordo örtü, mum, kadeh, kase | PH WoodenTable_01, velour_velvet, brass_goblets, wooden_bowl_02 |
| Testi/küp | PH brass_pot_01, brass_vase_03 — MAN: toprak amfora |
| Izgara (zemin gideri) | ÜRET |

## Sarnıç (görsel bekleniyor)
| Öğe | Kaynak |
|---|---|
| Duvar / zemin | PH mossy_stone_wall, mossy_cobblestone |
| Su | ÜRET (koyu, baked probe yansıması) |
| Kova, merdiven, fıçı, zincir, ızgara | PH wooden_bucket_01, wooden_ladder_02, wine_barrel_01 + ÜRET |

## Manuel indir listesi (hepsi opsiyonel; yoksa ÜRET sürümü kullanılır)
Hedef: `Assets/ThirdParty/Manual/<ad>/` (FBX/GLB + dokular). Yalnızca CC0 ya da CC-BY seç; linki bana yaz, CREDITS.md'ye eklerim.
| Öğe | Arama | Hedef klasör |
|---|---|---|
| Demir çember avize | https://sketchfab.com/search?q=medieval+chandelier&type=models&features=downloadable&licenses=7c23a1ba438d4306920229c12afcb5f9 | Manual/chandelier |
| Kadın heykeli (niş) | https://sketchfab.com/search?q=statue+scan&type=models&features=downloadable&licenses=7c23a1ba438d4306920229c12afcb5f9 | Manual/statue |
| Halkalı küre / dünya küresi | https://sketchfab.com/search?q=armillary+sphere&type=models&features=downloadable | Manual/armillary |
| Bronz çan | https://sketchfab.com/search?q=church+bell&type=models&features=downloadable | Manual/bell |
| Toprak amfora | https://sketchfab.com/search?q=amphora&type=models&features=downloadable&licenses=7c23a1ba438d4306920229c12afcb5f9 | Manual/amphora |
