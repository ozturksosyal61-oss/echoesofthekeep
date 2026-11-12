using UnityEditor;
using UnityEngine;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Bulmaca 4 "Şifreli mektup" + lobi ölçeğinde iki oda.
    /// X: Arşiv (ref arsiv.png) kule yerel (40,0,0); 18x22, tavan 9, kuzeyde galeri + doğuda merdiven, ortada şifre çarkı.
    /// O: Mezar kütüphanesi (ref kutuphane.png) roomO yerel (40,0,0); 18x28, tonozlu orta nef + yan nefler, kürsüde mühürlü kapak (anahtar),
    /// 5 sembol 3 yırtık parçada (sıra = mürekkep noktası) kütüphaneye dağılmış, güneyde harf kilitli kapı.
    /// Girişler: Hazne doğu kapısı / sarnıç güney nişi → Passage (bulmaca 3 çözülünce ışınlar).
    /// Düğümler: 35 tohum, 36 çark, 37-41 kilit kadranları, 42-43 geçitler.
    /// </summary>
    public static partial class CoopSetup
    {
        static void BuildPuzzle4(Transform roomX, Transform roomO)
        {
            var def = new GameObject("Puzzle4").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 4;
            dso.FindProperty("title").stringValue = "Bulmaca 4 (mektup)";
            dso.FindProperty("startsActive").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();
            foreach (var d in Object.FindObjectsByType<PuzzleDef>(FindObjectsSortMode.None))
                if (d.Id == 3)
                {
                    var so = new SerializedObject(d);
                    var up = so.FindProperty("unlocks"); up.arraySize = 1; up.GetArrayElementAtIndex(0).intValue = 4;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

            var archive = BuildArchive(roomX, out var wheelRoot, out var archiveArrive);
            var library = BuildLibrary(roomO, out var lockRoot, out var lectern, out var libraryArrive, out var libDoor);

            var lp = new GameObject("Puzzle4_Letter").AddComponent<LetterPuzzle>();
            lp.transform.SetParent(library, false);

            // Çark (X)
            var wheel = wheelRoot.gameObject.AddComponent<CipherWheel>();
            var wso = new SerializedObject(wheel);
            wso.FindProperty("puzzleId").intValue = 4;
            wso.FindProperty("nodeId").intValue = 36;
            SetRef(wso, "letter", lp);
            SetRef(wso, "outerRing", wheelRoot.Find("OuterGlyphs"));
            SetRef(wso, "innerRing", wheelRoot.Find("Inner"));
            SetRef(wso, "numeralAnchor", wheelRoot.Find("NumeralAnchor"));
            SetRef(wso, "glyphMat", A.gold);
            SetRef(wso, "font", U.head);
            wso.ApplyModifiedPropertiesWithoutUndo();

            // Kilit kadranları (O)
            var dials = new LockDial[LetterPuzzle.Len];
            for (int i = 0; i < dials.Length; i++)
            {
                var dr = lockRoot.Find("LockDial" + i);
                dials[i] = dr.gameObject.AddComponent<LockDial>();
                var so = new SerializedObject(dials[i]);
                so.FindProperty("puzzleId").intValue = 4;
                so.FindProperty("nodeId").intValue = 37 + i;
                so.FindProperty("index").intValue = i;
                SetRef(so, "letter", lp);
                SetRef(so, "label", dr.GetComponentInChildren<TextMesh>());
                SetRef(so, "ring", dr.Find("Ring"));
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var lso = new SerializedObject(lp);
            lso.FindProperty("puzzleId").intValue = 4;
            lso.FindProperty("nodeId").intValue = 35;
            SetRefArray(lso, "dials", dials);
            var glyphs = new Transform[LetterPuzzle.Len];
            var all = library.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < glyphs.Length; i++) glyphs[i] = System.Array.Find(all, t => t.name == "Glyph" + i); // yırtık parçalarda (LetterFragments)
            SetRefArray(lso, "letterGlyphs", glyphs);
            SetRef(lso, "sealGlyph", lectern.Find("SealGlyph"));
            SetRef(lso, "inkMat", MakeMat("K_Ink", new Color(0.12f, 0.08f, 0.06f)));
            SetRef(lso, "sealMat", A.gold);
            lso.ApplyModifiedPropertiesWithoutUndo();

            var pd = libDoor.gameObject.AddComponent<PuzzleDoor>();
            var pso = new SerializedObject(pd);
            pso.FindProperty("puzzleId").intValue = 4;
            SetRef(pso, "door", libDoor);
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Geçitler: Hazne doğu kapısı (X) ve sarnıç güney nişi (O) → yeni odalar
            var hazne = roomX.Find("RoomA2_SaatOdasi/RoomA3_Hazne");
            var cis = roomO.Find("OWest/Cistern"); // mahzen + sarnıç OWest altında (kasa büyüyünce batıya kaydı)
            if (hazne != null) HazneExit(hazne, archiveArrive);
            if (cis != null) MakePassage("PassageO", cis, 43, cis.TransformPoint(new Vector3(CsNicheX, 1f, -CsZ - 1.65f)), new Vector3(3f, 2f, 1.0f), libraryArrive);
            if (cis != null)
            {
                var t = ArchTrim(cis, "NicheDark", new Vector3(CsNicheX, 0, -CsZ - 2.24f), Vector3.forward, 2.6f, 3.0f, true);
                KeepBaked(t);
            }
            KeepBaked(archive);
            KeepBaked(library);

            // Kontrol noktaları (menüden "Bulmaca N" ile başlayınca rol alan oyuncu buraya ışınlanır; 1 için rolün ilk odası)
            var clock = roomX.Find("RoomA2_SaatOdasi");
            if (clock != null) MakeCheckpoint(roomX, 2, Roles.X, clock.TransformPoint(new Vector3(-1.5f, 0.05f, -1.5f)));
            MakeCheckpoint(roomO, 2, Roles.O, roomO.TransformPoint(new Vector3(VtCx + 6.5f, 0.05f, 0)));
            if (hazne != null) MakeCheckpoint(roomX, 3, Roles.X, hazne.TransformPoint(new Vector3(0, 0.05f, 2.5f)));
            MakeCheckpoint(roomO, 3, Roles.O, roomO.TransformPoint(new Vector3(-21f + OWestShift, 0.05f, 0)));
            MakeCheckpoint(roomX, 4, Roles.X, archiveArrive.position);
            MakeCheckpoint(roomO, 4, Roles.O, libraryArrive.position);
        }

        static void MakeCheckpoint(Transform parent, int puzzle, int role, Vector3 worldPos)
        {
            var go = new GameObject("Checkpoint_" + Roles.Name(role) + puzzle);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var c = go.AddComponent<Checkpoint>();
            var so = new SerializedObject(c);
            so.FindProperty("puzzle").intValue = puzzle;
            so.FindProperty("role").intValue = role;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void MakePassage(string name, Transform parent, int node, Vector3 worldCenter, Vector3 size, Transform target, int puzzleId = 3)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<Passage>();
            var so = new SerializedObject(p);
            so.FindProperty("puzzleId").intValue = puzzleId;
            so.FindProperty("nodeId").intValue = node;
            so.FindProperty("zoneCenter").vector3Value = worldCenter;
            so.FindProperty("zoneSize").vector3Value = size;
            SetRef(so, "target", target);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Hazne doğu duvarında kapı + yukarı çıkan kısa merdiven ağzı; geçit arşive ışınlar.</summary>
        static void HazneExit(Transform r, Transform target)
        {
            const float z = 2.7f;
            var door = MakeDoor("ArchiveDoor", r, new Vector3(5.25f, 1.5f, z), false, K.panel);
            foreach (var dr in door.GetComponentsInChildren<Renderer>()) dr.sharedMaterial = dr.transform == door.transform ? K.panel : K.iron;
            var pd = door.gameObject.AddComponent<PuzzleDoor>();
            var so = new SerializedObject(pd);
            so.FindProperty("puzzleId").intValue = 3;
            SetRef(so, "door", door);
            so.ApplyModifiedPropertiesWithoutUndo();
            var c = new GameObject("ArchiveStair").transform;
            c.SetParent(r, false);
            Deck(c, "Floor", new Vector3(6.75f, -0.25f, z), 2.5f, 2.6f, 0.5f, K.castleStone, 1, 1);
            Deck(c, "Ceil", new Vector3(6.75f, 3.75f, z), 2.5f, 2.6f, 0.5f, K.beam, 1, 1);
            Wall(c, "N", new Vector3(6.75f, 0, z + 1.35f), true, 2.5f, 3.5f, K.castleStone, 2f);
            Wall(c, "S", new Vector3(6.75f, 0, z - 1.35f), true, 2.5f, 3.5f, K.castleStone, 2f);
            for (int i = 0; i < 6; i++)
                Slab("Step" + i, c, new Vector3(6.2f + i * 0.3f, 0.1f + i * 0.2f, z), new Vector3(0.3f, 0.2f + i * 0.4f, 2.2f), K.castleStone, 1f, false);
            ArchTrim(c, "Dark", new Vector3(8.0f, 0, z), Vector3.left, 2.2f, 3.2f, true);
            foreach (int s in new[] { -1, 1 })
                Slab("Jamb", r, new Vector3(4.92f, 1.55f, z + s * 1.2f), new Vector3(0.16f, 3.1f, 0.22f), K.beam, 1.2f, false);
            Slab("Lintel", r, new Vector3(4.92f, 3.12f, z), new Vector3(0.16f, 0.24f, 2.62f), K.beam, 1.2f, false);
            MakePassage("PassageX", r, 42, r.TransformPoint(new Vector3(7.2f, 1f, z)), new Vector3(1.4f, 2f, 2.2f), target);
        }

        // ---------------- Arşiv (X) ----------------

        static Transform BuildArchive(Transform roomX, out Transform wheelRoot, out Transform arrive)
        {
            const float SX = 18f, SZ = 22f, H = 9f, hx = SX * 0.5f, hz = SZ * 0.5f, GY = 4f, GZ = 7f;
            var root = new GameObject("RoomA4_Arsiv").transform;
            root.SetParent(roomX, false);
            root.localPosition = new Vector3(40f, 0, 0);
            var stone = K.castleStone;

            Deck(root, "Floor", new Vector3(0, -0.25f, 0), SX, SZ, 0.5f, K.hallFloor, 3, 4);
            Wall(root, "WallN", new Vector3(0, 0, hz + 0.25f), true, SX + 1f, H, stone, 2.2f);
            Wall(root, "WallS", new Vector3(0, 0, -hz - 0.25f), true, SX + 1f, H, stone, 2.2f);
            Wall(root, "WallW", new Vector3(-hx - 0.25f, 0, 0), false, SZ, H, stone, 2.2f);
            Wall(root, "WallE", new Vector3(hx + 0.25f, 0, 0), false, SZ, H, stone, 2.2f);
            BeamCeiling(root, "Ceil", new Vector3(0, H, 0), SX, SZ, 2.2f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, H + 0.6f, 0), new Vector3(SX + 1.5f, 0.5f, SZ + 1.5f), K.beam, 2f, false), 0);
            // Kemerli makaslar (ref: ahşap çatı kemerleri)
            foreach (float z in new[] { -7.5f, -2.5f, 2.5f })
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("TrussPost", root, new Vector3(s * (hx - 0.2f), H - 2.0f, z), new Vector3(0.36f, 4.0f, 0.36f), K.beam, 1.2f, false);
                    Rod("TrussArch", root, new Vector3(s * (hx - 0.3f), H - 3.6f, z), new Vector3(s * (hx - 2.6f), H - 0.4f, z), 0.28f, K.beam);
                    Rod("TrussCollar", root, new Vector3(s * (hx - 2.6f), H - 0.4f, z), new Vector3(0, H - 0.4f, z), 0.24f, K.beam);
                    Slab("Corbel", root, new Vector3(s * (hx - 0.3f), H - 4.15f, z), new Vector3(0.6f, 0.3f, 0.5f), stone, 1.2f, false);
                }

            // --- Kuzey galeri (y 4) + doğu merdiveni ---
            Deck(root, "Gallery", new Vector3(0, GY - 0.2f, (GZ + hz) * 0.5f), SX, hz - GZ, 0.4f, K.parquet, 3, 1);
            Slab("GalleryFascia", root, new Vector3(0, GY - 0.45f, GZ + 0.12f), new Vector3(SX, 0.9f, 0.24f), K.beam, 1.5f, false);
            foreach (float x in new[] { -6f, -2f, 2f })
                Pillar(root, "GalleryPost", new Vector3(x, 0, GZ + 0.5f), GY - 0.4f, 0.6f);
            const float sx0 = 6.4f, sw = 2.4f, sz0 = -1f; // merdiven: x 6.4..8.8, z -1 → 7
            {
                float run = GZ - sz0; int steps = 16; float sr = run / steps, sh = GY / steps;
                for (int i = 0; i < steps; i++)
                    Slab("Step" + i, root, new Vector3(sx0 + sw * 0.5f, sh * (i + 1) * 0.5f, sz0 + sr * (i + 0.5f)), new Vector3(sw, sh * (i + 1), sr), stone, 1.5f, false);
                float ang = Mathf.Atan2(GY, run);
                var ramp = Slab("StairRamp", root, Vector3.zero, new Vector3(sw, 0.3f, Mathf.Sqrt(run * run + GY * GY) + 0.3f), stone, 1f);
                ramp.GetComponent<MeshRenderer>().enabled = false;
                GameObjectUtility.SetStaticEditorFlags(ramp, 0);
                ramp.transform.localPosition = new Vector3(sx0 + sw * 0.5f, GY * 0.5f - 0.15f * Mathf.Cos(ang), sz0 + run * 0.5f + 0.15f * Mathf.Sin(ang));
                ramp.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
                Balustrade(root, "StairRail", new Vector3(sx0, 0, sz0 + 0.1f), new Vector3(sx0, GY, GZ), false);
                Balustrade(root, "GalleryRail", new Vector3(-hx + 0.1f, GY, GZ + 0.12f), new Vector3(sx0, GY, GZ + 0.12f), true);
                Slab("StairSide", root, new Vector3(sx0 - 0.02f, 0.2f, sz0 + run * 0.5f), new Vector3(0.05f, 0.4f, run), stone, 1.5f, false);
            }
            // Galeri altı ve üstü kitaplıklar, galeri kapısı (sonraki bulmaca)
            foreach (float x in new[] { -7.6f, -6.0f, -4.4f, 3.4f, 5.0f })
            {
                Books(root, new Vector3(x, 0, hz - 0.4f), 180f);
                if (x < -7f) continue; // galeride çarkın yeri
                Books(root, new Vector3(x, GY, hz - 0.4f), 180f);
            }
            Kit("Door", root, new Vector3(0, GY, hz - 0.12f), 180f);
            ArchTrim(root, "GalleryDoorArch", new Vector3(0, GY, hz), Vector3.back, 2.5f, 3.6f, true);
            Kit("WindowRose", root, new Vector3(0, 7.6f, hz), 180f, 0.8f);
            Kit("StudyTable", root, new Vector3(-3.0f, GY, 9.6f), 180f);
            Kit("Stool", root, new Vector3(-3.2f, GY, 8.7f), 20f);
            Kit("L_Lantern", root, new Vector3(-6.4f, GY, 7.7f), 30f); // çarkı aydınlatır
            Kit("Armillary", root, new Vector3(3.0f, GY, 8.4f), 30f);
            Kit("WallSconce", root, new Vector3(-2.1f, GY + 2.1f, hz - 0.03f), 180f);
            Kit("L_Candle", root, new Vector3(-2.1f, GY + 2.2f, hz - 0.7f));

            // --- Batı: nişlerde heykeller, üstte vitraylar, harita ---
            foreach (var (z, statue) in new[] { (-5.5f, "StatueAngel"), (0.5f, "StatueKing") })
            {
                ArchTrim(root, "NicheW", new Vector3(-hx, 0, z), Vector3.right, 2.3f, 4.4f, true);
                Slab("Plinth", root, new Vector3(-hx + 0.75f, 0.35f, z), new Vector3(1.3f, 0.7f, 1.3f), stone, 1.5f);
                Kit(statue, root, new Vector3(-hx + 0.75f, 0.7f, z), 90f, 1.2f);
                KitR("WindowLancet", root, new Vector3(-hx, 5.0f, z), Quaternion.Euler(0, 90, 0), 1.3f);
                Kit("WallSconce", root, new Vector3(-hx + 0.05f, 2.6f, z + 1.6f), 90f);
                Kit("L_Candle", root, new Vector3(-hx + 0.7f, 2.7f, z + 1.6f));
            }
            KitR("PaintingLandscape", root, new Vector3(-hx + 0.04f, 2.2f, 5.2f), Quaternion.Euler(0, 90, 0), 1.4f);
            Kit("Cabinet", root, new Vector3(-hx + 0.45f, 0, -9.0f), 90f);
            // --- Doğu: niş + heykel, vitray, harita, büst ---
            ArchTrim(root, "NicheE", new Vector3(hx, 0, -6f), Vector3.left, 2.3f, 4.4f, true);
            Slab("PlinthE", root, new Vector3(hx - 0.75f, 0.35f, -6f), new Vector3(1.3f, 0.7f, 1.3f), stone, 1.5f);
            Kit("StatueKing", root, new Vector3(hx - 0.75f, 0.7f, -6f), -90f, 1.2f);
            foreach (float z in new[] { -5f, 5f })
                KitR("WindowLancet", root, new Vector3(hx, 5.0f, z), Quaternion.Euler(0, -90, 0), 1.3f);
            KitR("PaintingKeep", root, new Vector3(hx - 0.04f, 2.6f, -9.3f), Quaternion.Euler(0, -90, 0), 1.3f);
            // --- Güney: giriş kapısı (geliş), iki büyük vitray, büstler ---
            Kit("Door", root, new Vector3(0, 0, -hz + 0.12f), 0f, 1.1f);
            ArchTrim(root, "EntryArch", new Vector3(0, 0, -hz), Vector3.forward, 2.6f, 3.8f, true);
            foreach (int s in new[] { -1, 1 })
            {
                KitR("WindowLancet", root, new Vector3(s * 4.6f, 3.0f, -hz), Quaternion.identity, 1.7f);
                Slab("BustPedestal", root, new Vector3(s * 7.6f, 0.55f, -9.6f), new Vector3(0.6f, 1.1f, 0.6f), stone, 1.2f);
                Kit("Bust", root, new Vector3(s * 7.6f, 1.1f, -9.6f), s * -30f, 0.9f);
                Kit("Banner", root, new Vector3(s * 4.6f, 8.0f, -hz), 0f, 1.8f);
            }
            // Kirişlerden sarkan sancaklar
            foreach (var p in new[] { new Vector2(-4.5f, -5f), new Vector2(4.5f, -5f), new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f) })
                KitR("Banner", root, new Vector3(p.x, 8.4f, p.y), Quaternion.Euler(0, 90, 0), 1.4f);

            // --- Orta: şifre çarkı (ahşap kaide üstünde, güneye bakar) + yazı masası ---
            {
                var stand = new GameObject("WheelStand").transform;
                stand.SetParent(root, false);
                stand.localPosition = new Vector3(-hx + 0.55f, GY, 9.0f); // galerinin batı ucu: girişten görünmez, aranır
                stand.localRotation = Quaternion.Euler(0, -90, 0);
                foreach (int s in new[] { -1, 1 })
                    Slab("Post", stand, new Vector3(s * 1.3f, 1.6f, 0.15f), new Vector3(0.18f, 3.2f, 0.18f), K.furniture, 1.2f);
                Slab("Cross", stand, new Vector3(0, 3.25f, 0.15f), new Vector3(2.8f, 0.18f, 0.18f), K.furniture, 1.2f, false);
                Slab("Foot", stand, new Vector3(0, 0.1f, 0.15f), new Vector3(2.9f, 0.2f, 0.5f), K.furniture, 1.2f);
                ProbeLit(stand.gameObject);

                var w = new GameObject("CipherWheel").transform;
                w.SetParent(root, false);
                w.localPosition = new Vector3(-hx + 0.5f, GY + 2.0f, 9.0f);
                w.localRotation = Quaternion.Euler(0, -90, 0); // yüz doğuya (galeriye)
                var bc = w.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(2.4f, 2.4f, 0.25f);
                w.gameObject.AddComponent<Interactable>().Label = "Çarkı çevir";
                var axisZ = Quaternion.Euler(90, 0, 0);
                Prim("Back", w, PrimitiveType.Cylinder, new Vector3(0, 0, 0.08f), new Vector3(2.3f, 0.04f, 2.3f), K.bronze).transform.localRotation = axisZ;
                MeshObj("OuterRim", w, new Vector3(0, 0, 0.02f), RingMesh(1.12f, 0.08f, 0.06f), K.brass).transform.localRotation = axisZ;
                MeshObj("MidRim", w, new Vector3(0, 0, 0.02f), RingMesh(0.7f, 0.05f, 0.05f), K.brass).transform.localRotation = axisZ;
                var inner = new GameObject("Inner").transform;
                inner.SetParent(w, false);
                inner.localPosition = new Vector3(0, 0, -0.03f);
                Prim("Disc", inner, PrimitiveType.Cylinder, new Vector3(0, 0, 0.04f), new Vector3(1.32f, 0.02f, 1.32f), K.furniture).transform.localRotation = axisZ;
                Prim("Hub", inner, PrimitiveType.Sphere, new Vector3(0, 0, -0.02f), Vector3.one * 0.18f, K.brass);
                var og = new GameObject("OuterGlyphs").transform;
                og.SetParent(w, false);
                og.localPosition = new Vector3(0, 0, -0.02f);
                Prim("Pointer", w, PrimitiveType.Cube, new Vector3(0, 1.22f, -0.04f), new Vector3(0.1f, 0.18f, 0.06f), K.brass);
                Slab("NumeralPlate", w, new Vector3(0, 1.55f, 0.03f), new Vector3(0.7f, 0.36f, 0.05f), K.furniture, 1f, false);
                var na = new GameObject("NumeralAnchor").transform;
                na.SetParent(w, false);
                na.localPosition = new Vector3(0, 1.55f, -0.01f);
                var pa = new GameObject("ProbeAnchor").transform;
                pa.SetParent(w, false);
                pa.localPosition = new Vector3(0, -0.3f, -1.2f);
                foreach (var t in w.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    var rr = t.GetComponent<Renderer>(); if (rr != null) rr.probeAnchor = pa;
                }
                wheelRoot = w;
            }
            // Orta: harita masası (dikkat dağıtır; çark galeride)
            Kit("RoundTable", root, new Vector3(0, 0, 0.2f), 0f, 1.3f);
            Kit("Compass", root, new Vector3(0.3f, 1.05f, 0.1f), 40f);
            Kit("Magnifier", root, new Vector3(-0.35f, 1.05f, 0.35f), 120f);
            Kit("OilLamp", root, new Vector3(0.5f, 1.04f, 0.6f));
            Kit("PaperPile", root, new Vector3(-0.2f, 1.04f, -0.2f), 15f);
            Kit("CandlestickWood", root, new Vector3(-0.55f, 1.04f, -0.1f));
            Kit("L_Candelabra", root, new Vector3(0.4f, 1.8f, 0.2f));
            Kit("L_Lantern", root, new Vector3(-0.9f, 0, -1.1f));
            Kit("Stool", root, new Vector3(1.1f, 0, -0.9f), 30f);
            Kit("Stool", root, new Vector3(-1.2f, 0, 1.2f), -50f);
            // Silah duvarı (doğu, merdiven başı) + ayna + portre + at heykeli
            KitR("Shield", root, new Vector3(hx - 0.06f, 1.55f, -2.6f), Quaternion.Euler(0, -90, 0));
            foreach (int s in new[] { -1, 1 })
                KitR("Sword", root, new Vector3(hx - 0.1f, 1.45f, -2.6f + s * 0.25f), Quaternion.Euler(0, -90, 0) * Quaternion.Euler(0, 0, s * 28f));
            KitR("Mirror", root, new Vector3(-hx + 0.04f, 1.4f, -7.5f), Quaternion.Euler(0, 90, 0));
            KitR("PaintingPortrait", root, new Vector3(-hx + 0.04f, 2.3f, 3.6f), Quaternion.Euler(0, 90, 0), 1.3f);
            Slab("HorsePlinth", root, new Vector3(4.6f, 0.4f, -7.4f), new Vector3(1.2f, 0.8f, 0.8f), stone, 1.2f);
            Kit("HorseStatue", root, new Vector3(4.6f, 0.8f, -7.4f), -120f);
            Kit("DisplayShelf", root, new Vector3(-hx + 0.3f, 0, -6.9f + 4.6f), 90f);
            Kit("VaseCeramic", root, new Vector3(-hx + 0.5f, 0, -3.3f));
            Kit("Jug", root, new Vector3(0.1f, 1.04f, 0.6f), 40f);
            Kit("Plate", root, new Vector3(-0.2f, 1.04f, 0.55f));
            Kit("Basket", root, new Vector3(-6.8f, 0, -9.6f), 20f);
            Kit("LanternChandelier", root, new Vector3(0, H - 1.0f, 4.2f - 4.2f), 0f);
            Kit("PaperScatter", root, new Vector3(-1.4f, 0, -2.2f), 30f);
            Kit("PaperScatter", root, new Vector3(1.8f, 0, -1.8f), 150f);
            Kit("PaperPile", root, new Vector3(2.2f, 0, -0.4f), 70f);
            Kit("BookStack", root, new Vector3(1.9f, 0, 0.4f), 10f);
            Kit("CandleCluster", root, new Vector3(-1.9f, 0, -1.0f));
            Kit("Rug", root, new Vector3(0, 0, -2.6f), 0f, 1.3f);
            Kit("Rug", root, new Vector3(-4.5f, 0, 4.0f), 90f);
            Kit("Armillary", root, new Vector3(-2.6f, 0, 0.6f), 0f, 1.2f);
            Kit("CandelabraFloor", root, new Vector3(2.6f, 0, -0.2f), 0f);
            Kit("Chest", root, new Vector3(-7.3f, 0, 8.2f), 90f);
            Kit("CrateStack", root, new Vector3(-7.4f, 0, -2.6f), 90f);
            Kit("Ladder", root, new Vector3(-hx + 0.3f, 0, 7.6f), 90f);

            // --- Çan (çatıdan) + avize ---
            {
                const float bh = 1.4f, top = H - 1.2f;
                Kit("Bell", root, new Vector3(0, top - bh, 4.2f), 0f, bh / 1.1f);
                Slab("BellYoke", root, new Vector3(0, top + 0.12f, 4.2f), new Vector3(1.6f, 0.26f, 0.32f), K.beam, 1.2f, false);
                Rod("BellHanger", root, new Vector3(0, top + 0.25f, 4.2f), new Vector3(0, H, 4.2f), 0.1f, K.iron);
            }
            Rod("ChandelierChain", root, new Vector3(0, H - 2.0f, -5f), new Vector3(0, H, -5f), 0.03f, K.iron);
            Kit("ChandelierIron", root, new Vector3(0, H - 2.0f, -5f), 0f, 1.5f);
            Kit("L_Chandelier", root, new Vector3(0, 5.6f, -5f));
            Kit("L_Chandelier", root, new Vector3(-4f, 3.0f, 3f));
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, H - 0.3f, -hz + 0.03f), Quaternion.Euler(0, 180, 180), 2.2f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, H - 0.3f, hz - 0.03f), Quaternion.Euler(0, 0, 180), 2.0f);
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, 0, hz - 0.03f), Quaternion.identity, 1.2f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, 0, -hz + 0.03f), Quaternion.Euler(0, 180, 0), 1.2f);

            arrive = new GameObject("ArchiveArrive").transform;
            arrive.SetParent(root, false);
            arrive.localPosition = new Vector3(0, 0.05f, -9.3f);

            KitProbes(root, new Vector3(0, H * 0.5f, 0), new Vector3(SX, H, SZ), 6, 4, 7, new Bounds(new Vector3(7.6f, 2f, 3f), new Vector3(2.8f, 4.2f, 8.4f)));
            KitReflection(root, new Vector3(0, 3f, 0), new Vector3(SX, H, SZ));
            return root;
        }

        // ---------------- Mezar kütüphanesi (O) ----------------

        /// <summary>
        /// Yırtık mektup parçaları (bulmaca 4 zorlaştırma): semboller 3 parçada (sıra = köşedeki mürekkep noktası sayısı),
        /// biri doğu mezar nişinin rafında, biri batı lahdinin kapağında, biri doğu okuma masasında; 2 boş sahte parça.
        /// Sembol çapaları "Glyph0..4" adıyla kökte (BuildPuzzle4 adla bulur).
        /// </summary>
        static void LetterFragments(Transform root, float hx)
        {
            var ink = MakeMat("K_Ink", new Color(0.12f, 0.08f, 0.06f));
            void Piece(string name, Vector3 pos, float yaw, Vector2 size, int[] glyphs, int order)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root, false);
                t.localPosition = pos;
                t.localRotation = Quaternion.Euler(0, yaw, 0);
                Slab("Paper", t, new Vector3(0, 0.002f, 0), new Vector3(size.x, 0.004f, size.y), K.paperA, 1f, false);
                // yırtık kenar: köşeye eğik ikinci yaprak parçası
                Slab("Tear", t, new Vector3(size.x * 0.32f, 0.003f, -size.y * 0.42f), new Vector3(size.x * 0.4f, 0.004f, size.y * 0.25f), K.paperA, 1f, false).transform.localRotation = Quaternion.Euler(0, 18, 0);
                for (int k = 0; k < order; k++) // sıra noktaları (okuyucuya göre sol üst köşe)
                    Prim("OrderDot", t, PrimitiveType.Cylinder, new Vector3(size.x * 0.5f - 0.035f - k * 0.032f, 0.006f, -size.y * 0.5f + 0.03f), new Vector3(0.018f, 0.001f, 0.018f), ink);
                for (int k = 0; k < glyphs.Length; k++)
                {
                    var g = new GameObject("Glyph" + glyphs[k]).transform;
                    g.SetParent(t, false);
                    g.localPosition = new Vector3(-(k - (glyphs.Length - 1) * 0.5f) * 0.18f, 0.007f, 0.01f); // okuyucu yerel +z tarafında: soldan sağa = -x
                    g.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.back); // düz yatan kâğıt (kürsü 0° eğim karşılığı)
                }
                foreach (var tt in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tt.gameObject, 0);
            }
            Piece("LetterFragmentA", new Vector3(hx - 0.27f, 1.635f, 6.62f), -90f, new Vector2(0.44f, 0.26f), new[] { 0, 1 }, 1); // doğu mezar nişi rafı
            Piece("LetterFragmentB", new Vector3(-6.85f, 1.026f, 1.62f), 90f, new Vector2(0.26f, 0.22f), new[] { 2 }, 2);       // batı lahdi kapağı
            Piece("LetterFragmentC", new Vector3(6.72f, 0.826f, -11.72f), -90f, new Vector2(0.44f, 0.26f), new[] { 3, 4 }, 3);  // doğu okuma masası
            Piece("LetterDecoyA", new Vector3(-2.7f, 0.005f, 5.7f), 30f, new Vector2(0.3f, 0.22f), new int[0], 0);           // sahte kürsü dibinde boş parça
            Piece("LetterDecoyB", new Vector3(-hx + 0.27f, 1.635f, -2.2f), 90f, new Vector2(0.3f, 0.2f), new int[0], 2);      // batı nişinde, noktalı ama boş
        }

        /// <summary>Mezar kütüphanesi v2 dekor turu: yan neflerde kitaplıklar + okuma masaları, nefte halı ve şamdanlar, kâtibin dağınıklığı.</summary>
        static void LibraryDressing(Transform root, float hx, float hz, float NW, float SP)
        {
            foreach (int s in new[] { -1, 1 })
            {
                float wy = s < 0 ? 90f : -90f, wx = s * (hx - 0.4f);
                foreach (float z in new[] { -4.5f, 4.0f, -12.9f }) Books(root, new Vector3(wx, 0, z), wy);
                Kit("Ladder", root, new Vector3(s * (hx - 0.75f), 0, s < 0 ? 0.9f : -3.8f), wy, 0.95f);
                // Okuma masası (yan nef): kitaplar, mum, kâğıt, mürekkep
                var dp = new Vector3(s * 6.6f, 0, s < 0 ? 6.4f : -11.0f);
                Kit("Table", root, dp, 90f);
                Kit("ChairWood", root, dp + new Vector3(-s * 0.85f, 0, 0), s < 0 ? 90f : -90f);
                Kit("BookStack", root, dp + new Vector3(0.05f, 0.82f, 0.5f), 30f, 0.8f);
                Kit("PaperA", root, dp + new Vector3(0f, 0.825f, -0.1f), 70f);
                Kit("PaperC", root, dp + new Vector3(0.12f, 0.826f, -0.45f), 15f);
                Kit("Candlestick", root, dp + new Vector3(-0.1f, 0.82f, 0.8f));
                Kit("L_Candle", root, dp + new Vector3(-0.1f, 1.35f, 0.8f));
                Kit(s < 0 ? "Magnifier" : "Spectacles", root, dp + new Vector3(0.1f, 0.82f, 0.2f), 40f);
                Kit("PaperScatter", root, dp + new Vector3(-s * 0.4f, 0.01f, -0.9f), 110f);
                // Zeminde kitap kuleleri, mumlar
                foreach (float z in new[] { -1.2f, 7.6f, -13.0f })
                {
                    Kit("BookStack", root, new Vector3(s * 5.4f, 0, z), z * 17f);
                    Kit("CandleCluster", root, new Vector3(s * 5.25f, 0, z + 0.6f));
                }
                Kit("BookStack", root, new Vector3(s * 7.9f, 0, s < 0 ? -7.9f : 1.2f), 200f, 0.9f);
                Kit("BookStack", root, new Vector3(s * 7.95f, 0.38f, s < 0 ? -7.85f : 1.25f), 160f, 0.8f);
                // Sütunlardan nefe sarkan sancaklar
                foreach (float z in new[] { -9f, 0f, 9f })
                    Kit("Banner", root, new Vector3(s * (NW - 0.46f), SP - 0.5f, z), s < 0 ? 90f : -90f, 1.1f);
                // Lahit başlarında mum + kadeh, ayak ucunda dağılmış kemik/kâğıt
                foreach (float z in new[] { -6.8f, 2.2f })
                {
                    Kit("CandleCluster", root, new Vector3(s * 6.7f, 1.02f, z - 1.05f));
                    Kit("Goblet1", root, new Vector3(s * 6.4f, 1.02f, z - 1.0f));
                    Kit("PaperB", root, new Vector3(s * 5.75f, 0.005f, z + 1.4f), z * 13f);
                }
                Kit("Crate", root, new Vector3(s * 8.3f, 0, 12.9f), s * 20f, 0.8f);
                Kit("CrateLong", root, new Vector3(s * 8.4f, 0, -10.0f), 90f, 0.85f);
            }
            // Orta nef: halı yolu, ayaklı şamdanlar, kâtip sandalyesi
            foreach (float z in new[] { -5.5f, 5.5f }) Kit("Rug", root, new Vector3(0, 0, z), 90f, 1.1f);
            foreach (int s2 in new[] { -1, 1 })
                foreach (float z in new[] { -9f, 0f, 9f })
                {   // yan nef dolgusu (baked): kitaplık yüzleri okunsun
                    var f = LightGo("AisleFill", LightType.Point, Pal.Candle, 2.2f, 7f, true);
                    f.transform.SetParent(root, false); f.transform.localPosition = new Vector3(s2 * 7.2f, 3.2f, z);
                }
            foreach (float z in new[] { -11.5f, -2.3f, 5.5f })
                foreach (int s in new[] { -1, 1 })
                    Kit("CandelabraFloor", root, new Vector3(s * 3.2f, 0, z), s * 30f, 0.9f);
            Kit("L_Candelabra", root, new Vector3(-3.2f, 1.6f, -2.3f));
            Kit("L_Candelabra", root, new Vector3(3.2f, 1.6f, 5.5f));
            // Kâtibin köşesi (kuzeybatı, StudyTable yanında): sandık, kitaplar, saat, küre
            Kit("Chest", root, new Vector3(-8.1f, 0, 12.6f), 90f, 0.85f);
            Kit("Armillary", root, new Vector3(-5.6f, 0, 12.7f), 30f, 0.9f);
            Kit("MantelClock", root, new Vector3(-6.7f, 0.78f, 11.2f), 90f);
            Kit("BookStack", root, new Vector3(-7.6f, 0, 11.0f), 40f);
            Kit("Lantern", root, new Vector3(-8.2f, 0, 9.9f), 10f);
            // Hikâye: kürsünün yanına devrilmiş taburesi ve dökülmüş mürekkep (mektubu yazan kaçmış)
            Kit("Stool", root, new Vector3(-6.4f, 0.2f, -11.4f), 0f).transform.localRotation = Quaternion.Euler(0, 20, 86);
            Prim("InkSpill", root, PrimitiveType.Cylinder, new Vector3(-6.6f, 0.003f, -10.7f), new Vector3(0.5f, 0.002f, 0.32f), MakeMat("K_InkSpill", new Color(0.03f, 0.03f, 0.05f)));
            Kit("Bottle3", root, new Vector3(-6.45f, 0.05f, -10.6f), 0f).transform.localRotation = Quaternion.Euler(0, 60, 90);
            Kit("Rat", root, new Vector3(7.4f, 0, 7.8f), 210f);
            Kit("Rat", root, new Vector3(-5.6f, 0, -2.9f), 30f, 0.9f);
            foreach (var (p, r) in new[] { (new Vector3(-hx + 0.03f, 0, -hz + 0.03f), Quaternion.Euler(0, 90, 0)), (new Vector3(hx - 0.03f, 0, 0.9f), Quaternion.Euler(0, -90, 0)),
                                          (new Vector3(-NW - 0.5f, SP - 0.4f, 9f), Quaternion.Euler(0, -90, 180)), (new Vector3(NW + 0.5f, SP - 0.4f, -9f), Quaternion.Euler(0, 90, 180)) })
                KitR("Cobweb", root, p, r, 1.3f);
        }

        static Transform BuildLibrary(Transform roomO, out Transform lockRoot, out Transform lectern, out Transform arrive, out LiftDoor door)
        {
            const float SX = 18f, SZ = 28f, hx = SX * 0.5f, hz = SZ * 0.5f, NW = 4.5f, SP = 6f, RS = 2.6f, AH = 6.2f;
            var root = new GameObject("RoomB4_Kutuphane").transform;
            root.SetParent(roomO, false);
            root.localPosition = new Vector3(40f, 0, 0);
            var stone = K.castleStone;
            float top = SP + RS;

            Deck(root, "Floor", new Vector3(0, -0.25f, 0), SX, SZ, 0.5f, KTex("K_LibraryFloor", "rectangular_paving", new Color(0.58f, 0.57f, 0.6f), 0.35f, 1.1f), 3, 5); // dikdörtgen taş döşeme (turuncu derz yok)
            Wall(root, "WallE", new Vector3(hx + 0.25f, 0, 0), false, SZ, AH + 0.3f, stone, 2f);
            Wall(root, "WallW", new Vector3(-hx - 0.25f, 0, 0), false, SZ, AH + 0.3f, stone, 2f);
            Wall(root, "WallN", new Vector3(0, 0, hz + 0.25f), true, SX + 1f, top + 0.4f, stone, 2f);
            Wall(root, "WallS", new Vector3(0, 0, -hz - 0.25f), true, SX + 1f, top + 0.4f, stone, 2f, new Opening(0f, 3f, 0f, 3f));
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, top + 0.6f, 0), new Vector3(SX + 1.5f, 0.5f, SZ + 1.5f), stone, 2f, false), 0);
            // Yan nef tavanları + orta nef tonozu ve kaburgalar
            foreach (int s in new[] { -1, 1 })
                Deck(root, "AisleCeil", new Vector3(s * (NW + hx) * 0.5f, AH + 0.25f, 0), hx - NW, SZ, 0.5f, stone, 1, 4);
            var bay = new GameObject("Nave").transform;
            bay.SetParent(root, false);
            bay.localRotation = Quaternion.Euler(0, 90, 0);
            VaultStrip(bay, "Vault", 0, SZ, NW + 0.1f, SP, RS, 0.3f, K.cellarBrick, 14, 2f);
            float[] cz = { -9f, -4.5f, 0f, 4.5f, 9f };
            foreach (float z in cz)
            {
                VaultStrip(bay, "Rib" + z, -z, 0.5f, NW - 0.05f, SP, RS - 0.15f, 0.3f, stone, 14, 1.2f);
                foreach (int s in new[] { -1, 1 })
                    Pillar(root, "Col", new Vector3(s * NW, 0, z), SP, 0.9f);
            }
            foreach (int s in new[] { -1, 1 })
                Slab("Arcade", root, new Vector3(s * NW, SP - 0.2f, 0), new Vector3(0.9f, 0.4f, SZ), stone, 1.5f, false);

            // --- Güney: harf kilitli büyük kapı + kilit kaidesi ---
            door = MakeDoor("LibraryDoor", root, new Vector3(0, 1.5f, -hz), true, K.iron);
            {
                var dt = door.transform;
                foreach (var rr in dt.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = K.iron;
                var axisZ = Quaternion.Euler(90, 0, 0);
                Prim("Disc", dt, PrimitiveType.Cylinder, new Vector3(0, 0, 0.2f), new Vector3(2.8f, 0.05f, 2.8f), K.iron).transform.localRotation = axisZ;
                MeshObj("RingA", dt, new Vector3(0, 0, 0.26f), RingMesh(1.3f, 0.07f, 0.05f), K.bronze).transform.localRotation = axisZ;
                MeshObj("RingB", dt, new Vector3(0, 0, 0.26f), RingMesh(0.9f, 0.05f, 0.05f), K.bronze).transform.localRotation = axisZ;
                MeshObj("RingC", dt, new Vector3(0, 0, 0.26f), RingMesh(0.5f, 0.05f, 0.05f), K.brass).transform.localRotation = axisZ;
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    Prim("Rivet", dt, PrimitiveType.Sphere, new Vector3(Mathf.Sin(a) * 1.12f, Mathf.Cos(a) * 1.12f, 0.27f), Vector3.one * 0.08f, K.brass);
                }
                var da = new GameObject("DoorProbeAnchor").transform;
                da.SetParent(root, false);
                da.localPosition = new Vector3(0, 1.5f, -hz + 1.5f);
                foreach (var t in dt.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    var rr = t.GetComponent<Renderer>(); if (rr != null) rr.probeAnchor = da;
                    var c = t.GetComponent<Collider>(); if (c != null && t != dt) Object.DestroyImmediate(c);
                }
            }
            ArchTrim(root, "DoorArch", new Vector3(0, 0, -hz), Vector3.forward, 4.2f, 5.4f, false);
            Deck(root, "BeyondFloor", new Vector3(0, -0.25f, -hz - 1.5f), 3.5f, 3f, 0.5f, stone, 1, 1);
            Deck(root, "BeyondCeil", new Vector3(0, 3.25f, -hz - 1.5f), 3.5f, 3f, 0.5f, stone, 1, 1);
            Wall(root, "BeyondE", new Vector3(1.75f, 0, -hz - 1.5f), false, 3f, 3f, stone, 2f);
            Wall(root, "BeyondW", new Vector3(-1.75f, 0, -hz - 1.5f), false, 3f, 3f, stone, 2f);
            Wall(root, "BeyondS", new Vector3(0, 0, -hz - 3f), true, 3.5f, 3f, stone, 2f);
            foreach (int s in new[] { -1, 1 })
            {
                Slab("StatuePlinth", root, new Vector3(s * 2.9f, 0.3f, -hz + 1.0f), new Vector3(1.0f, 0.6f, 1.0f), stone, 1.2f);
                Kit("StatueAngel", root, new Vector3(s * 2.9f, 0.6f, -hz + 1.0f), 0f, 1.1f);
                Kit("WallSconce", root, new Vector3(s * 2.6f, 3.6f, -hz + 0.03f), 0f);
                Kit("L_Candle", root, new Vector3(s * 2.6f, 3.6f, -hz + 0.7f));
            }
            {
                // Kilit kapının önünde değil: doğu yan nefte, güney lahdinin arkasında duvara dayalı (girişten ve nefin ortasından görünmez)
                lockRoot = new GameObject("LockPanel").transform;
                lockRoot.SetParent(root, false);
                lockRoot.localPosition = new Vector3(hx - 0.3f, 0, -6.6f);
                lockRoot.localRotation = Quaternion.Euler(0, -90, 0); // ön yüz (+z) batıya, yan nefe
                Slab("Pedestal", lockRoot, new Vector3(0, 0.7f, 0), new Vector3(2.6f, 1.4f, 0.5f), stone, 1.2f);
                Slab("PedestalCap", lockRoot, new Vector3(0, 1.45f, 0), new Vector3(2.75f, 0.1f, 0.6f), stone, 1.2f, false);
                var ink = MakeMat("K_LockLetter", new Color(0.92f, 0.82f, 0.55f));
                for (int i = 0; i < LetterPuzzle.Len; i++)
                {
                    var d = new GameObject("LockDial" + i).transform;
                    d.SetParent(lockRoot, false);
                    d.localPosition = new Vector3(-1.0f + i * 0.5f, 0.85f, 0.27f);
                    d.localRotation = Quaternion.Euler(0, 180, 0); // -Z kuzeye (odaya)
                    var bc = d.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.58f, 0.58f, 0.12f);
                    d.gameObject.AddComponent<Interactable>().Label = "Harfi çevir";
                    var ring = new GameObject("Ring").transform;
                    ring.SetParent(d, false);
                    MeshObj("Rim", ring, Vector3.zero, RingMesh(0.25f, 0.04f, 0.05f), K.bronze).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * Mathf.PI / 3f;
                        Prim("Notch", ring, PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.25f, Mathf.Cos(a) * 0.25f, -0.03f), new Vector3(0.04f, 0.04f, 0.04f), K.brass);
                    }
                    Prim("Face", d, PrimitiveType.Cylinder, new Vector3(0, 0, 0.02f), new Vector3(0.44f, 0.01f, 0.44f), K.furniture).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var t = new GameObject("Letter");
                    t.transform.SetParent(d, false);
                    t.transform.localPosition = new Vector3(0, 0, -0.01f);
                    var tm = t.AddComponent<TextMesh>();
                    tm.font = U.head; tm.text = ""; tm.fontSize = 64; tm.characterSize = 0.045f;
                    tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(0.92f, 0.82f, 0.55f);
                    t.GetComponent<MeshRenderer>().sharedMaterial = U.head.material;
                    var pa = new GameObject("ProbeAnchor").transform;
                    pa.SetParent(d, false);
                    pa.localPosition = new Vector3(0, 0.3f, -0.8f);
                    foreach (var tt in d.GetComponentsInChildren<Transform>(true))
                    {
                        GameObjectUtility.SetStaticEditorFlags(tt.gameObject, 0);
                        var rr = tt.GetComponent<Renderer>(); if (rr != null) rr.probeAnchor = pa;
                        var c = tt.GetComponent<Collider>(); if (c != null && tt != d) Object.DestroyImmediate(c);
                    }
                }
                Kit("Candlestick", lockRoot, new Vector3(1.15f, 1.5f, 0.05f), 0f);
                Kit("L_Candle", lockRoot, new Vector3(1.15f, 2.05f, 0.3f));
                Kit("L_Lantern", root, new Vector3(1.9f, 1.5f, -hz + 1.6f), 20f);
                // Kapı önünde boş sunak (kilit burada değil): mumlar + kapalı kitap
                Slab("Altar", root, new Vector3(0, 0.45f, -hz + 2.6f), new Vector3(1.6f, 0.9f, 0.7f), stone, 1.2f);
                Slab("AltarCap", root, new Vector3(0, 0.93f, -hz + 2.6f), new Vector3(1.75f, 0.08f, 0.8f), stone, 1.2f, false);
                Kit("CandleCluster", root, new Vector3(-0.5f, 0.97f, -hz + 2.6f));
                Kit("BookStack", root, new Vector3(0.35f, 0.97f, -hz + 2.6f), 15f, 0.8f);
            }

            // --- Kürsü + mektup (orta nefin batısı, kuzeye bakar) ---
            {
                lectern = new GameObject("Lectern").transform;
                lectern.SetParent(root, false);
                lectern.localPosition = new Vector3(-7.4f, 0, -10.4f); // batı yan nef, gölgede: aranır
                lectern.localRotation = Quaternion.Euler(0, 90, 0);
                Slab("Base", lectern, new Vector3(0, 0.08f, 0), new Vector3(0.7f, 0.16f, 0.6f), stone, 1f);
                Slab("Stem", lectern, new Vector3(0, 0.6f, 0), new Vector3(0.32f, 1.0f, 0.32f), stone, 1f, false);
                const float tilt = 28f;
                var n = new Vector3(0, Mathf.Cos(tilt * Mathf.Deg2Rad), Mathf.Sin(tilt * Mathf.Deg2Rad));
                var up = new Vector3(0, Mathf.Sin(tilt * Mathf.Deg2Rad), -Mathf.Cos(tilt * Mathf.Deg2Rad));
                var face = Quaternion.LookRotation(-n, up);
                var topC = new Vector3(0, 1.2f, 0);
                Slab("Board", lectern, topC, new Vector3(0.9f, 0.62f, 0.06f), stone, 1f, false).transform.localRotation = face;
                Slab("Ledge", lectern, topC - up * 0.32f + n * 0.04f, new Vector3(0.9f, 0.04f, 0.06f), stone, 1f, false).transform.localRotation = face;
                var paper = Slab("Parchment", lectern, topC + n * 0.035f, new Vector3(0.7f, 0.5f, 0.004f), K.paperA, 1f, false);
                paper.transform.localRotation = face;
                // Mektup yırtılmış: kürsüde yalnız mühürlü kapak kalır, 5 sembol 3 parçaya dağılmış (LetterFragments)
                var waxMat = MakeMat("K_SealWax", Color.Lerp(Pal.Bordo, Color.black, 0.2f));
                var seal = Prim("Seal", lectern, PrimitiveType.Cylinder, topC + n * 0.04f - up * 0.14f + new Vector3(0.22f, 0, 0), new Vector3(0.14f, 0.008f, 0.14f), waxMat);
                seal.transform.localRotation = face * Quaternion.Euler(90, 0, 0);
                var sg = new GameObject("SealGlyph").transform;
                sg.SetParent(lectern, false);
                sg.localPosition = topC + n * 0.05f - up * 0.14f + new Vector3(0.22f, 0, 0);
                sg.localRotation = face;
                Kit("CandleCluster", root, new Vector3(-6.8f, 0, -9.7f));
                Kit("L_Lantern", root, new Vector3(-7.0f, 0, -11.3f), 30f);
                Kit("PaperScatter", root, new Vector3(-6.2f, 0, -10.0f), 60f);
                // Nefteki sahte kürsü: açık kitap, mum (mektup burada değil)
                var decoy = new GameObject("DecoyLectern").transform;
                decoy.SetParent(root, false);
                decoy.localPosition = new Vector3(-2.4f, 0, 5.0f);
                Slab("Base", decoy, new Vector3(0, 0.08f, 0), new Vector3(0.7f, 0.16f, 0.6f), stone, 1f);
                Slab("Stem", decoy, new Vector3(0, 0.6f, 0), new Vector3(0.32f, 1.0f, 0.32f), stone, 1f, false);
                Slab("Board", decoy, new Vector3(0, 1.15f, 0), new Vector3(0.9f, 0.08f, 0.62f), stone, 1f, false).transform.localRotation = Quaternion.Euler(-28, 0, 0);
                KitR("BookStack", decoy, new Vector3(0, 1.2f, 0), Quaternion.Euler(-28, 90, 0), 0.8f);
                Kit("CandleCluster", root, new Vector3(-1.7f, 0, 5.5f));
                Kit("L_Candle", root, new Vector3(-1.9f, 1.6f, 5.6f));
                Kit("BookStack", root, new Vector3(-3.0f, 0, 4.4f), 20f);
            }

            // --- Yan nefler: lahitler (üstte yatan heykel), nişler, kitaplıklar ---
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float z in new[] { -6.8f, 2.2f })
                {
                    float x = s * 6.7f;
                    Slab("Sarcophagus", root, new Vector3(x, 0.45f, z), new Vector3(1.2f, 0.9f, 2.4f), stone, 1.2f);
                    Slab("Lid", root, new Vector3(x, 0.96f, z), new Vector3(1.3f, 0.12f, 2.5f), stone, 1.2f, false);
                    Slab("CrossV", root, new Vector3(x + s * 0.61f, 0.5f, z), new Vector3(0.02f, 0.5f, 0.1f), stone, 1f, false);
                    Slab("CrossH", root, new Vector3(x + s * 0.61f, 0.58f, z), new Vector3(0.02f, 0.1f, 0.34f), stone, 1f, false);
                    Kit("StatueKing", root, new Vector3(x, 1.02f, z + 0.75f), s < 0 ? 90f : -90f, 0.75f); // lahit başında ayakta
                }
                foreach (float z in new[] { -11f, -2.3f, 6.5f })
                {
                    ArchTrim(root, "BurialNiche", new Vector3(s * hx, 0.6f, z), s < 0 ? Vector3.right : Vector3.left, 2.0f, 2.4f, true);
                    Slab("NicheShelf", root, new Vector3(s * (hx - 0.25f), 1.6f, z), new Vector3(0.5f, 0.06f, 1.8f), stone, 1f, false);
                    Kit("BookRow", root, new Vector3(s * (hx - 0.25f), 1.64f, z - 0.4f), s < 0 ? 90f : -90f, 0.8f);
                    Kit("Pot", root, new Vector3(s * (hx - 0.3f), 1.64f, z + 0.55f));
                    Kit("CandleCluster", root, new Vector3(s * (hx - 0.3f), 0.6f, z));
                }
                foreach (float z in new[] { -8.6f, 0.2f, 9.0f, 11.6f })
                {
                    Books(root, new Vector3(s * (hx - 0.4f), 0, z), s < 0 ? 90f : -90f);
                }
                KitR("WindowLancet", root, new Vector3(s * hx, 3.0f, -4.5f), Quaternion.Euler(0, s < 0 ? 90 : -90, 0), 1.1f);
            }
            // Orta nef: kukuletalı heykeller (sütun önlerinde), kitap yığınları, kağıtlar
            Kit("StatueKing", root, new Vector3(-3.4f, 0, -4.5f), 90f, 1.1f);
            Kit("StatueKing", root, new Vector3(3.4f, 0, 0f), -90f, 1.1f);
            Kit("StatueAngel", root, new Vector3(3.4f, 0, 9f), -90f, 1.0f);
            Kit("BookStack", root, new Vector3(2.6f, 0, -6.5f), 70f);
            Kit("PaperScatter", root, new Vector3(1.2f, 0, -9.2f), 200f);
            Kit("StudyTable", root, new Vector3(-6.7f, 0, 10.6f), 90f); // aradaki mektuplar (hikâye)
            Kit("Stool", root, new Vector3(-5.8f, 0, 10.2f), -30f);
            Kit("Chest", root, new Vector3(6.9f, 0, 12.6f), -90f);
            // Yeni dekor: giriş mangalı, lahitlerde kılıç/kalkan, testi/vazo, balta, sepet
            Kit("FirePit", root, new Vector3(0, 0, 9.5f), 0f, 0.8f);
            Prim("Embers", root, PrimitiveType.Sphere, new Vector3(0, 0.1f, 9.5f), new Vector3(0.75f, 0.1f, 0.75f),
                 GlowMat("K_Embers", new Color(0.25f, 0.06f, 0.02f), new Color(1f, 0.38f, 0.1f) * 1.4f));
            Kit("L_Candelabra", root, new Vector3(0, 0.8f, 9.5f));
            foreach (int s in new[] { -1, 1 })
                foreach (float z in new[] { -6.8f, 2.2f })
                {
                    KitR("Shield", root, new Vector3(s * (hx - 0.06f), s > 0 && z < 0 ? 3.1f : 1.6f, z), Quaternion.Euler(0, s < 0 ? 90 : -90, 0));
                    KitR("Sword", root, new Vector3(s * 6.05f, 0.02f, z - 0.4f), Quaternion.Euler(0, s < 0 ? 90 : -90, 0) * Quaternion.Euler(0, 0, s * 8f));
                }
            Kit("VaseCeramic", root, new Vector3(-8.4f, 0, 4.0f));
            Kit("Jug", root, new Vector3(8.3f, 0, -4.0f), 30f);
            KitR("Axe", root, new Vector3(8.6f, 0.02f, 4.2f), Quaternion.Euler(0, -90, -12));
            Kit("Basket", root, new Vector3(7.8f, 0, 11.0f), 10f);
            Kit("HorseHead", root, new Vector3(-8.65f, 1.66f, -2.3f), 90f);
            Kit("Mace", root, new Vector3(-6.9f, 0.02f, 9.4f), 30f);
            Kit("Amphora", root, new Vector3(7.6f, 0, -12.8f), 20f);
            // Avizeler (orta nef)
            foreach (float z in new[] { -8f, 0f, 8f })
            {
                Rod("ChandelierChain", root, new Vector3(0, 6.4f, z), new Vector3(0, top - 0.1f, z), 0.03f, K.iron);
                Kit("ChandelierIron", root, new Vector3(0, 6.4f, z), 0f, 1.4f);
                Kit("L_Chandelier", root, new Vector3(0, 5.0f, z));
            }
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, AH - 0.2f, hz - 0.03f), Quaternion.Euler(0, 0, -90), 2f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, AH - 0.2f, -hz + 0.03f), Quaternion.Euler(0, 180, 180), 2f);
            KitR("Cobweb", root, new Vector3(NW + 0.5f, SP - 0.4f, 4.5f), Quaternion.Euler(0, 90, 180), 1.4f);
            KitR("Cobweb", root, new Vector3(-NW - 0.5f, SP - 0.4f, -4.5f), Quaternion.Euler(0, -90, 180), 1.4f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, 0, hz - 0.03f), Quaternion.Euler(0, 0, 0), 1.2f);

            LibraryDressing(root, hx, hz, NW, SP);
            LetterFragments(root, hx);

            arrive = new GameObject("LibraryArrive").transform;
            arrive.SetParent(root, false);
            arrive.localPosition = new Vector3(0, 0.05f, hz - 1.8f);
            arrive.localRotation = Quaternion.Euler(0, 180, 0);
            Kit("Door", root, new Vector3(0, 0, hz - 0.12f), 180f, 1.1f); // geliş kapısı (kapalı, dekor)

            KitProbes(root, new Vector3(0, 3.1f, 0), new Vector3(SX, 6.0f, SZ), 6, 3, 8);
            KitReflection(root, new Vector3(0, 3f, 0), new Vector3(SX, 7f, SZ));
            return root;
        }
    }
}
