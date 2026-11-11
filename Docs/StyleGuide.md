# Echoes of the Keep – Stil Rehberi

Kaynak: Docs/Reference (lobi_hol, mekanizma, mahzen, genel_ic). Ton: terk edilmiş gotik şato, gece. Soğuk ay ışığı + sıcak mum karşıtlığı; doygunluk düşük, parlak/neon renk yok.

## Palet (hex)
| Grup | Renk | Hex | Kullanım |
|---|---|---|---|
| Gölge | Gece laciverti | #141A26 | sis, ambient, en koyu gölge |
| Gölge | Sıcak siyah | #1A120C | yer altı sis/gölge |
| Ay | Ay ışığı | #8CA8E6 | pencere huzmesi, ay lekesi |
| Ay | Soluk mavi-gri | #5F7396 | ay alan taş/sıva yüzeyi |
| Mum | Alev çekirdeği | #FFE3A0 | emissive çekirdek |
| Mum | Mum ışığı | #FFB066 | noktasal ışık rengi |
| Mum | Kor turuncu | #E0701F | alev dışı, fener camı |
| Ahşap | Lambri | #3A2A1E | duvar paneli, kapı |
| Ahşap | Parke | #5A4636 | zemin tahtası |
| Ahşap | Kiriş | #2B211A | tavan kirişi, karkas |
| Sıva | Kirli sıva | #8C8478 | üst duvar |
| Sıva | Gölge sıva | #6E675E | köşe, çatlak |
| Taş | Şato taşı (soğuk) | #6B6A70 | hol, kemer, sütun |
| Taş | Mahzen tuğlası (sıcak) | #6A4E3A | tonoz, mahzen duvarı |
| Taş | Islak zemin | #2E2622 | mahzen/sarnıç zemini |
| Taş | Yosun | #4A5A3A | sarnıç, su hattı |
| Metal | Pirinç | #B08A3E | dişli, şamdan, kol |
| Metal | Bronz | #8A5A2B | çan, eski dişli |
| Metal | Demir | #3B3836 | menteşe, avize, ızgara |
| Kumaş | Bordo | #6E1F24 | flama, halı, örtü |
| Kumaş | Altın işleme | #B8923F | flama deseni, çerçeve |
| Kumaş | Solgun mor | #4A3550 | perde |
| Kağıt | Parşömen | #CDBE9A | mektup, not, harita |
| Su | Sarnıç suyu | #0F2A2E | su yüzeyi |
| Vitray | Kırmızı/yeşil/mavi/amber | #7A3B3B #3E6A5A #3B4E7A #A8893A | yalnızca vitray camı |
| Bulmaca | Kırmızı/yeşil/mavi/sarı | #A8322B #3F7D45 #2F5F9E #C9A227 | kol plakaları, semboller (tek doygun vurgu) |

Kural: bir karede en fazla bir doygun vurgu (bulmaca rengi ya da bordo kumaş). UI teması aynı pirinç/parşömen/lacivert.

## Malzemeler (doku → yer)
| Kit malzemesi | Doku | Nerede |
|---|---|---|
| Panel | PH wooden_panels (koyu tint) | şato iç alt duvar (lambri, ~1.6 m) |
| Plaster | PH worn_plaster_wall | şato iç üst duvar |
| Beam | PH dark_wooden_planks | kiriş, tavan, karkas |
| Parquet | PH old_wood_floor | şato iç zemin |
| Furniture | PH wood_cabinet_worn_long / medieval_wood | masa, raf, tezgah |
| CastleStone | PH medieval_blocks_05 | hol duvarı, sütun, kemer |
| HallFloor | PH monastery_stone_floor | hol zemini |
| CellarBrick | PH castle_brick_01 | mahzen tonoz/duvar |
| CellarFloor | PH slab_tiles (smoothness yüksek = ıslak) | mahzen zemini |
| CisternWall | PH mossy_stone_wall | sarnıç duvarı |
| CisternFloor | PH mossy_cobblestone | sarnıç zemini/basamak |
| DungeonWall/Floor | PH castle_wall_slates / cobblestone_floor_08 | zindan, kasa |
| Velvet | PH velour_velvet (bordo/mor tint) | flama, perde, örtü, koltuk |
| Carpet | PH dirty_carpet (bordo tint) | halı |
| Leather / Books | PH brown_leather / book_pattern | kitap, sandık kayışı |
| Paper | aCG Paper001 (parşömen tint) | mektup, not |
| Iron | PH rusty_metal_02 | menteşe, avize, zincir |
| Brass / Bronze | aCG Metal008 + tint | dişli, şamdan / çan |
| Flame, FlameCore, LanternGlass | emissive | mum, fener |
| Cobweb, Vitray, Banner deseni | üretilen (Assets/Generated) | alfa kart / emissive cam |

Doku 1K, ASTC 6x6 (normal 5x5). Yalnızca diffuse + normal; metal/pürüz malzeme sabitinden.

## Işık
| Kaynak | Renk | Değer |
|---|---|---|
| Mum (baked) | #FFB066 | noktasal, şiddet 2.5–3, menzil 4–5 m |
| Şamdan/avize (baked) | #FFB066 | şiddet 4–5, menzil 7–9 m |
| Fener (realtime, oyuncuya yakın) | #FFA24A | şiddet 3, menzil 5 m, titreşim ±15% |
| Ay (baked, pencere başına spot) | #8CA8E6 | şiddet 1.2–1.8, açı 35–50°, sahte huzme kartı alfa 0.08–0.12 |
| Ambient | #141A26 (şato) / #1A120C (yer altı) | şiddet 0.6 / 0.35 |
| Sis (exp²) | şato #1B2233, yoğunluk 0.018 | yer altı #1A120C 0.03; sarnıç #0F1E22 0.04 |
| Emissive | alev çekirdeği ×3, fener camı ×3, vitray ×0.8 | |

Oda başına en fazla 2 realtime ışık (gölgesiz); gerisi baked + light probe. Yansıma: oda başına 1 baked reflection probe (ıslak zemin).

## Post-process (tek profil)
Bloom eşik 1.0 / şiddet 0.5, ACES tonemap, vinyet 0.28, renk filtresi #FFF7ED, kontrast +10, doygunluk −8. DoF/motion blur/SSAO yok. Düşük cihazda PostFx=false.
