using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Bulmaca 7 "Çan ve kemik org" (ref can.png, org.png). Simge seti: nota işaretleri (Notes).
    /// X: Çan Dairesi, kule yerel (95,0,70): 18x18, 12 m taş duvar + açık ahşap çatı makası (tepe 17.5 m), ortada serbest döner merdiven
    ///    ve köprülü galeri, iki yanda çan kasnakları (4 çan, farklı boy/yükseklik), kuzeyde büyük çan, pirinç kadran kürsüsü.
    /// O: Kemik Org Kriptası, zindan yerel (90,0,60): 16x30 sivri tonoz (13.5 m), kemer sıraları + şövalye heykelleri + mangallar,
    ///    kuzeyde kemik borulu anıtsal org (6 tuş + kitabe), tavanda kuyuya açılan ızgara (çan sesi buradan iner).
    /// Girişler (bulmaca 6 çözülünce): çatı güney duvarındaki ikinci kapı / kuyu odası kuzey kapısı. Çıkış: kafes asansörler → final salonu.
    /// Düğümler: 59 hata, 60 tohum, 61 aşama, 62 ilerleme, 63 kadran, 64-67 çanlar, 68 büyük çan, 69/76 vuruş anı, 70-75 org tuşları,
    /// 77-78 girişler, 79-80 asansörler (CoopSetup.Final.cs).
    /// </summary>
    public static partial class CoopSetup
    {
        static void BuildPuzzle7(Transform roomX, Transform roomO)
        {
            var def = new GameObject("Puzzle7").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 7;
            dso.FindProperty("title").stringValue = "Bulmaca 7 (çan ve org)";
            dso.FindProperty("startsActive").boolValue = false;
            var ul = dso.FindProperty("unlocks"); ul.arraySize = 1; ul.GetArrayElementAtIndex(0).intValue = 8;
            dso.ApplyModifiedPropertiesWithoutUndo();
            foreach (var d in Object.FindObjectsByType<PuzzleDef>(FindObjectsSortMode.None))
                if (d.Id == 6)
                {
                    var so = new SerializedObject(d);
                    var up = so.FindProperty("unlocks"); up.arraySize = 1; up.GetArrayElementAtIndex(0).intValue = 7;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

            var ink = GlowMat("K_NoteInk", new Color(0.86f, 0.7f, 0.36f), new Color(0.55f, 0.4f, 0.16f), 0.5f);
            ink.SetFloat("_Metallic", 0.6f);
            var dark = MakeMat("K_NoteDark", new Color(0.05f, 0.04f, 0.035f)); dark.SetFloat("_Smoothness", 0.2f);

            var chamber = BuildBellChamber(roomX, out var bellNodes, out var great, out var bellPlaques, out var dialGlyphs, out var dialCover, out var dialHand, out var chamberArrive);
            var crypt = BuildOrganCrypt(roomO, out var keyNodes, out var keyPlaques, out var script, out var cryptArrive);

            var go = new GameObject("Puzzle7_Bells");
            go.transform.SetParent(chamber, false);
            var bp = go.AddComponent<BellPuzzle>();
            var pso = new SerializedObject(bp);
            pso.FindProperty("puzzleId").intValue = 7;
            pso.FindProperty("nodeId").intValue = 60;
            pso.ApplyModifiedPropertiesWithoutUndo();
            void Node(BellNode n, int id)
            {
                var so = new SerializedObject(n);
                so.FindProperty("puzzleId").intValue = 7;
                so.FindProperty("nodeId").intValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            for (int i = 0; i < bellNodes.Length; i++) Node(bellNodes[i], 64 + i);
            Node(great, 68);
            for (int i = 0; i < keyNodes.Length; i++) Node(keyNodes[i], 70 + i);

            var view = go.AddComponent<BellView>();
            var vso = new SerializedObject(view);
            SetRef(vso, "puzzle", bp);
            SetRefArray(vso, "bellGlyphs", bellPlaques);
            SetRefArray(vso, "scriptGlyphs", script);
            SetRefArray(vso, "keyGlyphs", keyPlaques);
            SetRefArray(vso, "dialGlyphs", dialGlyphs);
            SetRef(vso, "dialCover", dialCover);
            SetRef(vso, "dialHand", dialHand);
            SetRef(vso, "inkMat", ink);
            SetRef(vso, "boneInkMat", dark);
            SetRef(vso, "font", U.body);
            vso.FindProperty("chamberCenter").vector3Value = chamber.TransformPoint(new Vector3(0, 5f, 0));
            vso.FindProperty("chamberSize").vector3Value = new Vector3(19f, 12f, 19f);
            vso.FindProperty("cryptCenter").vector3Value = crypt.TransformPoint(new Vector3(0, 4f, 0));
            vso.FindProperty("cryptSize").vector3Value = new Vector3(17f, 9f, 31f);
            vso.ApplyModifiedPropertiesWithoutUndo();

            // Girişler (bulmaca 6 çözülünce): çatı güney duvarı ikinci kapı / kuyu odası kuzey kapısı
            var roof = roomX.Find("RoomA6_Cati");
            if (roof != null)
            {
                const float hz = 14f;
                Kit("Door", roof, new Vector3(-4.5f, 0, -hz + 0.02f), 0f, 1.1f);
                ArchTrim(roof, "BellDoorArch", new Vector3(-4.5f, 0, -hz), Vector3.forward, 2.6f, 3.6f, false);
                Kit("Lantern", roof, new Vector3(-3.4f, 0.02f, -hz + 0.5f), 0f);
                MakePassage("PassageX7", roof, 77, roof.TransformPoint(new Vector3(-4.5f, 1f, -hz + 0.55f)), new Vector3(1.6f, 2f, 0.9f), chamberArrive, 6);
            }
            var well = roomO.Find("RoomB6_Kuyu");
            if (well != null)
            {
                const float R = 8f;
                Kit("Door", well, Vector3.forward * (R - 0.02f), 180f, 1.1f);
                Kit("CandleCluster", well, new Vector3(1.4f, 0, R - 0.6f), 180f);
                MakePassage("PassageO7", well, 78, well.TransformPoint(new Vector3(0, 1f, R - 0.6f)), new Vector3(2.0f, 2f, 0.9f), cryptArrive, 6);
            }
            MakeCheckpoint(roomX, 7, Roles.X, chamberArrive.position);
            MakeCheckpoint(roomO, 7, Roles.O, cryptArrive.position);
            KeepBaked(chamber);
            KeepBaked(crypt);
        }

        // ---------------- Ortak parçalar ----------------

        /// <summary>Kafatası (küre parçaları). pos: zemin.</summary>
        static void Skull(Transform parent, Vector3 pos, float yaw, float s, Material bone, Material voidM)
        {
            var t = new GameObject("Skull").transform;
            t.SetParent(parent, false); t.localPosition = pos; t.localRotation = Quaternion.Euler(0, yaw, 0); t.localScale = Vector3.one * s;
            Prim("Cranium", t, PrimitiveType.Sphere, new Vector3(0, 0.1f, 0.01f), new Vector3(0.17f, 0.17f, 0.2f), bone);
            Prim("Face", t, PrimitiveType.Sphere, new Vector3(0, 0.06f, -0.07f), new Vector3(0.13f, 0.12f, 0.1f), bone);
            Prim("Jaw", t, PrimitiveType.Cube, new Vector3(0, 0.015f, -0.06f), new Vector3(0.09f, 0.03f, 0.07f), bone);
            foreach (int k in new[] { -1, 1 })
                Prim("Eye", t, PrimitiveType.Sphere, new Vector3(k * 0.035f, 0.08f, -0.115f), new Vector3(0.04f, 0.035f, 0.02f), voidM);
            Prim("Nose", t, PrimitiveType.Cube, new Vector3(0, 0.05f, -0.12f), new Vector3(0.018f, 0.025f, 0.01f), voidM);
        }

        /// <summary>Demir mangal: üç ayak + çanak + alevler + kor. light: pişmiş sıcak ışık.</summary>
        static void Brazier(Transform parent, Vector3 pos, float s, bool light)
        {
            var t = new GameObject("Brazier").transform;
            t.SetParent(parent, false); t.localPosition = pos; t.localScale = Vector3.one * s;
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Tube("Leg", t, d * 0.42f, d * 0.3f + Vector3.up * 0.75f, 0.03f, K.iron);
                Prim("Foot", t, PrimitiveType.Sphere, d * 0.42f + Vector3.up * 0.02f, Vector3.one * 0.07f, K.iron);
            }
            MeshObj("Bowl", t, Vector3.up * 0.72f, LatheMesh("kit_brazier", new[] { new Vector2(0.05f, 0f), new Vector2(0.32f, 0.04f), new Vector2(0.48f, 0.2f), new Vector2(0.52f, 0.26f), new Vector2(0.46f, 0.25f), new Vector2(0.3f, 0.1f), new Vector2(0f, 0.08f) }, 16), K.iron);
            var ember = GlowMat("K_Ember", new Color(0.25f, 0.08f, 0.03f), new Color(1.6f, 0.45f, 0.1f));
            Prim("Coals", t, PrimitiveType.Sphere, Vector3.up * 0.92f, new Vector3(0.8f, 0.12f, 0.8f), ember);
            for (int i = 0; i < 5; i++)
            {
                var o = i == 0 ? Vector3.zero : Quaternion.Euler(0, i * 90f + 20f, 0) * Vector3.forward * 0.2f;
                var f = MeshObj("Flame", t, Vector3.up * 0.95f + o, FlameMesh(), K.flame);
                f.transform.localScale = i == 0 ? new Vector3(0.26f, 0.5f, 0.26f) : new Vector3(0.18f, 0.32f, 0.18f);
                f.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (light)
            {
                var l = LightGo("BrazierLight", LightType.Point, Pal.Candle, 2.6f, 9f, true);
                l.transform.SetParent(t, false); l.transform.localPosition = Vector3.up * 1.6f;
            }
        }

        /// <summary>Halat kangalı (yerde üst üste halkalar).</summary>
        static void RopeCoil(Transform parent, Vector3 pos, float s, Material rope)
        {
            for (int k = 0; k < 4; k++)
                MeshObj("Coil", parent, pos + Vector3.up * (k * 0.045f * s), RingMesh((0.32f - k * 0.025f) * s, 0.05f * s, 0.045f * s), rope);
        }

        /// <summary>Tavandan inen makara + halat.</summary>
        static void Pulley(Transform parent, Vector3 top, float len, Material rope)
        {
            Tube("PulleyRope", parent, top, top + Vector3.down * len, 0.018f, rope);
            var p = top + Vector3.down * len;
            MeshObj("PulleyWheel", parent, p, RingMesh(0.16f, 0.06f, 0.05f), K.beam).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Slab("PulleyBlock", parent, p + Vector3.up * 0.08f, new Vector3(0.08f, 0.3f, 0.12f), K.iron, 1f, false);
            Tube("PulleyRope2", parent, p + new Vector3(0, -0.02f, 0.16f), p + new Vector3(0, -len * 0.45f, 0.16f), 0.015f, rope);
        }

        /// <summary>Kafes asansör: demir çerçeve + parmaklık + tavan + yukarı zincir; açık yüz -Z (yerel). pos: zemin.</summary>
        static Transform Cage(Transform parent, string name, Vector3 pos, float yaw, float chainUp)
        {
            var c = new GameObject(name).transform;
            c.SetParent(parent, false); c.localPosition = pos; c.localRotation = Quaternion.Euler(0, yaw, 0);
            const float w = 1.1f, h = 3.0f;
            Slab("CageFloor", c, new Vector3(0, 0.06f, 0), new Vector3(2.4f, 0.12f, 2.4f), K.iron, 1f, false);
            Slab("CageRoof", c, new Vector3(0, h, 0), new Vector3(2.4f, 0.12f, 2.4f), K.iron, 1f, false);
            foreach (var (x, z) in new[] { (-w, -w), (w, -w), (-w, w), (w, w) }) Rod("CagePost", c, new Vector3(x, 0, z), new Vector3(x, h, z), 0.09f, K.iron);
            for (float a = -w + 0.2f; a < w; a += 0.2f)
            {
                Rod("Bar", c, new Vector3(a, 0.1f, w), new Vector3(a, h, w), 0.03f, K.iron);
                Rod("Bar", c, new Vector3(-w, 0.1f, a), new Vector3(-w, h, a), 0.03f, K.iron);
                Rod("Bar", c, new Vector3(w, 0.1f, a), new Vector3(w, h, a), 0.03f, K.iron);
            }
            foreach (float y in new[] { 1.1f, 2.2f })
            {
                Slab("Rail", c, new Vector3(0, y, w), new Vector3(2.2f, 0.05f, 0.05f), K.brass, 1f, false);
                Slab("Rail", c, new Vector3(-w, y, 0), new Vector3(0.05f, 0.05f, 2.2f), K.brass, 1f, false);
                Slab("Rail", c, new Vector3(w, y, 0), new Vector3(0.05f, 0.05f, 2.2f), K.brass, 1f, false);
            }
            MeshObj("Ornament", c, new Vector3(0, h + 0.06f, 0), RingMesh(0.7f, 0.06f, 0.06f), K.brass);
            Rod("Chain", c, new Vector3(0, h, 0), new Vector3(0, h + chainUp, 0), 0.07f, K.iron);
            Slab("CageGuide", c, new Vector3(0, h + 0.3f, 0), new Vector3(0.5f, 0.6f, 0.5f), K.iron, 1f, false);
            var col = c.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, h * 0.5f, w + 0.05f); col.size = new Vector3(2.3f, h, 0.15f);
            foreach (int s in new[] { -1, 1 }) { var cc = c.gameObject.AddComponent<BoxCollider>(); cc.center = new Vector3(s * (w + 0.05f), h * 0.5f, 0); cc.size = new Vector3(0.15f, h, 2.3f); }
            return c;
        }

        // ---------------- Çan Dairesi (X) ----------------

        static Transform BuildBellChamber(Transform roomX, out BellNode[] bells, out BellNode great, out Transform[] plaques, out Transform[] dialGlyphs, out GameObject dialCover, out Transform dialHand, out Transform arrive)
        {
            const float HX = 9f, HZ = 9f, WH = 12f, RIDGE = 5.5f, GAL = 6.5f;
            var root = new GameObject("RoomA7_CanDairesi").transform;
            root.SetParent(roomX, false);
            root.localPosition = new Vector3(95f, 0, 70f);
            var wallM = KTex("K_BellWall", "castle_wall_slates", new Color(0.6f, 0.58f, 0.56f), 0.2f);
            var floorM = KTex("K_BellFloor", "medieval_wood", new Color(0.62f, 0.55f, 0.47f), 0.25f);
            var wood = K.beam; var stone = K.castleStone;
            var rope = MakeMat("K_Rope", new Color(0.47f, 0.38f, 0.26f)); rope.SetFloat("_Smoothness", 0.08f);
            var feather = MakeMat("K_Feather", new Color(0.72f, 0.7f, 0.66f)); feather.SetFloat("_Smoothness", 0.1f);
            var voidM = MakeMat("K_WellVoid", new Color(0.01f, 0.012f, 0.02f));

            // --- Kabuk: döşeme, taş duvarlar, köşe payandaları, kör kemerler ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), HX * 2f + 1f, HZ * 2f + 1f, 0.5f, floorM, 3, 3);
            Wall(root, "WallS", new Vector3(0, 0, -HZ - 0.25f), true, HX * 2f + 1f, WH, wallM, 2.5f);
            Wall(root, "WallN", new Vector3(0, 0, HZ + 0.25f), true, HX * 2f + 1f, WH, wallM, 2.5f);
            Wall(root, "WallW", new Vector3(-HX - 0.25f, 0, 0), false, HZ * 2f, WH, wallM, 2.5f);
            Wall(root, "WallE", new Vector3(HX + 0.25f, 0, 0), false, HZ * 2f, WH, wallM, 2.5f);
            for (int k = 0; k < 6; k++) // alın duvarları (üçgen, kademeli)
            {
                float y = WH + k * RIDGE / 6f, w = (HX * 2f + 1f) * (1f - (k + 0.5f) / 6f);
                foreach (int s in new[] { -1, 1 }) Slab("Gable", root, new Vector3(0, y + RIDGE / 12f, s * (HZ + 0.25f)), new Vector3(w, RIDGE / 6f, 0.5f), wallM, 2.5f, false);
            }
            foreach (var (x, z) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
                Slab("Buttress", root, new Vector3(x * (HX - 0.45f), WH * 0.5f, z * (HZ - 0.45f)), new Vector3(0.9f, WH, 0.9f), stone, 1.5f);
            foreach (int s in new[] { -1, 1 })
            {
                Slab("Pilaster", root, new Vector3(s * (HX - 0.3f), WH * 0.5f, 0), new Vector3(0.6f, WH, 0.8f), stone, 1.5f, false);
                ArchTrim(root, "BlindArchS", new Vector3(s * 5.2f, 0, -HZ), Vector3.forward, 3.0f, 5.2f, true);
                ArchTrim(root, "BlindArchN", new Vector3(s * 6.2f, 0, HZ), Vector3.back, 2.4f, 4.6f, true);
            }
            Slab("Plinth", root, new Vector3(0, 0.2f, -HZ + 0.15f), new Vector3(HX * 2f, 0.4f, 0.3f), stone, 1.5f, false);
            Slab("Plinth", root, new Vector3(0, 0.2f, HZ - 0.15f), new Vector3(HX * 2f, 0.4f, 0.3f), stone, 1.5f, false);
            Slab("CorniceW", root, new Vector3(-HX + 0.2f, WH - 0.3f, 0), new Vector3(0.6f, 0.4f, HZ * 2f), stone, 1.5f, false);
            Slab("CorniceE", root, new Vector3(HX - 0.2f, WH - 0.3f, 0), new Vector3(0.6f, 0.4f, HZ * 2f), stone, 1.5f, false);

            // --- Pencereler: yanlarda sivri lanset, kuzeyde gül pencere ---
            foreach (float z in new[] { -4.6f, 4.6f })
            {
                Kit("WindowLancet", root, new Vector3(-HX, 4.6f, z), 90f, 1.6f);
                Kit("WindowLancet", root, new Vector3(HX, 4.6f, z), -90f, 1.6f);
            }
            Kit("WindowRose", root, new Vector3(0, WH + 1.6f, HZ), 180f, 1.6f);

            // --- Açık ahşap çatı: kiriş, makas, mertek, mahya ---
            float slope = Mathf.Atan2(RIDGE, HX) * Mathf.Rad2Deg, rafterLen = Mathf.Sqrt(HX * HX + RIDGE * RIDGE);
            foreach (int s in new[] { -1, 1 })
            {
                var plane = Slab("RoofBoards", root, new Vector3(s * HX * 0.5f, WH + RIDGE * 0.5f + 0.25f, 0), new Vector3(rafterLen + 0.6f, 0.2f, HZ * 2f + 1f), wood, 2f, false);
                plane.transform.localRotation = Quaternion.Euler(0, 0, -s * slope);
            }
            Slab("Ridge", root, new Vector3(0, WH + RIDGE, 0), new Vector3(0.35f, 0.45f, HZ * 2f + 1f), wood, 1f, false);
            for (float z = -7.5f; z <= 7.6f; z += 2.5f)
            {
                Rod("Rafter", root, new Vector3(-HX, WH, z), new Vector3(0, WH + RIDGE, z), 0.24f, wood);
                Rod("Rafter", root, new Vector3(HX, WH, z), new Vector3(0, WH + RIDGE, z), 0.24f, wood);
            }
            foreach (float z in new[] { -6.25f, -1.25f, 3.75f })
            {
                Slab("TieBeam", root, new Vector3(0, WH, z), new Vector3(HX * 2f, 0.4f, 0.4f), wood, 1f, false);
                Rod("KingPost", root, new Vector3(0, WH, z), new Vector3(0, WH + RIDGE, z), 0.28f, wood);
                foreach (int s in new[] { -1, 1 }) Rod("Strut", root, new Vector3(0, WH + 0.4f, z), new Vector3(s * HX * 0.5f, WH + RIDGE * 0.5f, z), 0.2f, wood);
                foreach (int s in new[] { -1, 1 }) Rod("Brace", root, new Vector3(s * HX, WH - 2f, z), new Vector3(s * (HX - 1.6f), WH, z), 0.22f, wood);
            }

            // --- Galeri (batı ve doğu duvarı boyunca) + merdivenden batı galeriye köprü ---
            foreach (int s in new[] { -1, 1 })
            {
                float gx = s * (HX - 0.75f);
                Slab("Gallery", root, new Vector3(gx, GAL, 0), new Vector3(1.5f, 0.16f, HZ * 2f - 2f), floorM, 1.5f, false);
                Slab("GalleryRail", root, new Vector3(gx - s * 0.7f, GAL + 1.0f, 0), new Vector3(0.08f, 0.08f, HZ * 2f - 2f), wood, 1f, false);
                for (float z = -HZ + 1.2f; z < HZ - 1f; z += 1.1f)
                {
                    Rod("Baluster", root, new Vector3(gx - s * 0.7f, GAL, z), new Vector3(gx - s * 0.7f, GAL + 1.0f, z), 0.06f, wood);
                    if (Mathf.Abs(z) > 1.4f && Mathf.Repeat(z + 20f, 3.3f) < 1.1f) Rod("GalleryBracket", root, new Vector3(s * HX, GAL - 1.4f, z), new Vector3(gx - s * 0.5f, GAL - 0.08f, z), 0.14f, wood);
                }
            }
            Slab("Bridge", root, new Vector3(-4.1f, GAL, 0), new Vector3(6.6f, 0.14f, 1.1f), floorM, 1.5f, false);
            foreach (int s in new[] { -1, 1 }) Slab("BridgeRail", root, new Vector3(-4.1f, GAL + 0.9f, s * 0.52f), new Vector3(6.6f, 0.06f, 0.06f), wood, 1f, false);
            for (float x = -7.2f; x < -0.9f; x += 1.0f) foreach (int s in new[] { -1, 1 }) Rod("BridgePost", root, new Vector3(x, GAL, s * 0.52f), new Vector3(x, GAL + 0.9f, s * 0.52f), 0.05f, wood);

            // --- Ortada serbest döner merdiven (demir direk, ahşap basamak, korkuluk) ---
            Tube("StairPost", root, Vector3.zero, Vector3.up * (GAL + 1.2f), 0.14f, K.iron);
            Vector3 prevTop = Vector3.zero;
            for (int k = 0; k < 30; k++)
            {
                float a = k * 24f, y = 0.2f + k * (GAL - 0.2f) / 29f;
                var d = Quaternion.Euler(0, a, 0) * Vector3.right;
                var st = Slab("Step", root, d * 0.72f + Vector3.up * y, new Vector3(1.2f, 0.07f, 0.34f), floorM, 1f, false);
                st.transform.localRotation = Quaternion.Euler(0, a, 0);
                var top = d * 1.3f + Vector3.up * (y + 0.95f);
                Rod("StairBaluster", root, d * 1.3f + Vector3.up * y, top, 0.035f, K.iron);
                if (k > 0) Rod("StairRail", root, prevTop, top, 0.05f, K.iron);
                prevTop = top;
            }
            var stairCol = root.gameObject.AddComponent<CapsuleCollider>(); stairCol.center = new Vector3(0, 3.5f, 0); stairCol.radius = 1.35f; stairCol.height = 7f;

            // --- Dört çan: kasnak (A ayak + başlık), sallanan pivot (çan + tekerlek), ip + tutamak + nota levhası ---
            var bpos = new[] { new Vector3(-5f, 8.4f, 3.4f), new Vector3(-5f, 6.0f, -3.4f), new Vector3(5f, 7.6f, 3.2f), new Vector3(5f, 5.6f, -3.4f) };
            var bscale = new[] { 1.3f, 1.7f, 2.1f, 2.5f };
            bells = new BellNode[4]; plaques = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var b = bpos[i]; float s = bscale[i], side = Mathf.Sign(b.x), P = 0.55f * s + 0.45f, W = 0.55f * s + 0.1f;
                foreach (int k in new[] { -1, 1 })
                {
                    var px = b.x + k * P;
                    Slab("FramePost", root, new Vector3(px, (b.y + 0.5f) * 0.5f, b.z), new Vector3(0.32f, b.y + 0.5f, 0.32f), wood, 1f);
                    foreach (int zz in new[] { -1, 1 }) Rod("FrameLeg", root, new Vector3(px, 0, b.z + zz * 1.3f), new Vector3(px, b.y * 0.62f, b.z), 0.22f, wood);
                    Slab("FrameShoe", root, new Vector3(px, 0.12f, b.z), new Vector3(0.5f, 0.24f, 2.9f), wood, 1f, false);
                }
                Tube("Axle", root, new Vector3(b.x - P, b.y, b.z), new Vector3(b.x + P, b.y, b.z), 0.06f, K.iron);
                Slab("FrameCap", root, new Vector3(b.x, b.y + 0.45f, b.z), new Vector3(P * 2f + 0.5f, 0.3f, 0.45f), wood, 1f, false);
                var pv = new GameObject("BellPivot" + i).transform;
                pv.SetParent(root, false); pv.localPosition = b;
                Slab("Headstock", pv, new Vector3(0, 0.05f, 0), new Vector3(W * 2f - 0.2f, 0.32f, 0.42f), wood, 1f, false);
                foreach (int k in new[] { -1, 1 }) Slab("Strap", pv, new Vector3(k * 0.18f * s, -0.12f, 0), new Vector3(0.06f, 0.4f, 0.46f), K.iron, 1f, false);
                Kit("Bell", pv, new Vector3(0, -1.1f * s, 0), 0f, s);
                var wx = -side * W;
                MeshObj("Wheel", pv, new Vector3(wx, 0, 0), RingMesh(0.42f * s, 0.07f, 0.09f), wood).transform.localRotation = Quaternion.Euler(0, 0, 90);
                for (int k = 0; k < 4; k++)
                {
                    var d = Quaternion.Euler(k * 45f, 0, 0) * Vector3.up * 0.4f * s;
                    Rod("Spoke", pv, new Vector3(wx, 0, 0) - d, new Vector3(wx, 0, 0) + d, 0.05f, wood);
                }
                var ropeTop = new Vector3(b.x + wx, b.y, b.z + 0.42f * s);
                Tube("BellRope", root, ropeTop, new Vector3(ropeTop.x, 0.95f, ropeTop.z), 0.02f, rope);
                Tube("Sally", root, new Vector3(ropeTop.x, 1.05f, ropeTop.z), new Vector3(ropeTop.x, 1.75f, ropeTop.z), 0.055f, K.velvet);
                var rn = new GameObject("BellRope" + i);
                rn.transform.SetParent(root, false); rn.transform.localPosition = new Vector3(ropeTop.x, 0, ropeTop.z);
                var bc = rn.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.4f, 0); bc.size = new Vector3(0.45f, 1.8f, 0.45f);
                rn.AddComponent<Interactable>().Label = "Çanı çal";
                var node = rn.AddComponent<BellNode>();
                var so = new SerializedObject(node);
                so.FindProperty("kind").enumValueIndex = (int)BellNode.Kind.Bell;
                so.FindProperty("index").intValue = i;
                SetRef(so, "swing", pv);
                so.ApplyModifiedPropertiesWithoutUndo();
                bells[i] = node;
                foreach (var t in pv.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                // ip yanında levha direği: demir levha + pirinç nota (oda ortasına bakar)
                var post = new Vector3(ropeTop.x, 0, ropeTop.z + 0.55f);
                Slab("PlaquePost", root, post + Vector3.up * 0.95f, new Vector3(0.12f, 1.9f, 0.12f), wood, 1f);
                var pl = new GameObject("BellPlaque" + i).transform;
                pl.SetParent(root, false); pl.localPosition = post + new Vector3(-side * 0.08f, 1.62f, 0); pl.localRotation = Quaternion.LookRotation(new Vector3(side, 0, 0));
                Slab("Plate", pl, new Vector3(0, 0, 0.03f), new Vector3(0.44f, 0.44f, 0.04f), K.iron, 1f, false);
                MeshObj("Rim", pl, Vector3.zero, RingMesh(0.2f, 0.02f, 0.03f), K.brass).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var g = new GameObject("Glyph").transform; g.SetParent(pl, false); g.localPosition = new Vector3(0, 0, -0.005f);
                plaques[i] = g;
            }

            // --- Büyük çan (kuzey ortası) + tokmak kürsüsü ---
            {
                var b = new Vector3(0, 9.7f, 5.8f); const float s = 3.4f; float P = 0.55f * s + 0.45f;
                foreach (int k in new[] { -1, 1 })
                {
                    Slab("GreatPost", root, new Vector3(k * P, (b.y + 0.6f) * 0.5f, b.z), new Vector3(0.45f, b.y + 0.6f, 0.45f), wood, 1f);
                    foreach (int zz in new[] { -1, 1 }) Rod("GreatLeg", root, new Vector3(k * P, 0, b.z + zz * 1.8f), new Vector3(k * P, b.y * 0.6f, b.z), 0.3f, wood);
                }
                Slab("GreatCap", root, new Vector3(0, b.y + 0.55f, b.z), new Vector3(P * 2f + 0.8f, 0.45f, 0.6f), wood, 1f, false);
                Tube("GreatAxle", root, new Vector3(-P, b.y, b.z), new Vector3(P, b.y, b.z), 0.09f, K.iron);
                var pv = new GameObject("GreatBellPivot").transform;
                pv.SetParent(root, false); pv.localPosition = b;
                Slab("Headstock", pv, new Vector3(0, 0.08f, 0), new Vector3(P * 2f - 0.6f, 0.45f, 0.6f), wood, 1f, false);
                Kit("Bell", pv, new Vector3(0, -1.1f * s, 0), 0f, s);
                var st = new GameObject("GreatBellStriker");
                st.transform.SetParent(root, false); st.transform.localPosition = new Vector3(0, 0, 3.2f);
                Slab("StrikerBase", st.transform, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 1.0f, 0.6f), stone, 1f, false);
                Slab("StrikerCap", st.transform, new Vector3(0, 1.04f, 0), new Vector3(0.9f, 0.08f, 0.7f), K.bronze, 1f, false);
                Rod("StrikerHandle", st.transform, new Vector3(0, 1.08f, 0), new Vector3(0, 1.75f, 0.35f), 0.07f, K.iron);
                Prim("StrikerKnob", st.transform, PrimitiveType.Sphere, new Vector3(0, 1.78f, 0.37f), Vector3.one * 0.14f, K.brass);
                Tube("StrikerRope", root, new Vector3(0, 1.75f, 3.55f), new Vector3(0, b.y - 3.0f, b.z - 0.6f), 0.03f, rope);
                var bc = st.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.9f, 0); bc.size = new Vector3(0.9f, 1.8f, 0.8f);
                st.AddComponent<Interactable>().Label = "Büyük çanı çal";
                great = st.AddComponent<BellNode>();
                var so = new SerializedObject(great);
                so.FindProperty("kind").enumValueIndex = (int)BellNode.Kind.GreatBell;
                SetRef(so, "swing", pv);
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (var t in pv.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                Kit("CandleCluster", root, new Vector3(-0.8f, 0, 2.6f), 30f);
                Kit("CandleCluster", root, new Vector3(0.9f, 0, 2.7f), 200f);
            }

            // --- Pirinç kadran kürsüsü (güneye bakar): 4 pencere, ibre, kapak ---
            {
                var dp = new Vector3(2.2f, 0, -5.4f);
                Slab("DialPedestal", root, dp + Vector3.up * 0.6f, new Vector3(0.7f, 1.2f, 0.5f), stone, 1f);
                Slab("DialPedestalCap", root, dp + Vector3.up * 1.24f, new Vector3(0.85f, 0.08f, 0.62f), K.bronze, 1f, false);
                var dial = new GameObject("Dial").transform;
                dial.SetParent(root, false); dial.localPosition = dp + new Vector3(0, 1.85f, 0);
                Prim("Face", dial, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.15f, 0.03f, 1.15f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("Bezel", dial, new Vector3(0, 0, -0.03f), RingMesh(0.6f, 0.06f, 0.06f), K.bronze).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                foreach (var g in new[] { new Vector3(-0.6f, -0.45f, 0.05f), new Vector3(0.55f, 0.5f, 0.05f) })
                    MeshObj("Gear", dial, g, RingMesh(0.22f, 0.07f, 0.04f), K.bronze).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                dialGlyphs = new Transform[4];
                var wp = new[] { new Vector3(0, 0.32f, 0), new Vector3(0.32f, 0, 0), new Vector3(0, -0.32f, 0), new Vector3(-0.32f, 0, 0) };
                for (int i = 0; i < 4; i++)
                {
                    Prim("Window", dial, PrimitiveType.Cylinder, wp[i] + new Vector3(0, 0, -0.02f), new Vector3(0.3f, 0.01f, 0.3f), voidM).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var g = new GameObject("DialGlyph" + i).transform; g.SetParent(dial, false); g.localPosition = wp[i] + new Vector3(0, 0, -0.03f);
                    dialGlyphs[i] = g;
                }
                dialHand = new GameObject("Hand").transform; dialHand.SetParent(dial, false); dialHand.localPosition = new Vector3(0, 0, -0.05f);
                Slab("HandBar", dialHand, new Vector3(0, 0.07f, 0), new Vector3(0.035f, 0.14f, 0.012f), K.iron, 1f, false);
                Prim("HandCap", dialHand, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.06f, K.brass);
                var cov = new GameObject("DialCover").transform; cov.SetParent(dial, false); cov.localPosition = new Vector3(0, 0, -0.08f);
                Prim("Lid", cov, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.0f, 0.02f, 1.0f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4f; Prim("Rivet", cov, PrimitiveType.Sphere, new Vector3(Mathf.Sin(a) * 0.42f, Mathf.Cos(a) * 0.42f, -0.02f), Vector3.one * 0.04f, K.brass); }
                Kit("Bell", cov, new Vector3(0, -0.12f, -0.03f), 0f, 0.22f);
                dialCover = cov.gameObject;
                foreach (var t in dial.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                Kit("Candle", root, dp + new Vector3(0.32f, 1.28f, -0.18f));
                KCandle(root, dp + new Vector3(-0.3f, 1.28f, -0.2f), 0.18f, 0.02f);
            }

            // --- Kafes asansör (güneybatı; bulmaca 7 çözülünce finale iner) ---
            var cage = Cage(root, "LiftCageX", new Vector3(-6.9f, 0, -6.6f), -90f, WH - 3f);

            // --- Giriş (güney), aplikler, mumlar ---
            Kit("Door", root, new Vector3(0, 0, -HZ + 0.02f), 0f, 1.15f);
            ArchTrim(root, "DoorArch", new Vector3(0, 0, -HZ), Vector3.forward, 2.7f, 3.8f, false);
            foreach (int s in new[] { -1, 1 })
            {
                Kit("WallSconce", root, new Vector3(s * 1.9f, 2.6f, -HZ + 0.03f), 0f);
                Kit("Banner", root, new Vector3(s * 3.4f, 9.6f, -HZ + 0.02f), 0f, 1.6f);
                Kit("CandelabraFloor", root, new Vector3(s * 2.4f, 0, -HZ + 1.0f), 0f);
            }
            Kit("L_Candle", root, new Vector3(-1.9f, 2.7f, -HZ + 0.6f));
            Kit("L_Candle", root, new Vector3(1.9f, 2.7f, -HZ + 0.6f));
            arrive = new GameObject("ChamberArrive").transform;
            arrive.SetParent(root, false); arrive.localPosition = new Vector3(0, 0.05f, -HZ + 1.6f);

            // --- Doğu: pencere masası (kitaplar, mumlar, notlar), kitaplık, sandık ---
            Kit("Table", root, new Vector3(HX - 1.0f, 0, 0.6f), -90f);
            Kit("BookStack", root, new Vector3(HX - 1.0f, 0.82f, 1.2f), 20f);
            Kit("BookStack", root, new Vector3(HX - 0.9f, 0.82f, -0.2f), 80f);
            Kit("Candlestick", root, new Vector3(HX - 1.1f, 0.82f, 0.5f), 0f);
            Kit("PaperPile", root, new Vector3(HX - 1.2f, 0.82f, 0.9f), 30f);
            Kit("OilLamp", root, new Vector3(HX - 0.8f, 0.82f, 0.2f), 0f);
            Kit("Stool", root, new Vector3(HX - 2.0f, 0, 0.4f), 15f);
            Kit("L_Candle", root, new Vector3(HX - 1.1f, 1.25f, 0.5f));
            Kit("BookshelfFull1", root, new Vector3(HX - 0.35f, 0, -6.6f), -90f);
            Kit("Chest", root, new Vector3(HX - 1.1f, 0, 7.4f), -100f);
            Kit("CandleCluster", root, new Vector3(HX - 0.7f, 0, 2.4f), 0f);
            Kit("Barrel2", root, new Vector3(HX - 0.9f, 0, -2.6f), 40f);
            Kit("Bucket", root, new Vector3(HX - 2.1f, 0, -1.0f), 0f);

            // --- Batı: çan dökümcüsü tezgâhı, aletler, sandıklar, merdiven ---
            Kit("Workbench", root, new Vector3(-HX + 1.0f, 0, -0.2f), 90f);
            Kit("L_Lantern", root, new Vector3(-HX + 1.1f, 0.95f, 0.9f), 0f);
            Kit("CrateStack", root, new Vector3(-HX + 1.0f, 0, 7.4f), 10f);
            Kit("Crate", root, new Vector3(-HX + 2.4f, 0, 7.8f), 35f);
            Kit("Ladder", root, new Vector3(-HX + 0.5f, 0, 5.4f), 90f);
            Kit("BarrelLying", root, new Vector3(-HX + 1.2f, 0, -3.6f), 0f);
            Kit("Barrel", root, new Vector3(-HX + 0.9f, 0, -4.9f), 0f);

            // --- Yerde: aletler, halat kangalları, makaralar, tüyler, kâğıtlar, fenerler, mumlar ---
            var tools = new[] { ("Hammer", 3.2f, -1.6f), ("Wrench", 3.6f, -1.9f), ("Mallet", -2.6f, 1.6f), ("Wrench", -2.2f, -6.2f), ("Hammer", 6.4f, 4.6f), ("Vice", -HX + 1.0f, -1.4f) };
            foreach (var (n, x, z) in tools) Kit(n, root, new Vector3(x, n == "Vice" ? 0.9f : 0.02f, z), x * 37f);
            foreach (var (x, z, s) in new[] { (-2.8f, -3.6f, 1.2f), (3.0f, 1.6f, 1.0f), (-7.4f, 2.6f, 1.3f), (6.9f, -5.0f, 1.1f), (-2.4f, 6.8f, 0.9f), (3.4f, 7.6f, 1.2f) })
                RopeCoil(root, new Vector3(x, 0.02f, z), s, rope);
            foreach (var (x, z, l) in new[] { (-2.6f, -6.5f, 6f), (2.6f, 6.0f, 4.5f), (6.8f, 0.3f, 5f), (-3.0f, 1.0f, 3.5f) })
                Pulley(root, new Vector3(x, WH, z), l, rope);
            for (int k = 0; k < 24; k++)
            {
                var p = new Vector3(-8f + Hash01(k, 71) * 16f, 0.012f, -8f + Hash01(k, 72) * 16f);
                if (p.magnitude < 1.6f) continue;
                Slab("Feather", root, p, new Vector3(0.03f, 0.008f, 0.14f), feather, 1f, false).transform.localRotation = Quaternion.Euler(0, Hash01(k, 73) * 360f, 0);
            }
            foreach (var (x, z, r) in new[] { (-3.6f, -7.6f, 30f), (4.6f, -1.2f, 200f), (-6.0f, 4.2f, 90f), (1.0f, -3.0f, 140f) }) Kit("PaperScatter", root, new Vector3(x, 0.01f, z), r);
            foreach (var (x, z) in new[] { (-3.8f, -1.0f), (3.9f, -0.6f), (-4.6f, 6.4f), (6.2f, 2.0f), (-1.6f, -7.8f) }) Kit("Lantern", root, new Vector3(x, 0.02f, z), x * 30f);
            foreach (var (x, z) in new[] { (-5.6f, 1.1f), (5.6f, 0.9f), (-6.4f, -5.6f), (4.2f, -6.2f), (-1.2f, 4.4f), (6.2f, 6.4f) }) Kit("Candle", root, new Vector3(x, 0.02f, z), 0f);
            Kit("Basket", root, new Vector3(1.4f, 0, -7.6f), 0f);
            Kit("Jug", root, new Vector3(HX - 2.7f, 0, -1.4f), 0f);
            Kit("Bottle2", root, new Vector3(-HX + 2.0f, 0, -4.4f), 0f);
            Kit("Goblet0", root, new Vector3(HX - 0.8f, 0.82f, 1.6f), 0f);

            // --- Galeri üstü: fenerler, sandıklar, kitaplar ---
            foreach (int s in new[] { -1, 1 })
            {
                Kit("Lantern", root, new Vector3(s * (HX - 0.6f), GAL + 0.08f, -6f), 0f);
                Kit("Lantern", root, new Vector3(s * (HX - 0.6f), GAL + 0.08f, 6.5f), 0f);
                Kit("Crate", root, new Vector3(s * (HX - 0.7f), GAL + 0.08f, 2.4f), 20f);
                Kit("BookStack", root, new Vector3(s * (HX - 0.7f), GAL + 0.68f, 2.4f), 70f);
                Kit("CandleCluster", root, new Vector3(s * (HX - 0.6f), GAL + 0.08f, -3.0f), 0f);
            }

            // --- Örümcek ağları (köşeler, makas araları) ---
            foreach (var (x, y, z, yaw) in new[] { (-HX + 0.2f, WH - 1.2f, -HZ + 0.2f, 45f), (HX - 0.2f, WH - 1.2f, -HZ + 0.2f, -45f), (-HX + 0.2f, WH - 1.2f, HZ - 0.2f, 135f), (HX - 0.2f, WH - 1.2f, HZ - 0.2f, -135f),
                                                 (-HX + 0.3f, 3.0f, 8.6f, 135f), (HX - 0.3f, 2.6f, -8.6f, -45f), (0, WH + 3.5f, -6.25f, 0f), (0, WH + 3.5f, 3.75f, 180f) })
                KitR("Cobweb", root, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 180f), 1.7f);

            KitProbes(root, new Vector3(0, WH * 0.5f, 0), new Vector3(HX * 2f - 0.6f, WH - 0.6f, HZ * 2f - 0.6f), 5, 4, 5, new Bounds(new Vector3(0, 3.5f, 0), new Vector3(1.6f, 7f, 1.6f)));
            KitReflection(root, new Vector3(0, WH * 0.5f, 0), new Vector3(HX * 2f, WH, HZ * 2f));
            return root;
        }

        // ---------------- Kemik Org Kriptası (O) ----------------

        static Transform BuildOrganCrypt(Transform roomO, out BellNode[] keys, out Transform[] keyPlaques, out Transform[] script, out Transform arrive)
        {
            const float HX = 8f, HZ = 15f, WH = 8.5f, VAULT = 5f;
            var root = new GameObject("RoomB7_KemikOrg").transform;
            root.SetParent(roomO, false);
            root.localPosition = new Vector3(90f, 0, 60f);
            var wallM = KTex("K_CryptWall", "granite_wall", new Color(0.55f, 0.52f, 0.48f), 0.2f);
            var floorM = KTex("K_CryptFloor", "granite_tile_03", new Color(0.32f, 0.32f, 0.35f), 0.6f);
            var stone = K.castleStone;
            var bone = MakeMat("K_Bone", new Color(0.6f, 0.55f, 0.46f));
            var oldBone = MakeMat("K_BoneOld", new Color(0.46f, 0.41f, 0.33f)); oldBone.SetFloat("_Smoothness", 0.25f);
            var voidM = MakeMat("K_WellVoid", new Color(0.01f, 0.012f, 0.02f));
            var inlay = MakeMat("K_CryptInlay", new Color(0.22f, 0.2f, 0.18f)); inlay.SetFloat("_Smoothness", 0.7f);

            // --- Kabuk: zemin, duvarlar, sivri tonoz + kaburgalar ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), HX * 2f + 1f, HZ * 2f + 1f, 0.5f, floorM, 3, 5);
            Wall(root, "WallW", new Vector3(-HX - 0.25f, 0, 0), false, HZ * 2f + 1f, WH, wallM, 2.5f);
            Wall(root, "WallE", new Vector3(HX + 0.25f, 0, 0), false, HZ * 2f + 1f, WH, wallM, 2.5f);
            Wall(root, "WallS", new Vector3(0, 0, -HZ - 0.25f), true, HX * 2f, WH + VAULT, wallM, 2.5f);
            Wall(root, "WallN", new Vector3(0, 0, HZ + 0.25f), true, HX * 2f, WH + VAULT, wallM, 2.5f);
            const int seg = 8;
            Vector2 Arc(float t) { float x = HX * (1f - t); return new Vector2(x, WH + VAULT * Mathf.Sqrt(Mathf.Clamp01(1f - (x / HX) * (x / HX))) * (0.75f + 0.25f * t)); }
            for (int k = 0; k < seg; k++)
            {
                var a = Arc(k / (float)seg); var b = Arc((k + 1) / (float)seg);
                foreach (int s in new[] { -1, 1 })
                {
                    var pa = new Vector3(s * a.x, a.y, 0); var pb = new Vector3(s * b.x, b.y, 0);
                    var v = Slab("Vault", root, (pa + pb) * 0.5f + Vector3.up * 0.2f, new Vector3((pb - pa).magnitude + 0.1f, 0.4f, HZ * 2f + 0.5f), wallM, 2.5f, false);
                    v.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
                    for (float z = -12f; z <= 12.1f; z += 6f) Rod("Rib", root, pa + new Vector3(0, -0.1f, z), pb + new Vector3(0, -0.1f, z), 0.3f, stone);
                }
            }
            Slab("RidgeRib", root, new Vector3(0, Arc(1f).y - 0.15f, 0), new Vector3(0.35f, 0.3f, HZ * 2f), stone, 1f, false);

            // --- Tavan ızgarası (kuyuya açılır; çan sesi buradan iner) + ışık huzmesi ---
            var grateY = Arc(1f).y - 0.1f;
            Slab("SkyGlow", root, new Vector3(0, grateY + 0.3f, 4f), new Vector3(2.4f, 0.05f, 2.4f), GlowMat("K_SkyGlow", new Color(0.1f, 0.13f, 0.2f), new Color(0.25f, 0.33f, 0.55f)), 1f, false);
            for (float g = -1.0f; g <= 1.01f; g += 0.4f)
            {
                Slab("GrateBar", root, new Vector3(g, grateY, 4f), new Vector3(0.06f, 0.08f, 2.4f), K.iron, 1f, false);
                Slab("GrateBar", root, new Vector3(0, grateY, 4f + g), new Vector3(2.4f, 0.08f, 0.06f), K.iron, 1f, false);
            }
            for (int k = 0; k < 3; k++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = "MoonShaft"; q.transform.SetParent(root, false);
                q.transform.localPosition = new Vector3(0, grateY * 0.5f, 5.6f);
                q.transform.localRotation = Quaternion.Euler(-12f, k * 60f, 0);
                q.transform.localScale = new Vector3(2.4f, grateY, 1f);
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = K.shaft; r.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(q, 0);
            }
            var moon = LightGo("GrateMoon", LightType.Spot, Pal.Moon, 6f, 16f, true);
            moon.transform.SetParent(root, false); moon.transform.localPosition = new Vector3(0, grateY - 0.3f, 4f);
            moon.transform.localRotation = Quaternion.LookRotation(new Vector3(0, -1f, 0.25f));
            var ml = moon.GetComponent<Light>(); ml.spotAngle = 50f; ml.innerSpotAngle = 25f;

            // --- Yan kemer sıraları: niş + lahit/kemik yığını, kafatası bantlı sütunlar, şövalye heykelleri, mangallar ---
            var bays = new[] { -10.5f, -4.5f, 1.5f, 7.5f };
            var rnd = new System.Random(7070);
            foreach (int s in new[] { -1, 1 })
            {
                for (int b = 0; b < bays.Length; b++)
                {
                    float z = bays[b];
                    ArchTrim(root, "Bay", new Vector3(s * HX, 0, z), new Vector3(-s, 0, 0), 3.6f, 5.4f, true);
                    if ((b + (s > 0 ? 1 : 0)) % 2 == 0)
                    {   // lahit: taş sanduka + kapak + üstünde kafatası ve mum
                        Slab("Sarcophagus", root, new Vector3(s * (HX - 1.0f), 0.45f, z), new Vector3(1.0f, 0.9f, 2.3f), stone, 1f);
                        Slab("SarcoLid", root, new Vector3(s * (HX - 1.0f), 0.98f, z), new Vector3(1.15f, 0.16f, 2.45f), stone, 1f, false);
                        Slab("SarcoCross", root, new Vector3(s * (HX - 1.0f), 1.08f, z), new Vector3(0.12f, 0.05f, 1.6f), K.bronze, 1f, false);
                        Skull(root, new Vector3(s * (HX - 1.0f), 1.06f, z + 0.85f), s * 90f, 1.2f, bone, voidM);
                        Kit("CandleCluster", root, new Vector3(s * (HX - 0.9f), 1.06f, z - 0.8f), 0f);
                    }
                    else
                    {   // kemik nişi: yığın + kafatası dizisi
                        for (int k = 0; k < 9; k++)
                        {
                            var p = new Vector3(s * (HX - 0.7f - (float)rnd.NextDouble() * 0.8f), 0.05f + k * 0.03f, z - 1.1f + (float)rnd.NextDouble() * 2.2f);
                            float ang = rnd.Next(360) * Mathf.Deg2Rad; var dd = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.3f;
                            Rod("Bone", root, p - dd, p + dd + Vector3.up * 0.04f, 0.05f, oldBone);
                        }
                        for (int k = 0; k < 5; k++) Skull(root, new Vector3(s * (HX - 0.5f), 0.32f, z - 1.2f + k * 0.6f), -s * 90f, 1.1f, k % 2 == 0 ? bone : oldBone, voidM);
                        for (int k = 0; k < 4; k++) Skull(root, new Vector3(s * (HX - 0.45f), 3.2f, z - 0.9f + k * 0.6f), -s * 90f, 1.0f, oldBone, voidM);
                        Kit("Candle", root, new Vector3(s * (HX - 1.2f), 0.02f, z + 0.4f), 0f);
                    }
                }
                foreach (float z in new[] { -13.5f, -7.5f, -1.5f, 4.5f, 10.5f })
                {
                    Slab("Column", root, new Vector3(s * (HX - 0.4f), WH * 0.5f, z), new Vector3(0.9f, WH, 0.9f), stone, 1.5f);
                    Slab("ColumnCap", root, new Vector3(s * (HX - 0.45f), WH - 0.2f, z), new Vector3(1.1f, 0.4f, 1.1f), stone, 1f, false);
                    for (int k = 0; k < 3; k++) Skull(root, new Vector3(s * (HX - 0.92f), 2.0f + k * 1.6f, z), -s * 90f, 0.9f, k == 1 ? bone : oldBone, voidM);
                    if (z > -13f && z < 10f)
                    {
                        Slab("StatuePlinth", root, new Vector3(s * (HX - 1.6f), 0.4f, z), new Vector3(1.2f, 0.8f, 1.2f), stone, 1f);
                        Kit("StatueKing", root, new Vector3(s * (HX - 1.6f), 0.8f, z), s > 0 ? -90f : 90f, 1.45f);
                    }
                    if (z > 10f) Kit("WallSconce", root, new Vector3(s * (HX - 0.85f), 3.4f, z - 0.8f), s > 0 ? -90f : 90f);
                }
                foreach (float z in new[] { -7.5f, 4.5f }) Brazier(root, new Vector3(s * (HX - 3.0f), 0, z), 1.0f, true);
                foreach (float z in new[] { -1.5f }) Brazier(root, new Vector3(s * (HX - 3.0f), 0, z), 0.9f, false);
                foreach (float z in new[] { -9.0f, 3.0f }) Kit("CandelabraFloor", root, new Vector3(s * (HX - 2.6f), 0, z), 0f);
                Kit("Banner", root, new Vector3(s * (HX - 0.02f), WH - 0.4f, -4.5f), s > 0 ? -90f : 90f, 1.5f);
                Kit("Banner", root, new Vector3(s * (HX - 0.02f), WH - 0.4f, 7.5f), s > 0 ? -90f : 90f, 1.5f);
            }

            // --- Zemin işlemesi: iç içe halkalar + ışınlar (ref org.png) ---
            foreach (var (z, rr) in new[] { (-2f, 2.2f), (-2f, 3.6f), (-2f, 5.2f), (9.5f, 1.8f), (9.5f, 3.0f) })
                MeshObj("FloorRing", root, new Vector3(0, 0.005f, z), RingMesh(rr, 0.1f, 0.012f), inlay);
            for (int k = 0; k < 8; k++)
                Slab("FloorRay", root, Quaternion.Euler(0, k * 45f + 22.5f, 0) * Vector3.forward * 3.7f + new Vector3(0, 0.008f, -2f), new Vector3(0.08f, 0.012f, 3.0f), inlay, 1f, false)
                    .transform.localRotation = Quaternion.Euler(0, k * 45f + 22.5f, 0);

            // --- Kemik org (kuzey): gövde, tuşlar, kitabe, borular, kafatası süslemesi ---
            var organ = new GameObject("BoneOrgan").transform;
            organ.SetParent(root, false); organ.localPosition = new Vector3(0, 0, 12.4f);
            Slab("Console", organ, new Vector3(0, 0.55f, 1.0f), new Vector3(7.2f, 1.1f, 1.8f), stone, 1.2f);
            Slab("ConsoleTrim", organ, new Vector3(0, 1.12f, 1.0f), new Vector3(7.4f, 0.1f, 1.95f), oldBone, 1f, false);
            Slab("KeyShelf", organ, new Vector3(0, 0.96f, 0.05f), new Vector3(3.4f, 0.1f, 0.6f), K.furniture, 1f, false);
            Slab("KeyBack", organ, new Vector3(0, 1.32f, 0.38f), new Vector3(3.4f, 0.5f, 0.12f), K.furniture, 1f, false);
            keys = new BellNode[6]; keyPlaques = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                var kp = new Vector3(-1.25f + i * 0.5f, 1.04f, -0.02f);
                var kgo = new GameObject("OrganKey" + i);
                kgo.transform.SetParent(organ, false); kgo.transform.localPosition = kp;
                var vis = Slab("Key", kgo.transform, Vector3.zero, new Vector3(0.42f, 0.07f, 0.44f), bone, 1f, false);
                var bc = kgo.AddComponent<BoxCollider>(); bc.size = new Vector3(0.44f, 0.2f, 0.5f);
                kgo.AddComponent<Interactable>().Label = "Tuşa bas";
                var node = kgo.AddComponent<BellNode>();
                var so = new SerializedObject(node);
                so.FindProperty("kind").enumValueIndex = (int)BellNode.Kind.Key;
                so.FindProperty("index").intValue = i;
                SetRef(so, "swing", vis.transform);
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (var t in kgo.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                keys[i] = node;
                var pl = new GameObject("KeyPlaque" + i).transform;
                pl.SetParent(organ, false); pl.localPosition = new Vector3(kp.x, 1.34f, 0.31f);
                Slab("Plate", pl, new Vector3(0, 0, 0.012f), new Vector3(0.34f, 0.34f, 0.02f), bone, 1f, false);
                var g = new GameObject("Glyph").transform; g.SetParent(pl, false); g.localPosition = new Vector3(0, 0, -0.002f);
                keyPlaques[i] = g;
            }
            // kitabe (sağda, nota kâğıdı gibi: 5 çizgi + 4 nota)
            {
                // Gerçek kitabe orgda değil: doğu yan nefin z 1.5 kemik nişinde, heykel kaidelerinin ardında (girişten görünmez, aranır)
                var sp = new GameObject("Inscription").transform;
                sp.SetParent(root, false); sp.localPosition = new Vector3(HX - 0.12f, 1.8f, 1.5f);
                sp.localRotation = Quaternion.Euler(0, 90, 0); // ön yüz (-Z) batıya, nefe
                Slab("Frame", sp, new Vector3(0, 0, 0.04f), new Vector3(1.85f, 0.95f, 0.08f), oldBone, 1f, false);
                Slab("Sheet", sp, new Vector3(0, 0, -0.005f), new Vector3(1.7f, 0.8f, 0.01f), K.paperB, 1f, false);
                for (int l = 0; l < 5; l++) Slab("StaffLine", sp, new Vector3(0, -0.2f + l * 0.1f, -0.012f), new Vector3(1.6f, 0.008f, 0.004f), MakeMat("K_NoteDark", new Color(0.05f, 0.04f, 0.035f)), 1f, false);
                script = new Transform[4];
                for (int i = 0; i < 4; i++)
                {
                    var g = new GameObject("ScriptGlyph" + i).transform; g.SetParent(sp, false); g.localPosition = new Vector3(-0.55f + i * 0.37f, 0, -0.016f);
                    script[i] = g;
                }
                Kit("CandleCluster", root, new Vector3(HX - 0.9f, 0.05f, 2.3f), 0f);
                Kit("L_Candle", root, new Vector3(HX - 1.0f, 1.2f, 1.5f));
                // Orgdaki nota sehpası: sahte (boş, yırtık kâğıt) — asıl notalar başka yerde
                Slab("Stand", organ, new Vector3(2.85f, 0.65f, 0.2f), new Vector3(0.2f, 1.3f, 0.2f), oldBone, 1f, false);
                Skull(organ, new Vector3(2.85f, 2.22f, 0.15f), 180f, 1.3f, bone, voidM);
                var ds = new GameObject("DecoySheet").transform; ds.SetParent(organ, false); ds.localPosition = new Vector3(2.85f, 1.75f, 0.12f);
                Slab("Frame", ds, new Vector3(0, 0, 0.04f), new Vector3(1.85f, 0.95f, 0.08f), oldBone, 1f, false);
                Slab("TornSheet", ds, new Vector3(-0.45f, 0.05f, -0.005f), new Vector3(0.75f, 0.6f, 0.01f), K.paperB, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, 7);
                Kit("Candlestick", organ, new Vector3(2.0f, 1.17f, 0.6f), 0f);
                Kit("Candlestick", organ, new Vector3(-2.0f, 1.17f, 0.6f), 0f);
            }
            // borular: üç kule (orta en yüksek), kemik rengi, ağızlık yarıkları, tepede kafatası
            for (int i = -13; i <= 13; i++)
            {
                float x = i * 0.25f, ai = Mathf.Abs(i);
                float h = ai <= 4 ? 7.2f - ai * 0.45f : ai <= 9 ? 5.6f - (ai - 5) * 0.35f + (i % 2 == 0 ? 0.3f : 0f) : 4.0f - (ai - 10) * 0.4f;
                float r = ai <= 4 ? 0.11f : 0.09f;
                Tube("Pipe", organ, new Vector3(x, 1.2f, 1.55f), new Vector3(x, 1.2f + h, 1.55f), r, ai % 3 == 0 ? oldBone : bone);
                Slab("PipeMouth", organ, new Vector3(x, 1.7f, 1.55f - r), new Vector3(r * 1.2f, 0.14f, 0.02f), voidM, 1f, false);
                if (ai == 0 || ai == 7) Skull(organ, new Vector3(x, 1.2f + h, 1.55f), 180f, ai == 0 ? 1.6f : 1.1f, bone, voidM);
            }
            for (int k = 0; k < 3; k++) Slab("PipeBand", organ, new Vector3(0, 2.6f + k * 1.6f, 1.42f), new Vector3(6.8f - k * 1.6f, 0.12f, 0.08f), oldBone, 1f, false);
            Slab("OrganCase", organ, new Vector3(0, 4.5f, 2.2f), new Vector3(7.4f, 9f, 0.6f), stone, 1.5f, false);
            // ön süsleme: büyük kafatası + kanat gibi açılan kemikler + kafatası dizisi
            Skull(organ, new Vector3(0, 1.55f, 0.55f), 180f, 2.6f, bone, voidM);
            for (int k = 0; k < 6; k++)
            {
                float a = (k < 3 ? 160f + k * 22f : 20f - (k - 3) * 22f) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f, 0);
                Rod("WingBone", organ, new Vector3(0, 1.85f, 0.5f) + d * 0.35f, new Vector3(0, 1.85f, 0.5f) + d * 1.1f, 0.06f, bone);
            }
            for (int k = 0; k < 10; k++) Skull(organ, new Vector3(-3.4f + k * 0.75f + (k >= 5 ? 0.0f : 0f), 1.17f, 1.7f), 180f, 0.9f, k % 2 == 0 ? bone : oldBone, voidM);
            foreach (int s in new[] { -1, 1 })
            {
                Tube("OrganTower", organ, new Vector3(s * 3.7f, 0, 1.2f), new Vector3(s * 3.7f, 6.5f, 1.2f), 0.35f, oldBone);
                for (int k = 0; k < 6; k++) Skull(organ, new Vector3(s * 3.7f, 1.2f + k * 0.9f, 0.82f), 180f, 1.0f, bone, voidM);
                Kit("CandelabraFloor", organ, new Vector3(s * 4.6f, 0, -0.4f), 0f);
                Brazier(organ, new Vector3(s * 2.2f, 0, -1.6f), 0.7f, false);
            }
            Kit("L_Candelabra", root, new Vector3(-3.6f, 2.0f, 11.6f));
            Kit("L_Candelabra", root, new Vector3(3.6f, 2.0f, 11.6f));
            Slab("OrganBench", root, new Vector3(0, 0.25f, 11.2f), new Vector3(2.4f, 0.12f, 0.45f), K.furniture, 1f, false);
            foreach (int s in new[] { -1, 1 }) Slab("BenchLeg", root, new Vector3(s * 1.0f, 0.1f, 11.2f), new Vector3(0.1f, 0.2f, 0.4f), K.furniture, 1f, false);

            // --- Avize (ortada) + zincirler ---
            Kit("ChandelierIron", root, new Vector3(0, Arc(1f).y - 0.4f, -2f), 0f, 1.5f);
            Kit("L_Chandelier", root, new Vector3(0, Arc(1f).y - 3.2f, -2f));
            foreach (var (x, z) in new[] { (-4.5f, -12f), (4.5f, -6f), (-4.0f, 6.5f), (5.0f, 9.6f), (-5.4f, 0.4f), (3.6f, -12.6f) })
            {
                float topY = Arc(Mathf.Clamp01(1f - Mathf.Abs(x) / HX)).y;
                Rod("HangChain", root, new Vector3(x, topY - 0.2f, z), new Vector3(x, topY - 3.5f - Hash01((int)(x * 10), (int)z) * 2f, z), 0.05f, K.iron);
            }

            // --- Giriş (güney) + asansör kafesi (güneydoğu) ---
            Kit("Door", root, new Vector3(0, 0, -HZ + 0.02f), 0f, 1.15f);
            ArchTrim(root, "DoorArch", new Vector3(0, 0, -HZ), Vector3.forward, 2.8f, 4.0f, false);
            foreach (int s in new[] { -1, 1 }) { Kit("WallSconce", root, new Vector3(s * 2.0f, 2.6f, -HZ + 0.03f), 0f); Kit("L_Candle", root, new Vector3(s * 2.0f, 2.7f, -HZ + 0.6f)); }
            arrive = new GameObject("CryptArrive").transform;
            arrive.SetParent(root, false); arrive.localPosition = new Vector3(0, 0.05f, -HZ + 1.6f);
            Cage(root, "LiftCageO", new Vector3(5.6f, 0, -13.0f), 90f, WH + VAULT - 4f);

            // --- Eşyalar: ilahi kürsüsü, sandıklar, küpler, kâğıtlar, ağlar ---
            Kit("Console", root, new Vector3(-5.0f, 0, 10.6f), 120f);
            Kit("BookStack", root, new Vector3(-5.0f, 0.85f, 10.6f), 10f);
            Kit("PaperA", root, new Vector3(-4.8f, 0.86f, 10.4f), 40f);
            Kit("Chest", root, new Vector3(-5.6f, 0, -12.8f), 70f);
            Kit("Amphora", root, new Vector3(-6.2f, 0, -9.0f), 0f, 1.1f);
            Kit("VaseCeramic", root, new Vector3(6.2f, 0, -3.0f), 0f);
            Kit("Vase", root, new Vector3(-6.2f, 0, 3.0f), 0f);
            Kit("Pot", root, new Vector3(6.3f, 0, 9.2f), 0f);
            Kit("PaperScatter", root, new Vector3(1.2f, 0.01f, 8.6f), 40f);
            Kit("PaperScatter", root, new Vector3(-2.4f, 0.01f, -9.0f), 160f);
            Kit("Lantern", root, new Vector3(1.4f, 0.02f, -13.6f), 0f);
            Kit("L_Lantern", root, new Vector3(-1.4f, 0.02f, -13.6f), 0f);
            Kit("Shield", root, new Vector3(-HX + 0.06f, 2.2f, -13.0f), 90f);
            Kit("Sword", root, new Vector3(-6.6f, 0.02f, -10.5f), 70f);
            foreach (var (x, z, s) in new[] { (-2.6f, 5.6f, 1.2f), (3.0f, -8.4f, 1.0f), (2.2f, 6.8f, 0.9f), (-3.4f, -5.2f, 1.1f) }) Skull(root, new Vector3(x, 0.02f, z), x * 50f, s, oldBone, voidM);
            foreach (var (x, z) in new[] { (-3.0f, 5.2f), (3.2f, -8.0f), (-3.8f, -5.6f) })
                for (int k = 0; k < 3; k++) { var a = (x * 10f + k * 70f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.28f; Rod("Bone", root, new Vector3(x, 0.05f, z) - d, new Vector3(x, 0.07f, z) + d, 0.05f, oldBone); }
            foreach (var (x, y, z, yaw) in new[] { (-HX + 0.3f, WH - 0.6f, -HZ + 0.3f, 45f), (HX - 0.3f, WH - 0.6f, -HZ + 0.3f, -45f), (-HX + 0.3f, WH - 0.6f, HZ - 0.3f, 135f), (HX - 0.3f, WH - 0.6f, HZ - 0.3f, -135f), (-HX + 0.5f, 2.6f, -1.5f, 90f), (HX - 0.5f, 2.4f, 4.5f, -90f) })
                KitR("Cobweb", root, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 180f), 1.8f);

            KitProbes(root, new Vector3(0, (WH + VAULT) * 0.5f, 0), new Vector3(HX * 2f - 0.6f, WH + VAULT - 1f, HZ * 2f - 0.6f), 5, 4, 9);
            KitReflection(root, new Vector3(0, WH * 0.5f, 0), new Vector3(HX * 2f, WH + VAULT, HZ * 2f));
            return root;
        }
    }
}
