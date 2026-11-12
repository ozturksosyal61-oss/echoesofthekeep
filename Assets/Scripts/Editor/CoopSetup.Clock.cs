using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// X'in 2. ve 3. odaları, Art kit ile (ref: Docs/Reference/saat.jpg, hazne.jpg). Lobi tarzı: lambri + sıva + kirişli tavan,
    /// gotik/tedirgin dokunuşlar (asılı çan, portre, mum kümeleri, ağlar). Baked; oda başına en fazla 2 realtime fener.
    /// Saat Odası v2 kökü: kule yerel (-18,-4,0); iç x -18..1.5, z -5..10, h 7.5, kasetli tavan. x=-9.5'te dev saat kadranlı bölme: doğuda atölye,
    /// batı odacığında bulmaca 2 (BuildPuzzle2, ClkXW duvarı; girişten görünmez). Doğu girişi, güney Hazne kapısı (x -0.5).
    /// Hazne kökü: saat yerel (-0.5,-3,-17); iç ±5, h 5. Kuzey merdiven ağzı, güneyde gösterge + önünde sarnıç kuyusu, batıda tablo.
    /// </summary>
    public static partial class CoopSetup
    {
        // Saat odası v2 (saat yerel): doğu girişi (kule merdiveni) ve güney Hazne kapısı sabit; oda batıya ve kuzeye büyür.
        const float ClkXW = -18f, ClkXE = 1.5f, ClkZS = -5f, ClkZN = 10f, ClkPart = -9.5f;

        /// <summary>Kasetli ahşap tavan: iki yönde kiriş ızgarası + koyu panolar (kuledeki tek yönlü kirişlerden farklı).</summary>
        static void CofferCeiling(Transform root, Vector3 center, float sx, float sz, float cell)
        {
            var dark = MakeMat("K_Coffer", new Color(0.16f, 0.12f, 0.1f)); dark.SetFloat("_Smoothness", 0.2f);
            Deck(root, "CofferBack", center + Vector3.up * 0.12f, sx, sz, 0.12f, dark, 2, 2);
            var g = new GameObject("CofferGrid").transform; g.SetParent(root, false);
            for (float x = -sx * 0.5f; x <= sx * 0.5f + 0.01f; x += cell) Slab("RibZ", g, center + new Vector3(x, -0.12f, 0), new Vector3(0.22f, 0.3f, sz), K.beam, 1.2f, false);
            for (float z = -sz * 0.5f; z <= sz * 0.5f + 0.01f; z += cell) Slab("RibX", g, center + new Vector3(0, -0.12f, z), new Vector3(sx, 0.3f, 0.22f), K.beam, 1.2f, false);
            for (float x = -sx * 0.5f + cell * 0.5f; x < sx * 0.5f; x += cell)
                for (float z = -sz * 0.5f + cell * 0.5f; z < sz * 0.5f; z += cell)
                    Prim("Boss", g, PrimitiveType.Sphere, center + new Vector3(x - cell * 0.5f, -0.28f, z - cell * 0.5f), new Vector3(0.18f, 0.1f, 0.18f), K.brass);
            CombineChildren(g, "clock_coffer_" + F(sx) + "_" + F(sz), K.beam);
        }

        static void BuildClockShell(Transform root, float h)
        {
            const float x0 = ClkXW, x1 = ClkXE, z0 = ClkZS, z1 = ClkZN, px = ClkPart;
            float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, W = x1 - x0, D = z1 - z0;
            var wallM = KTex("K_ClockWall", "worn_cracked_plaster", new Color(0.72f, 0.68f, 0.62f), 0.2f, 1.2f);
            var floorM = KTex("K_ClockFloor", "stone_floor", new Color(0.55f, 0.55f, 0.57f), 0.35f, 1.2f);
            var chalk = MakeMat("K_Chalk", new Color(0.78f, 0.76f, 0.7f));
            var faceM = KTex("K_ClockFace", "worn_cracked_plaster", new Color(0.62f, 0.56f, 0.44f), 0.3f, 0.6f); // eskimiş, çatlak mine
            var det = new GameObject("Details").transform; det.SetParent(root, false);
            var entry = new Opening(0f - cz, 3f, 0f, 3f);
            var hazneDoor = new Opening(-0.5f - cx, 3f, 0f, 3f);

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(cx, -0.25f, cz), W + 1f, D + 1f, 0.5f, floorM, 3, 2);
            Wall(root, "WallW", new Vector3(x0 - 0.25f, 0, cz), false, D + 1f, h, wallM, 2.5f);
            Wall(root, "WallE", new Vector3(x1 + 0.25f, 0, cz), false, D + 1f, h, wallM, 2.5f, entry);
            Slab("StairHeader", root, new Vector3(1.75f, 5.75f, 0), new Vector3(0.5f, 2.5f, 4f), wallM, 2f, false);
            Wall(root, "WallN", new Vector3(cx, 0, z1 + 0.25f), true, W + 1f, h, wallM, 2.5f);
            Wall(root, "WallS", new Vector3(cx, 0, z0 - 0.25f), true, W + 1f, h, wallM, 2.5f, hazneDoor);
            CofferCeiling(root, new Vector3(cx, h, cz), W, D, 2.2f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(cx, h + 0.6f, cz), new Vector3(W + 1.5f, 0.6f, D + 1.5f), K.beam, 2f, false), 0);
            Wainscot(root, "WainN", new Vector3(cx, 0, z1), true, W, Vector3.back, 1.6f);
            Wainscot(root, "WainS", new Vector3(cx, 0, z0), true, W, Vector3.forward, 1.6f, hazneDoor);
            Wainscot(root, "WainE", new Vector3(x1, 0, cz), false, D, Vector3.left, 1.6f, entry);
            Wainscot(root, "WainW", new Vector3(x0, 0, cz), false, D, Vector3.right, 1.4f);
            foreach (int s in new[] { -1, 1 })
            {
                Slab("EntryJamb", root, new Vector3(x1 - 0.08f, 1.55f, s * 1.6f), new Vector3(0.16f, 3.1f, 0.22f), K.beam, 1.2f, false);
                Slab("HazneJamb", root, new Vector3(-0.5f + s * 1.6f, 1.55f, z0 + 0.08f), new Vector3(0.22f, 3.1f, 0.16f), K.beam, 1.2f, false);
            }
            Slab("EntryLintel", root, new Vector3(x1 - 0.08f, 3.12f, 0), new Vector3(0.16f, 0.24f, 3.42f), K.beam, 1.2f, false);
            Slab("HazneLintel", root, new Vector3(-0.5f, 3.12f, z0 + 0.08f), new Vector3(3.42f, 0.24f, 0.16f), K.beam, 1.2f, false);
            foreach (float x in new[] { x0 + 0.18f, x1 - 0.18f })
                foreach (float z in new[] { z0 + 0.18f, z1 - 0.18f })
                    Slab("Post", root, new Vector3(x, h * 0.5f, z), new Vector3(0.32f, h, 0.32f), K.beam, 1.2f, false);

            // --- Kahraman: odayı bölen dev saat kadranı (doğuya bakar); iki yan geçit ---
            {
                const float fy = 3.9f, fr = 2.6f;
                var pass = new[] { new Opening(-3.4f - cz, 1.9f, 0f, 2.9f), new Opening(8.6f - cz, 1.9f, 0f, 2.9f) };
                Wall(root, "Partition", new Vector3(px - 0.25f, 0, cz), false, D, h, K.panel, 2.2f, pass);
                foreach (var o in new[] { -3.4f, 8.6f })
                {
                    ArchTrim(root, "PassArch", new Vector3(px, 0, o), Vector3.right, 2.1f, 3.4f, false);
                    ArchTrim(root, "PassArchW", new Vector3(px - 0.5f, 0, o), Vector3.left, 2.1f, 3.4f, false);
                }
                var face = new GameObject("GreatClock").transform; face.SetParent(root, false); face.localPosition = new Vector3(px + 0.06f, fy, 2.6f);
                face.localRotation = Quaternion.Euler(0, -90, 0); // ön yüz (-Z) doğuya (+x)
                Prim("Dial", face, PrimitiveType.Cylinder, Vector3.zero, new Vector3(fr * 2f, 0.04f, fr * 2f), faceM).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("Bezel", face, new Vector3(0, 0, -0.04f), RingMesh(fr + 0.05f, 0.18f, 0.12f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("InnerRing", face, new Vector3(0, 0, -0.03f), RingMesh(fr * 0.72f, 0.04f, 0.02f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("ChapterRing", face, new Vector3(0, 0, -0.035f), RingMesh(fr * 0.97f, 0.06f, 0.03f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("CenterRing", face, new Vector3(0, 0, -0.035f), RingMesh(fr * 0.2f, 0.05f, 0.03f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int k = 0; k < 60; k++) // dakika noktaları
                {
                    if (k % 5 == 0) continue;
                    float a2 = k * 6f * Mathf.Deg2Rad;
                    Prim("MinDot", face, PrimitiveType.Cylinder, new Vector3(Mathf.Sin(a2), Mathf.Cos(a2), 0) * (fr * 0.9f) + new Vector3(0, 0, -0.04f), new Vector3(0.04f, 0.005f, 0.04f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                for (int k = 0; k < 12; k++)
                {
                    float a = k * 30f * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0);
                    Rod("Tick", face, d * (fr * 0.78f) + new Vector3(0, 0, -0.04f), d * (fr * (k % 3 == 0 ? 0.95f : 0.9f)) + new Vector3(0, 0, -0.04f), k % 3 == 0 ? 0.12f : 0.06f, K.iron);
                }
                // Akrep/yelkovan: 3'ü 17 geçe durmuş (Detay 1: odadaki tüm saatler aynı anda)
                var hm = Quaternion.Euler(0, 0, -(3f + 17f / 60f) * 30f) * Vector3.up; var mm = Quaternion.Euler(0, 0, -17f * 6f) * Vector3.up;
                Rod("HourHand", face, new Vector3(0, 0, -0.1f), hm * (fr * 0.5f) + new Vector3(0, 0, -0.1f), 0.12f, K.iron);
                Rod("MinuteHand", face, new Vector3(0, 0, -0.13f), mm * (fr * 0.82f) + new Vector3(0, 0, -0.13f), 0.08f, K.iron);
                Prim("Hub", face, PrimitiveType.Sphere, new Vector3(0, 0, -0.15f), Vector3.one * 0.26f, K.brass);
                // Kadranın arkası (batı odacığı): mekanizma + durmuş sarkaç (Detay 3: kopmuş ağırlık yerde)
                var back = new GameObject("ClockWorks").transform; back.SetParent(root, false);
                var gs = new[] { new Vector4(-0.6f, 4.6f, 1.0f, 18), new Vector4(0.9f, 3.4f, 0.7f, 12), new Vector4(-1.4f, 2.9f, 0.55f, 10), new Vector4(1.6f, 5.2f, 0.5f, 10), new Vector4(0.3f, 6.1f, 0.6f, 12) };
                for (int i = 0; i < gs.Length; i++)
                    MeshObj("Gear" + i, back, new Vector3(px - 0.62f - (i % 2) * 0.12f, gs[i].y, 2.6f + gs[i].x), GearMesh((int)gs[i].w, gs[i].z, 0.12f, gs[i].z * 0.2f, 0.1f), i % 2 == 0 ? K.bronze : K.iron)
                        .transform.localRotation = Quaternion.Euler(0, 90, i * 33f);
                Rod("PendulumRod", back, new Vector3(px - 1.0f, 6.4f, 2.6f), new Vector3(px - 1.0f, 1.3f, 2.6f), 0.05f, K.brass);
                Prim("PendulumHook", back, PrimitiveType.Sphere, new Vector3(px - 1.0f, 1.3f, 2.6f), Vector3.one * 0.12f, K.brass);
                Prim("FallenBob", back, PrimitiveType.Cylinder, new Vector3(px - 1.6f, 0.1f, 1.6f), new Vector3(0.7f, 0.08f, 0.7f), K.brass).transform.localRotation = Quaternion.Euler(80, 30, 0);
                Slab("Dent", back, new Vector3(px - 1.6f, 0.004f, 1.45f), new Vector3(0.5f, 0.006f, 0.3f), MakeMat("K_Dent", new Color(0.1f, 0.09f, 0.08f)), 1f, false);
                ProbeLit(back.gameObject);
                ProbeLit(face.gameObject);
            }

            // --- Batı odacığı (bulmaca 2 duvarı, BuildPuzzle2): şamdanlar, mum kümesi, sandık ---
            foreach (int s in new[] { -1, 1 })
            {
                Kit("CandelabraFloor", root, new Vector3(x0 + 0.55f, 0, s * 3.7f), 90f);
                Kit("L_Candelabra", root, new Vector3(x0 + 0.9f, 1.9f, s * 3.4f));
            }
            Kit("L_Candelabra", root, new Vector3(x0 + 2.1f, 2.6f, 0)); // kadranlar okunsun
            Kit("CandleCluster", root, new Vector3(x0 + 0.35f, 0, -2.6f));
            Kit("Chest", root, new Vector3(x0 + 2.2f, 0, z1 - 0.7f), 180f);
            Books(root, new Vector3(x0 + 0.4f, 0, 6.6f), 90f);
            Books(root, new Vector3(x0 + 0.4f, 0, 8.6f), 90f);
            Kit("L_Lantern", root, new Vector3(x0 + 4.5f, 0, 7.6f), 10f);
            Kit("Lantern", root, new Vector3(x0 + 4.5f, 0, 7.6f), 10f);
            Kit("Ladder", root, new Vector3(x0 + 0.9f, 0, 4.9f), 90f);
            KitR("PaintingPortrait", root, new Vector3(x0 + 0.04f, 3.6f, 7.6f), Quaternion.Euler(0, 90, 0), 1.1f);
            // Odacık: duvara dayalı yedek kadranlar, parça sandıkları, onarım masası
            foreach (var (pz, r, tilt) in new[] { (z0 + 1.2f, 0.7f, -12f), (z0 + 2.4f, 0.5f, -8f) })
            {
                var sf = Prim("SpareFace", root, PrimitiveType.Cylinder, new Vector3(px - 0.75f, r + 0.02f, pz), new Vector3(r * 2f, 0.03f, r * 2f), MakeMat("K_SpareFace", new Color(0.55f, 0.5f, 0.42f)));
                sf.transform.localRotation = Quaternion.Euler(0, 0, 90f + tilt);
                MeshObj("SpareBezel", root, new Vector3(px - 0.73f, r + 0.02f, pz), RingMesh(r, 0.06f, 0.05f), K.brass).transform.localRotation = Quaternion.Euler(0, 0, 90f + tilt);
            }
            Kit("Crate", root, new Vector3(px - 1.0f, 0, 8.0f), 15f, 0.8f);
            Kit("Crate", root, new Vector3(px - 1.1f, 0.48f, 8.05f), 40f, 0.65f);
            Kit("Table", root, new Vector3(x0 + 3.6f, 0, z0 + 0.8f), 0f);
            MeshObj("RepairGear", det, new Vector3(x0 + 3.3f, 0.86f, z0 + 0.8f), GearMesh(16, 0.3f, 0.07f, 0.07f, 0.04f), K.brass).transform.localRotation = Quaternion.Euler(90, 10, 0);
            Kit("MantelClock", root, new Vector3(x0 + 4.0f, 0.82f, z0 + 0.8f), 160f);
            Kit("Magnifier", root, new Vector3(x0 + 3.0f, 0.82f, z0 + 0.65f), 30f);
            Kit("Candlestick", root, new Vector3(x0 + 4.3f, 0.82f, z0 + 0.6f));
            Kit("Stool", root, new Vector3(x0 + 3.6f, 0, z0 + 1.7f), 10f);
            Kit("Barrel", root, new Vector3(x0 + 0.7f, 0, z0 + 0.7f), 30f);
            Kit("PaperScatter", root, new Vector3(x0 + 5.2f, 0, 3.2f), 30f);
            var af = LightGo("AlcoveFill", LightType.Point, Pal.Candle, 2.2f, 7f, true);
            af.transform.SetParent(root, false); af.transform.localPosition = new Vector3(px - 1.6f, 4.5f, 2.6f); // arka mekanizma seçilsin

            // --- Güney: vitray (ay), takvim duvarı (Detay 4: işaretlenmiş günler), çalışma masası ---
            KitR("WindowLancet", root, new Vector3(-6.4f, 2.4f, z0), Quaternion.identity, 1.4f);
            KitR("WindowLancet", root, new Vector3(-13.6f, 2.4f, z0), Quaternion.identity, 1.2f);
            Kit("StudyTable", root, new Vector3(-4.0f, 0, z0 + 0.65f), 0f);
            Kit("Stool", root, new Vector3(-3.8f, 0, z0 + 1.6f), 35f);
            Kit("L_Candelabra", root, new Vector3(-3.7f, 1.75f, z0 + 1.1f));
            for (int k = 0; k < 6; k++)
            {
                var pp = new Vector3(-8.7f + (k % 3) * 0.42f, 1.9f - (k / 3) * 0.5f, z0 + 0.03f);
                Slab("CalendarPage", det, pp, new Vector3(0.34f, 0.42f, 0.006f), k % 2 == 0 ? K.paperA : K.paperB, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, (k - 2.5f) * 2f);
                for (int m = 0; m < 2 + k % 3; m++)
                {
                    var c = pp + new Vector3(-0.1f + m * 0.07f, 0.08f - m * 0.06f, 0.006f);
                    Slab("CrossA", det, c, new Vector3(0.06f, 0.008f, 0.004f), chalk, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, 45);
                    Slab("CrossB", det, c, new Vector3(0.06f, 0.008f, 0.004f), chalk, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, -45);
                }
            }

            // --- Kuzey: durmuş saatler galerisi (hepsi 3:17) + çırak yatağı (Detay 2: altında saklı günlük) ---
            for (int k = 0; k < 5; k++)
            {
                float x = -8.2f + k * 1.6f;
                Kit("GrandfatherClock", root, new Vector3(x, 0, z1 - 0.38f), 180f, 0.9f + (k % 2) * 0.12f);
            }
            foreach (var x in new[] { -7.4f, -4.2f, -1.0f })
            {
                Slab("ClockShelf", root, new Vector3(x, 2.6f, z1 - 0.2f), new Vector3(1.0f, 0.06f, 0.36f), K.beam, 1f, false);
                Kit("MantelClock", root, new Vector3(x, 2.63f, z1 - 0.22f), 180f);
            }
            {
                var bp = new Vector3(x1 - 1.3f, 0, z1 - 1.4f);
                Slab("CotFrame", root, bp + new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.1f, 2.0f), K.beam, 1f);
                foreach (var o in new[] { new Vector3(-0.4f, 0.12f, -0.9f), new Vector3(0.4f, 0.12f, -0.9f), new Vector3(-0.4f, 0.12f, 0.9f), new Vector3(0.4f, 0.12f, 0.9f) })
                    Slab("CotLeg", root, bp + o, new Vector3(0.08f, 0.25f, 0.08f), K.beam, 1f, false);
                Slab("Blanket", root, bp + new Vector3(0, 0.34f, -0.15f), new Vector3(0.85f, 0.08f, 1.5f), K.velvet, 1f, false);
                Slab("Pillow", root, bp + new Vector3(0, 0.36f, 0.75f), new Vector3(0.6f, 0.1f, 0.3f), K.paperB, 1f, false);
                Slab("HiddenDiary", root, bp + new Vector3(0.15f, 0.02f, 0.2f), new Vector3(0.18f, 0.04f, 0.24f), K.leatherRed, 1f, false).transform.localRotation = Quaternion.Euler(0, 20, 0);
                Kit("Candle", root, bp + new Vector3(-0.65f, 0, 0.8f), 0f, 0.8f);
                Kit("Bucket", root, bp + new Vector3(0.75f, 0, -0.7f));
            }

            // --- Doğu: tezgah + fener, alet panosu, raflarda şişe/dişli; Detay 5: ölü kanaryanın kafesi ---
            Kit("Workbench", root, new Vector3(x1 - 0.7f, 0, 4.4f), -90f);
            Kit("L_Lantern", root, new Vector3(x1 - 0.55f, 0.82f, 5.3f));
            Slab("ToolBoard", root, new Vector3(x1 - 0.07f, 1.45f, 4.3f), new Vector3(0.03f, 0.7f, 1.8f), K.furniture, 1.2f, false);
            var onWallE = Quaternion.Euler(0, 0, 90);
            KitR("Wrench", root, new Vector3(x1 - 0.1f, 1.5f, 3.75f), onWallE * Quaternion.Euler(0, 15, 0));
            KitR("Hammer", root, new Vector3(x1 - 0.1f, 1.47f, 4.3f), onWallE * Quaternion.Euler(0, -10, 0));
            KitR("Mallet", root, new Vector3(x1 - 0.1f, 1.47f, 4.85f), onWallE * Quaternion.Euler(0, 8, 0));
            KitR("PaperA", root, new Vector3(x1 - 0.07f, 1.55f, 3.0f), onWallE * Quaternion.Euler(0, 6, 0));
            foreach (float y in new[] { 2.25f, 2.85f })
            {
                Slab("Shelf", root, new Vector3(x1 - 0.16f, y, 4.3f), new Vector3(0.3f, 0.05f, 1.8f), K.beam, 1f, false);
                foreach (float z in new[] { 3.55f, 5.05f }) Rod("ShelfBracket", root, new Vector3(x1 - 0.02f, y - 0.22f, z), new Vector3(x1 - 0.26f, y - 0.03f, z), 0.03f, K.iron);
            }
            Kit("Bottle0", root, new Vector3(x1 - 0.16f, 2.28f, 3.6f));
            Kit("Bottle2", root, new Vector3(x1 - 0.16f, 2.28f, 3.9f));
            Kit("Bottle1", root, new Vector3(x1 - 0.18f, 2.88f, 4.9f));
            Kit("BookStack", root, new Vector3(x1 - 0.17f, 2.28f, 4.8f), 80f, 0.6f);
            MeshObj("GearS0", det, new Vector3(x1 - 0.16f, 2.29f, 4.35f), GearMesh(10, 0.2f, 0.06f, 0.05f, 0.05f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
            MeshObj("GearS1", det, new Vector3(x1 - 0.1f, 3.1f, 4.0f), GearMesh(12, 0.24f, 0.06f, 0.06f, 0.05f), K.bronze).transform.localRotation = Quaternion.Euler(-12, 90, 0);
            Kit("Stool", root, new Vector3(0.1f, 0, 4.0f), -30f);
            {
                var cg = new Vector3(-6.4f, 3.1f, z0 + 1.0f); // vitray önünde asılı kafes
                Rod("CageHook", det, cg + Vector3.up * 0.9f, new Vector3(cg.x, h - 0.3f, cg.z), 0.02f, K.iron);
                for (int k = 0; k < 10; k++)
                {
                    float a = k * 36f * Mathf.Deg2Rad;
                    Rod("CageBar", det, cg + new Vector3(Mathf.Cos(a) * 0.22f, -0.35f, Mathf.Sin(a) * 0.22f), cg + new Vector3(Mathf.Cos(a) * 0.12f, 0.35f, Mathf.Sin(a) * 0.12f), 0.012f, K.brass);
                }
                Prim("CageFloor", det, PrimitiveType.Cylinder, cg + Vector3.down * 0.36f, new Vector3(0.46f, 0.02f, 0.46f), K.brass);
                Prim("Canary", det, PrimitiveType.Sphere, cg + new Vector3(0.05f, -0.32f, 0), new Vector3(0.09f, 0.05f, 0.06f), MakeMat("K_Canary", new Color(0.62f, 0.55f, 0.2f)));
            }
            KitR("PaintingKeep", root, new Vector3(x1 - 0.04f, 3.4f, -2.8f), Quaternion.Euler(0, -90, 0), 1.2f);
            Kit("Commode", root, new Vector3(x1 - 0.42f, 0, -3.1f), -90f, 0.9f);
            Kit("Bust", root, new Vector3(x1 - 0.42f, 0.86f, -2.85f), -110f, 0.7f);
            Kit("Candlestick", root, new Vector3(x1 - 0.4f, 0.86f, -3.45f));
            Kit("L_Candle", root, new Vector3(x1 - 0.7f, 1.6f, -3.4f));

            // --- Orta (doğu bölüm): halı, iki avize, yer dağınıklığı; ağlar ---
            Kit("Rug", root, new Vector3(-4.2f, 0, 2.6f), 90f, 1.2f);
            Kit("RoundTable", root, new Vector3(-4.2f, 0, 2.6f), 0f);
            Kit("TeaSet", root, new Vector3(-4.2f, 0.75f, 2.6f), 30f);
            Kit("ChairWood", root, new Vector3(-3.2f, 0, 2.8f), -80f);
            foreach (float z in new[] { -1.0f, 6.2f })
            {
                Kit("ChandelierIron", root, new Vector3(-4.5f, h - 1.4f, z), 0f, 1.1f);
                Rod("ChandChain", root, new Vector3(-4.5f, h - 0.9f, z), new Vector3(-4.5f, h - 0.1f, z), 0.03f, K.iron);
                Kit("L_Chandelier", root, new Vector3(-4.5f, h - 2.2f, z));
            }
            Kit("PaperScatter", root, new Vector3(-2.4f, 0, -1.6f), 50f);
            Kit("PaperScatter", root, new Vector3(-13.2f, 0, 1.0f), 150f);
            Kit("CrateStack", root, new Vector3(-8.0f, 0, z0 + 0.9f), 0f, 0.8f);
            KitR("Cobweb", root, new Vector3(x0 + 0.03f, h - 0.2f, z0 + 0.03f), Quaternion.Euler(0, 180, 180), 1.8f);
            KitR("Cobweb", root, new Vector3(x1 - 0.03f, h - 0.2f, z1 - 0.03f), Quaternion.Euler(0, 0, 180), 1.6f);
            KitR("Cobweb", root, new Vector3(x0 + 0.03f, h - 0.2f, z1 - 0.03f), Quaternion.Euler(0, 0, -90), 1.8f);
            KitR("Cobweb", root, new Vector3(px - 0.55f, 0, z0 + 0.03f), Quaternion.Euler(0, 180, 0), 1.0f);
            foreach (var p in new[] { new Vector3(-4.5f, 4.2f, 2.5f), new Vector3(-13.8f, 3.6f, 2.5f) })
            {
                var f = LightGo("ClockFill", LightType.Point, Pal.Candle, 2.8f, 10f, true);
                f.transform.SetParent(root, false); f.transform.localPosition = p;
            }

            KitProbes(root, new Vector3(cx, h * 0.5f, cz), new Vector3(W, h, D), 7, 3, 5, new Bounds(new Vector3(px - 0.25f, h * 0.5f, cz), new Vector3(0.7f, h, D)));
            KitReflection(root, new Vector3(cx, 2.4f, cz), new Vector3(W, h, D));
        }

        /// <summary>Hazne Odası kabuğu + dekor. Gösterge/tablo/mühür kolu BuildPuzzle3'te.</summary>
        static void BuildHazneShell(Transform r, float h)
        {
            const float hs = 5f;
            var stone = K.castleStone;
            // Zemin: ortası güneyde sarnıç kuyusu (x ±1.6, z -3.5..-1.5)
            Deck(r, "FloorN", new Vector3(0, -0.25f, 1.875f), 10.5f, 6.75f, 0.5f, K.cisternFloor, 2, 2);
            Deck(r, "FloorS", new Vector3(0, -0.25f, -4.375f), 10.5f, 1.75f, 0.5f, K.cisternFloor, 2, 1);
            foreach (int s in new[] { -1, 1 })
                Deck(r, "FloorSide", new Vector3(s * 3.425f, -0.25f, -2.5f), 3.65f, 2f, 0.5f, K.cisternFloor, 1, 1);
            Wall(r, "WallN", new Vector3(0, 0, hs + 0.25f), true, 10.5f, h, stone, 2f, new Opening(0f, 3f, 0f, 3f));
            Slab("StairHeader", r, new Vector3(0, 5.75f, hs + 0.25f), new Vector3(4f, 1.5f, 0.5f), stone, 2f, false);
            Wall(r, "WallS", new Vector3(0, 0, -hs - 0.25f), true, 10.5f, h, stone, 2f);
            Wall(r, "WallE", new Vector3(hs + 0.25f, 0, 0), false, 10.5f, h, stone, 2f, new Opening(2.7f, 2.2f, 0f, 3f)); // arşiv geçidi (bulmaca 4)
            Wall(r, "WallW", new Vector3(-hs - 0.25f, 0, 0), false, 10.5f, h, stone, 2f);
            BeamCeiling(r, "Ceil", new Vector3(0, h, 0), 10f, 10f, 1.3f);

            // Üst bant: taş üstünde tahta kaplama + kuşak, ahşap dikmeler ve kavisli payandalar
            const float band = 3.1f;
            void Band(string n, Vector3 face, bool alongX, Vector3 inward)
            {
                var size = alongX ? new Vector3(10f, h - band, 0.04f) : new Vector3(0.04f, h - band, 10f);
                Slab(n + "Planks", r, face + inward * 0.02f + Vector3.up * ((h + band) * 0.5f), size, K.beam, 1.6f, false);
                var rail = alongX ? new Vector3(10f, 0.16f, 0.16f) : new Vector3(0.16f, 0.16f, 10f);
                Slab(n + "Rail", r, face + inward * 0.08f + Vector3.up * band, rail, K.beam, 1.2f, false);
            }
            Band("BandN", new Vector3(0, 0, hs), true, Vector3.back);
            Band("BandS", new Vector3(0, 0, -hs), true, Vector3.forward);
            Band("BandE", new Vector3(hs, 0, 0), false, Vector3.left);
            Band("BandW", new Vector3(-hs, 0, 0), false, Vector3.right);
            foreach (var p in new[] { new Vector2(-4.82f, -4.82f), new Vector2(4.82f, -4.82f), new Vector2(-4.82f, 4.82f), new Vector2(4.82f, 4.82f),
                                      new Vector2(-2.1f, -4.82f), new Vector2(2.1f, -4.82f), new Vector2(-4.82f, 1.4f), new Vector2(4.82f, 1.4f) })
                Slab("Post", r, new Vector3(p.x, h * 0.5f, p.y), new Vector3(0.3f, h, 0.3f), K.beam, 1.2f, false);
            foreach (int s in new[] { -1, 1 })
            {
                Rod("Brace", r, new Vector3(s * 2.1f, h - 1.2f, -4.82f), new Vector3(s * 1.0f, h - 0.2f, -4.82f), 0.18f, K.beam);
                Rod("Brace", r, new Vector3(s * 4.82f, h - 1.2f, 1.4f), new Vector3(s * 4.82f, h - 0.2f, 0.3f), 0.18f, K.beam);
            }

            // Sarnıç kuyusu: taş duvarlar, karanlık su, aşağıdaki tonozun sütun uçları; kenarda alçak taş bordür (düşmeyi önler)
            {
                var pit = new GameObject("CisternWell").transform;
                pit.SetParent(r, false);
                const float pb = -2.6f, wz0 = -3.5f, wz1 = -1.5f, wx = 1.6f;
                Deck(pit, "PitFloor", new Vector3(0, pb - 0.25f, (wz0 + wz1) * 0.5f), 3.2f, 2f, 0.5f, K.cisternFloor, 1, 1);
                Slab("PitN", pit, new Vector3(0, (pb - 0.01f) * 0.5f, wz1 + 0.15f), new Vector3(3.8f, -pb - 0.01f, 0.3f), K.cisternWall, 1.5f, false);
                Slab("PitS", pit, new Vector3(0, (pb - 0.01f) * 0.5f, wz0 - 0.15f), new Vector3(3.8f, -pb - 0.01f, 0.3f), K.cisternWall, 1.5f, false);
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("PitSide", pit, new Vector3(s * (wx + 0.15f), (pb - 0.01f) * 0.5f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, -pb - 0.01f, 2f), K.cisternWall, 1.5f, false);
                    Slab("PitColumn", pit, new Vector3(s * 0.85f, pb * 0.5f - 0.1f, wz0 + 0.12f), new Vector3(0.32f, -pb - 0.2f, 0.32f), stone, 1.2f, false);
                }
                var wmat = MakeMat("K_HazneWater", new Color(0.04f, 0.12f, 0.13f));
                wmat.SetFloat("_Smoothness", 0.94f);
                var water = Slab("WellWater", pit, new Vector3(0, -1.3f, (wz0 + wz1) * 0.5f), new Vector3(3.2f, 0.04f, 2f), wmat, 1f, false);
                water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                // Demir ızgara: kuyu görünür kalır, içine düşülmez (görünmez çarpışma kutusu + çubuklar)
                var lid = Slab("GrateCollider", pit, new Vector3(0, -0.06f, (wz0 + wz1) * 0.5f), new Vector3(3.3f, 0.12f, 2.1f), K.iron, 1f);
                lid.GetComponent<MeshRenderer>().enabled = false;
                for (int i = 0; i < 9; i++)
                {
                    float x = -wx + 0.18f + i * (2f * wx - 0.36f) / 8f;
                    Rod("GrateBar", pit, new Vector3(x, -0.05f, wz0), new Vector3(x, -0.05f, wz1), 0.05f, K.iron);
                }
                foreach (float z in new[] { wz0 + 0.5f, wz1 - 0.5f })
                    Rod("GrateRail", pit, new Vector3(-wx, -0.08f, z), new Vector3(wx, -0.08f, z), 0.07f, K.iron);
                // Bordür
                const float kh = 0.45f, kt = 0.25f;
                Slab("KerbN", pit, new Vector3(0, kh * 0.5f, wz1 - kt * 0.5f + 0.25f), new Vector3(3.7f, kh, kt), stone, 1.2f);
                Slab("KerbS", pit, new Vector3(0, kh * 0.5f, wz0 - 0.125f), new Vector3(3.7f, kh, kt), stone, 1.2f);
                foreach (int s in new[] { -1, 1 })
                    Slab("KerbSide", pit, new Vector3(s * (wx + 0.125f), kh * 0.5f, (wz0 + wz1) * 0.5f), new Vector3(kt, kh, 2.25f), stone, 1.2f);
                Kit("CandleCluster", pit, new Vector3(1.65f, kh, -1.4f), 30f);
                Kit("L_Candle", pit, new Vector3(1.6f, 0.95f, -1.2f));
            }

            // Borular: göstergeye iki yandan girer, ikisi kuyuya iner (renkler bulmaca renkleri DEĞİL: pirinç/bakır/demir)
            {
                var pipes = new GameObject("Pipes").transform;
                pipes.SetParent(r, false);
                var copper = MakeMat("K_PipeCopper", new Color(0.34f, 0.3f, 0.22f));
                copper.SetFloat("_Metallic", 0.55f); copper.SetFloat("_Smoothness", 0.35f);
                const float pz = -hs + 0.16f;
                void Run(Vector3 a, Vector3 b, Material m)
                {
                    Rod("Pipe", pipes, a, b, 0.09f, m);
                    Prim("Joint", pipes, PrimitiveType.Sphere, b, Vector3.one * 0.14f, m);
                }
                Run(new Vector3(-4.7f, 2.2f, pz), new Vector3(-0.7f, 2.2f, pz), K.brass);
                Run(new Vector3(4.7f, 2.2f, pz), new Vector3(0.7f, 2.2f, pz), copper);
                Run(new Vector3(-4.7f, 3.4f, pz), new Vector3(-0.7f, 3.4f, pz), copper);
                Run(new Vector3(4.7f, 3.4f, pz), new Vector3(0.7f, 3.4f, pz), K.iron);
                foreach (int s in new[] { -1, 1 })
                {
                    var pm = s < 0 ? K.brass : copper;
                    Run(new Vector3(s * 1.3f, 2.2f, pz), new Vector3(s * 1.3f, 0.65f, pz), pm);
                    Run(new Vector3(s * 1.3f, 0.65f, pz), new Vector3(s * 1.25f, 0.65f, -3.3f), pm);
                    Run(new Vector3(s * 1.25f, 0.65f, -3.3f), new Vector3(s * 1.25f, -1.2f, -3.3f), pm);
                    // Küçük el çarkı (dekor)
                    var wheel = MeshObj("HandWheel", pipes, new Vector3(s * 3.2f, 2.2f, pz + 0.14f), RingMesh(0.14f, 0.025f, 0.03f), K.iron);
                    wheel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Rod("WheelStem", pipes, new Vector3(s * 3.2f, 2.2f, pz), new Vector3(s * 3.2f, 2.2f, pz + 0.14f), 0.03f, K.iron);
                }
                ProbeLit(pipes.gameObject);
            }

            // Asılı iki fener (göstergenin iki yanı) — odanın 2 realtime ışığı
            foreach (int s in new[] { -1, 1 })
            {
                var basePos = new Vector3(s * 2.5f, 3.35f, -4.25f);
                Rod("Chain", r, basePos + Vector3.up * 0.5f, new Vector3(s * 2.5f, h - 0.2f, -4.25f), 0.025f, K.iron);
                Kit("L_Lantern", r, basePos, s * 20f);
            }

            // Batı: tablonun önünde şamdan; sandıklar/fıçılar; kuzeyde sandık yığını
            Kit("L_Candelabra", r, new Vector3(-3.9f, 2.6f, 0));
            Kit("CandelabraFloor", r, new Vector3(-4.5f, 0, 2.1f), 90f);
            Kit("Barrel", r, new Vector3(-4.25f, 0, -4.25f), 20f);
            Kit("Crate", r, new Vector3(-4.3f, 0, -3.1f), 8f);
            Kit("Crate", r, new Vector3(-4.3f, 0.6f, -3.15f), 35f, 0.85f);
            Kit("CrateStack", r, new Vector3(-4.0f, 0, 4.0f), 90f);
            Kit("Bucket", r, new Vector3(-2.6f, 0, 4.3f));

            // Doğu: yazı masası (mektuplar, mum, kitap), duvarda notlar, fıçılar
            Kit("StudyTable", r, new Vector3(4.35f, 0, -0.8f), -90f);
            Kit("Stool", r, new Vector3(3.4f, 0, -1.0f), -20f);
            Kit("L_Candle", r, new Vector3(4.0f, 1.35f, -1.1f));
            var onWallE = Quaternion.Euler(0, 0, 90);
            KitR("PaperA", r, new Vector3(hs - 0.07f, 1.75f, -0.2f), onWallE * Quaternion.Euler(0, 5, 0));
            KitR("PaperC", r, new Vector3(hs - 0.07f, 1.55f, -1.5f), onWallE * Quaternion.Euler(0, -8, 0));
            KitR("PaperB", r, new Vector3(hs - 0.07f, 1.9f, -0.9f), onWallE * Quaternion.Euler(0, 3, 0));
            Kit("Barrel", r, new Vector3(4.2f, 0, -4.2f), 60f);
            Kit("Barrel", r, new Vector3(3.2f, 0, -4.4f), 140f, 0.9f);
            Kit("Bucket", r, new Vector3(2.5f, 0, -3.9f));
            Kit("Crate", r, new Vector3(4.3f, 0, 4.2f), -15f);
            Kit("Chest", r, new Vector3(3.0f, 0, 4.3f), 180f);

            // Tedirginlik: kuyunun önünde saçılmış kağıtlar, devrik kova, ağlar
            Kit("PaperScatter", r, new Vector3(-0.6f, 0, -0.6f), 110f);
            KitR("Bucket", r, new Vector3(-2.1f, 0.17f, -0.9f), Quaternion.Euler(0, 40, 90));
            KitR("Cobweb", r, new Vector3(-hs + 0.03f, h - 0.2f, -hs + 0.03f), Quaternion.Euler(0, 180, 180), 1.6f);
            KitR("Cobweb", r, new Vector3(hs - 0.03f, h - 0.2f, -hs + 0.03f), Quaternion.Euler(0, 90, 180), 1.4f);
            KitR("Cobweb", r, new Vector3(hs - 0.03f, h - 0.2f, hs - 0.03f), Quaternion.Euler(0, 0, 180), 1.5f);
            KitR("Cobweb", r, new Vector3(-hs + 0.03f, 0, hs - 0.03f), Quaternion.identity, 1.0f);

            KitProbes(r, new Vector3(0, h * 0.5f, 0), new Vector3(10f, h, 10f), 5, 3, 5);
            KitReflection(r, new Vector3(0, 2f, 0), new Vector3(10f, h, 10f));
        }
    }
}
