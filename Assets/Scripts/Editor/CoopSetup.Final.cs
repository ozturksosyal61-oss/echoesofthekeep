using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Final (bulmaca 8) "Büyük Salon" (ref final.png), Layout.Final (-500,0,0): 26x56 m, 16 m duvar + sivri tonoz (26 m),
    /// iki yanda kemerli kör sıra + balkonlu galeri, dev şövalye heykelleri + mangallar, iki büyük avize, kuzeyde gül pencere ve mühürlü kapı.
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
            var wallM = KTex("K_FinalWall", "church_bricks_02", new Color(0.56f, 0.56f, 0.6f), 0.22f);
            var floorM = KTex("K_FinalFloor", "marble_tiles", new Color(0.28f, 0.28f, 0.32f), 0.8f);
            var carpet = KTex("K_FinalCarpet", "velour_velvet", new Color(0.5f, 0.07f, 0.07f), 0.25f);
            var stone = K.castleStone;
            var inlay = MakeMat("K_FinalInlay", new Color(0.62f, 0.5f, 0.3f)); inlay.SetFloat("_Smoothness", 0.7f); inlay.SetFloat("_Metallic", 0.6f);
            var voidM = MakeMat("K_WellVoid", new Color(0.01f, 0.012f, 0.02f));

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), HX * 2f + 1f, HZ * 2f + 1f, 0.5f, floorM, 4, 8);
            Wall(root, "WallW", new Vector3(-HX - 0.25f, 0, 0), false, HZ * 2f + 1f, WH, wallM, 3f);
            Wall(root, "WallE", new Vector3(HX + 0.25f, 0, 0), false, HZ * 2f + 1f, WH, wallM, 3f);
            Wall(root, "WallS", new Vector3(0, 0, -HZ - 0.25f), true, HX * 2f, WH + VAULT, wallM, 3f);
            Wall(root, "WallN", new Vector3(0, 0, HZ + 0.25f), true, HX * 2f, WH + VAULT, wallM, 3f);
            const int seg = 10;
            Vector2 Arc(float t) { float x = HX * (1f - t); return new Vector2(x, WH + VAULT * Mathf.Sqrt(Mathf.Clamp01(1f - (x / HX) * (x / HX))) * (0.7f + 0.3f * t)); }
            for (int k = 0; k < seg; k++)
            {
                var a = Arc(k / (float)seg); var b = Arc((k + 1) / (float)seg);
                foreach (int s in new[] { -1, 1 })
                {
                    var pa = new Vector3(s * a.x, a.y, 0); var pb = new Vector3(s * b.x, b.y, 0);
                    var v = Slab("Vault", root, (pa + pb) * 0.5f + Vector3.up * 0.25f, new Vector3((pb - pa).magnitude + 0.15f, 0.5f, HZ * 2f + 0.5f), wallM, 3f, false);
                    v.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
                    for (float z = -21f; z <= 21.1f; z += 7f) Rod("Rib", root, pa + new Vector3(0, -0.15f, z), pb + new Vector3(0, -0.15f, z), 0.45f, stone);
                }
            }
            Slab("RidgeRib", root, new Vector3(0, Arc(1f).y - 0.2f, 0), new Vector3(0.5f, 0.4f, HZ * 2f), stone, 1f, false);

            // --- Yanlar: kör kemer sırası, sütunlar, balkon galerisi, üst pencereler, sancaklar ---
            float[] bayZ = { -24.5f, -17.5f, -10.5f, -3.5f, 3.5f, 10.5f, 17.5f, 24.5f };
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float z in bayZ) ArchTrim(root, "Arcade", new Vector3(s * HX, 0, z), new Vector3(-s, 0, 0), 4.4f, 6.6f, true);
                for (float z = -21f; z <= 21.1f; z += 7f)
                {
                    Slab("Column", root, new Vector3(s * (HX - 0.5f), WH * 0.5f, z), new Vector3(1.1f, WH, 1.1f), stone, 1.5f);
                    Slab("ColumnBase", root, new Vector3(s * (HX - 0.55f), 0.4f, z), new Vector3(1.5f, 0.8f, 1.5f), stone, 1f, false);
                    Slab("ColumnCap", root, new Vector3(s * (HX - 0.55f), WH - 0.3f, z), new Vector3(1.4f, 0.6f, 1.4f), stone, 1f, false);
                    Rod("Bracket", root, new Vector3(s * (HX - 0.4f), BAL - 2.2f, z), new Vector3(s * (HX - 2.2f), BAL - 0.15f, z), 0.3f, stone);
                }
                Slab("Balcony", root, new Vector3(s * (HX - 1.2f), BAL, 0), new Vector3(2.4f, 0.3f, HZ * 2f), stone, 2f, false);
                Slab("BalconyRail", root, new Vector3(s * (HX - 2.3f), BAL + 1.1f, 0), new Vector3(0.22f, 0.14f, HZ * 2f), stone, 1f, false);
                Slab("BalconyBase", root, new Vector3(s * (HX - 2.3f), BAL + 0.2f, 0), new Vector3(0.26f, 0.12f, HZ * 2f), stone, 1f, false);
                for (float z = -HZ + 0.6f; z < HZ; z += 0.7f) Tube("Baluster", root, new Vector3(s * (HX - 2.3f), BAL + 0.25f, z), new Vector3(s * (HX - 2.3f), BAL + 1.05f, z), 0.06f, stone);
                foreach (float z in new[] { -17.5f, -3.5f, 10.5f, 24.5f, -24.5f, -10.5f, 3.5f, 17.5f })
                    Kit("WindowLancet", root, new Vector3(s * HX, BAL + 2.0f, z), s > 0 ? -90f : 90f, 1.6f);
                foreach (float z in new[] { -21f, -14f, -7f, 7f, 14f, 21f })
                    Kit("Banner", root, new Vector3(s * (HX - 2.42f), BAL + 0.9f, z), s > 0 ? -90f : 90f, 1.7f);
                foreach (float z in new[] { -24.5f, -10.5f, 10.5f, 24.5f })
                {
                    Kit("WallSconce", root, new Vector3(s * (HX - 1.1f), BAL + 2.4f, z + 1.6f), s > 0 ? -90f : 90f);
                    Kit("CandleCluster", root, new Vector3(s * (HX - 1.2f), BAL + 0.15f, z), 0f);
                }
            }

            // --- Dev şövalye heykelleri + mangallar (iki yanda 4'er) ---
            foreach (int s in new[] { -1, 1 })
                foreach (float z in new[] { -21f, -10.5f, 10.5f, 21f })
                {
                    float x = s * (HX - 3.0f);
                    Slab("ColossusPlinth", root, new Vector3(x, 0.75f, z), new Vector3(2.4f, 1.5f, 2.4f), stone, 1.2f);
                    Slab("ColossusPlinthCap", root, new Vector3(x, 1.55f, z), new Vector3(2.6f, 0.12f, 2.6f), stone, 1f, false);
                    Kit("StatueKing", root, new Vector3(x, 1.6f, z), s > 0 ? -90f : 90f, 2.6f);
                    Brazier(root, new Vector3(x - s * 2.2f, 0, z + (z > 0 ? -1.8f : 1.8f)), 1.4f, Mathf.Abs(z) < 15f);
                }

            // --- Zemin: kırmızı halı (altın kenar), işlemeli halkalar ---
            Slab("Carpet", root, new Vector3(0, 0.02f, -2.0f), new Vector3(3.2f, 0.04f, HZ * 2f - 8f), carpet, 2f, false);
            foreach (int s in new[] { -1, 1 }) Slab("CarpetBorder", root, new Vector3(s * 1.55f, 0.045f, -2.0f), new Vector3(0.12f, 0.01f, HZ * 2f - 8f), inlay, 1f, false);
            foreach (float zc in new[] { -14f, 14f })
            {
                foreach (float rr in new[] { 3.2f, 5.0f, 7.0f }) MeshObj("FloorRing", root, new Vector3(0, 0.006f, zc), RingMesh(rr, 0.12f, 0.012f), inlay);
                for (int k = 0; k < 12; k++)
                    Slab("FloorRay", root, Quaternion.Euler(0, k * 30f + 15f, 0) * Vector3.forward * 5.1f + new Vector3(0, 0.008f, zc), new Vector3(0.08f, 0.012f, 3.6f), inlay, 1f, false)
                        .transform.localRotation = Quaternion.Euler(0, k * 30f + 15f, 0);
            }

            // --- Avizeler + zincirler ---
            foreach (float z in new[] { -14f, 14f })
            {
                Kit("ChandelierIron", root, new Vector3(0, Arc(1f).y - 4f, z), 0f, 2.6f);
                Kit("L_Chandelier", root, new Vector3(0, Arc(1f).y - 9f, z));
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
                Kit("StatueAngel", root, new Vector3(s * 5.4f, 0, HZ - 1.2f), 180f, 1.4f);
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

            // --- Dekor: halılar dışında silahlar, kalkanlar, sandıklar, mumlar, büstler, vazolar ---
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float z in new[] { -24.5f, -17.5f, -3.5f, 3.5f, 17.5f, 24.5f })
                {
                    float x = s * (HX - 0.9f);
                    int k = Mathf.Abs((int)z) % 3;
                    if (k == 0) { Kit("Shield", root, new Vector3(s * (HX - 0.05f), 3.4f, z), s > 0 ? -90f : 90f, 1.4f); Kit("Sword", root, new Vector3(s * (HX - 0.08f), 2.2f, z - 0.6f), s > 0 ? -90f : 90f); Kit("Chest", root, new Vector3(x, 0, z), s > 0 ? -90f : 90f); }
                    else if (k == 1) { Kit("Bust", root, new Vector3(x, 0, z), s > 0 ? -90f : 90f, 1.3f); Kit("CandleCluster", root, new Vector3(x + s * -0.6f, 0, z + 1.0f), 0f); }
                    else { Kit("Amphora", root, new Vector3(x, 0, z - 0.6f), 0f, 1.2f); Kit("VaseCeramic", root, new Vector3(x, 0, z + 0.6f), 0f, 1.2f); Kit("PaintingPortrait", root, new Vector3(s * (HX - 0.05f), 3.6f, z), s > 0 ? -90f : 90f, 1.4f); }
                }
                foreach (float z in new[] { -26f, -7f, 7f, 26f }) Kit("CandelabraFloor", root, new Vector3(s * (HX - 1.4f), 0, z), 0f);
                Kit("Mace", root, new Vector3(s * (HX - 0.1f), 2.4f, -21f + 1.4f), s > 0 ? -90f : 90f);
                Kit("WarHammer", root, new Vector3(s * (HX - 0.1f), 2.4f, 21f - 1.4f), s > 0 ? -90f : 90f);
                Kit("Axe", root, new Vector3(s * (HX - 0.1f), 2.4f, -1.2f), s > 0 ? -90f : 90f);
                Kit("Rug", root, new Vector3(s * 7.5f, 0.03f, s * -16f), 90f, 1.4f);
            }
            foreach (var (x, z) in new[] { (-2.4f, -26f), (2.4f, -26f), (-2.6f, -1.2f), (2.6f, 1.2f), (-2.4f, 22f), (2.4f, 22f) }) Kit("CandleCluster", root, new Vector3(x, 0, z), x * 30f);
            foreach (var (x, z, yaw) in new[] { (-HX + 0.4f, 8.4f, 90f), (HX - 0.4f, -8.4f, -90f) })
                KitR("Cobweb", root, new Vector3(x, 6.2f, z), Quaternion.Euler(0, yaw, 180f), 2.2f);

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

            KitProbes(root, new Vector3(0, 7f, 0), new Vector3(HX * 2f - 1f, 13f, HZ * 2f - 1f), 7, 4, 13);
            KitReflection(root, new Vector3(0, 7f, -14f), new Vector3(HX * 2f, 16f, HZ));
            KitReflection(root, new Vector3(0, 7f, 14f), new Vector3(HX * 2f, 16f, HZ));
        }
    }
}
