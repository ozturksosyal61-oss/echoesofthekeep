using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Bulmaca 5 "Katakomb labirenti".
    /// X: Harita odası (ref harita.png) kule yerel (40,0,44); 20x24, tavan ~9.8 ahşap beşik tonoz, U biçimli galeri (y 4.5),
    ///    ortada sahte harita masası; asıl maket doğu galerinin altındaki girintide (aranır).
    /// O: Katakomb (ref katakomb.png) roomO yerel (40,0,-48); güneyde giriş odası, 6x6 hücre labirent (iç duvarlar çalışma anında),
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
        }

        // ---------------- Harita odası (X) ----------------

        static Transform BuildMapRoom(Transform roomX, out Transform modelRoot, out Transform arrive)
        {
            const float SX = 20f, SZ = 24f, hx = SX * 0.5f, hz = SZ * 0.5f, SP = 6.5f, RS = 3.3f, GY = 4.5f, GD = 3f;
            var root = new GameObject("RoomA5_Harita").transform;
            root.SetParent(roomX, false);
            root.localPosition = new Vector3(40f, 0, 44f);
            var stone = K.castleStone;
            float top = SP + RS;

            Deck(root, "Floor", new Vector3(0, -0.25f, 0), SX, SZ, 0.5f, K.hallFloor, 3, 4);
            Wall(root, "WallE", new Vector3(hx + 0.25f, 0, 0), false, SZ, SP + 0.4f, stone, 2.2f);
            Wall(root, "WallW", new Vector3(-hx - 0.25f, 0, 0), false, SZ, SP + 0.4f, stone, 2.2f);
            Wall(root, "WallN", new Vector3(0, 0, hz + 0.25f), true, SX + 1f, top + 0.4f, stone, 2.2f);
            Wall(root, "WallS", new Vector3(0, 0, -hz - 0.25f), true, SX + 1f, top + 0.4f, stone, 2.2f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, top + 0.6f, 0), new Vector3(SX + 1.5f, 0.5f, SZ + 1.5f), K.beam, 2f, false), 0);
            // Ahşap beşik tonoz (z boyunca) + kaburgalar + gergi kirişleri
            var vault = new GameObject("WoodVault").transform;
            vault.SetParent(root, false);
            vault.localRotation = Quaternion.Euler(0, 90, 0);
            VaultStrip(vault, "Vault", 0, SZ, hx + 0.1f, SP, RS, 0.25f, K.beam, 16, 1.6f);
            foreach (float z in new[] { -8f, -3f, 3f, 8f })
            {
                VaultStrip(vault, "Rib" + z, -z, 0.4f, hx - 0.1f, SP, RS - 0.2f, 0.32f, K.beam, 16, 1.2f);
                Rod("TieBeam", root, new Vector3(-hx + 0.2f, SP, z), new Vector3(hx - 0.2f, SP, z), 0.3f, K.beam);
                Rod("KingPost", root, new Vector3(0, SP, z), new Vector3(0, top - 0.25f, z), 0.24f, K.beam);
            }
            foreach (int s in new[] { -1, 1 })
                Wainscot(root, "Wain", new Vector3(s * hx, 0, 0), false, SZ, s < 0 ? Vector3.right : Vector3.left, 1.4f);

            // --- U biçimli galeri (y 4.5): batı, doğu, kuzey kanatları + güneybatı merdiveni ---
            const float gz0 = -5.6f;
            foreach (int s in new[] { -1, 1 })
            {
                Deck(root, "GalleryS" + s, new Vector3(s * (hx - GD * 0.5f), GY - 0.2f, (gz0 + hz) * 0.5f), GD, hz - gz0, 0.4f, K.parquet, 1, 2);
                Slab("FasciaSide", root, new Vector3(s * (hx - GD - 0.12f), GY - 0.45f, (gz0 + hz) * 0.5f), new Vector3(0.24f, 0.9f, hz - gz0), K.beam, 1.5f, false);
                Slab("FasciaEnd", root, new Vector3(s * (hx - GD * 0.5f), GY - 0.45f, gz0 - 0.12f), new Vector3(GD, 0.9f, 0.24f), K.beam, 1.5f, false);
                Balustrade(root, "RailSide", new Vector3(s * (hx - GD - 0.1f), GY, gz0), new Vector3(s * (hx - GD - 0.1f), GY, hz - GD), true);
                foreach (float z in new[] { -4.6f, 0.5f, 5.5f })
                    Pillar(root, "GalleryPost", new Vector3(s * (hx - GD - 0.3f), 0, z), GY - 0.4f, 0.55f);
            }
            Deck(root, "GalleryN", new Vector3(0, GY - 0.2f, hz - GD * 0.5f), SX - 2f * GD, GD, 0.4f, K.parquet, 2, 1);
            Slab("FasciaN", root, new Vector3(0, GY - 0.45f, hz - GD - 0.12f), new Vector3(SX - 2f * GD, 0.9f, 0.24f), K.beam, 1.5f, false);
            Balustrade(root, "RailN", new Vector3(-hx + GD, GY, hz - GD - 0.1f), new Vector3(hx - GD, GY, hz - GD - 0.1f), true);
            Balustrade(root, "RailEndE", new Vector3(hx - GD, GY, gz0 - 0.1f), new Vector3(hx - 0.1f, GY, gz0 - 0.1f), true);
            foreach (float x in new[] { -3.5f, 3.5f })
                Pillar(root, "GalleryPostN", new Vector3(x, 0, hz - GD - 0.3f), GY - 0.4f, 0.55f);
            {   // merdiven: x -10..-7 boyunca, z -11.6 → -5.6
                const float sx0 = -hx + 0.3f, sw = 2.5f, sz0 = -11.6f;
                float run = gz0 - sz0; int steps = 18; float sr = run / steps, sh = GY / steps;
                for (int i = 0; i < steps; i++)
                    Slab("Step" + i, root, new Vector3(sx0 + sw * 0.5f, sh * (i + 1) * 0.5f, sz0 + sr * (i + 0.5f)), new Vector3(sw, sh * (i + 1), sr), stone, 1.5f, false);
                float ang = Mathf.Atan2(GY, run);
                var ramp = Slab("StairRamp", root, Vector3.zero, new Vector3(sw, 0.3f, Mathf.Sqrt(run * run + GY * GY) + 0.3f), stone, 1f);
                ramp.GetComponent<MeshRenderer>().enabled = false;
                GameObjectUtility.SetStaticEditorFlags(ramp, 0);
                ramp.transform.localPosition = new Vector3(sx0 + sw * 0.5f, GY * 0.5f - 0.15f * Mathf.Cos(ang), sz0 + run * 0.5f + 0.15f * Mathf.Sin(ang));
                ramp.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
                Balustrade(root, "StairRail", new Vector3(sx0 + sw, 0, sz0 + 0.1f), new Vector3(sx0 + sw, GY, gz0), false);
            }

            // --- Güney: giriş kapısı (geliş), vitraylar, sancaklar ---
            Kit("Door", root, new Vector3(0, 0, -hz + 0.12f), 0f, 1.15f);
            ArchTrim(root, "EntryArch", new Vector3(0, 0, -hz), Vector3.forward, 2.7f, 4.0f, true);
            foreach (int s in new[] { -1, 1 })
            {
                KitR("WindowLancet", root, new Vector3(s * 4.2f, 4.6f, -hz), Quaternion.identity, 1.6f);
                Kit("Banner", root, new Vector3(s * 2.4f, 4.4f, -hz), 0f, 1.6f);
                Kit("WallSconce", root, new Vector3(s * 2.0f, 2.4f, -hz + 0.03f), 0f);
                Kit("L_Candle", root, new Vector3(s * 2.0f, 2.5f, -hz + 0.7f));
            }
            // --- Kuzey: galeri arkası pencereler + gül pencere, galeride kitaplıklar ---
            Kit("WindowRose", root, new Vector3(0, 7.6f, hz), 180f, 1.0f);
            foreach (float x in new[] { -4.5f, 4.5f })
                KitR("WindowLancet", root, new Vector3(x, GY + 0.6f, hz), Quaternion.Euler(0, 180, 0), 1.25f);
            foreach (float x in new[] { -7.5f, -6f, -1.2f, 1.2f, 6f, 7.5f })
            {
                Kit("Bookshelf", root, new Vector3(x, GY, hz - 0.4f), 180f);
                Kit("BookRow", root, new Vector3(x, GY + 0.98f, hz - 0.4f), 180f);
            }
            Kit("StudyTable", root, new Vector3(-2.6f, GY, hz - 1.9f), 0f);
            Kit("OilLamp", root, new Vector3(-2.0f, GY + 0.78f, hz - 1.8f));
            Kit("L_Candle", root, new Vector3(-2.0f, GY + 1.3f, hz - 1.8f));
            Kit("Armillary", root, new Vector3(3.0f, GY, hz - 1.6f), 20f, 1.1f);
            // Galeri altı (kuzey): kitaplıklar, sandık
            foreach (float x in new[] { -5.6f, -4f, 4f, 5.6f })
            {
                Kit("Bookshelf", root, new Vector3(x, 0, hz - 0.4f), 180f);
                Kit("BookRow", root, new Vector3(x, 0.98f, hz - 0.4f), 180f);
            }
            Kit("Chest", root, new Vector3(0, 0, hz - 0.6f), 180f);
            Kit("CandelabraFloor", root, new Vector3(-1.5f, 0, hz - 0.6f), 0f);
            Kit("CandelabraFloor", root, new Vector3(1.5f, 0, hz - 0.6f), 0f);
            Kit("L_Candelabra", root, new Vector3(0, 1.9f, hz - 1.2f));

            // --- Batı: galeri altında büstler + duvar haritaları, galeride kitaplık ---
            foreach (float z in new[] { -2.5f, 3.2f })
            {
                Slab("BustPedestal", root, new Vector3(-hx + 0.6f, 0.55f, z), new Vector3(0.6f, 1.1f, 0.6f), stone, 1.2f);
                Kit("Bust", root, new Vector3(-hx + 0.6f, 1.1f, z), 90f, 0.9f);
            }
            KitR("PaintingLandscape", root, new Vector3(-hx + 0.04f, 2.4f, 0.4f), Quaternion.Euler(0, 90, 0), 1.6f);
            KitR("PaintingLandscape", root, new Vector3(-hx + 0.04f, 2.4f, 7.0f), Quaternion.Euler(0, 90, 0), 1.4f);
            foreach (float z in new[] { -3.5f, -1.9f, 4.5f, 6.1f })
            {
                Kit("Bookshelf", root, new Vector3(-hx + 0.4f, GY, z), 90f);
                Kit("BookRow", root, new Vector3(-hx + 0.4f, GY + 0.98f, z), 90f);
            }
            Kit("Ladder", root, new Vector3(-hx + 0.6f, GY, 1.3f), 90f);
            KitR("WindowLancet", root, new Vector3(-hx, GY + 0.8f, 1.3f), Quaternion.Euler(0, 90, 0), 1.2f);

            // --- Doğu: galeri altında maket girintisi (ASIL BULMACA), şövalye heykeli, harita ---
            {
                var alc = new GameObject("ModelAlcove").transform;
                alc.SetParent(root, false);
                const float tx = 8.45f, tz = 1.8f, th = 0.95f;
                Slab("TableTop", alc, new Vector3(tx, th - 0.04f, tz), new Vector3(2.1f, 0.08f, 2.1f), K.furniture, 1f);
                foreach (int a in new[] { -1, 1 })
                    foreach (int b in new[] { -1, 1 })
                        Slab("Leg", alc, new Vector3(tx + a * 0.9f, (th - 0.08f) * 0.5f, tz + b * 0.9f), new Vector3(0.12f, th - 0.08f, 0.12f), K.furniture, 1f, false);
                Slab("ModelBase", alc, new Vector3(tx, th + 0.01f, tz), new Vector3(1.75f, 0.02f, 1.75f), K.cellarFloor, 0.6f, false);
                ProbeLit(alc.gameObject);
                modelRoot = new GameObject("LabyrinthModel").transform;
                modelRoot.SetParent(root, false);
                modelRoot.localPosition = new Vector3(tx - 0.8f, th + 0.02f, tz - 0.8f);
                Kit("L_Lantern", root, new Vector3(tx + 0.85f, th, tz + 0.85f), 30f);
                Kit("L_Candelabra", root, new Vector3(tx - 0.3f, 2.4f, tz));
                Kit("Stool", root, new Vector3(tx - 1.4f, 0, tz - 0.6f), 20f);
                Kit("PaperScatter", root, new Vector3(tx - 1.3f, 0, tz + 1.0f), 80f);
                KitR("PaperA", root, new Vector3(hx - 0.07f, 1.7f, tz - 0.6f), Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 6, 0));
                KitR("PaperC", root, new Vector3(hx - 0.07f, 1.55f, tz + 0.7f), Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, -8, 0));
                // Girinti önünde kitaplık perdesi (girişten doğrudan görünmesin)
                foreach (float z in new[] { -1.0f, 4.6f })
                {
                    Kit("Bookshelf", root, new Vector3(hx - GD + 0.5f, 0, z), 0f);
                    Kit("BookRow", root, new Vector3(hx - GD + 0.5f, 0.98f, z), 0f);
                }
            }
            ArchTrim(root, "KnightNiche", new Vector3(hx, 0, -8.6f), Vector3.left, 2.2f, 4.0f, true);
            Slab("KnightPlinth", root, new Vector3(hx - 0.7f, 0.3f, -8.6f), new Vector3(1.2f, 0.6f, 1.2f), stone, 1.2f);
            Kit("StatueKing", root, new Vector3(hx - 0.7f, 0.6f, -8.6f), -90f, 1.25f);
            KitR("PaintingPortrait", root, new Vector3(hx - 0.04f, 2.5f, 7.2f), Quaternion.Euler(0, -90, 0), 1.2f);
            KitR("WindowLancet", root, new Vector3(hx, GY + 0.8f, 1.3f), Quaternion.Euler(0, -90, 0), 1.2f);
            foreach (float z in new[] { -3.5f, 4.5f, 6.1f })
            {
                Kit("Bookshelf", root, new Vector3(hx - 0.4f, GY, z), -90f);
                Kit("BookRow", root, new Vector3(hx - 0.4f, GY + 0.98f, z), -90f);
            }

            // --- Orta: sahte harita masası (dünya haritaları, tomarlar, gök küresi) ---
            {
                Kit("Table", root, new Vector3(0, 0, -0.85f), 0f, 1.2f);
                Kit("Table", root, new Vector3(0, 0, -0.05f), 0f, 1.2f);
                float th = 0.82f * 1.2f;
                Slab("MapA", root, new Vector3(-0.6f, th + 0.004f, -0.6f), new Vector3(1.1f, 0.004f, 0.8f), K.paperA, 1f, false).transform.localRotation = Quaternion.Euler(0, 8, 0);
                Slab("MapB", root, new Vector3(0.7f, th + 0.006f, -0.2f), new Vector3(0.9f, 0.004f, 0.7f), K.paperB, 1f, false).transform.localRotation = Quaternion.Euler(0, -12, 0);
                for (int i = 0; i < 5; i++)
                    Prim("Scroll", root, PrimitiveType.Cylinder, new Vector3(-1.1f + i * 0.5f, th + 0.05f, 0.45f + (i % 2) * 0.12f), new Vector3(0.09f, 0.32f, 0.09f), i % 2 == 0 ? K.paperA : K.paperC)
                        .transform.localRotation = Quaternion.Euler(0, 15f * i, 90);
                Kit("Armillary", root, new Vector3(1.1f, th, 0.3f), 40f, 0.8f);
                Kit("CandlestickWood", root, new Vector3(-0.2f, th, 0.2f));
                Kit("Goblet1", root, new Vector3(0.3f, th, -0.9f));
                Kit("L_Candelabra", root, new Vector3(0, 2.2f, -0.4f));
                Kit("Stool", root, new Vector3(1.6f, 0, -1.2f), -30f);
                Kit("Stool", root, new Vector3(-1.7f, 0, 0.4f), 50f);
                Kit("Rug", root, new Vector3(0, 0, -0.4f), 90f, 1.6f);
            }
            Kit("Rug", root, new Vector3(0, 0, -7.5f), 0f, 1.2f);
            Kit("Barrel2", root, new Vector3(6.5f, 0, -11.2f), 20f);
            Kit("Basket", root, new Vector3(5.6f, 0, -11.3f), 40f);
            Kit("VaseCeramic", root, new Vector3(-6.0f, 0, -11.3f), 0f);
            Kit("CrateStack", root, new Vector3(8.8f, 0, -11.0f), 0f);
            // Avizeler + tonozdan sancaklar
            foreach (float z in new[] { -6f, 0f, 6f })
            {
                Rod("Chain", root, new Vector3(0, 6.6f, z), new Vector3(0, top - 0.2f, z), 0.03f, K.iron);
                Kit(z == 0f ? "LanternChandelier" : "ChandelierIron", root, new Vector3(0, z == 0f ? 5.8f : 6.6f, z), 0f, z == 0f ? 1.3f : 1.4f);
                Kit("L_Chandelier", root, new Vector3(0, 5.2f, z));
            }
            foreach (float z in new[] { -8f, 3f })
                foreach (int s in new[] { -1, 1 })
                    KitR("Banner", root, new Vector3(s * 4.2f, SP - 0.1f, z), Quaternion.Euler(0, 90, 0), 1.3f);
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, SP - 0.2f, -hz + 0.03f), Quaternion.Euler(0, 180, 180), 2.4f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, SP - 0.2f, hz - 0.03f), Quaternion.Euler(0, 0, 180), 2.2f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, 0, -hz + 0.03f), Quaternion.Euler(0, 180, 0), 1.3f);
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, GY, hz - 0.03f), Quaternion.identity, 1.2f);

            arrive = new GameObject("MapArrive").transform;
            arrive.SetParent(root, false);
            arrive.localPosition = new Vector3(0, 0.05f, -hz + 1.6f);

            var stair = new Bounds(new Vector3(-hx + 1.55f, 2.2f, -8.6f), new Vector3(2.8f, 4.6f, 6.4f));
            KitProbes(root, new Vector3(0, top * 0.5f, 0), new Vector3(SX, top - 0.6f, SZ), 6, 4, 7, stair);
            KitReflection(root, new Vector3(0, 3f, 0), new Vector3(SX, top, SZ));
            return root;
        }

        // ---------------- Katakomb (O) ----------------

        static Transform BuildCatacomb(Transform roomO, out Transform mazeRoot, out Transform arrive, out Vector3 anteCenter, out Vector3 anteSize)
        {
            const int N = LabyrinthPuzzle.N; const float C = LabyrinthPuzzle.Cell, H = 4.2f;
            float L = N * C, h = L * 0.5f; // 19.2
            var root = new GameObject("RoomB5_Katakomb").transform;
            root.SetParent(roomO, false);
            root.localPosition = new Vector3(40f, 0, -48f);
            var stone = K.castleStone; var brick = K.cellarBrick;
            var boneMat = MakeMat("K_Bone", new Color(0.6f, 0.55f, 0.46f));
            boneMat.SetFloat("_Smoothness", 0.25f);
            var voidM = MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f));
            float entryX = -h + 0.5f * C, exitX = -h + (N - 0.5f) * C;

            // --- Labirent kabuğu: zemin, tavan, dış duvarlar (giriş güneyde, çıkış kuzeyde), ızgara sütunları, tavan kaburgaları ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), L + 0.6f, L + 0.6f, 0.5f, K.cellarFloor, 3, 3);
            Deck(root, "Ceil", new Vector3(0, H + 0.25f, 0), L + 0.6f, L + 0.6f, 0.5f, brick, 3, 3);
            Wall(root, "WallS", new Vector3(0, 0, -h - 0.25f), true, L + 1f, H, stone, 2f, new Opening(entryX, 2.4f, 0f, 3f));
            Wall(root, "WallN", new Vector3(0, 0, h + 0.25f), true, L + 1f, H, stone, 2f, new Opening(exitX, 2.4f, 0f, 3f));
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
                    if (edge == 3 && i == N - 1) continue; // çıkış
                    float a = -h + (i + 0.5f) * C;
                    Vector3 wallPt = edge == 0 ? new Vector3(-h, 0, a) : edge == 1 ? new Vector3(h, 0, a) : edge == 2 ? new Vector3(a, 0, -h) : new Vector3(a, 0, h);
                    var along = edge < 2 ? Vector3.forward : Vector3.right;
                    foreach (float y in new[] { 0.9f, 1.65f })
                    {
                        var c = wallPt + inward * 0.18f + Vector3.up * y;
                        Slab("Shelf", osu, c, edge < 2 ? new Vector3(0.36f, 0.05f, 2.2f) : new Vector3(2.2f, 0.05f, 0.36f), stone, 1f, false);
                        for (int k = -1; k <= 1; k++)
                            if (rnd.Next(4) != 0) Skull(c + along * (k * 0.62f) + Vector3.up * 0.025f, yaw + rnd.Next(-25, 25));
                        Bone(c + along * -0.9f + Vector3.up * 0.06f + inward * 0.05f, c + along * 0.9f + Vector3.up * 0.06f + inward * 0.08f);
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

            // --- Giriş odası (güney): fener rafı, yağ fıçıları, rahip heykeli, yazıt ---
            {
                const float az0 = -6.5f;
                float zc = -h - 0.5f + az0 * 0.5f, ax = entryX + 2f;
                Deck(root, "AnteFloor", new Vector3(ax, -0.25f, zc), 9f, -az0, 0.5f, K.cellarFloor, 2, 1);
                Deck(root, "AnteCeil", new Vector3(ax, 3.75f, zc), 9f, -az0, 0.5f, brick, 2, 1);
                Wall(root, "AnteS", new Vector3(ax, 0, -h - 0.5f + az0 - 0.25f), true, 9.5f, 3.5f, stone, 2f);
                Wall(root, "AnteW", new Vector3(ax - 4.75f, 0, zc), false, -az0, 3.5f, stone, 2f);
                Wall(root, "AnteE", new Vector3(ax + 4.75f, 0, zc), false, -az0, 3.5f, stone, 2f);
                ArchTrim(root, "EntryArch", new Vector3(entryX, 0, -h - 0.5f), Vector3.back, 2.6f, 3.3f, false);
                arrive = new GameObject("CatacombArrive").transform;
                arrive.SetParent(root, false);
                arrive.localPosition = new Vector3(ax, 0.05f, -h - 0.5f + az0 + 1.2f);
                anteCenter = root.TransformPoint(new Vector3(ax, 1.5f, zc));
                anteSize = new Vector3(9f, 3f, -az0);
                Kit("L_Lantern", root, new Vector3(ax + 3.6f, 0, zc + 1.2f), 20f);
                Kit("Barrel2", root, new Vector3(ax + 3.8f, 0, zc - 1.8f), 30f);
                Kit("Barrel2", root, new Vector3(ax + 3.0f, 0, zc - 2.4f), 110f);
                Kit("OilLamp", root, new Vector3(ax + 3.6f, 0, zc + 0.3f));
                Kit("OilLamp", root, new Vector3(ax + 3.3f, 0, zc + 0.7f));
                Slab("MonkPlinth", root, new Vector3(ax - 3.8f, 0.3f, zc), new Vector3(1.0f, 0.6f, 1.0f), stone, 1.2f);
                Kit("StatueKing", root, new Vector3(ax - 3.8f, 0.6f, zc), 90f, 1.1f);
                Kit("CandleCluster", root, new Vector3(ax - 3.2f, 0, zc + 1.2f));
                Kit("CandleCluster", root, new Vector3(entryX + 1.6f, 0, -h - 1.0f));
                Kit("Basket", root, new Vector3(ax + 2.0f, 0, zc - 2.6f), 20f);
                Kit("Axe", root, new Vector3(ax + 4.5f, 0, zc + 2.4f), -90f);
                Skull(new Vector3(ax - 1.2f, 0.03f, zc - 1.6f), 40f);
                Bone(new Vector3(ax - 0.7f, 0.04f, zc - 2.1f), new Vector3(ax - 0.2f, 0.04f, zc - 1.8f));
            }
            // --- Çıkış odası (kuzey): kapalı kapı (sonraki bölüm), mumlar ---
            {
                float zc = h + 0.5f + 2f;
                Deck(root, "ExitFloor", new Vector3(exitX, -0.25f, zc), 4f, 4f, 0.5f, K.cellarFloor, 1, 1);
                Deck(root, "ExitCeil", new Vector3(exitX, 3.75f, zc), 4f, 4f, 0.5f, brick, 1, 1);
                Wall(root, "ExitN", new Vector3(exitX, 0, zc + 2.25f), true, 4.5f, 3.5f, stone, 2f);
                Wall(root, "ExitW", new Vector3(exitX - 2.25f, 0, zc), false, 4f, 3.5f, stone, 2f);
                Wall(root, "ExitE", new Vector3(exitX + 2.25f, 0, zc), false, 4f, 3.5f, stone, 2f);
                Kit("Door", root, new Vector3(exitX, 0, zc + 1.88f), 180f);
                Kit("CandleCluster", root, new Vector3(exitX - 1.4f, 0, zc + 1.4f));
                Kit("CandleCluster", root, new Vector3(exitX + 1.4f, 0, zc + 1.4f));
            }
            KitProbes(root, new Vector3(0, 2f, 0), new Vector3(L, 3.6f, L), 4, 2, 4);
            return root;
        }
    }
}
