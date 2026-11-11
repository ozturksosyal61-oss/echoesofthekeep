using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// X'in 2. ve 3. odaları, Art kit ile (ref: Docs/Reference/saat.jpg, hazne.jpg). Lobi tarzı: lambri + sıva + kirişli tavan,
    /// gotik/tedirgin dokunuşlar (asılı çan, portre, mum kümeleri, ağlar). Baked; oda başına en fazla 2 realtime fener.
    /// Saat Odası kökü: kule yerel (-18,-4,0); iç x -8..1.5, z ±5, h 4.5. Batı duvarı bulmaca 2 (BuildPuzzle2), doğu girişi, güney Hazne kapısı (x -0.5).
    /// Hazne kökü: saat yerel (-0.5,-3,-17); iç ±5, h 5. Kuzey merdiven ağzı, güneyde gösterge + önünde sarnıç kuyusu, batıda tablo.
    /// </summary>
    public static partial class CoopSetup
    {
        static void BuildClockShell(Transform root, float h)
        {
            const float cx = -3.25f, x0 = -8f, x1 = 1.5f, hz = 5f;
            var entry = new Opening(0f, 3f, 0f, 3f);
            var hazneDoor = new Opening(2.75f, 3f, 0f, 3f);
            Deck(root, "Floor", new Vector3(cx, -0.25f, 0), 10.5f, 10.5f, 0.5f, K.hallFloor, 2, 2);
            Wall(root, "WallW", new Vector3(x0 - 0.25f, 0, 0), false, 10.5f, h, K.plaster, 2.5f);
            Wall(root, "WallE", new Vector3(x1 + 0.25f, 0, 0), false, 10.5f, h, K.plaster, 2.5f, entry);
            Slab("StairHeader", root, new Vector3(1.75f, 5.75f, 0), new Vector3(0.5f, 2.5f, 4f), K.plaster, 2f, false);
            Wall(root, "WallN", new Vector3(cx, 0, hz + 0.25f), true, 10.5f, h, K.plaster, 2.5f);
            Wall(root, "WallS", new Vector3(cx, 0, -hz - 0.25f), true, 10.5f, h, K.plaster, 2.5f, hazneDoor);
            BeamCeiling(root, "Ceil", new Vector3(cx, h, 0), 9.5f, 10f, 1.4f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(cx, h + 0.5f, 0), new Vector3(11f, 0.6f, 11f), K.beam, 2f, false), 0);
            Wainscot(root, "WainN", new Vector3(cx, 0, hz), true, 9.5f, Vector3.back, 1.3f);
            Wainscot(root, "WainS", new Vector3(cx, 0, -hz), true, 9.5f, Vector3.forward, 1.3f, hazneDoor);
            Wainscot(root, "WainE", new Vector3(x1, 0, 0), false, 10f, Vector3.left, 1.3f, entry);
            Wainscot(root, "WainW", new Vector3(x0, 0, 0), false, 10f, Vector3.right, 1.2f);
            // Kapı söveleri
            foreach (int s in new[] { -1, 1 })
            {
                Slab("EntryJamb", root, new Vector3(x1 - 0.08f, 1.55f, s * 1.6f), new Vector3(0.16f, 3.1f, 0.22f), K.beam, 1.2f, false);
                Slab("HazneJamb", root, new Vector3(-0.5f + s * 1.6f, 1.55f, -hz + 0.08f), new Vector3(0.22f, 3.1f, 0.16f), K.beam, 1.2f, false);
            }
            Slab("EntryLintel", root, new Vector3(x1 - 0.08f, 3.12f, 0), new Vector3(0.16f, 0.24f, 3.42f), K.beam, 1.2f, false);
            Slab("HazneLintel", root, new Vector3(-0.5f, 3.12f, -hz + 0.08f), new Vector3(3.42f, 0.24f, 0.16f), K.beam, 1.2f, false);

            // Karkas: köşe dikmeleri + payandalar
            foreach (float x in new[] { x0 + 0.18f, x1 - 0.18f })
                foreach (int sz in new[] { -1, 1 })
                {
                    Slab("Post", root, new Vector3(x, h * 0.5f, sz * (hz - 0.18f)), new Vector3(0.32f, h, 0.32f), K.beam, 1.2f, false);
                    float dir = x < cx ? 1f : -1f;
                    Rod("Brace", root, new Vector3(x, h - 1.3f, sz * (hz - 0.18f)), new Vector3(x + dir * 1.2f, h - 0.25f, sz * (hz - 0.18f)), 0.18f, K.beam);
                }

            // --- Kuzey: ahşap karkas içinde saat mekanizması (arka statik dişli yığını + önde sürekli dönen dişliler) ---
            {
                var mech = new GameObject("ClockMechanism").transform;
                mech.SetParent(root, false);
                var anchor = new GameObject("ProbeAnchor").transform;
                anchor.SetParent(mech, false);
                anchor.localPosition = new Vector3(cx, 2.4f, hz - 1.2f);
                Slab("Back", mech, new Vector3(-3.3f, 2.85f, hz - 0.015f), new Vector3(7.6f, 3.0f, 0.03f), K.beam, 2f, false);
                foreach (float x in new[] { -7.0f, -4.5f, -2.0f, 0.5f })
                    Slab("FramePost", mech, new Vector3(x, h * 0.5f, hz - 0.2f), new Vector3(0.26f, h, 0.2f), K.beam, 1.2f, false);
                foreach (float y in new[] { 1.38f, 4.25f })
                    Slab("FrameRail", mech, new Vector3(-3.25f, y, hz - 0.18f), new Vector3(7.8f, 0.2f, 0.14f), K.beam, 1.2f, false);
                var pile = new GameObject("GearPile").transform;
                pile.SetParent(mech, false);
                var gs = new[]
                {
                    new Vector4(-6.3f, 2.3f, 0.75f, 14), new Vector4(-5.6f, 3.6f, 0.55f, 10), new Vector4(-1.2f, 3.65f, 0.6f, 12),
                    new Vector4(-0.3f, 2.2f, 0.7f, 12), new Vector4(-3.3f, 3.85f, 0.4f, 8), new Vector4(-6.6f, 3.85f, 0.35f, 8),
                };
                for (int i = 0; i < gs.Length; i++)
                {
                    var g = MeshObj("Gear" + i, pile, new Vector3(gs[i].x, gs[i].y, hz - 0.08f), GearMesh((int)gs[i].w, gs[i].z, 0.12f, gs[i].z * 0.2f, 0.1f), K.iron);
                    g.transform.localRotation = Quaternion.Euler(0, 0, i * 41f);
                }
                Rod("Shaft", pile, new Vector3(-6.8f, 4.0f, hz - 0.36f), new Vector3(0.3f, 4.0f, hz - 0.36f), 0.06f, K.iron);
                foreach (float x in new[] { -5.6f, -3.3f, -1.2f })
                    Prim("Pulley", pile, PrimitiveType.Cylinder, new Vector3(x, 4.0f, hz - 0.36f), new Vector3(0.42f, 0.04f, 0.42f), K.iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
                ProbeLit(pile.gameObject);

                // Önde dönen dişliler (saat hep işler)
                void Gear(string n, int teeth, float r, Vector2 p, float speed, Material mat)
                {
                    var g = MeshObj(n, mech, new Vector3(p.x, p.y, hz - 0.42f), GearMesh(teeth, r, 0.14f, r * 0.22f, 0.1f), mat);
                    Prim("Axle", g.transform, PrimitiveType.Cylinder, new Vector3(0, 0, 0.12f), new Vector3(r * 0.3f, 0.2f, r * 0.3f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    AddSpin(g, speed);
                }
                Gear("GearA", 18, 1.15f, new Vector2(-4.3f, 2.55f), 6f, K.bronze);
                Gear("GearB", 10, 0.68f, new Vector2(-2.48f, 2.95f), -10.8f, K.brass);
                Gear("GearC", 8, 0.5f, new Vector2(-1.45f, 2.3f), 14f, K.bronze);
                foreach (var rr in mech.GetComponentsInChildren<Renderer>()) rr.probeAnchor = anchor;
            }

            // --- Ana kirişten asılı çan + iki halat ---
            {
                const float bellH = 0.95f, top = 4.05f, bz = 2.5f;
                Kit("Bell", root, new Vector3(cx, top - bellH, bz), 20f, bellH / 1.1f);
                var rig = new GameObject("BellRig").transform;
                rig.SetParent(root, false);
                Slab("Yoke", rig, new Vector3(cx, top + 0.08f, bz), new Vector3(0.8f, 0.18f, 0.22f), K.furniture, 1.2f, false);
                foreach (float dx in new[] { -0.14f, 0.14f })
                    Rod("Rope", rig, new Vector3(cx + dx, top - bellH + 0.25f, bz), new Vector3(cx + dx * 1.6f, 1.5f, bz + 0.1f), 0.03f, K.furniture);
                ProbeLit(rig.gameObject);
            }

            // --- Batı (bulmaca duvarı): yerde şamdanlar, mum kümesi ---
            foreach (int s in new[] { -1, 1 })
            {
                Kit("CandelabraFloor", root, new Vector3(x0 + 0.55f, 0, s * 3.7f), 90f);
                Kit("L_Candelabra", root, new Vector3(x0 + 0.9f, 1.9f, s * 3.4f));
            }
            Kit("L_Candelabra", root, new Vector3(-5.9f, 2.6f, 0)); // kadranlar okunsun
            Kit("CandleCluster", root, new Vector3(x0 + 0.35f, 0, -2.6f));
            Kit("Chest", root, new Vector3(-6.2f, 0, 4.2f), 180f);

            // --- Güney: vitray (ay), çalışma masası, sarkaçlı saat ---
            KitR("WindowLancet", root, new Vector3(-5.6f, 1.38f, -hz), Quaternion.identity, 1.0f);
            Kit("StudyTable", root, new Vector3(-4.3f, 0, -4.35f), 0f); // vitrayın önünde
            Kit("Stool", root, new Vector3(-4.0f, 0, -3.4f), 35f);
            Kit("L_Candelabra", root, new Vector3(-4.0f, 1.75f, -3.9f));
            Kit("GrandfatherClock", root, new Vector3(-2.65f, 0, -hz + 0.35f), 0f);
            Kit("PaperScatter", root, new Vector3(-4.6f, 0, -2.4f), 50f);

            // --- Doğu: tezgah + fener, alet panosu, raflarda şişe/dişli; güneyde portre + konsol + büst ---
            Kit("Workbench", root, new Vector3(x1 - 0.7f, 0, 3.4f), -90f);
            Kit("L_Lantern", root, new Vector3(x1 - 0.55f, 0.82f, 4.3f));
            Slab("ToolBoard", root, new Vector3(x1 - 0.07f, 1.45f, 3.3f), new Vector3(0.03f, 0.7f, 1.8f), K.furniture, 1.2f, false);
            var onWallE = Quaternion.Euler(0, 0, 90); // yatık nesneyi doğu duvarına asar (Tower ile aynı)
            KitR("Wrench", root, new Vector3(x1 - 0.1f, 1.5f, 2.75f), onWallE * Quaternion.Euler(0, 15, 0));
            KitR("Hammer", root, new Vector3(x1 - 0.1f, 1.47f, 3.3f), onWallE * Quaternion.Euler(0, -10, 0));
            KitR("Mallet", root, new Vector3(x1 - 0.1f, 1.47f, 3.85f), onWallE * Quaternion.Euler(0, 8, 0));
            KitR("PaperA", root, new Vector3(x1 - 0.07f, 1.55f, 2.0f), onWallE * Quaternion.Euler(0, 6, 0));
            KitR("PaperC", root, new Vector3(x1 - 0.07f, 1.4f, 4.55f), onWallE * Quaternion.Euler(0, -9, 0));
            foreach (float y in new[] { 2.25f, 2.85f })
            {
                Slab("Shelf", root, new Vector3(x1 - 0.16f, y, 3.3f), new Vector3(0.3f, 0.05f, 1.8f), K.beam, 1f, false);
                foreach (float z in new[] { 2.55f, 4.05f })
                    Rod("ShelfBracket", root, new Vector3(x1 - 0.02f, y - 0.22f, z), new Vector3(x1 - 0.26f, y - 0.03f, z), 0.03f, K.iron);
            }
            Kit("Bottle0", root, new Vector3(x1 - 0.16f, 2.28f, 2.6f));
            Kit("Bottle2", root, new Vector3(x1 - 0.16f, 2.28f, 2.9f));
            Kit("Bottle1", root, new Vector3(x1 - 0.18f, 2.88f, 3.9f));
            Kit("Bottle3", root, new Vector3(x1 - 0.14f, 2.88f, 4.15f));
            Kit("BookStack", root, new Vector3(x1 - 0.17f, 2.28f, 3.8f), 80f, 0.6f);
            {
                var spare = new GameObject("ShelfGears").transform;
                spare.SetParent(root, false);
                MeshObj("GearS0", spare, new Vector3(x1 - 0.16f, 2.29f, 3.35f), GearMesh(10, 0.2f, 0.06f, 0.05f, 0.05f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("GearS1", spare, new Vector3(x1 - 0.1f, 3.1f, 3.0f), GearMesh(12, 0.24f, 0.06f, 0.06f, 0.05f), K.bronze).transform.localRotation = Quaternion.Euler(-12, 90, 0);
                MeshObj("GearS2", spare, new Vector3(-0.3f, 0.04f, 1.9f), GearMesh(8, 0.28f, 0.08f, 0.07f, 0.06f), K.iron).transform.localRotation = Quaternion.Euler(90, 30, 0);
                ProbeLit(spare.gameObject);
            }
            Kit("Stool", root, new Vector3(0.1f, 0, 3.0f), -30f);
            KitR("PaintingKeep", root, new Vector3(x1 - 0.04f, 2.55f, -3.3f), Quaternion.Euler(0, -90, 0), 1.1f);
            Kit("Commode", root, new Vector3(x1 - 0.42f, 0, -3.3f), -90f, 0.9f);
            Kit("Bust", root, new Vector3(x1 - 0.42f, 0.86f, -3.05f), -110f, 0.7f);
            Kit("Candlestick", root, new Vector3(x1 - 0.4f, 0.86f, -3.65f));
            Kit("L_Candle", root, new Vector3(x1 - 0.7f, 1.6f, -3.6f));

            // --- Orta: halı, avize; köşelerde ağlar ---
            Kit("Rug", root, new Vector3(-3.6f, 0, -0.4f), 90f);
            Kit("ChandelierIron", root, new Vector3(-3.6f, h - 0.15f, -1.6f), 0f, 0.95f);
            Kit("L_Chandelier", root, new Vector3(-3.6f, 2.9f, -1.6f));
            KitR("Cobweb", root, new Vector3(x0 + 0.03f, h - 0.2f, -hz + 0.03f), Quaternion.Euler(0, 180, 180), 1.5f);
            KitR("Cobweb", root, new Vector3(x1 - 0.03f, h - 0.2f, hz - 0.03f), Quaternion.Euler(0, 0, 180), 1.4f);
            KitR("Cobweb", root, new Vector3(x0 + 0.03f, h - 0.2f, hz - 0.03f), Quaternion.Euler(0, 0, -90), 1.7f);
            KitR("Cobweb", root, new Vector3(x0 + 0.03f, 0, hz - 0.03f), Quaternion.identity, 1.0f);

            KitProbes(root, new Vector3(cx, h * 0.5f, 0), new Vector3(9.5f, h, 10f), 5, 3, 5);
            KitReflection(root, new Vector3(cx, 2f, 0), new Vector3(9.5f, h, 10f));
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
            Wall(r, "WallE", new Vector3(hs + 0.25f, 0, 0), false, 10.5f, h, stone, 2f);
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
            Kit("Chest", r, new Vector3(4.3f, 0, 2.6f), -90f);

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
