using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Bulmaca 5 "Katakomb labirenti".
    /// X: Harita odası kule yerel (40,0,44); 20x24, düz kirişli tavan 8.6, sıva+ahşap iskelet; ortada serbest merdivenli gözlem kürsüsü (y 3.6),
    ///    maket kürsünün üstünde (girişten görünmez); güneybatıda sahte harita masası.
    /// O: Katakomb (ref katakomb.png) roomO yerel (40,0,-48); güneyde giriş odası, 8x8 hücre labirent (iç duvarlar çalışma anında),
    ///    kuzeyde çıkış odası. Bake dışı: karanlıkta yalnızca el feneri.
    /// Girişler: arşiv galeri kapısı / kütüphane kilitli kapısının ardı → Passage (bulmaca 4 çözülünce).
    /// Düğümler: 44 tohum, 45 evre, 47 deneme, 48-49 geçitler.
    /// </summary>
    public static partial class CoopSetup
    {
        static void BuildPuzzle5(Transform roomX, Transform roomO)
        {
            var def = new GameObject("Puzzle5").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 5;
            dso.FindProperty("title").stringValue = "Bulmaca 5 (labirent)";
            dso.FindProperty("startsActive").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();
            foreach (var d in Object.FindObjectsByType<PuzzleDef>(FindObjectsSortMode.None))
                if (d.Id == 4)
                {
                    var so = new SerializedObject(d);
                    var up = so.FindProperty("unlocks"); up.arraySize = 1; up.GetArrayElementAtIndex(0).intValue = 5;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

            var mapRoom = BuildMapRoom(roomX, out var modelRoot, out var mapArrive);
            var cata = BuildCatacomb(roomO, out var mazeRoot, out var cataArrive, out var anteCenter, out var anteSize);

            var lab = new GameObject("Puzzle5_Labyrinth").AddComponent<LabyrinthPuzzle>();
            lab.transform.SetParent(cata, false);
            var lso = new SerializedObject(lab);
            lso.FindProperty("puzzleId").intValue = 5;
            lso.FindProperty("nodeId").intValue = 44;
            SetRef(lso, "origin", mazeRoot);
            SetRef(lso, "respawn", cataArrive);
            lso.FindProperty("anteCenter").vector3Value = anteCenter;
            lso.FindProperty("anteSize").vector3Value = anteSize;
            lso.ApplyModifiedPropertiesWithoutUndo();

            var view = lab.gameObject.AddComponent<LabyrinthView>();
            var vso = new SerializedObject(view);
            SetRef(vso, "puzzle", lab);
            SetRef(vso, "mazeRoot", mazeRoot);
            SetRef(vso, "modelRoot", modelRoot);
            vso.FindProperty("modelScale").floatValue = 1f / 12f;
            SetRef(vso, "wallMat", K.castleStone);
            SetRef(vso, "plaqueMat", K.iron);
            SetRef(vso, "glyphMat", A.gold);
            SetRef(vso, "modelWallMat", K.castleStone);
            SetRef(vso, "modelGlyphMat", A.gold);
            SetRef(vso, "trapMat", GlowMat("K_TrapPin", Pal.Puzzle[0], Pal.Puzzle[0] * 0.6f));
            SetRef(vso, "exitMat", GlowMat("K_ExitPin", Pal.Puzzle[1], Pal.Puzzle[1] * 0.6f));
            vso.ApplyModifiedPropertiesWithoutUndo();

            var hud = lab.gameObject.AddComponent<LabyrinthHud>();
            var hso = new SerializedObject(hud);
            SetRef(hso, "puzzle", lab);
            SetRef(hso, "font", U.body);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Geçitler (bulmaca 4 çözülünce): arşiv galeri kapısı → harita odası, kütüphane kapısının ardı → katakomb girişi
            var archive = roomX.Find("RoomA4_Arsiv");
            var library = roomO.Find("RoomB4_Kutuphane");
            if (archive != null) MakePassage("PassageX5", archive, 48, archive.TransformPoint(new Vector3(0, 5f, 10.3f)), new Vector3(2.4f, 2f, 1.2f), mapArrive, 4);
            if (library != null) MakePassage("PassageO5", library, 49, library.TransformPoint(new Vector3(0, 1f, -16.2f)), new Vector3(3f, 2f, 1.4f), cataArrive, 4);

            MakeCheckpoint(roomX, 5, Roles.X, mapArrive.position);
            MakeCheckpoint(roomO, 5, Roles.O, cataArrive.position);
            KeepBaked(mapRoom);
            KeepBaked(cata); // katakomb artık baked (karanlık ve düz görünüyordu)
        }

        // ---------------- Harita odası (X) ----------------
        // Arşivden farklı: sıva + ahşap iskelet duvar, parke, düz kirişli tavan, ortada serbest merdivenli gözlem kürsüsü
        // (maket kürsünün üstünde, girişten görünmez), batıda şömine, kuzeyde dev duvar haritası + tomar rafları, doğuda perdeli pencereler.

        static Transform BuildMapRoom(Transform roomX, out Transform modelRoot, out Transform arrive)
        {
            const float SX = 20f, SZ = 24f, hx = SX * 0.5f, hz = SZ * 0.5f, TOP = 8.6f, PY = 3.6f;
            const float px0 = -4f, px1 = 4f, pz0 = -1f, pz1 = 6f; // kürsü
            var root = new GameObject("RoomA5_Harita").transform;
            root.SetParent(roomX, false);
            root.localPosition = new Vector3(40f, 0, 44f);
            var stone = K.castleStone; var wood = K.beam; var plaster = K.plaster;
            var ember = GlowMat("K_Ember", new Color(0.35f, 0.12f, 0.04f), new Color(1.6f, 0.55f, 0.15f));
            var soot = MakeMat("K_Soot", new Color(0.03f, 0.025f, 0.02f));

            // --- Kabuk: parke, sıva duvar, ahşap iskelet, lambri, kirişli tavan ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), SX, SZ, 0.5f, K.parquet, 3, 4);
            Wall(root, "WallE", new Vector3(hx + 0.25f, 0, 0), false, SZ, TOP + 0.5f, plaster, 2.2f);
            Wall(root, "WallW", new Vector3(-hx - 0.25f, 0, 0), false, SZ, TOP + 0.5f, plaster, 2.2f);
            Wall(root, "WallN", new Vector3(0, 0, hz + 0.25f), true, SX + 1f, TOP + 0.5f, plaster, 2.2f);
            Wall(root, "WallS", new Vector3(0, 0, -hz - 0.25f), true, SX + 1f, TOP + 0.5f, plaster, 2.2f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, TOP + 0.6f, 0), new Vector3(SX + 1.5f, 0.4f, SZ + 1.5f), wood, 2f, false), 0);
            BeamCeiling(root, "Ceil", new Vector3(0, TOP, 0), SX, SZ, 1.6f);
            foreach (var (c, alongX, len, inward) in new[] {
                (new Vector3(hx, 0, 0), false, SZ, Vector3.left), (new Vector3(-hx, 0, 0), false, SZ, Vector3.right),
                (new Vector3(0, 0, hz), true, SX, Vector3.back), (new Vector3(0, 0, -hz), true, SX, Vector3.forward) })
            {
                var al = alongX ? Vector3.right : Vector3.forward;
                // dikmeler + iki kuşak + çapraz payandalar (yarım ahşap)
                int n = Mathf.RoundToInt(len / 3f);
                for (int i = 0; i <= n; i++)
                {
                    var p = c + al * (-len * 0.5f + len * i / n) + inward * 0.1f;
                    Slab("Post", root, p + Vector3.up * (TOP * 0.5f), alongX ? new Vector3(0.26f, TOP, 0.2f) : new Vector3(0.2f, TOP, 0.26f), wood, 1.5f, false);
                    if (i < n && i % 2 == 0)
                    {
                        var q = c + al * (-len * 0.5f + len * (i + 1) / n) + inward * 0.1f;
                        Rod("Brace", root, p + Vector3.up * 3.0f, q + Vector3.up * 5.6f, 0.16f, wood);
                    }
                }
                foreach (float y in new[] { 3.0f, 5.8f })
                    Slab("Girt", root, c + inward * 0.1f + Vector3.up * y, alongX ? new Vector3(len, 0.24f, 0.2f) : new Vector3(0.2f, 0.24f, len), wood, 1.5f, false);
                Wainscot(root, "Wain", c, alongX, len, inward, 1.3f, alongX && inward == Vector3.forward ? new[] { new Opening(0f, 2.8f, 0f, 3.2f) } : new Opening[0]);
            }

            // --- Orta: gözlem kürsüsü (ahşap platform, 6 dikme, payandalar) + serbest merdiven ---
            Deck(root, "Platform", new Vector3((px0 + px1) * 0.5f, PY - 0.2f, (pz0 + pz1) * 0.5f), px1 - px0, pz1 - pz0, 0.4f, K.parquet, 2, 2);
            foreach (var (a, b) in new[] { (new Vector3(px0, 0, pz0), new Vector3(px1, 0, pz0)), (new Vector3(px0, 0, pz1), new Vector3(px1, 0, pz1)),
                                           (new Vector3(px0, 0, pz0), new Vector3(px0, 0, pz1)), (new Vector3(px1, 0, pz0), new Vector3(px1, 0, pz1)) })
                Slab("Fascia", root, (a + b) * 0.5f + Vector3.up * (PY - 0.45f), new Vector3(Mathf.Abs(b.x - a.x) + 0.24f, 0.5f, Mathf.Abs(b.z - a.z) + 0.24f), wood, 1.5f, false);
            foreach (float x in new[] { px0 + 0.25f, px1 - 0.25f })
                foreach (float z in new[] { pz0 + 0.25f, (pz0 + pz1) * 0.5f, pz1 - 0.25f })
                {
                    Slab("PlatPost", root, new Vector3(x, (PY - 0.4f) * 0.5f, z), new Vector3(0.36f, PY - 0.4f, 0.36f), wood, 1.5f);
                    Slab("PlatPostFoot", root, new Vector3(x, 0.15f, z), new Vector3(0.5f, 0.3f, 0.5f), stone, 1f, false);
                    float sx = x < 0 ? 1f : -1f;
                    Rod("Knee", root, new Vector3(x, PY - 1.3f, z), new Vector3(x + sx * 0.8f, PY - 0.42f, z), 0.14f, wood);
                }
            const float sw = 2.4f; int steps = 15; float tread = 0.42f, rise = PY / steps, sz0 = pz0 - steps * tread;
            for (int i = 0; i < steps; i++)
                Slab("Step" + i, root, new Vector3(0, rise * (i + 1) * 0.5f, sz0 + tread * (i + 0.5f)), new Vector3(sw, rise * (i + 1), tread), i % 2 == 0 ? wood : K.furniture, 1.2f, false);
            {
                float run = steps * tread, ang = Mathf.Atan2(PY, run);
                var ramp = Slab("StairRamp", root, Vector3.zero, new Vector3(sw, 0.3f, Mathf.Sqrt(run * run + PY * PY) + 0.3f), stone, 1f);
                ramp.GetComponent<MeshRenderer>().enabled = false;
                GameObjectUtility.SetStaticEditorFlags(ramp, 0);
                ramp.transform.localPosition = new Vector3(0, PY * 0.5f - 0.15f * Mathf.Cos(ang), sz0 + run * 0.5f + 0.15f * Mathf.Sin(ang));
                ramp.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
            }
            foreach (int s in new[] { -1, 1 })
            {
                Balustrade(root, "StairRail", new Vector3(s * (sw * 0.5f - 0.08f), 0, sz0 + 0.1f), new Vector3(s * (sw * 0.5f - 0.08f), PY, pz0), false);
                Slab("NewelPost", root, new Vector3(s * (sw * 0.5f - 0.08f), 0.6f, sz0 + 0.1f), new Vector3(0.22f, 1.2f, 0.22f), wood, 1f, false);
                Prim("NewelCap", root, PrimitiveType.Sphere, new Vector3(s * (sw * 0.5f - 0.08f), 1.27f, sz0 + 0.1f), Vector3.one * 0.2f, K.brass);
                Balustrade(root, "PlatRailS", new Vector3(s * (sw * 0.5f), PY, pz0 + 0.1f), new Vector3(s * (px1 - 0.1f), PY, pz0 + 0.1f), true);
                Balustrade(root, "PlatRailSide", new Vector3(s * (px1 - 0.1f), PY, pz0 + 0.1f), new Vector3(s * (px1 - 0.1f), PY, pz1 - 0.1f), true);
            }
            Balustrade(root, "PlatRailN", new Vector3(px0 + 0.1f, PY, pz1 - 0.1f), new Vector3(px1 - 0.1f, PY, pz1 - 0.1f), true);

            // --- Kürsü üstü: maket masası (ASIL BULMACA), teleskop, küre, çalışma masası ---
            {
                var alc = new GameObject("ModelDesk").transform;
                alc.SetParent(root, false);
                const float tx = 0f, tz = 3.6f, th = 0.95f;
                Slab("TableTop", alc, new Vector3(tx, PY + th - 0.04f, tz), new Vector3(2.2f, 0.08f, 2.2f), K.furniture, 1f);
                foreach (int a in new[] { -1, 1 })
                    foreach (int b in new[] { -1, 1 })
                        Slab("Leg", alc, new Vector3(tx + a * 0.95f, PY + (th - 0.08f) * 0.5f, tz + b * 0.95f), new Vector3(0.14f, th - 0.08f, 0.14f), K.furniture, 1f, false);
                Slab("Apron", alc, new Vector3(tx, PY + th - 0.16f, tz), new Vector3(2.05f, 0.16f, 2.05f), K.furniture, 1f, false);
                Slab("ModelBase", alc, new Vector3(tx, PY + th + 0.01f, tz), new Vector3(1.9f, 0.02f, 1.9f), K.cellarFloor, 0.6f, false);
                ProbeLit(alc.gameObject);
                modelRoot = new GameObject("LabyrinthModel").transform;
                modelRoot.SetParent(root, false);
                modelRoot.localPosition = new Vector3(tx - 0.8f, PY + th + 0.02f, tz - 0.8f);
                Kit("L_Lantern", root, new Vector3(tx + 0.95f, PY + th, tz + 0.95f), 30f);
                KitR("PaperB", root, new Vector3(tx - 1.0f, PY + th + 0.005f, tz - 0.95f), Quaternion.Euler(0, 20, 0));
                Kit("Magnifier", root, new Vector3(tx + 0.9f, PY + th, tz - 0.9f), 60f);
                Kit("Stool", root, new Vector3(tx - 1.5f, PY, tz - 0.4f), 20f);
                Kit("Stool", root, new Vector3(tx + 1.5f, PY, tz + 0.2f), -40f);
            }
            {   // teleskop (pirinç tüp, üç ayak) gül pencereye dönük
                var tp = new Vector3(2.9f, PY, 0.2f);
                foreach (float a in new[] { 0f, 120f, 240f })
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward * 0.55f;
                    Rod("TripodLeg", root, tp + d, tp + Vector3.up * 1.25f, 0.05f, wood);
                }
                Rod("Tube", root, tp + new Vector3(0, 1.1f, -0.7f), tp + new Vector3(0, 1.9f, 0.9f), 0.13f, K.brass);
                Rod("Eyepiece", root, tp + new Vector3(0, 1.02f, -0.88f), tp + new Vector3(0, 1.1f, -0.7f), 0.06f, K.iron);
                Prim("Lens", root, PrimitiveType.Sphere, tp + new Vector3(0, 1.9f, 0.9f), new Vector3(0.16f, 0.16f, 0.05f), K.lanternGlass);
            }
            void Globe(Vector3 p, float s)
            {
                Rod("GlobeStem", root, p, p + Vector3.up * 0.75f * s, 0.07f * s, wood);
                foreach (float a in new[] { 0f, 120f, 240f })
                    Rod("GlobeFoot", root, p + Vector3.up * 0.05f, p + Quaternion.Euler(0, a, 0) * Vector3.forward * 0.3f * s, 0.05f * s, wood);
                Prim("Globe", root, PrimitiveType.Sphere, p + Vector3.up * 1.05f * s, Vector3.one * 0.55f * s, K.paperB);
                var ring = Prim("Meridian", root, PrimitiveType.Cylinder, p + Vector3.up * 1.05f * s, new Vector3(0.64f * s, 0.008f, 0.64f * s), K.brass);
                ring.transform.localRotation = Quaternion.Euler(0, 0, 66);
            }
            Globe(new Vector3(-2.9f, PY, 0.4f), 1f);
            Kit("StudyTable", root, new Vector3(-2.7f, PY, 5.3f), 180f);
            Kit("Armchair", root, new Vector3(-2.7f, PY, 4.3f), 0f, 0.95f);
            Kit("BookStack", root, new Vector3(-3.2f, PY + 0.78f, 5.4f), 15f);
            Kit("PaperPile", root, new Vector3(-2.5f, PY + 0.78f, 5.3f), -10f);
            Kit("Compass", root, new Vector3(-2.0f, PY + 0.78f, 5.2f), 40f);
            Kit("L_Candle", root, new Vector3(-2.2f, PY + 1.3f, 5.3f));
            Kit("CandlestickWood", root, new Vector3(-2.2f, PY + 0.78f, 5.45f));
            Kit("Chest", root, new Vector3(3.0f, PY, 5.3f), 180f, 0.85f);
            Kit("BookStack", root, new Vector3(3.2f, PY, 3.4f), 70f);
            Kit("PaperScatter", root, new Vector3(1.6f, PY, 1.0f), 40f);
            Kit("Rug", root, new Vector3(0, PY, 3.4f), 0f, 1.1f);
            Kit("Bottle1", root, new Vector3(3.4f, PY, 2.6f), 0f);

            // --- Kürsü altı: harita arşivi (çekmeceler, harita masası, mumlar) ---
            Kit("Drawer", root, new Vector3(-2.6f, 0, pz1 - 0.6f), 180f);
            Kit("Drawer", root, new Vector3(2.6f, 0, pz1 - 0.6f), 180f);
            Kit("Commode", root, new Vector3(0, 0, pz1 - 0.55f), 180f);
            Kit("VaseCeramic", root, new Vector3(0.4f, 0.95f, pz1 - 0.5f), 0f, 0.7f);
            Kit("Candlestick", root, new Vector3(-0.4f, 0.95f, pz1 - 0.5f));
            Kit("Table", root, new Vector3(0, 0, 3.0f), 90f, 1.1f);
            {
                float th = 0.82f * 1.1f;
                Slab("UnderMap", root, new Vector3(0.1f, th + 0.004f, 3.0f), new Vector3(1.0f, 0.004f, 1.3f), K.paperA, 1f, false).transform.localRotation = Quaternion.Euler(0, 6, 0);
                Kit("TeaSet", root, new Vector3(-0.1f, th, 2.5f), 90f);
                Kit("L_Candle", root, new Vector3(0, PY - 0.9f, 3.0f));
            }
            Kit("CandleCluster", root, new Vector3(px0 + 0.7f, 0, pz0 + 0.8f));
            Kit("Bucket", root, new Vector3(px1 - 0.6f, 0, pz0 + 0.9f), 30f);
            Kit("CrateLong", root, new Vector3(px1 - 0.9f, 0, 2.6f), 90f);

            // --- Kuzey duvarı: dev duvar haritası + iki tomar rafı + gül pencere ---
            {
                var mz = hz - 0.08f;
                Slab("WallMapFrame", root, new Vector3(0, 5.6f, mz), new Vector3(6.4f, 3.4f, 0.08f), wood, 1f, false);
                Slab("WallMap", root, new Vector3(0, 5.6f, mz - 0.05f), new Vector3(6.0f, 3.0f, 0.02f), K.paperA, 1f, false);
                for (int i = 0; i < 6; i++) // harita üzerinde "kıta" lekeleri ve rota çizgileri
                    Slab("Land", root, new Vector3(-2.2f + i * 0.9f, 5.0f + Hash01(i, 5) * 1.3f, mz - 0.065f), new Vector3(0.5f + Hash01(i, 6) * 0.6f, 0.4f + Hash01(i, 7) * 0.5f, 0.01f), K.paperC, 1f, false);
                for (int i = 0; i < 4; i++)
                    Slab("Route", root, new Vector3(-1.6f + i * 1.1f, 5.4f + (i % 2) * 0.5f, mz - 0.07f), new Vector3(1.0f, 0.025f, 0.01f), soot, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 18 : -14);
                Kit("WindowRose", root, new Vector3(0, 7.9f, hz), 180f, 0.6f);
                foreach (int s in new[] { -1, 1 })
                {
                    MapRack(root, new Vector3(s * 6.6f, 0, hz - 0.35f), 180f, 3.2f, 2.6f);
                    Kit("Banner", root, new Vector3(s * 3.9f, 7.4f, hz - 0.12f), 180f, 1.4f);
                    Kit("WallSconce", root, new Vector3(s * 3.9f, 2.6f, hz - 0.05f), 180f);
                    Kit("Ladder", root, new Vector3(s * 8.6f, 0, hz - 0.9f), 180f + s * 8f);
                }
                Kit("PaperScatter", root, new Vector3(-5.2f, 0, hz - 1.3f), 120f);
            }

            // --- Batı duvarı: şömine (taş ocak, kütükler, kor), koltuklar, silah panosu, dede saati ---
            {
                float fx = -hx + 0.1f, fz = -1.0f;
                Slab("Hearth", root, new Vector3(fx + 0.8f, 0.06f, fz), new Vector3(1.6f, 0.12f, 3.4f), stone, 1f);
                Slab("JambN", root, new Vector3(fx + 0.4f, 0.8f, fz + 1.3f), new Vector3(0.8f, 1.6f, 0.4f), stone, 1f);
                Slab("JambS", root, new Vector3(fx + 0.4f, 0.8f, fz - 1.3f), new Vector3(0.8f, 1.6f, 0.4f), stone, 1f);
                Slab("FireBack", root, new Vector3(fx + 0.04f, 0.8f, fz), new Vector3(0.08f, 1.6f, 2.2f), soot, 1f, false);
                Slab("FireBox", root, new Vector3(fx + 0.4f, 1.62f, fz), new Vector3(0.8f, 0.05f, 2.2f), soot, 1f, false);
                Slab("Mantel", root, new Vector3(fx + 0.5f, 1.72f, fz), new Vector3(1.0f, 0.2f, 3.4f), wood, 1f, false);
                Slab("Hood", root, new Vector3(fx + 0.35f, 2.9f, fz), new Vector3(0.7f, 2.2f, 2.6f), stone, 1.2f, false);
                Slab("Chimney", root, new Vector3(fx + 0.25f, 5.9f, fz), new Vector3(0.5f, 3.8f, 1.6f), stone, 1.2f, false);
                for (int i = 0; i < 3; i++)
                    Rod("Log", root, new Vector3(fx + 0.3f + i * 0.1f, 0.2f + (i == 2 ? 0.14f : 0f), fz - 0.6f + i * 0.08f), new Vector3(fx + 0.4f, 0.2f + (i == 2 ? 0.14f : 0f), fz + 0.6f - i * 0.1f), 0.16f, wood);
                Slab("Embers", root, new Vector3(fx + 0.4f, 0.14f, fz), new Vector3(0.5f, 0.04f, 1.3f), ember, 1f, false);
                Kit("L_Candelabra", root, new Vector3(fx + 1.0f, 0.8f, fz));
                foreach (int s in new[] { -1, 1 })
                {
                    Kit("BrassCandleholder" + (s < 0 ? "0" : "2"), root, new Vector3(fx + 0.5f, 1.82f, fz + s * 1.3f), 0f);
                    Kit("Armchair", root, new Vector3(fx + 2.9f, 0, fz + s * 1.3f), -90f + s * 35f);
                }
                Kit("Goblet0", root, new Vector3(fx + 0.5f, 1.82f, fz + 0.5f));
                Kit("Goblet1", root, new Vector3(fx + 0.5f, 1.82f, fz - 0.35f));
                Kit("Jug", root, new Vector3(fx + 0.55f, 1.82f, fz + 0.9f), 30f);
                Kit("Console", root, new Vector3(fx + 3.6f, 0, fz), 90f, 0.8f);
                Kit("ChessSet", root, new Vector3(fx + 3.6f, 0.68f, fz), 90f, 0.8f);
                Kit("Bottle2", root, new Vector3(fx + 3.3f, 0, fz + 2.1f));
                Kit("Goblet2", root, new Vector3(fx + 0.5f, 1.82f, fz - 0.8f));
                Kit("MantelClock", root, new Vector3(fx + 0.5f, 1.82f, fz + 0.1f), 90f);
                Kit("Rug", root, new Vector3(fx + 2.6f, 0, fz), 90f, 1.2f);
                // silah panosu (ocak üstü): kalkan, topuz ve savaş çekici çapraz
                KitR("Shield", root, new Vector3(fx + 0.53f, 4.6f, fz), Quaternion.Euler(0, 90, 0), 1.0f);
                KitR("Mace", root, new Vector3(fx + 0.57f, 4.6f, fz - 0.3f), Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 0, 30f));
                KitR("WarHammer", root, new Vector3(fx + 0.57f, 4.6f, fz + 0.3f), Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 0, -30f));
                Kit("GrandfatherClock", root, new Vector3(-hx + 0.4f, 0, 5.5f), 90f);
                KitR("PaintingPortrait", root, new Vector3(-hx + 0.04f, 3.9f, 8.6f), Quaternion.Euler(0, 90, 0), 1.3f);
                Kit("Commode", root, new Vector3(-hx + 0.45f, 0, 8.6f), 90f);
                Kit("FrameStanding", root, new Vector3(-hx + 0.4f, 0.95f, 8.3f), 100f);
                Kit("Amphora", root, new Vector3(-hx + 0.45f, 0.95f, 9.1f), 0f, 0.7f);
                KitR("PaintingLandscape", root, new Vector3(-hx + 0.04f, 3.4f, -6.5f), Quaternion.Euler(0, 90, 0), 1.6f);
                Books(root, new Vector3(-hx + 0.4f, 0, -7.4f), 90f);
                Books(root, new Vector3(-hx + 0.4f, 0, -5.6f), 90f);
                Kit("BookStack", root, new Vector3(-hx + 1.1f, 0, -6.5f), 30f);
                Kit("WallSconce", root, new Vector3(-hx + 0.05f, 2.6f, 3.0f), 90f);
                Kit("WallSconce", root, new Vector3(-hx + 0.05f, 2.6f, -4.3f), 90f);
            }

            // --- Doğu duvarı: perdeli uzun pencereler, aralarda çekmeceler, amforalar, yolculuk sandıkları ---
            foreach (float z in new[] { -6.5f, 0f, 6.5f })
            {
                KitR("WindowLancet", root, new Vector3(hx, 3.6f, z), Quaternion.Euler(0, -90, 0), 1.5f);
                foreach (int s in new[] { -1, 1 })
                    Kit("Curtain", root, new Vector3(hx - 0.12f, 6.0f, z + s * 1.1f), -90f, 1.2f);
            }
            foreach (float z in new[] { -3.25f, 3.25f })
            {
                Kit("Drawer", root, new Vector3(hx - 0.5f, 0, z), -90f);
                Kit("Pot", root, new Vector3(hx - 0.45f, 0.85f, z - 0.3f));
                Kit("BrassCandleholder1", root, new Vector3(hx - 0.45f, 0.85f, z + 0.3f));
                KitR("PaperA", root, new Vector3(hx - 0.07f, 1.9f, z - 0.2f), Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 6, 0));
                KitR("PaperC", root, new Vector3(hx - 0.07f, 2.1f, z + 0.35f), Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, -8, 0));
            }
            Slab("AmphoraPlinthN", root, new Vector3(hx - 0.6f, 0.4f, 9.6f), new Vector3(0.8f, 0.8f, 0.8f), stone, 1f);
            Kit("Amphora", root, new Vector3(hx - 0.6f, 0.8f, 9.6f), 0f, 1.0f);
            Kit("Chest", root, new Vector3(hx - 0.8f, 0, -10.0f), -90f);
            Kit("BarrelLying", root, new Vector3(hx - 1.0f, 0, -8.6f), 0f);
            Kit("Basket", root, new Vector3(hx - 2.0f, 0, -10.8f), 30f);
            Kit("Lantern", root, new Vector3(hx - 1.6f, 0, -9.4f), 0f);
            Kit("CrateStack", root, new Vector3(hx - 0.9f, 0, -11.0f), 0f);

            // --- Güney: giriş kapısı, sancaklar, sahte harita masası (batı köşe), avizeler ---
            Kit("Door", root, new Vector3(0, 0, -hz + 0.12f), 0f, 1.15f);
            foreach (int s in new[] { -1, 1 })
            {
                Kit("Banner", root, new Vector3(s * 2.6f, 6.2f, -hz + 0.12f), 0f, 1.6f);
                Kit("CandelabraFloor", root, new Vector3(s * 2.0f, 0, -hz + 0.6f), 0f);
                KitR("PaintingLandscape", root, new Vector3(s * 6.0f, 3.6f, -hz + 0.04f), Quaternion.identity, 1.4f);
            }
            {
                var dc = new Vector3(-6.4f, 0, -8.4f);
                Kit("Table", root, dc, 20f, 1.15f);
                Kit("TableCloth", root, dc + Vector3.up * 0.94f, 20f);
                float th = 0.82f * 1.15f;
                Slab("DecoyMapA", root, dc + new Vector3(-0.2f, th + 0.012f, 0.1f), new Vector3(0.9f, 0.004f, 0.7f), K.paperB, 1f, false).transform.localRotation = Quaternion.Euler(0, 30, 0);
                Slab("DecoyMapB", root, dc + new Vector3(0.4f, th + 0.016f, -0.2f), new Vector3(0.7f, 0.004f, 0.6f), K.paperA, 1f, false).transform.localRotation = Quaternion.Euler(0, 2, 0);
                for (int i = 0; i < 4; i++)
                    Prim("Scroll", root, PrimitiveType.Cylinder, dc + new Vector3(-0.6f + i * 0.4f, th + 0.06f, 0.5f), new Vector3(0.09f, 0.3f, 0.09f), i % 2 == 0 ? K.paperA : K.paperC)
                        .transform.localRotation = Quaternion.Euler(0, 20f + 12f * i, 90);
                Kit("OilLamp", root, dc + new Vector3(0.5f, th + 0.01f, 0.35f));
                Kit("Dagger", root, dc + new Vector3(0.1f, th + 0.02f, -0.35f), 70f);
                Kit("Stool", root, dc + new Vector3(1.2f, 0, -0.7f), -30f);
                Kit("Stool", root, dc + new Vector3(-1.2f, 0, 0.6f), 50f);
                Kit("L_Candle", root, dc + new Vector3(0.5f, 1.5f, 0.35f));
            }
            Kit("Rug", root, new Vector3(0, 0, -9.6f), 0f, 1.3f);
            Globe(new Vector3(5.6f, 0, -5.2f), 1.25f);
            foreach (float z in new[] { -8.5f, 8.5f })
            {
                Rod("Chain", root, new Vector3(0, 6.4f, z), new Vector3(0, TOP - 0.1f, z), 0.03f, K.iron);
                Kit("ChandelierOrnate", root, new Vector3(0, 6.4f, z), 0f, 1.3f);
                Kit("L_Chandelier", root, new Vector3(0, 5.8f, z));
            }
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, TOP - 1.2f, -hz + 0.03f), Quaternion.Euler(0, 180, 180), 2.4f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, TOP - 1.2f, hz - 0.03f), Quaternion.Euler(0, 0, 180), 2.2f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, 0, -hz + 0.03f), Quaternion.Euler(0, 180, 0), 1.3f);
            KitR("Cobweb", root, new Vector3(px0 + 0.3f, PY - 0.42f, pz0 + 0.3f), Quaternion.Euler(0, 180, 180), 1.0f);
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, 0, hz - 0.03f), Quaternion.identity, 1.2f);

            // --- Kâşif koleksiyonu (yeni CC0 modeller): gemi maketi, top, balina heykeli, aslan başları, kılıçlar, deniz kabukları ---
            Kit("GothicTable", root, new Vector3(hx - 1.5f, 0, 0), 90f);
            Kit("ShipModel", root, new Vector3(hx - 1.5f, 0.56f, 0), 0f);
            Kit("Cannon", root, new Vector3(hx - 2.2f, 0, -4.6f), -100f);
            Kit("Bucket", root, new Vector3(hx - 1.2f, 0, -5.4f), 0f, 0.8f);
            Slab("WhalePlinth", root, new Vector3(-6.0f, 0.3f, 8.2f), new Vector3(1.3f, 0.6f, 1.3f), stone, 1f);
            Kit("WhaleStatue", root, new Vector3(-6.0f, 0.6f, 8.2f), 140f);
            foreach (int s in new[] { -1, 1 })
            {
                Kit("LionHead", root, new Vector3(s * 1.8f, 3.9f, -hz + 0.06f), 0f);
                Kit("BookSet", root, new Vector3(s * 6.6f, 2.78f, hz - 0.4f), 180f, 0.42f);
                Kit("BrassVase", root, new Vector3(s * 4.2f, 0, -hz + 0.5f), 0f);
                foreach (float z in new[] { -3.25f, 3.25f })
                    KitR("Saber", root, new Vector3(hx - 0.06f, 2.0f, z), Quaternion.Euler(s * 38f, 0, 0));
            }
            Kit("Shell", root, new Vector3(hx - 0.4f, 0.85f, -3.0f), 40f);
            Kit("Shell", root, new Vector3(hx - 0.4f, 0.85f, 3.55f), 200f);
            Kit("Shell", root, new Vector3(-1.9f, PY + 0.78f, 5.05f), 80f);
            Kit("Elephant", root, new Vector3(-3.4f, PY + 0.78f, 5.1f), 150f);
            Kit("Elephant", root, new Vector3(-hx + 0.4f, 0.95f, 8.95f), 70f);
            // çerçeveli haritalar (duvar)
            foreach (var (pos, rot) in new[] { (new Vector3(-hx + 0.06f, 4.6f, 3.0f), Quaternion.Euler(0, 90, 0)), (new Vector3(-hx + 0.06f, 4.6f, -4.3f), Quaternion.Euler(0, 90, 0)),
                                               (new Vector3(-6.0f, 6.0f, -hz + 0.06f), Quaternion.identity), (new Vector3(6.0f, 6.0f, -hz + 0.06f), Quaternion.identity) })
            {
                var fm = new GameObject("FramedMap").transform;
                fm.SetParent(root, false); fm.localPosition = pos; fm.localRotation = rot;
                Slab("Frame", fm, Vector3.zero, new Vector3(1.5f, 1.1f, 0.06f), wood, 1f, false);
                Slab("Map", fm, new Vector3(0, 0, 0.035f), new Vector3(1.32f, 0.92f, 0.01f), K.paperB, 1f, false);
                Slab("Coast", fm, new Vector3(-0.2f, 0.1f, 0.042f), new Vector3(0.5f, 0.4f, 0.005f), K.paperC, 1f, false);
                Slab("Route", fm, new Vector3(0.15f, -0.1f, 0.045f), new Vector3(0.7f, 0.02f, 0.004f), soot, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, 20);
            }
            Kit("BookStack", root, new Vector3(-5.0f, 0, hz - 0.9f), 50f);
            Kit("PaperPile", root, new Vector3(5.2f, 0, hz - 1.0f), 20f);
            Kit("CandleCluster", root, new Vector3(4.9f, 0, -hz + 0.6f));
            Kit("CandleCluster", root, new Vector3(-hx + 1.4f, 0, 6.3f));
            KitR("Cobweb", root, new Vector3(px1 - 0.3f, PY - 0.42f, pz1 - 0.3f), Quaternion.Euler(0, 0, 180), 0.9f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, TOP - 1.2f, -hz + 0.03f), Quaternion.Euler(0, -90, 180), 1.8f);

            arrive = new GameObject("MapArrive").transform;
            arrive.SetParent(root, false);
            arrive.localPosition = new Vector3(0, 0.05f, -hz + 1.6f);

            var stair = new Bounds(new Vector3(0, PY * 0.35f, (sz0 + pz0) * 0.5f), new Vector3(sw + 0.1f, PY * 0.7f, steps * tread));
            var deck = new Bounds(new Vector3(0, PY - 0.2f, (pz0 + pz1) * 0.5f), new Vector3(px1 - px0 + 0.2f, 0.6f, pz1 - pz0 + 0.2f));
            KitProbes(root, new Vector3(0, TOP * 0.5f, 0), new Vector3(SX, TOP - 0.4f, SZ), 6, 5, 7, stair, deck);
            KitReflection(root, new Vector3(0, 3f, 0), new Vector3(SX, TOP, SZ));
            return root;
        }

        /// <summary>Tomar rafı (gözlü dolap, her gözde rulo haritalar). pos: taban orta; yaw 180: +Z duvara yaslı, -Z odaya.</summary>
        static void MapRack(Transform root, Vector3 pos, float yaw, float w, float h)
        {
            var t = new GameObject("MapRack").transform;
            t.SetParent(root, false); t.localPosition = pos; t.localRotation = Quaternion.Euler(0, yaw, 0);
            const float d = 0.5f; int cols = 5, rows = 5;
            Slab("Back", t, new Vector3(0, h * 0.5f, -d * 0.5f + 0.02f), new Vector3(w, h, 0.04f), K.furniture, 1f, false);
            Slab("Body", t, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), K.furniture, 1f).GetComponent<MeshRenderer>().enabled = false;
            for (int i = 0; i <= cols; i++)
                Slab("Div", t, new Vector3(-w * 0.5f + w * i / cols, h * 0.5f, 0), new Vector3(0.05f, h, d), K.furniture, 1f, false);
            for (int j = 0; j <= rows; j++)
                Slab("Shelf", t, new Vector3(0, 0.1f + (h - 0.12f) * j / rows, 0), new Vector3(w, 0.04f, d), K.furniture, 1f, false);
            Slab("Crown", t, new Vector3(0, h + 0.06f, 0.03f), new Vector3(w + 0.12f, 0.12f, d + 0.08f), K.beam, 1f, false);
            float cw = w / cols, ch = (h - 0.12f) / rows;
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                {
                    int n = (int)(Hash01(i, j, 77) * 4f);
                    for (int k = 0; k < n; k++)
                    {
                        float rr = 0.035f + 0.015f * Hash01(i * 7 + k, j, 78);
                        var s = Prim("Roll", t, PrimitiveType.Cylinder, new Vector3(-w * 0.5f + cw * (i + 0.5f) + (k - 1) * 0.11f, 0.1f + ch * j + 0.04f + rr + (k == 3 ? 0.08f : 0f), 0.02f),
                                     new Vector3(rr * 2f, 0.21f + 0.03f * k, rr * 2f), (i + j + k) % 3 == 0 ? K.paperC : (i + k) % 2 == 0 ? K.paperA : K.paperB);
                        s.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    }
                }
        }

        // ---------------- Katakomb (O) ----------------

        static Transform BuildCatacomb(Transform roomO, out Transform mazeRoot, out Transform arrive, out Vector3 anteCenter, out Vector3 anteSize)
        {
            const int N = LabyrinthPuzzle.N; const float C = LabyrinthPuzzle.Cell, H = 4.2f;
            float L = N * C, h = L * 0.5f; // 19.2 (8x2.4)
            var root = new GameObject("RoomB5_Katakomb").transform;
            root.SetParent(roomO, false);
            root.localPosition = new Vector3(40f, 0, -48f);
            var stone = K.castleStone; var brick = K.cellarBrick;
            var boneMat = MakeMat("K_Bone", new Color(0.6f, 0.55f, 0.46f));
            boneMat.SetFloat("_Smoothness", 0.25f);
            var voidM = MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f));
            var floorM = KTex("K_CatacombFloor", "mossy_cobblestone", new Color(0.46f, 0.46f, 0.47f), 0.3f, 1.2f); // turuncu derzli mahzen zemini yerine
            float entryX = -h + 0.5f * C;

            // --- Labirent kabuğu: zemin, tavan, dış duvarlar (giriş güneyde, çıkış kuzeyde), ızgara sütunları, tavan kaburgaları ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), L + 1.2f, L + 1.2f, 0.5f, floorM, 3, 3); // kapı eşiklerinin altını da kaplar
            Deck(root, "Ceil", new Vector3(0, H + 0.25f, 0), L + 0.6f, L + 0.6f, 0.5f, brick, 3, 3);
            Wall(root, "WallS", new Vector3(0, 0, -h - 0.25f), true, L + 5f, H, stone, 2f, new Opening(entryX, 2.4f, 0f, 3f));
            var northOps = new Opening[N]; // her hücreye açıklık; çıkış dışındakileri LabyrinthView çalışma anında kapatır
            for (int i = 0; i < N; i++) northOps[i] = new Opening(-h + (i + 0.5f) * C, C - 0.5f, 0f, 3f);
            Wall(root, "WallN", new Vector3(0, 0, h + 0.25f), true, L + 5f, H, stone, 2f, northOps);
            Wall(root, "WallW", new Vector3(-h - 0.25f, 0, 0), false, L, H, stone, 2f);
            Wall(root, "WallE", new Vector3(h + 0.25f, 0, 0), false, L, H, stone, 2f);
            for (int i = 0; i <= N; i++)
            {
                float g = -h + i * C;
                Slab("RibX", root, new Vector3(0, H - 0.18f, g), new Vector3(L, 0.36f, 0.34f), stone, 1.2f, false);
                Slab("RibZ", root, new Vector3(g, H - 0.18f, 0), new Vector3(0.34f, 0.36f, L), stone, 1.2f, false);
                for (int j = 0; j <= N; j++)
                {
                    float gz = -h + j * C;
                    Slab("Pillar", root, new Vector3(g, (H - 0.2f) * 0.5f, gz), new Vector3(0.5f, H - 0.2f, 0.5f), stone, 1.2f);
                    Slab("PillarBase", root, new Vector3(g, 0.12f, gz), new Vector3(0.66f, 0.24f, 0.66f), stone, 1.2f, false);
                    if ((i + j) % 2 == 0) Slab("Keystone", root, new Vector3(g, H - 0.42f, gz), new Vector3(0.5f, 0.2f, 0.5f), stone, 1f, false);
                }
            }
            mazeRoot = new GameObject("MazeOrigin").transform;
            mazeRoot.SetParent(root, false);
            mazeRoot.localPosition = new Vector3(-h, 0, -h);

            // --- Kemik nişleri (dış duvarlar): raflarda kafatası ve kemikler ---
            var osu = new GameObject("Ossuary").transform;
            osu.SetParent(root, false);
            var rnd = new System.Random(5150);
            void Skull(Vector3 p, float yaw)
            {
                var s = new GameObject("Skull").transform;
                s.SetParent(osu, false); s.localPosition = p; s.localRotation = Quaternion.Euler(0, yaw, 0);
                Prim("Cranium", s, PrimitiveType.Sphere, new Vector3(0, 0.1f, 0.01f), new Vector3(0.17f, 0.17f, 0.2f), boneMat);
                Prim("Face", s, PrimitiveType.Sphere, new Vector3(0, 0.06f, -0.07f), new Vector3(0.13f, 0.12f, 0.1f), boneMat);
                Prim("Jaw", s, PrimitiveType.Cube, new Vector3(0, 0.015f, -0.07f), new Vector3(0.09f, 0.03f, 0.07f), boneMat);
                foreach (int k in new[] { -1, 1 })
                    Prim("Eye", s, PrimitiveType.Sphere, new Vector3(k * 0.035f, 0.08f, -0.115f), new Vector3(0.04f, 0.035f, 0.02f), voidM);
            }
            void Bone(Vector3 a, Vector3 b)
            {
                Rod("Bone", osu, a, b, 0.035f, boneMat);
                Prim("Knob", osu, PrimitiveType.Sphere, a, Vector3.one * 0.06f, boneMat);
                Prim("Knob", osu, PrimitiveType.Sphere, b, Vector3.one * 0.06f, boneMat);
            }
            for (int i = 0; i < N; i++)
                foreach (var (edge, inward, yaw) in new[] { (0, Vector3.right, 90f), (1, Vector3.left, -90f), (2, Vector3.forward, 0f), (3, Vector3.back, 180f) })
                {
                    if ((i + edge) % 2 == 1) continue; // her iki kenardan biri
                    if (edge == 2 && i == 0) continue; // giriş
                    if (edge == 3) continue; // kuzey: açıklıklar (çıkış her denemede değişir)
                    float a = -h + (i + 0.5f) * C;
                    Vector3 wallPt = edge == 0 ? new Vector3(-h, 0, a) : edge == 1 ? new Vector3(h, 0, a) : edge == 2 ? new Vector3(a, 0, -h) : new Vector3(a, 0, h);
                    var along = edge < 2 ? Vector3.forward : Vector3.right;
                    foreach (float y in new[] { 0.9f, 1.65f })
                    {
                        var c = wallPt + inward * 0.18f + Vector3.up * y;
                        Slab("Shelf", osu, c, edge < 2 ? new Vector3(0.36f, 0.05f, C - 0.7f) : new Vector3(C - 0.7f, 0.05f, 0.36f), stone, 1f, false);
                        for (int k = -1; k <= 1; k++)
                            if (rnd.Next(4) != 0) Skull(c + along * (k * C * 0.2f) + Vector3.up * 0.025f, yaw + rnd.Next(-25, 25));
                        Bone(c + along * -(C * 0.5f - 0.45f) + Vector3.up * 0.06f + inward * 0.05f, c + along * (C * 0.5f - 0.45f) + Vector3.up * 0.06f + inward * 0.08f);
                    }
                    if (i % 3 == 1) // bazı raflarda mum kümesi (loş, yön bulmaya yardım etmez)
                    {
                        Kit("CandleCluster", root, wallPt + inward * 0.2f + Vector3.up * 0.925f + along * (C * 0.32f), 0f, 0.7f);
                        Kit("L_Candle", root, wallPt + inward * 0.6f + Vector3.up * 1.3f);
                    }
                }
            // Yerde dağınık kemik/kafatası (hücre merkezlerinden kayık; duvar çizgilerine taşmaz)
            for (int k = 0; k < 22; k++)
            {
                int cx = rnd.Next(N), cz = rnd.Next(N);
                var p = new Vector3(-h + (cx + 0.3f + 0.4f * (float)rnd.NextDouble()) * C, 0.03f, -h + (cz + 0.3f + 0.4f * (float)rnd.NextDouble()) * C);
                if (k % 3 == 0) Skull(p, rnd.Next(360));
                else { float ang = rnd.Next(360) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.22f; Bone(p - d, p + d); }
            }
            foreach (var t in osu.GetComponentsInChildren<Transform>(true))
            {
                var col = t.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col);
            }
            for (int k = 0; k < 5; k++)
            {
                float x = -h + (rnd.Next(N) + 0.5f) * C, z = -h + (rnd.Next(N) + 0.5f) * C;
                KitR("Cobweb", root, new Vector3(x - C * 0.5f + 0.3f, H - 0.4f, z - C * 0.5f + 0.3f), Quaternion.Euler(0, 45, 180), 1.4f);
            }

            // --- Giriş odası v2 (güney): tonozlu kemik odası + kayıp kâşifin kampı (sahte tebeşir okları, yarım günlük) ---
            {
                const float az0 = -8.5f, AW = 12f, asp = 3.0f, arise = 1.5f;
                float zc = -h - 0.5f + az0 * 0.5f, ax = entryX + 2.6f, zS = -h - 0.5f + az0, zN = -h - 0.5f;
                var chalk = MakeMat("K_Chalk", new Color(0.78f, 0.76f, 0.7f));
                var burlap = MakeMat("K_Burlap", new Color(0.36f, 0.29f, 0.2f)); burlap.SetFloat("_Smoothness", 0.05f);
                Deck(root, "AnteFloor", new Vector3(ax, -0.25f, zc), AW, -az0, 0.5f, floorM, 2, 2);
                Wall(root, "AnteS", new Vector3(ax, 0, zS - 0.25f), true, AW + 1f, asp + arise + 0.4f, stone, 2f);
                Wall(root, "AnteW", new Vector3(ax - AW * 0.5f - 0.25f, 0, zc), false, -az0, asp + arise + 0.4f, stone, 2f);
                Wall(root, "AnteE", new Vector3(ax + AW * 0.5f + 0.25f, 0, zc), false, -az0, asp + arise + 0.4f, stone, 2f);
                var av = new GameObject("AnteVault").transform; av.SetParent(root, false); av.localPosition = new Vector3(0, 0, zc);
                VaultStrip(av, "Vault", ax, AW + 0.2f, -az0 * 0.5f + 0.05f, asp, arise, 0.3f, brick, 10, 2f);
                foreach (float x in new[] { ax - 3.6f, ax, ax + 3.6f })
                {
                    VaultStrip(av, "Rib", x, 0.45f, -az0 * 0.5f - 0.08f, asp, arise - 0.12f, 0.28f, stone, 10, 1.2f);
                    foreach (int s in new[] { -1, 1 }) Slab("Pilaster", root, new Vector3(x, asp * 0.5f, zc + s * (-az0 * 0.5f - 0.14f)), new Vector3(0.45f, asp, 0.28f), stone, 1.2f);
                }
                ArchTrim(root, "EntryArch", new Vector3(entryX, 0, zN), Vector3.back, 2.6f, 3.3f, false);
                arrive = new GameObject("CatacombArrive").transform;
                arrive.SetParent(root, false);
                arrive.localPosition = new Vector3(ax - 1.2f, 0.05f, zS + 2.3f);
                anteCenter = root.TransformPoint(new Vector3(ax, 1.5f, zc));
                anteSize = new Vector3(AW, 3f, -az0);
                // Kemik rafları (doğu/batı duvarları): üç kat kafatası + uzun kemik
                foreach (int s in new[] { -1, 1 })
                {
                    float wx = ax + s * (AW * 0.5f - 0.2f);
                    var inward = s < 0 ? Vector3.right : Vector3.left; float yaw = s < 0 ? 90f : -90f;
                    for (int r = 0; r < 3; r++)
                    {
                        float y = 0.6f + r * 0.75f;
                        foreach (float z in new[] { zc - 2.2f, zc + 1.4f })
                        {
                            var c = new Vector3(wx, y, z);
                            Slab("Shelf", osu, c, new Vector3(0.38f, 0.05f, 2.6f), stone, 1f, false);
                            for (int k = -2; k <= 2; k++) if (rnd.Next(5) != 0) Skull(c + Vector3.forward * (k * 0.48f) + Vector3.up * 0.025f, yaw + rnd.Next(-20, 20));
                            Bone(c + Vector3.forward * -1.15f + Vector3.up * 0.06f + inward * 0.06f, c + Vector3.forward * 1.15f + Vector3.up * 0.06f + inward * 0.1f);
                        }
                    }
                    Kit("WallSconce", root, new Vector3(ax + s * (AW * 0.5f - 0.03f), 2.5f, zc - 0.4f), yaw);
                    Kit("L_Candle", root, new Vector3(ax + s * (AW * 0.5f - 0.7f), 2.6f, zc - 0.4f));
                }
                // Rahip heykeli + yarı açık lahit (güney duvarı ortası)
                Slab("MonkPlinth", root, new Vector3(ax - 3.4f, 0.3f, zS + 0.7f), new Vector3(1.0f, 0.6f, 1.0f), stone, 1.2f);
                Kit("StatueKing", root, new Vector3(ax - 3.4f, 0.6f, zS + 0.7f), 0f, 1.1f);
                Slab("Sarcophagus", root, new Vector3(ax + 0.6f, 0.45f, zS + 0.9f), new Vector3(2.4f, 0.9f, 1.1f), stone, 1.2f);
                Slab("SarcoLid", root, new Vector3(ax + 0.9f, 0.97f, zS + 1.05f), new Vector3(2.5f, 0.14f, 1.2f), stone, 1.2f, false).transform.localRotation = Quaternion.Euler(0, 14, 4); // kapak kaydırılmış
                Prim("SarcoVoid", root, PrimitiveType.Cube, new Vector3(ax - 0.4f, 0.91f, zS + 0.75f), new Vector3(0.6f, 0.02f, 0.8f), voidM);
                Kit("CandleCluster", root, new Vector3(ax + 1.4f, 1.04f, zS + 0.8f));
                Kit("CandelabraFloor", root, new Vector3(ax - 2.0f, 0, zS + 0.6f), 20f);
                Kit("CandelabraFloor", root, new Vector3(ax + 2.6f, 0, zS + 0.6f), -20f);
                Kit("L_Candelabra", root, new Vector3(ax + 2.6f, 1.6f, zS + 0.6f));
                // Kâşifin kampı (batı köşe): yatak, söndürülmüş fener, günlük, ip, erzak
                var cp = new Vector3(ax - 4.0f, 0, zc + 1.6f);
                Slab("Bedroll", root, cp + new Vector3(0, 0.04f, 0), new Vector3(0.8f, 0.08f, 1.9f), burlap, 1f, false).transform.localRotation = Quaternion.Euler(0, 12, 0);
                Slab("Pillow", root, cp + new Vector3(0.05f, 0.1f, 0.75f), new Vector3(0.6f, 0.1f, 0.3f), K.velvet, 1f, false).transform.localRotation = Quaternion.Euler(0, 12, 0);
                Kit("Lantern", root, cp + new Vector3(0.8f, 0.12f, -0.3f), 0f).transform.localRotation = Quaternion.Euler(0, 30, 80); // devrik fener
                Kit("BookStack", root, cp + new Vector3(0.7f, 0, 0.6f), 30f, 0.7f);
                Kit("PaperA", root, cp + new Vector3(0.9f, 0.005f, 0.2f), 70f);
                Kit("PaperScatter", root, cp + new Vector3(1.4f, 0.005f, -0.8f), 20f);
                Kit("Basket", root, cp + new Vector3(-0.5f, 0, -1.3f), 40f);
                Kit("Bowl", root, cp + new Vector3(0.6f, 0, 1.2f), 0f);
                Kit("Jug", root, cp + new Vector3(0.9f, 0, 1.0f), 50f);
                for (int i = 0; i < 3; i++) MeshObj("RopeCoil", root, cp + new Vector3(-0.3f, 0.03f + i * 0.04f, -0.4f), RingMesh(0.22f - i * 0.015f, 0.04f, 0.04f), K.beam);
                Kit("Spade", root, new Vector3(ax - AW * 0.5f + 0.3f, 0, zc + 3.3f), 100f);
                // Detay: duvarlarda tebeşir okları (bir kısmı yanlış yönü gösteriyor) + sayım çizgileri
                void Arrow(Vector3 p, float yawWall, float dir)
                {
                    var a = new GameObject("ChalkArrow").transform; a.SetParent(root, false); a.localPosition = p; a.localRotation = Quaternion.Euler(0, yawWall, dir);
                    Slab("Shaft", a, Vector3.zero, new Vector3(0.36f, 0.035f, 0.005f), chalk, 1f, false);
                    Slab("HeadA", a, new Vector3(0.15f, 0.05f, 0), new Vector3(0.14f, 0.03f, 0.005f), chalk, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, -40);
                    Slab("HeadB", a, new Vector3(0.15f, -0.05f, 0), new Vector3(0.14f, 0.03f, 0.005f), chalk, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, 40);
                }
                Arrow(new Vector3(entryX + 1.6f, 1.6f, zN - 0.03f), 180f, 0f);
                Arrow(new Vector3(entryX - 1.5f, 1.3f, zN - 0.03f), 180f, 180f);
                for (int l = 0; l < 7; l++) Slab("Tally", root, new Vector3(entryX + 2.3f + l * 0.07f, 1.1f, zN - 0.03f), new Vector3(0.012f, 0.18f, 0.005f), chalk, 1f, false);
                // Erzak (doğu köşe)
                Kit("Barrel", root, new Vector3(ax + 4.9f, 0, zS + 0.7f), 30f);
                Kit("Barrel", root, new Vector3(ax + 4.1f, 0, zS + 0.6f), 110f, 0.9f);
                Kit("Crate", root, new Vector3(ax + 4.8f, 0, zS + 1.8f), 15f);
                Kit("OilLamp", root, new Vector3(ax + 4.8f, 0.6f, zS + 1.8f));
                Kit("OilLamp", root, new Vector3(ax + 4.5f, 0, zc + 0.6f));
                Kit("Bucket", root, new Vector3(ax + 4.6f, 0, zc + 1.4f));
                Kit("Pot", root, new Vector3(ax + 3.9f, 0, zc + 2.4f));
                Kit("Axe", root, new Vector3(ax + AW * 0.5f - 0.15f, 0, zc + 2.8f), -90f);
                Kit("L_Lantern", root, new Vector3(ax + 3.6f, 0, zc + 1.2f), 20f);
                Kit("Rat", root, new Vector3(ax - 1.2f, 0, zc - 2.3f), 40f);
                Skull(new Vector3(ax - 1.2f, 0.03f, zc - 1.6f), 40f);
                Bone(new Vector3(ax - 0.7f, 0.04f, zc - 2.1f), new Vector3(ax - 0.2f, 0.04f, zc - 1.8f));
                KitR("Cobweb", root, new Vector3(ax - AW * 0.5f + 0.03f, asp - 0.1f, zS + 0.03f), Quaternion.Euler(0, 90, 180), 1.4f);
                KitR("Cobweb", root, new Vector3(ax + AW * 0.5f - 0.03f, 0, zN - 0.03f), Quaternion.Euler(0, 180, 0), 1.2f);
                foreach (float fx in new[] { -3f, 3f })
                {
                    var f = LightGo("AnteFill", LightType.Point, Pal.Candle, 3f, 8f, true);
                    f.transform.SetParent(root, false); f.transform.localPosition = new Vector3(ax + fx, 3.0f, zc);
                }
                KitProbes(root, new Vector3(ax, 1.9f, zc), new Vector3(AW, 3.4f, -az0), 4, 2, 3);
            }
            // --- Çıkış galerisi (kuzey, tüm genişlik): hangi açıklığın açık olduğu dışarıdan anlaşılmaz; ortada kapalı kapı (sonraki bölüm) ---
            {
                const float gw = 2.6f; float zc = h + 0.5f + gw * 0.5f, gl = L + 1f;
                Deck(root, "ExitFloor", new Vector3(0, -0.25f, zc), gl, gw, 0.5f, floorM, 3, 1);
                Deck(root, "ExitCeil", new Vector3(0, 3.25f, zc), gl, gw, 0.5f, brick, 3, 1);
                Wall(root, "ExitN", new Vector3(0, 0, zc + gw * 0.5f + 0.25f), true, gl + 1f, 3.5f, stone, 2f);
                Wall(root, "ExitW", new Vector3(-gl * 0.5f - 0.25f, 0, zc), false, gw, 3.5f, stone, 2f);
                Wall(root, "ExitE", new Vector3(gl * 0.5f + 0.25f, 0, zc), false, gw, 3.5f, stone, 2f);
                Kit("Door", root, new Vector3(0, 0, zc + gw * 0.5f - 0.12f), 180f);
                foreach (float x in new[] { -6.5f, -2.2f, 2.2f, 6.5f })
                    Kit("CandleCluster", root, new Vector3(x, 0, zc + gw * 0.5f - 0.4f));
                for (int k = 0; k < 6; k++)
                    Skull(new Vector3(-8.5f + k * 3.3f, 0.03f, zc + 0.6f - (k % 2) * 0.4f), k * 57f);
            }
            ProbeLit(osu.gameObject, false); // kafatası/kemikler ışık sondasından aydınlansın (mavimsi kalıyordu)
            for (int fx = -1; fx <= 1; fx++) for (int fz = -1; fz <= 1; fz++)
            {   // labirentte loş baked dolgu (duvarlar seçilsin, yol belli olmasın)
                var p = new Vector3(fx * 6.2f, 3.4f, fz * 6.2f);
                var f = LightGo("MazeFill", LightType.Point, Pal.Candle, 2.6f, 7.5f, true);
                f.transform.SetParent(root, false); f.transform.localPosition = p;
            }
            KitProbes(root, new Vector3(0, 2f, 0), new Vector3(L, 3.6f, L), 6, 2, 6);
            return root;
        }
    }
}
