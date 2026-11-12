using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Final (bulmaca 8) "Büyük Salon" v2 (ref final2.png), Layout.Final (-500,0,0): 26x56 m, 16 m sıvalı duvar + çekiç kirişli ahşap çatı (mahya 27 m, makas uçlarında melekler),
    /// iki yanda kör kemerler + ahşap galeri, X yarısında çift kollu taş merdiven (galeriye), O yarısında yarıda kalmış ziyafet masası + iki boş taht, iki şömine, duvar halıları.
    /// Ortada boydan boya dövme demir parmaklık (orta kapı iki kanat): X güney yarıya, O kuzey yarıya kafes asansörle iner.
    /// Her yarıda bir kol (ikisi çekilince parmaklık açılır), kapı önünde iki mühür taşı (ikisine birlikte basınca kapanış).
    /// Düğümler: 81 FinalHall, 82-83 kollar, 84-85 mühür taşları; asansör geçitleri 79-80 (bulmaca 7 kafeslerinden).
    /// </summary>
    public static partial class CoopSetup
    {
        static void BuildFinal(Vector3 center, Transform roomX, Transform roomO)
        {
            const float HX = 13f, HZ = 28f, WH = 16f, VAULT = 10f, BAL = 8f;
            var def = new GameObject("Puzzle8").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 8;
            dso.FindProperty("title").stringValue = "Final (büyük salon)";
            dso.FindProperty("startsActive").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();

            var root = new GameObject("Final_BuyukSalon").transform;
            root.position = center;
            var wallM = KTex("K_FinalWall", "worn_plaster_wall", new Color(0.6f, 0.56f, 0.5f), 0.12f);
            var floorM = KTex("K_FinalFloor", "marble_tiles", new Color(0.3f, 0.29f, 0.31f), 0.8f);
            var carpet = KTex("K_FinalCarpet", "velour_velvet", new Color(0.5f, 0.07f, 0.07f), 0.25f);
            var roofM = KTex("K_FinalRoof", "dark_wooden_planks", new Color(0.36f, 0.3f, 0.27f), 0.12f, 1.2f);
            var stone = K.castleStone;
            var inlay = MakeMat("K_FinalInlay", new Color(0.62f, 0.5f, 0.3f)); inlay.SetFloat("_Smoothness", 0.7f); inlay.SetFloat("_Metallic", 0.6f);
            var voidM = MakeMat("K_WellVoid", new Color(0.01f, 0.012f, 0.02f));
            const float RIDGE = 27f; // çatı mahyası (ahşap çekiç kirişli çatı)

            // --- Kabuk: sıvalı duvarlar, mermer zemin ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), HX * 2f + 1f, HZ * 2f + 1f, 0.5f, floorM, 4, 8);
            Wall(root, "WallW", new Vector3(-HX - 0.25f, 0, 0), false, HZ * 2f + 1f, WH, wallM, 3f);
            Wall(root, "WallE", new Vector3(HX + 0.25f, 0, 0), false, HZ * 2f + 1f, WH, wallM, 3f);
            Wall(root, "WallS", new Vector3(0, 0, -HZ - 0.25f), true, HX * 2f, RIDGE, wallM, 3f);
            Wall(root, "WallN", new Vector3(0, 0, HZ + 0.25f), true, HX * 2f, RIDGE, wallM, 3f);
            Wainscot(root, "WainN", new Vector3(0, 0, HZ), true, HX * 2f, Vector3.back, 2.6f, new Opening(0f, 7.0f, 0f, 9.8f));
            Wainscot(root, "WainS", new Vector3(0, 0, -HZ), true, HX * 2f, Vector3.forward, 2.6f, new Opening(0f, 6.6f, 0f, 8.5f));

            // --- Çatı: ahşap kaplama + 7 çekiç kirişli makas (uçlarında melekler), aşıklar, mahya ---
            float RoofY(float ax) => WH + (RIDGE - WH) * (1f - ax / HX); // |x| → çatı alt yüzü
            float pitch = Mathf.Atan2(RIDGE - WH, HX) * Mathf.Rad2Deg, rlen = Mathf.Sqrt(HX * HX + (RIDGE - WH) * (RIDGE - WH));
            foreach (int s in new[] { -1, 1 })
            {
                var rp = Slab("RoofBoards", root, new Vector3(s * HX * 0.5f, (WH + RIDGE) * 0.5f + 0.2f, 0), new Vector3(rlen + 0.6f, 0.4f, HZ * 2f + 1f), roofM, 2.5f, false);
                rp.transform.localRotation = Quaternion.Euler(0, 0, -s * pitch);
                Slab("WallPlate", root, new Vector3(s * (HX - 0.25f), WH + 0.15f, 0), new Vector3(0.5f, 0.4f, HZ * 2f), K.beam, 1.2f, false);
                foreach (float ax in new[] { 3.4f, 7.0f, 10.4f })
                    Slab("Purlin", root, new Vector3(s * ax, RoofY(ax) - 0.25f, 0), new Vector3(0.28f, 0.34f, HZ * 2f), K.beam, 1.2f, false);
            }
            Slab("RidgeBeam", root, new Vector3(0, RIDGE - 0.3f, 0), new Vector3(0.4f, 0.5f, HZ * 2f), K.beam, 1.2f, false);
            const float COLLAR = 23f;
            float collarX = HX * (RIDGE - COLLAR) / (RIDGE - WH);
            for (float z = -21f; z <= 21.1f; z += 7f)
            {
                var tr = new GameObject("Truss").transform; tr.SetParent(root, false); tr.localPosition = new Vector3(0, 0, z);
                foreach (int s in new[] { -1, 1 })
                {
                    float hx = s * (HX - 3.0f), postTop = RoofY(HX - 3.0f) - 0.2f;
                    Rod("Rafter", tr, new Vector3(s * (HX - 0.2f), WH - 0.1f, 0), new Vector3(0, RIDGE - 0.35f, 0), 0.38f, K.beam);
                    Rod("WallPost", tr, new Vector3(s * (HX - 1.25f), 11.6f, 0), new Vector3(s * (HX - 1.25f), WH + 0.1f, 0), 0.3f, K.beam); // sütun yüzünde
                    Slab("Corbel", tr, new Vector3(s * (HX - 1.25f), 11.35f, 0), new Vector3(0.7f, 0.5f, 0.7f), stone, 1f, false);
                    Rod("HammerBeam", tr, new Vector3(s * (HX - 0.1f), WH + 0.1f, 0), new Vector3(hx, WH + 0.1f, 0), 0.45f, K.beam);
                    Rod("HammerPost", tr, new Vector3(hx, WH + 0.1f, 0), new Vector3(hx, postTop, 0), 0.34f, K.beam);
                    Vector3 prev = new Vector3(s * (HX - 1.25f), 12.0f, 0);
                    for (int k = 1; k <= 6; k++) // duvar direğinden çekiç ucuna kavisli payanda
                    {
                        float a = k / 6f * Mathf.PI * 0.5f;
                        var p = new Vector3(s * (HX - 1.25f - 1.75f * (1f - Mathf.Cos(a))), 12.0f + 4.0f * Mathf.Sin(a), 0);
                        Rod("Brace", tr, prev, p, 0.24f, K.beam); prev = p;
                    }
                    prev = new Vector3(hx, postTop, 0);
                    for (int k = 1; k <= 6; k++) // çekiç direğinden kuşak kirişine kemer
                    {
                        float t = k / 6f;
                        var p = Vector3.Lerp(Vector3.Lerp(new Vector3(hx, postTop, 0), new Vector3(hx, COLLAR - 0.2f, 0), t), Vector3.Lerp(new Vector3(hx, COLLAR - 0.2f, 0), new Vector3(0, COLLAR - 0.2f, 0), t), t);
                        Rod("Arch", tr, prev, p, 0.24f, K.beam); prev = p;
                    }
                    KitR("StatueAngel", tr, new Vector3(s * (HX - 2.75f), WH + 0.33f, 0), Quaternion.Euler(0, s > 0 ? -90f : 90f, 0), 0.5f);
                }
                Rod("Collar", tr, new Vector3(-collarX, COLLAR, 0), new Vector3(collarX, COLLAR, 0), 0.36f, K.beam);
                Prim("Boss", tr, PrimitiveType.Sphere, new Vector3(0, COLLAR - 0.3f, 0), new Vector3(0.45f, 0.5f, 0.45f), K.brass);
                ProbeLit(tr.gameObject);
            }

            // --- Yanlar: kör kemerler (şömine ve duvar halısı bölmeleri hariç), sütunlar, ahşap galeri ---
            float[] bayZ = { -24.5f, -17.5f, -10.5f, -3.5f, 3.5f, 10.5f, 17.5f, 24.5f };
            float FireZ(int s) => s < 0 ? -3.5f : 3.5f; // X yarısında batı, O yarısında doğu şöminesi
            foreach (int s in new[] { -1, 1 })
            {
                float yaw = s > 0 ? -90f : 90f;
                foreach (float z in bayZ)
                    if (z != FireZ(s) && Mathf.Abs(z) != 17.5f) ArchTrim(root, "Arcade", new Vector3(s * HX, 0, z), new Vector3(-s, 0, 0), 4.4f, 6.6f, true);
                for (float z = -21f; z <= 21.1f; z += 7f)
                {
                    Slab("Column", root, new Vector3(s * (HX - 0.5f), WH * 0.5f, z), new Vector3(1.1f, WH, 1.1f), stone, 1.5f);
                    Slab("ColumnBase", root, new Vector3(s * (HX - 0.55f), 0.4f, z), new Vector3(1.5f, 0.8f, 1.5f), stone, 1f, false);
                    Slab("ColumnCap", root, new Vector3(s * (HX - 0.55f), BAL - 2.4f, z), new Vector3(1.4f, 0.4f, 1.4f), stone, 1f, false);
                    Rod("Bracket", root, new Vector3(s * (HX - 0.4f), BAL - 2.2f, z), new Vector3(s * (HX - 2.2f), BAL - 0.15f, z), 0.3f, K.beam);
                }
                // Galeri: ahşap döşeme, tırabzan (merdiven ağzı açık), ortada X/O yarılarını ayıran demir kafes
                Slab("Balcony", root, new Vector3(s * (HX - 1.2f), BAL, 0), new Vector3(2.4f, 0.3f, HZ * 2f), K.beam, 2f, true);
                Slab("BalconyFascia", root, new Vector3(s * (HX - 2.42f), BAL - 0.25f, 0), new Vector3(0.12f, 0.5f, HZ * 2f), K.beam, 1.2f, false);
                var bal = new GameObject("Balusters").transform; bal.SetParent(root, false);
                foreach (var (z0, z1) in new[] { (-HZ + 0.1f, -23.7f), (-21.3f, HZ - 0.1f) })
                {
                    float bx = s * (HX - 2.3f);
                    Slab("BalconyRail", bal, new Vector3(bx, BAL + 1.1f, (z0 + z1) * 0.5f), new Vector3(0.16f, 0.1f, z1 - z0), K.beam, 1f, false);
                    Slab("BalconyBase", bal, new Vector3(bx, BAL + 0.2f, (z0 + z1) * 0.5f), new Vector3(0.2f, 0.1f, z1 - z0), K.beam, 1f, false);
                    for (float z = z0 + 0.2f; z < z1; z += 0.45f) Tube("Baluster", bal, new Vector3(bx, BAL + 0.25f, z), new Vector3(bx, BAL + 1.05f, z), 0.05f, K.beam);
                    var bc = new GameObject("RailBlock").AddComponent<BoxCollider>(); bc.transform.SetParent(root, false);
                    bc.transform.localPosition = new Vector3(bx, BAL + 0.8f, (z0 + z1) * 0.5f); bc.size = new Vector3(0.25f, 1.3f, z1 - z0);
                }
                CombineChildren(bal, "final_balusters" + (s < 0 ? "W" : "E"), K.beam);
                ProbeLit(bal.gameObject);
                for (float x = HX - 2.3f; x < HX; x += 0.25f) Rod("GalleryGrille", root, new Vector3(s * x, BAL + 0.15f, -1f), new Vector3(s * x, BAL + 3.0f, -1f), 0.04f, K.iron);
                Slab("GalleryGrilleTop", root, new Vector3(s * (HX - 1.2f), BAL + 3.0f, -1f), new Vector3(2.4f, 0.08f, 0.08f), K.iron, 1f, false);
                var gb = new GameObject("GalleryBlock").AddComponent<BoxCollider>(); gb.transform.SetParent(root, false);
                gb.transform.localPosition = new Vector3(s * (HX - 1.2f), BAL + 1.6f, -1f); gb.size = new Vector3(2.5f, 3.2f, 0.3f);
                foreach (float z in bayZ) Kit("WindowLancet", root, new Vector3(s * HX, BAL + 2.4f, z), yaw, 1.6f);
                foreach (float z in new[] { -21f, -14f, -7f, 7f, 14f, 21f })
                    Kit("Banner", root, new Vector3(s * (HX - 2.42f), BAL - 0.55f, z), yaw, 1.5f);
                foreach (float z in new[] { -24.5f, -10.5f, 10.5f, 24.5f })
                {
                    Kit("WallSconce", root, new Vector3(s * (HX - 0.05f), BAL + 1.6f, z + 1.8f), yaw);
                    Kit("CandleCluster", root, new Vector3(s * (HX - 0.6f), BAL + 0.15f, z), 0f);
                }
                // Sütun yüzlerinde portreler, kalkan + çapraz kılıçlar
                foreach (float z in new[] { -14f, -7f, 7f, 14f })
                    Kit(Mathf.Abs(z) == 7f ? "PaintingPortrait" : "PaintingKeep", root, new Vector3(s * (HX - 1.07f), 4.2f, z), yaw, Mathf.Abs(z) == 7f ? 0.95f : 1.4f);
                foreach (float z in new[] { -21f, 21f })
                {
                    Kit("Shield", root, new Vector3(s * (HX - 1.07f), 4.0f, z), yaw, 1.3f);
                    Kit("Sword", root, new Vector3(s * (HX - 1.09f), 3.0f, z - 0.35f), yaw);
                    Kit("Sword", root, new Vector3(s * (HX - 1.09f), 3.0f, z + 0.35f), yaw);
                }
                // Duvar halıları (kemersiz bölmeler, z ±17.5)
                foreach (float z in new[] { -17.5f, 17.5f })
                {
                    KitR("Rug", root, new Vector3(s * (HX - 0.04f), 4.3f, z), Quaternion.LookRotation(new Vector3(0, 0, s < 0 ? -1f : 1f), new Vector3(-s, 0, 0)), 1.4f);
                    Rod("TapestryRod", root, new Vector3(s * (HX - 0.1f), 6.75f, z - 1.5f), new Vector3(s * (HX - 0.1f), 6.75f, z + 1.5f), 0.06f, K.brass);
                    foreach (int e in new[] { -1, 1 }) Prim("RodKnob", root, PrimitiveType.Sphere, new Vector3(s * (HX - 0.1f), 6.75f, z + e * 1.55f), Vector3.one * 0.12f, K.brass);
                }
            }

            // --- Şömineler: X yarısı batı, O yarısı doğu ---
            foreach (int s in new[] { -1, 1 })
            {
                var f = new GameObject("GreatFireplace").transform; f.SetParent(root, false);
                f.localPosition = new Vector3(s * HX, 0, FireZ(s)); f.localRotation = Quaternion.Euler(0, s > 0 ? -90f : 90f, 0); // +Z odaya
                foreach (int e in new[] { -1, 1 })
                {
                    Slab("Jamb", f, new Vector3(e * 1.9f, 1.4f, 0.6f), new Vector3(0.7f, 2.8f, 1.2f), stone, 1.5f);
                    Slab("JambCap", f, new Vector3(e * 1.9f, 2.9f, 0.65f), new Vector3(0.85f, 0.2f, 1.3f), stone, 1f, false);
                }
                Slab("Lintel", f, new Vector3(0, 3.25f, 0.6f), new Vector3(4.5f, 0.7f, 1.2f), stone, 1.5f, false);
                Slab("Mantel", f, new Vector3(0, 3.65f, 0.7f), new Vector3(4.9f, 0.14f, 1.4f), stone, 1.5f, false);
                Slab("Hood", f, new Vector3(0, 5.6f, 0.4f), new Vector3(3.8f, 3.8f, 0.8f), stone, 1.5f, false);
                Slab("HoodBand", f, new Vector3(0, 4.0f, 0.82f), new Vector3(4.0f, 0.25f, 0.1f), stone, 1f, false);
                Slab("Back", f, new Vector3(0, 1.4f, 0.05f), new Vector3(3.2f, 2.8f, 0.1f), voidM, 1f, false);
                Slab("Hearth", f, new Vector3(0, 0.06f, 1.0f), new Vector3(4.6f, 0.12f, 2.0f), stone, 1.5f);
                Kit("Shield", f, new Vector3(0, 5.6f, 0.82f), 0f, 1.8f);
                foreach (int e in new[] { -1, 1 }) Kit("Sword", f, new Vector3(e * 0.9f, 5.6f, 0.84f), 0f, 1.2f);
                for (int i = 0; i < 4; i++)
                {
                    var log = Prim("Log", f, PrimitiveType.Cylinder, new Vector3(-0.6f + 0.4f * i, 0.24f + 0.06f * (i % 2), 0.55f), new Vector3(0.2f, 0.55f, 0.2f), K.furniture);
                    log.transform.localRotation = Quaternion.Euler(0, 15f * (i - 1.5f), 90f);
                    var fl = MeshObj("Flame", f, new Vector3(-0.6f + 0.4f * i, 0.36f, 0.56f), FlameMesh(), K.flame);
                    fl.transform.localScale = new Vector3(0.26f, 0.5f + 0.15f * (i % 2), 0.26f);
                    fl.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                Prim("Embers", f, PrimitiveType.Sphere, new Vector3(0, 0.14f, 0.55f), new Vector3(1.8f, 0.12f, 0.8f), GlowMat("K_Ember", new Color(0.25f, 0.08f, 0.03f), new Color(1.6f, 0.45f, 0.1f)));
                foreach (int e in new[] { -1, 1 }) Rod("Firedog", f, new Vector3(e * 0.8f, 0, 0.95f), new Vector3(e * 0.8f, 0.45f, 0.95f), 0.06f, K.iron);
                Kit("Candelabra", f, new Vector3(-1.9f, 3.72f, 0.7f), 0f);
                Kit("MantelClock", f, new Vector3(0.6f, 3.72f, 0.75f), 0f);
                Kit("BrassVase", f, new Vector3(1.9f, 3.72f, 0.7f), 0f);
                Kit("Goblet2", f, new Vector3(-0.8f, 3.72f, 0.8f), 30f);
                Kit("Armchair", f, new Vector3(-1.5f, 0, 2.9f), 160f);
                Kit("Rug", f, new Vector3(0.3f, 0.02f, 3.1f), 0f, 1.2f);
                var fire = LightGo("HearthLight", LightType.Point, new Color(1f, 0.55f, 0.25f), 3.2f, 10f, true);
                fire.transform.SetParent(f, false); fire.transform.localPosition = new Vector3(0, 1.0f, 1.6f);
            }

            // --- Dev şövalye heykelleri + mangallar (güney uçta merdiven olduğundan 3 sıra) ---
            foreach (int s in new[] { -1, 1 })
                foreach (float z in new[] { -10.5f, 10.5f, 21f })
                {
                    float x = s * (HX - 3.0f);
                    Slab("ColossusPlinth", root, new Vector3(x, 0.75f, z), new Vector3(2.4f, 1.5f, 2.4f), stone, 1.2f);
                    Slab("ColossusPlinthCap", root, new Vector3(x, 1.55f, z), new Vector3(2.6f, 0.12f, 2.6f), stone, 1f, false);
                    Kit("StatueKing", root, new Vector3(x, 1.6f, z), s > 0 ? -90f : 90f, 2.6f);
                    Brazier(root, new Vector3(x - s * 2.2f, 0, z + (z > 0 ? -1.8f : 1.8f)), 1.4f, Mathf.Abs(z) < 15f);
                }

            // --- Zemin: kırmızı halı (altın kenar), işlemeli halkalar ---
            Slab("Carpet", root, new Vector3(0, 0.02f, 4.6f), new Vector3(3.2f, 0.04f, 34.8f), carpet, 2f, false);
            foreach (int s in new[] { -1, 1 }) Slab("CarpetBorder", root, new Vector3(s * 1.55f, 0.045f, 4.6f), new Vector3(0.12f, 0.01f, 34.8f), inlay, 1f, false);
            foreach (var (zc, sc) in new[] { (-6.5f, 0.66f), (14f, 1f) })
            {
                foreach (float rr in new[] { 3.2f, 5.0f, 7.0f }) MeshObj("FloorRing", root, new Vector3(0, 0.006f, zc), RingMesh(rr * sc, 0.12f, 0.012f), inlay);
                for (int k = 0; k < 12; k++)
                    Slab("FloorRay", root, Quaternion.Euler(0, k * 30f + 15f, 0) * Vector3.forward * (5.1f * sc) + new Vector3(0, 0.008f, zc), new Vector3(0.08f, 0.012f, 3.6f * sc), inlay, 1f, false)
                        .transform.localRotation = Quaternion.Euler(0, k * 30f + 15f, 0);
            }

            // --- Avizeler: kuşak kirişlerinden zincirle dört büyük demir avize ---
            foreach (float z in new[] { -21f, -7f, 7f, 21f })
            {
                const float top = 15.5f;
                Rod("ChandChain", root, new Vector3(0, COLLAR - 0.2f, z), new Vector3(0, top, z), 0.07f, K.iron);
                Kit("ChandelierIron", root, new Vector3(0, top, z), 20f, 2.4f);
                var cl = LightGo("ChandelierLight", LightType.Point, Pal.Candle, 5f, 17f, true);
                cl.transform.SetParent(root, false); cl.transform.localPosition = new Vector3(0, top - 4.4f, z);
            }

            // --- Ortada parmaklık: taş direkler, demir bölmeler, iki kanatlı orta kapı (kuzeye açılır) ---
            const float GH = 6.5f, GW = 2.2f;
            foreach (int s in new[] { -1, 1 })
            {
                Slab("GatePier", root, new Vector3(s * (GW + 0.45f), GH * 0.5f + 0.4f, 0), new Vector3(0.9f, GH + 0.8f, 0.9f), stone, 1.2f);
                MeshObj("PierFinial", root, new Vector3(s * (GW + 0.45f), GH + 0.8f, 0), SpireMesh(0.4f, 1.4f, 4), stone);
                float x0 = s * (GW + 0.9f), x1 = s * (HX - 0.1f);
                Slab("FenceBottom", root, new Vector3((x0 + x1) * 0.5f, 0.2f, 0), new Vector3(Mathf.Abs(x1 - x0), 0.4f, 0.35f), stone, 1f, false);
                foreach (float y in new[] { 0.6f, 3.2f, GH - 0.3f }) Slab("FenceRail", root, new Vector3((x0 + x1) * 0.5f, y, 0), new Vector3(Mathf.Abs(x1 - x0), 0.08f, 0.08f), K.iron, 1f, false);
                for (float x = Mathf.Min(x0, x1) + 0.2f; x < Mathf.Max(x0, x1); x += 0.32f)
                {
                    Rod("FenceBar", root, new Vector3(x, 0.4f, 0), new Vector3(x, GH + 0.2f, 0), 0.05f, K.iron);
                    MeshObj("FenceSpike", root, new Vector3(x, GH + 0.2f, 0), SpireMesh(0.06f, 0.3f, 4), K.iron);
                }
                for (float x = Mathf.Min(x0, x1) + 0.5f; x < Mathf.Max(x0, x1); x += 1.3f)
                    MeshObj("FenceScroll", root, new Vector3(x + 0.16f, GH - 0.9f, 0), RingMesh(0.22f, 0.04f, 0.05f), K.iron).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var col = root.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3((x0 + x1) * 0.5f, GH * 0.5f, 0); col.size = new Vector3(Mathf.Abs(x1 - x0), GH + 1f, 0.4f);
            }
            for (int k = 0; k <= 12; k++) // kapı üstü kemer (taç)
            {
                float a = Mathf.PI * k / 12f, b = Mathf.PI * (k + 1) / 12f;
                if (k == 12) break;
                Rod("CrownArc", root, new Vector3(Mathf.Cos(a) * (GW + 0.45f), GH + 0.8f + Mathf.Sin(a) * 1.6f, 0), new Vector3(Mathf.Cos(b) * (GW + 0.45f), GH + 0.8f + Mathf.Sin(b) * 1.6f, 0), 0.08f, K.iron);
                if (k % 2 == 0) Rod("CrownBar", root, new Vector3(Mathf.Cos(a) * (GW + 0.45f), GH + 0.8f, 0), new Vector3(Mathf.Cos(a) * (GW + 0.45f), GH + 0.8f + Mathf.Sin(a) * 1.6f, 0), 0.04f, K.iron);
            }
            Rod("CrownBase", root, new Vector3(-GW - 0.45f, GH + 0.8f, 0), new Vector3(GW + 0.45f, GH + 0.8f, 0), 0.1f, K.iron);
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                var lf = new GameObject(i == 0 ? "GateLeafW" : "GateLeafE").transform;
                lf.SetParent(root, false); lf.localPosition = new Vector3(s * GW, 0, 0);
                foreach (float y in new[] { 0.35f, 3.0f, GH - 0.2f }) Slab("LeafRail", lf, new Vector3(-s * GW * 0.5f, y, 0), new Vector3(GW, 0.09f, 0.09f), K.iron, 1f, false);
                for (float a = 0.15f; a < GW; a += 0.28f)
                {
                    Rod("LeafBar", lf, new Vector3(-s * a, 0.2f, 0), new Vector3(-s * a, GH + 0.2f, 0), 0.05f, K.iron);
                    MeshObj("LeafSpike", lf, new Vector3(-s * a, GH + 0.2f, 0), SpireMesh(0.06f, 0.3f, 4), K.iron);
                }
                MeshObj("LeafRose", lf, new Vector3(-s * GW * 0.5f, 1.7f, 0), RingMesh(0.5f, 0.06f, 0.06f), K.brass).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                MeshObj("LeafRoseIn", lf, new Vector3(-s * GW * 0.5f, 1.7f, 0), RingMesh(0.28f, 0.05f, 0.06f), K.iron).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var c = lf.gameObject.AddComponent<BoxCollider>(); c.center = new Vector3(-s * GW * 0.5f, GH * 0.5f, 0); c.size = new Vector3(GW, GH, 0.25f);
                foreach (var t in lf.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                leaves[i] = lf;
            }

            // --- Kollar: her yarıda bir heykelin yanında (X: güneybatı, O: kuzeydoğu) ---
            var levers = new SyncedLever[2];
            var lpos = new[] { new Vector3(-HX + 5.6f, 0, -10.5f), new Vector3(HX - 5.6f, 0, 10.5f) };
            for (int i = 0; i < 2; i++)
            {
                var lg = new GameObject(i == 0 ? "LeverX" : "LeverO");
                lg.transform.SetParent(root, false); lg.transform.localPosition = lpos[i]; lg.transform.localRotation = Quaternion.Euler(0, i == 0 ? 90f : -90f, 0);
                Slab("LeverBase", lg.transform, new Vector3(0, 0.5f, 0), new Vector3(0.7f, 1.0f, 0.5f), stone, 1f, false);
                Slab("LeverPlate", lg.transform, new Vector3(0, 1.04f, 0), new Vector3(0.6f, 0.08f, 0.4f), K.bronze, 1f, false);
                var handle = new GameObject("Handle").transform;
                handle.SetParent(lg.transform, false); handle.localPosition = new Vector3(0, 1.08f, 0);
                Rod("Shaft", handle, Vector3.zero, new Vector3(0, 0.75f, 0), 0.07f, K.iron);
                Prim("Knob", handle, PrimitiveType.Sphere, new Vector3(0, 0.8f, 0), Vector3.one * 0.15f, K.brass);
                foreach (var t in handle.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                var bc = lg.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.0f, 0); bc.size = new Vector3(0.8f, 2.0f, 0.7f);
                lg.AddComponent<Interactable>().Label = "Kolu çek";
                var lv = lg.AddComponent<SyncedLever>();
                var so = new SerializedObject(lv);
                so.FindProperty("puzzleId").intValue = 8;
                so.FindProperty("nodeId").intValue = 82 + i;
                SetRef(so, "handle", handle);
                so.ApplyModifiedPropertiesWithoutUndo();
                levers[i] = lv;
                Kit("Candlestick", root, lpos[i] + new Vector3(i == 0 ? 0.0f : 0.0f, 1.08f, i == 0 ? 0.25f : -0.25f), 0f);
            }

            // --- Kuzey: mühürlü kapı (titrer), gül pencere, lansetler, basamaklar, iki mühür taşı ---
            ArchTrim(root, "SealArch", new Vector3(0, 0.6f, HZ), Vector3.back, 6.4f, 9.2f, true);
            for (int k = 0; k < 3; k++) Slab("DoorStep", root, new Vector3(0, 0.1f + k * 0.2f, HZ - 2.4f + k * 0.6f), new Vector3(8f - k * 0.8f, 0.2f, 0.6f + (2 - k) * 0.6f), stone, 1f);
            Slab("DoorLanding", root, new Vector3(0, 0.6f, HZ - 0.5f), new Vector3(7f, 0.2f, 1.2f), stone, 1f);
            var door = new GameObject("SealDoor").transform;
            door.SetParent(root, false); door.localPosition = new Vector3(0, 0.7f, HZ - 0.15f);
            foreach (int s in new[] { -1, 1 })
            {
                Slab("DoorLeaf", door, new Vector3(s * 1.4f, 3.6f, 0), new Vector3(2.75f, 7.2f, 0.3f), K.beam, 1.5f, false);
                for (int k = 0; k < 5; k++) Slab("DoorBand", door, new Vector3(s * 1.4f, 0.8f + k * 1.5f, -0.17f), new Vector3(2.6f, 0.14f, 0.04f), K.iron, 1f, false);
                for (int k = 0; k < 6; k++) Prim("DoorStud", door, PrimitiveType.Sphere, new Vector3(s * (0.3f + k * 0.42f), 4.6f, -0.2f), Vector3.one * 0.1f, K.iron);
            }
            var seal = GlowMat("K_SealGlow", new Color(0.35f, 0.28f, 0.18f), new Color(0.9f, 0.55f, 0.2f) * 0.6f);
            foreach (float rr in new[] { 1.1f, 0.75f }) MeshObj("SealRing", door, new Vector3(0, 3.6f, -0.18f), RingMesh(rr, 0.08f, 0.06f), seal).transform.localRotation = Quaternion.Euler(-90, 0, 0);
            for (int k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f;
                Slab("SealRune", door, new Vector3(Mathf.Sin(a) * 0.92f, 3.6f + Mathf.Cos(a) * 0.92f, -0.2f), new Vector3(0.05f, 0.18f, 0.02f), seal, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, -k * 45f);
            }
            Prim("SealCore", door, PrimitiveType.Sphere, new Vector3(0, 3.6f, -0.18f), new Vector3(0.4f, 0.4f, 0.1f), seal);
            foreach (var t in door.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var dcol = door.gameObject.AddComponent<BoxCollider>(); dcol.center = new Vector3(0, 3.6f, 0); dcol.size = new Vector3(5.6f, 7.2f, 0.4f);
            var glowGo = LightGo("SealGlow", LightType.Point, new Color(1f, 0.6f, 0.3f), 0f, 8f, false);
            glowGo.transform.SetParent(root, false); glowGo.transform.localPosition = new Vector3(0, 4.3f, HZ - 1.6f);
            glowGo.GetComponent<Light>().enabled = false;
            Kit("WindowRose", root, new Vector3(0, 13.8f, HZ), 180f, 3.0f);
            foreach (int s in new[] { -1, 1 })
            {
                Kit("WindowLancet", root, new Vector3(s * 7.5f, 9.0f, HZ), 180f, 1.8f);
                Kit("CandelabraFloor", root, new Vector3(s * 3.1f, 0.7f, HZ - 0.6f), 0f);
                Kit("L_Candelabra", root, new Vector3(s * 3.8f, 2.2f, HZ - 1.4f));
                Brazier(root, new Vector3(s * 4.6f, 0, HZ - 5.0f), 1.0f, false);
            }
            var seals = new PressurePlate[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                var go = new GameObject(i == 0 ? "SealStoneX" : "SealStoneO");
                go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(s * 1.7f, 0, HZ - 4.6f);
                Slab("Rim", go.transform, new Vector3(0, 0.02f, 0), new Vector3(1.9f, 0.04f, 1.9f), K.iron, 1f, false);
                var top = Slab("Top", go.transform, new Vector3(0, 0.05f, 0), new Vector3(1.6f, 0.07f, 1.6f), stone, 1f, false);
                top.isStatic = false;
                MeshObj("SealMark", top.transform, new Vector3(0, 0.04f, 0), RingMesh(0.55f, 0.07f, 0.01f), seal);
                var pp = go.AddComponent<PressurePlate>();
                var so = new SerializedObject(pp);
                so.FindProperty("puzzleId").intValue = 8;
                so.FindProperty("nodeId").intValue = 84 + i;
                so.FindProperty("size").vector2Value = new Vector2(1.6f, 1.6f);
                SetFlt(so, "height", 1.2f);
                SetRef(so, "top", top.transform);
                so.ApplyModifiedPropertiesWithoutUndo();
                seals[i] = pp;
            }

            // --- Güney: büyük giriş kemeri (kapalı), lansetler, şamdanlar ---
            ArchTrim(root, "SouthArch", new Vector3(0, 0, -HZ), Vector3.forward, 6f, 8.5f, true);
            Kit("WindowRose", root, new Vector3(0, 13.0f, -HZ), 0f, 2.2f);
            foreach (int s in new[] { -1, 1 })
            {
                Kit("CandelabraFloor", root, new Vector3(s * 3.6f, 0, -HZ + 1.2f), 0f);
                Kit("Banner", root, new Vector3(s * 7.0f, 13f, -HZ + 0.02f), 0f, 2.0f);
                Kit("Armchair", root, new Vector3(s * 6.4f, 0, -HZ + 1.2f), s * -20f);
            }
            Kit("L_Candelabra", root, new Vector3(0, 3.0f, -HZ + 1.6f));

            // --- Kafes asansör varışları: X güney yarı (batı), O kuzey yarı (doğu); parmaklıktan birbirlerini görürler ---
            var cageX = Cage(root, "ArriveCageX", new Vector3(-6.5f, 0, -5.5f), -90f, WH - 2f);
            var cageO = Cage(root, "ArriveCageO", new Vector3(6.5f, 0, 5.5f), 90f, WH - 2f);
            var arriveX = new GameObject("FinalArriveX").transform; arriveX.SetParent(cageX, false); arriveX.localPosition = new Vector3(0, 0.15f, 0);
            arriveX.localRotation = Quaternion.Euler(0, 180f, 0);
            var arriveO = new GameObject("FinalArriveO").transform; arriveO.SetParent(cageO, false); arriveO.localPosition = new Vector3(0, 0.15f, 0);
            arriveO.localRotation = Quaternion.Euler(0, 180f, 0);
            Kit("L_Lantern", root, new Vector3(-4.6f, 0.02f, -4.2f), 0f);
            Kit("Lantern", root, new Vector3(4.6f, 0.02f, 4.2f), 0f);

            // --- X yarısı (güney): serbest duran çift kollu taş merdiven → sahanlık → iki kol galeriye ---
            {
                const float LY = 4.0f, LZ0 = -20f, LZ1 = -23.7f, SZc = -22.6f, SW = 1.8f;
                StoneFlight(root, "GrandFlight", new Vector3(0, 0, -12.5f), 180f, LY, 7.5f, 2.4f, stone, true);
                Slab("Landing", root, new Vector3(0, LY - 0.2f, (LZ0 + LZ1) * 0.5f), new Vector3(5.2f, 0.4f, LZ0 - LZ1), stone, 1.5f);
                foreach (var (x, z) in new[] { (-2.3f, LZ0 - 0.3f), (2.3f, LZ0 - 0.3f), (-2.3f, LZ1 + 0.3f), (2.3f, LZ1 + 0.3f) })
                    Slab("LandingPier", root, new Vector3(x, (LY - 0.4f) * 0.5f, z), new Vector3(0.55f, LY - 0.4f, 0.55f), stone, 1.2f);
                Parapet(root, new Vector3(-2.6f, LY, LZ1 - 0.12f), new Vector3(2.6f, LY, LZ1 - 0.12f), stone);
                foreach (int s in new[] { -1, 1 })
                {
                    Parapet(root, new Vector3(s * 1.32f, LY, LZ0 + 0.12f), new Vector3(s * 2.72f, LY, LZ0 + 0.12f), stone);
                    Parapet(root, new Vector3(s * 2.72f, LY, LZ0), new Vector3(s * 2.72f, LY, SZc + SW * 0.5f + 0.1f), stone);
                    StoneFlight(root, s < 0 ? "GalleryFlightW" : "GalleryFlightE", new Vector3(s * 2.6f, LY, SZc), s * 90f, BAL + 0.15f - LY, HX - 2.4f - 2.6f, SW, stone, false);
                    MeshObj("PostFinial", root, new Vector3(s * 2.72f, LY + 1.1f, LZ0 + 0.12f), SpireMesh(0.16f, 0.5f, 4), K.brass);
                    Kit("CandelabraFloor", root, new Vector3(s * 2.0f, 0, -11.6f), 0f, 1.1f); // merdiven ağzı
                }
                Kit("L_Candelabra", root, new Vector3(-2.0f, 1.9f, -11.6f));
                foreach (int s in new[] { -1, 1 }) Kit("PaintingHall", root, new Vector3(s * 8.2f, 3.4f, -HZ + 0.02f), 0f, 1.4f); // güney duvarı, kolların arkasında
                var lf = LightGo("StairFill", LightType.Point, Pal.Candle, 3.2f, 11f, true);
                lf.transform.SetParent(root, false); lf.transform.localPosition = new Vector3(0, 7.0f, -18f);
            }

            // --- O yarısı (kuzey): yarıda kalmış ziyafet masası (z 5.5 → 19.5) ---
            {
                const float T0 = 5.5f, T1 = 19.5f, TY = 0.84f;
                var tw = KTex("K_FinalTable", "wood_cabinet_worn_long", new Color(0.42f, 0.32f, 0.26f), 0.3f);
                Slab("FeastTop", root, new Vector3(0, TY - 0.05f, (T0 + T1) * 0.5f), new Vector3(1.4f, 0.1f, T1 - T0), tw, 1.5f);
                Slab("FeastApron", root, new Vector3(0, TY - 0.17f, (T0 + T1) * 0.5f), new Vector3(1.2f, 0.14f, T1 - T0 - 0.2f), tw, 1.5f, false);
                Slab("FeastRunner", root, new Vector3(0, TY + 0.003f, (T0 + T1) * 0.5f), new Vector3(0.55f, 0.006f, T1 - T0 + 0.6f), K.velvet, 1f, false);
                foreach (float ze in new[] { T0 - 0.1f, T1 + 0.1f }) Slab("RunnerDrop", root, new Vector3(0, TY - 0.2f, ze + (ze < 10f ? -0.2f : 0.2f)), new Vector3(0.55f, 0.4f, 0.01f), K.velvet, 1f, false);
                for (float z = T0 + 0.6f; z < T1; z += 2.6f)
                    foreach (int e in new[] { -1, 1 }) Slab("Trestle", root, new Vector3(e * 0.45f, (TY - 0.1f) * 0.5f, z), new Vector3(0.14f, TY - 0.1f, 0.6f), tw, 1f, false);
                Slab("Stretcher", root, new Vector3(0, 0.25f, (T0 + T1) * 0.5f), new Vector3(0.1f, 0.1f, T1 - T0 - 1.2f), tw, 1f, false);
                int seat = 0;
                for (float z = T0 + 0.9f; z < T1 - 0.5f; z += 1.6f, seat++)
                    foreach (int e in new[] { -1, 1 })
                    {
                        bool fallen = seat == 0 && e < 0; // detay: kapı inince devrilen sandalye
                        var ch = Kit("ChairWood", root, new Vector3(e * (fallen ? 1.6f : 1.05f), fallen ? 0.22f : 0, z - (fallen ? 0.4f : 0)), e < 0 ? 90f : -90f);
                        if (fallen) ch.transform.localRotation = Quaternion.Euler(0, 70f, 88f);
                        Kit("Plate", root, new Vector3(e * 0.42f, TY, z), seat * 37f);
                        if ((seat + (e > 0 ? 1 : 0)) % 2 == 0) Kit("Goblet" + (seat % 3), root, new Vector3(e * 0.35f, TY, z + 0.32f), seat * 20f);
                        else Kit("Spoon", root, new Vector3(e * 0.5f, TY, z + 0.2f), 90f);
                    }
                Kit("Armchair", root, new Vector3(0, 0, T1 + 0.75f), 180f, 1.15f); // başköşe
                Kit("Armchair", root, new Vector3(0, 0, T0 - 0.75f), 0f, 1.15f);
                foreach (float z in new[] { 7.6f, 12.4f, 17.2f }) { Kit("Candelabra", root, new Vector3(0, TY, z), z * 9f, 1.1f); }
                foreach (float z in new[] { 10.0f, 14.8f }) Kit("CandleCluster", root, new Vector3(0.05f, TY, z), z * 13f);
                Kit("Tray", root, new Vector3(0, TY, 9.0f), 10f);
                Kit("Bowl2", root, new Vector3(-0.1f, TY, 11.4f), 0f);
                Kit("Jug", root, new Vector3(0.18f, TY, 13.4f), 40f);
                Kit("MetalJug", root, new Vector3(-0.2f, TY, 16.0f), 120f);
                Kit("Bowl", root, new Vector3(0.15f, TY, 18.4f), 0f);
                Kit("Bottle1", root, new Vector3(-0.25f, TY, 6.6f), 0f);
                Kit("PaperScatter", root, new Vector3(1.8f, 0.01f, 12.5f), 40f);
                var fl = LightGo("FeastFill", LightType.Point, Pal.Candle, 3.0f, 11f, true);
                fl.transform.SetParent(root, false); fl.transform.localPosition = new Vector3(0, 3.6f, 12.5f);
                // Detay 1: devrilen kadeh + kapının altından X yarısına uzanan şarap izi
                Kit("Goblet1", root, new Vector3(1.3f, 0.05f, 3.2f), 0f).transform.localRotation = Quaternion.Euler(0, 30f, 90f);
                var wine = MakeMat("K_WineStain", new Color(0.16f, 0.02f, 0.03f)); wine.SetFloat("_Smoothness", 0.75f);
                Prim("WineTrail", root, PrimitiveType.Cylinder, new Vector3(1.1f, 0.004f, 1.2f), new Vector3(0.35f, 0.002f, 3.6f), wine);
                Prim("WinePool", root, PrimitiveType.Cylinder, new Vector3(0.95f, 0.005f, -1.0f), new Vector3(0.9f, 0.002f, 0.7f), wine);
            }

            // --- Detay 2: boş tahtlar (biri tacı, diğeri mühürlü mektubu bekliyor) ---
            foreach (int s in new[] { -1, 1 })
            {
                var th = new GameObject("Throne").transform; th.SetParent(root, false);
                th.localPosition = new Vector3(s * 5.2f, 0, HZ - 2.6f); th.localRotation = Quaternion.Euler(0, 180f + s * 12f, 0); // +Z salona
                Slab("Dais", th, new Vector3(0, 0.2f, 0), new Vector3(2.2f, 0.4f, 1.8f), stone, 1f);
                Slab("Seat", th, new Vector3(0, 0.85f, 0.1f), new Vector3(1.1f, 0.5f, 0.85f), K.furniture, 1f);
                Slab("Cushion", th, new Vector3(0, 1.14f, 0.12f), new Vector3(0.95f, 0.1f, 0.75f), K.velvet, 1f, false);
                Slab("Back", th, new Vector3(0, 2.3f, -0.3f), new Vector3(1.1f, 2.8f, 0.16f), K.furniture, 1f, false);
                Slab("BackVelvet", th, new Vector3(0, 2.0f, -0.21f), new Vector3(0.8f, 1.6f, 0.03f), K.velvet, 1f, false);
                foreach (int e in new[] { -1, 1 })
                {
                    Slab("Arm", th, new Vector3(e * 0.55f, 1.45f, 0.1f), new Vector3(0.14f, 0.12f, 0.85f), K.furniture, 1f, false);
                    Slab("ArmPost", th, new Vector3(e * 0.55f, 1.25f, 0.45f), new Vector3(0.12f, 0.35f, 0.12f), K.furniture, 1f, false);
                    MeshObj("Finial", th, new Vector3(e * 0.48f, 3.7f, -0.3f), SpireMesh(0.1f, 0.45f, 4), K.brass);
                }
                MeshObj("Crest", th, new Vector3(0, 3.55f, -0.2f), RingMesh(0.22f, 0.05f, 0.04f), K.brass).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                if (s < 0)
                {
                    MeshObj("Crown", th, new Vector3(0, 1.2f, 0.15f), RingMesh(0.12f, 0.025f, 0.08f), K.brass);
                    for (int k = 0; k < 6; k++)
                        MeshObj("CrownTip", th, new Vector3(Mathf.Sin(k * 1.047f) * 0.12f, 1.27f, 0.15f + Mathf.Cos(k * 1.047f) * 0.12f), SpireMesh(0.02f, 0.07f, 4), K.brass);
                }
                else
                {
                    Letter(th, new Vector3(0, 1.195f, 0.15f), 25f, 16); // Aurel ve Mira'nın son mektubu
                }
                ProbeLit(th.gameObject);
            }
            var tf = LightGo("ThroneFill", LightType.Point, Pal.Candle, 3.0f, 11f, true);
            tf.transform.SetParent(root, false); tf.transform.localPosition = new Vector3(0, 5f, HZ - 5f);
            // Detay 3: perdeyle örtülmüş portre (kuzey duvarı, tahtın üstü) + açık portre
            Kit("PaintingPortrait", root, new Vector3(-9.6f, 4.6f, HZ - 0.02f), 180f, 1.6f);
            Kit("PaintingPortrait", root, new Vector3(9.6f, 4.6f, HZ - 0.02f), 180f, 1.6f);
            Kit("Curtain", root, new Vector3(9.6f, 6.0f, HZ - 0.12f), 180f, 1.2f);
            Kit("Curtain", root, new Vector3(9.0f, 6.0f, HZ - 0.15f), 180f, 1.2f);

            // --- X yarısı dekoru: bitmemiş satranç, ikram dolabı, heykel kaidesi ---
            {
                // Detay 4: kapı inmeden önce yarım kalan satranç (iki koltuk)
                var cp = new Vector3(7.2f, 0, -3.2f);
                Kit("GothicTable", root, cp, 0f);
                Kit("ChessSet", root, cp + new Vector3(0, 0.56f, 0), 10f);
                Kit("Armchair", root, cp + new Vector3(0, 0, -1.1f), 0f);
                Kit("Armchair", root, cp + new Vector3(0, 0, 1.1f), 180f).transform.localRotation = Quaternion.Euler(0, 205f, 0);
                Kit("Candlestick", root, cp + new Vector3(0.35f, 0.56f, 0.2f), 0f);
                Kit("Rug", root, cp + new Vector3(0, 0.02f, 0), 90f, 1.1f);
                foreach (var (x, z, yaw) in new[] { (-HX + 1.6f, -24.5f, 90f), (HX - 1.6f, -24.5f, -90f), (-HX + 1.6f, 24.5f, 90f), (HX - 1.6f, 17.5f, -90f) })
                {
                    Kit("Console", root, new Vector3(x, 0, z), yaw);
                    var d = Quaternion.Euler(0, yaw, 0);
                    Kit("Tray", root, new Vector3(x, 0.85f, z) + d * new Vector3(-0.35f, 0, 0), 0f);
                    Kit("Jug", root, new Vector3(x, 0.85f, z) + d * new Vector3(0.25f, 0, 0.05f), 30f);
                    Kit("Goblet0", root, new Vector3(x, 0.85f, z) + d * new Vector3(0.45f, 0, -0.08f), 0f);
                    Kit("CandleCluster", root, new Vector3(x, 0.85f, z) + d * new Vector3(0.05f, 0, -0.1f), 0f);
                }
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("Pedestal", root, new Vector3(s * 5.6f, 0.55f, -17.5f), new Vector3(0.8f, 1.1f, 0.8f), stone, 1f);
                    Kit("Bust", root, new Vector3(s * 5.6f, 1.1f, -17.5f), s > 0 ? -90f : 90f, 1.2f);
                    Kit("Rug", root, new Vector3(s * 5.6f, 0.02f, -14.2f), 90f, 1.5f);
                    Kit("Rug", root, new Vector3(s * 5.4f, 0.02f, 15.0f), 90f, 1.5f);
                    foreach (float z in new[] { -8f, 4.5f, 18f }) Kit("CandelabraFloor", root, new Vector3(s * 4.2f, 0, z), z * 7f);
                    Slab("Pedestal", root, new Vector3(s * 8.0f, 0.55f, 24.0f), new Vector3(0.8f, 1.1f, 0.8f), stone, 1f);
                    Kit(s < 0 ? "HorseStatue" : "Elephant", root, new Vector3(s * 8.0f, 1.1f, 24.0f), s > 0 ? -90f : 90f);
                }
                Kit("L_Candelabra", root, new Vector3(4.2f, 1.7f, 4.5f));
                Kit("L_Candelabra", root, new Vector3(-4.2f, 1.7f, -8f));
            }

            // --- Detay 5: merdiven altında hizmetkârın saklandığı köşe (yatak, mum, çentikli gün sayımı) ---
            {
                var hp = new Vector3(-4.6f, 0, -25.6f);
                Slab("Bedroll", root, hp + new Vector3(0, 0.06f, 0), new Vector3(1.9f, 0.12f, 0.8f), KTex("K_Bedroll", "velour_velvet", new Color(0.28f, 0.22f, 0.18f), 0.1f), 1f, false).transform.localRotation = Quaternion.Euler(0, 8f, 0);
                Slab("Pillow", root, hp + new Vector3(-0.75f, 0.16f, 0.05f), new Vector3(0.35f, 0.12f, 0.5f), K.paperB, 1f, false);
                Kit("Candle", root, hp + new Vector3(1.2f, 0, 0.3f), 0f);
                Kit("Bowl", root, hp + new Vector3(1.1f, 0, -0.35f), 0f);
                Kit("Crate", root, hp + new Vector3(1.6f, 0, 1.2f), 15f);
                Kit("PaperC", root, hp + new Vector3(0.3f, 0.13f, 0.1f), 50f);
                var chalk = MakeMat("K_Chalk", new Color(0.72f, 0.7f, 0.64f));
                for (int g = 0; g < 4; g++)
                    for (int k = 0; k < 5; k++)
                    {
                        var m = Slab("Tally", root, new Vector3(-5.6f + g * 0.32f + (k < 4 ? k * 0.05f : 0.1f), 0.9f, -HZ + 0.015f), new Vector3(0.015f, k < 4 ? 0.2f : 0.26f, 0.01f), chalk, 1f, false);
                        if (k == 4) m.transform.localRotation = Quaternion.Euler(0, 0, 60f);
                    }
            }
            // Detay 6: galeride kaçan çalgıcıların nota sehpası ve devrik sandalyesi (merdivenin üstü)
            {
                var mp = new Vector3(-HX + 1.2f, BAL + 0.15f, -18.5f);
                Rod("MusicStand", root, mp, mp + Vector3.up * 1.1f, 0.03f, K.iron);
                Slab("StandDesk", root, mp + new Vector3(0, 1.15f, 0), new Vector3(0.4f, 0.3f, 0.02f), K.iron, 1f, false).transform.localRotation = Quaternion.Euler(-20, 90, 0);
                Kit("ChairWood", root, mp + new Vector3(0.1f, 0.2f, 1.0f), 0f).transform.localRotation = Quaternion.Euler(0, 40f, 85f);
                Kit("PaperScatter", root, mp + new Vector3(0, 0.01f, -1.2f), 70f);
                Kit("PaperScatter", root, new Vector3(-4.0f, 0.01f, -14.0f), 160f); // galeriden uçmuş notalar
            }
            // Galeri altı ve galeri ışıkları (bake dolgu)
            foreach (int s in new[] { -1, 1 })
                foreach (float z in new[] { -14f, 14f })
                {
                    var g = LightGo("AisleFill", LightType.Point, Pal.Candle, 2.4f, 10f, true);
                    g.transform.SetParent(root, false); g.transform.localPosition = new Vector3(s * (HX - 2.0f), 5.0f, z);
                    var u = LightGo("GalleryFill", LightType.Point, Pal.Candle, 1.8f, 8f, true);
                    u.transform.SetParent(root, false); u.transform.localPosition = new Vector3(s * (HX - 1.2f), BAL + 2.6f, z * 0.5f);
                }
            foreach (var (x, y, z, yaw) in new[] { (-HX + 0.4f, 6.2f, 8.4f, 90f), (HX - 0.4f, 6.2f, -8.4f, -90f), (-HX + 0.4f, 14.5f, -25f, 90f), (HX - 0.4f, 14.5f, 25f, -90f), (-2f, 10f, HZ - 0.4f, 180f) })
                KitR("Cobweb", root, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 180f), 2.2f);

            var fh = root.gameObject.AddComponent<FinalHall>();
            var fso = new SerializedObject(fh);
            fso.FindProperty("puzzleId").intValue = 8;
            fso.FindProperty("nodeId").intValue = 81;
            SetRefArray(fso, "levers", levers);
            SetRefArray(fso, "seals", seals);
            SetRefArray(fso, "gateLeaves", leaves);
            SetRef(fso, "sealDoor", door);
            SetRef(fso, "doorGlow", glowGo.GetComponent<Light>());
            SetRef(fso, "titleFont", U.head);
            SetRef(fso, "bodyFont", U.body);
            fso.FindProperty("hallCenter").vector3Value = root.TransformPoint(new Vector3(0, 6f, 0));
            fso.FindProperty("hallSize").vector3Value = new Vector3(HX * 2f + 2f, 20f, HZ * 2f + 2f);
            fso.ApplyModifiedPropertiesWithoutUndo();

            // Asansörler (bulmaca 7 çözülünce): çan dairesi / kripta kafesleri → final varışları
            var chamber = roomX.Find("RoomA7_CanDairesi");
            var cx = chamber != null ? chamber.Find("LiftCageX") : null;
            if (cx != null) MakePassage("PassageX8", chamber, 79, cx.TransformPoint(new Vector3(0, 1f, 0)), new Vector3(1.8f, 2f, 1.8f), arriveX, 7);
            var crypt = roomO.Find("RoomB7_KemikOrg");
            var co = crypt != null ? crypt.Find("LiftCageO") : null;
            if (co != null) MakePassage("PassageO8", crypt, 80, co.TransformPoint(new Vector3(0, 1f, 0)), new Vector3(1.8f, 2f, 1.8f), arriveO, 7);
            MakeCheckpoint(root, 8, Roles.X, arriveX.position);
            MakeCheckpoint(root, 8, Roles.O, arriveO.position);

            KitProbes(root, new Vector3(0, 11f, 0), new Vector3(HX * 2f - 1f, 21.6f, HZ * 2f - 1f), 7, 6, 13,
                new Bounds(new Vector3(0, 2f, -16.25f), new Vector3(2.4f, 4f, 7.5f)), new Bounds(new Vector3(0, 3.8f, -21.85f), new Vector3(5.2f, 0.4f, 3.7f)));
            KitReflection(root, new Vector3(0, 7f, -14f), new Vector3(HX * 2f, 16f, HZ));
            KitReflection(root, new Vector3(0, 7f, 14f), new Vector3(HX * 2f, 16f, HZ));
        }

        /// <summary>Taş merdiven kolu: bottom'dan yerel +z (yaw) yönünde yükselir. solid: basamaklar zemine kadar dolu; değilse alt yüzü eğimli taş.
        /// İki yanda 1 m taş korkuluk (çarpışmalı), uçlarda baba dikmeleri; yürüme yüzeyi görünmez rampa.</summary>
        static void StoneFlight(Transform root, string name, Vector3 bottom, float yaw, float rise, float run, float w, Material mat, bool solid)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false); t.localPosition = bottom; t.localRotation = Quaternion.Euler(0, yaw, 0);
            int n = Mathf.Max(4, Mathf.RoundToInt(rise / 0.18f));
            float sr = run / n, sh = rise / n, ang = Mathf.Atan2(rise, run), hyp = Mathf.Sqrt(run * run + rise * rise);
            for (int i = 0; i < n; i++)
            {
                float top = sh * (i + 1);
                if (solid) Slab("Step", t, new Vector3(0, top * 0.5f, sr * (i + 0.5f)), new Vector3(w, top, sr + 0.01f), mat, 1.2f, true);
                else Slab("Step", t, new Vector3(0, top - sh * 0.5f - 0.05f, sr * (i + 0.5f)), new Vector3(w, sh + 0.1f, sr + 0.01f), mat, 1.2f, false);
            }
            if (!solid)
            {
                var so = Slab("Soffit", t, new Vector3(0, rise * 0.5f - 0.35f * Mathf.Cos(ang), run * 0.5f + 0.35f * Mathf.Sin(ang)), new Vector3(w + 0.1f, 0.4f, hyp), mat, 1.5f, false);
                so.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
            }
            var ramp = Slab("Ramp", t, Vector3.zero, new Vector3(w, 0.3f, hyp + 0.3f), mat, 1f);
            ramp.GetComponent<MeshRenderer>().enabled = false;
            GameObjectUtility.SetStaticEditorFlags(ramp, 0);
            ramp.transform.localPosition = new Vector3(0, rise * 0.5f - 0.15f * Mathf.Cos(ang), run * 0.5f + 0.15f * Mathf.Sin(ang));
            ramp.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (w * 0.5f + 0.12f);
                Parapet(t, new Vector3(x, 0, 0), new Vector3(x, rise, run), mat);
                foreach (var p in new[] { new Vector3(x, 0, 0), new Vector3(x, rise, run) })
                {
                    Slab("Newel", t, p + Vector3.up * 0.6f, new Vector3(0.4f, 1.2f, 0.4f), mat, 1f);
                    MeshObj("NewelFinial", t, p + Vector3.up * 1.2f, SpireMesh(0.17f, 0.45f, 4), K.brass);
                }
            }
        }

        /// <summary>a→b boyunca 1 m taş korkuluk (eğimli olabilir) + kapak taşı; çarpışmalı.</summary>
        static void Parapet(Transform parent, Vector3 a, Vector3 b, Material mat)
        {
            var d = b - a; float len = d.magnitude;
            var flat = new Vector3(d.x, 0, d.z).normalized;
            var rot = Quaternion.LookRotation(flat, Vector3.up) * Quaternion.Euler(-Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg, 0, 0);
            var p = Slab("Parapet", parent, (a + b) * 0.5f + Vector3.up * 0.5f, new Vector3(0.24f, 1.0f, len), mat, 1.5f);
            p.transform.localRotation = rot;
            var c = Slab("Coping", parent, (a + b) * 0.5f + Vector3.up * 1.03f, new Vector3(0.34f, 0.08f, len + 0.1f), mat, 1.5f, false);
            c.transform.localRotation = rot;
        }
    }
}
