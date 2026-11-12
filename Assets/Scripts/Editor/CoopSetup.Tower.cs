using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// X'in ilk odası: çan kulesi mekanizma odası v2 (ref: Docs/Reference/mekanizma.jpg). 18x20, 9.4 m; çatlak sıvalı tuğla + lambri,
    /// kirişli tavanın ortasında çan şaftı (ay ışığı), kuzeyde iki katlı dişli duvarı + bakım iskelesi (y 4.4), ortada serbest L merdiven + köprü,
    /// zincirli saat ağırlıkları, doğuda saatçinin tezgahı. Kollar (BuildPuzzle1) odaya dağınık: iskelede, sahanlık altında, güney köşelerde.
    /// </summary>
    public static partial class CoopSetup
    {
        /// <summary>ExcludeFromBake'in dokunmayacağı (kit + baked ışıklı) nesneler.</summary>
        static readonly HashSet<GameObject> bakeKeep = new HashSet<GameObject>();

        static void KeepBaked(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) bakeKeep.Add(t.gameObject);
        }

        // Kule v2 iç sınırları (kule yerel): batı duvarı saat odası merdiveni yüzünden sabit (x -8), oda doğuya/kuzey-güneye büyür.
        const float TwXW = -8f, TwXE = 10f, TwZ = 10f, TwH = 9.4f, TwCat = 4.4f;

        static Transform BuildTower(Vector3 center)
        {
            bakeKeep.Clear();
            var root = new GameObject("RoomA_Mekanizma").transform;
            root.position = center;
            const float XW = TwXW, XE = TwXE, HZ = TwZ, h = TwH, CX = (TwXW + TwXE) * 0.5f, cat = TwCat;
            float W = XE - XW;
            var wallM = KTex("K_TowerWall", "rough_plaster_brick", new Color(0.78f, 0.74f, 0.7f), 0.2f, 1.3f);
            var floorM = KTex("K_TowerFloor", "weathered_planks", new Color(0.72f, 0.64f, 0.58f), 0.25f, 1.1f);
            var chalk = MakeMat("K_Chalk", new Color(0.78f, 0.76f, 0.7f));
            var feather = MakeMat("K_Feather", new Color(0.7f, 0.68f, 0.64f));
            var water = MakeMat("K_Puddle", new Color(0.2f, 0.19f, 0.17f)); water.SetFloat("_Smoothness", 0.95f);
            var det = new GameObject("Details").transform; det.SetParent(root, false);
            var gearIron = MakeMat("K_GearIron", new Color(0.2f, 0.19f, 0.18f)); gearIron.SetFloat("_Metallic", 0.7f); gearIron.SetFloat("_Smoothness", 0.45f);

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(CX, -0.25f, 0), W + 1f, HZ * 2 + 1f, 0.5f, floorM, 3, 3);
            Wall(root, "WallN", new Vector3(CX, 0, HZ + 0.25f), true, W + 1f, h, wallM, 2.5f);
            Wall(root, "WallS", new Vector3(CX, 0, -HZ - 0.25f), true, W + 1f, h, wallM, 2.5f);
            Wall(root, "WallE", new Vector3(XE + 0.25f, 0, 0), false, HZ * 2 + 1f, h, wallM, 2.5f);
            var stairDoor = new Opening(0f, 3f, 0f, 3f); // Saat Odası merdiveni (PuzzleDoor)
            Wall(root, "WallW", new Vector3(XW - 0.25f, 0, 0), false, HZ * 2 + 1f, h, wallM, 2.5f, stairDoor);
            Wainscot(root, "WainS", new Vector3(CX, 0, -HZ), true, W, Vector3.forward, 2.2f);
            Wainscot(root, "WainE", new Vector3(XE, 0, 0), false, HZ * 2, Vector3.left, 2.2f);
            Wainscot(root, "WainW", new Vector3(XW, 0, 0), false, HZ * 2, Vector3.right, 2.2f, stairDoor);
            foreach (int s in new[] { -1, 1 })
                Slab("DoorJamb", root, new Vector3(XW + 0.08f, 1.55f, s * 1.6f), new Vector3(0.16f, 3.1f, 0.22f), K.beam, 1.2f, false);
            Slab("DoorLintel", root, new Vector3(XW + 0.08f, 3.12f, 0), new Vector3(0.16f, 0.24f, 3.42f), K.beam, 1.2f, false);
            // Tavan: ortada çan şaftı açıklığı (3.2 x 3.2), çevresi kirişli tahta
            const float SH = 3.2f, bellZ = 1.5f;
            float sx0 = CX - SH * 0.5f, sx1 = CX + SH * 0.5f, sz0 = bellZ - SH * 0.5f, sz1 = bellZ + SH * 0.5f;
            foreach (var (c, ssx, ssz) in new[] {
                (new Vector3((XW + sx0) * 0.5f, h, 0), sx0 - XW, HZ * 2f), (new Vector3((sx1 + XE) * 0.5f, h, 0), XE - sx1, HZ * 2f),
                (new Vector3(CX, h, (sz1 + HZ) * 0.5f), SH, HZ - sz1), (new Vector3(CX, h, (-HZ + sz0) * 0.5f), SH, sz0 + HZ) })
                Deck(root, "CeilPlanks", c + Vector3.up * 0.08f, ssx, ssz, 0.16f, K.beam, 2, 2);
            for (float x = XW + 0.75f; x < XE; x += 1.5f)
                if (x < sx0 - 0.1f || x > sx1 + 0.1f) Slab("Joist", root, new Vector3(x, h - 0.1f, 0), new Vector3(0.16f, 0.2f, HZ * 2f), K.beam, 1.2f, false);
            foreach (float z in new[] { sz0 - 0.15f, sz1 + 0.15f, -6f, 6.5f })
                Slab("MainBeam", root, new Vector3(CX, h - 0.36f, z), new Vector3(W, 0.32f, 0.3f), K.beam, 1.2f, false);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(CX, h + 4.8f, 0), new Vector3(W + 2f, 0.6f, HZ * 2 + 2f), K.beam, 2f, false), 0);
            // Çan şaftı: dört duvar + panjurlu ay açıklıkları
            Wall(root, "ShaftN", new Vector3(CX, h, sz1 + 0.25f), true, SH + 1f, 4.4f, wallM, 2f);
            Wall(root, "ShaftS", new Vector3(CX, h, sz0 - 0.25f), true, SH + 1f, 4.4f, wallM, 2f);
            Wall(root, "ShaftE", new Vector3(sx1 + 0.25f, h, bellZ), false, SH, 4.4f, wallM, 2f);
            Wall(root, "ShaftW", new Vector3(sx0 - 0.25f, h, bellZ), false, SH, 4.4f, wallM, 2f);
            Slab("ShaftSky", root, new Vector3(CX, h + 4.35f, bellZ), new Vector3(SH, 0.05f, SH), GlowMat("K_TowerSky", new Color(0.1f, 0.13f, 0.2f), new Color(0.22f, 0.3f, 0.5f)), 1f, false);
            for (int k = 0; k < 6; k++) // panjur çıtaları (şaft tepesi)
                Slab("Louver", root, new Vector3(CX, h + 3.0f + k * 0.18f, sz1 - 0.02f), new Vector3(SH, 0.05f, 0.18f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(35, 0, 0);
            var moon = LightGo("ShaftMoon", LightType.Spot, Pal.Moon, 16f, 15f, true);
            moon.transform.SetParent(root, false); moon.transform.localPosition = new Vector3(CX, h + 3.6f, bellZ);
            moon.transform.localRotation = Quaternion.LookRotation(new Vector3(0.05f, -1f, -0.1f));
            moon.GetComponent<Light>().spotAngle = 55f; moon.GetComponent<Light>().innerSpotAngle = 30f;
            QuadObj("ShaftBeam", root, new Vector3(CX, h * 0.5f + 1.5f, bellZ - 0.3f), new Vector2(2.2f, h + 1f), K.shaft, Quaternion.LookRotation(Vector3.right, Vector3.up));
            // Köşe dikmeleri + payandalar
            foreach (float px in new[] { XW + 0.2f, XE - 0.2f })
                foreach (float pz in new[] { -HZ + 0.2f, HZ - 0.2f })
                {
                    Slab("Post", root, new Vector3(px, h * 0.5f, pz), new Vector3(0.36f, h, 0.36f), K.beam, 1.2f, false);
                    Rod("Brace", root, new Vector3(px, h - 1.8f, pz), new Vector3(px + (px < CX ? 1.6f : -1.6f), h - 0.4f, pz), 0.2f, K.beam);
                }

            // --- Çan: şaftın dibinde boyunduruktan asılı; ip kesilmiş (Detay 2) ---
            {
                const float bellH = 1.9f; float top = h + 0.9f;
                Slab("BellBeam", root, new Vector3(CX, top + 0.3f, bellZ), new Vector3(SH + 0.6f, 0.44f, 0.44f), K.beam, 1.2f, false);
                Kit("Bell", root, new Vector3(CX, top - bellH, bellZ), 0f, bellH / 1.1f);
                var rig = new GameObject("BellRig").transform; rig.SetParent(root, false);
                Slab("Yoke", rig, new Vector3(CX, top + 0.05f, bellZ), new Vector3(1.4f, 0.28f, 0.32f), K.furniture, 1.2f, false);
                foreach (int s in new[] { -1, 1 }) Slab("Strap", rig, new Vector3(CX + s * 0.55f, top + 0.2f, bellZ), new Vector3(0.07f, 0.62f, 0.5f), K.iron, 1f, false);
                Rod("Rope", rig, new Vector3(CX, top - bellH + 0.3f, bellZ), new Vector3(CX, 2.6f, bellZ), 0.035f, K.furniture);
                for (int k = 0; k < 4; k++) Rod("Fray", rig, new Vector3(CX, 2.62f, bellZ), new Vector3(CX + (k - 1.5f) * 0.03f, 2.48f, bellZ + (k % 2) * 0.03f), 0.012f, K.furniture);
                for (int i = 0; i < 4; i++) MeshObj("CutRope", rig, new Vector3(CX + 1.2f, 0.03f + i * 0.035f, bellZ - 1.4f), RingMesh(0.26f - i * 0.02f, 0.035f, 0.035f), K.furniture);
                Kit("Dagger", root, new Vector3(CX + 1.7f, 0.01f, bellZ - 1.0f), 50f);
                ProbeLit(rig.gameObject);
            }

            // --- Kuzey: iki katlı dişli duvarı + bakım iskelesi (y 4.4) ---
            {
                var mech = new GameObject("Mechanism").transform; mech.SetParent(root, false);
                var anchor = new GameObject("ProbeAnchor").transform; anchor.SetParent(mech, false); anchor.localPosition = new Vector3(CX, 3f, HZ - 1.8f);
                Slab("Back", mech, new Vector3(CX, h * 0.5f, HZ - 0.015f), new Vector3(W - 0.6f, h - 0.4f, 0.03f), K.panel, 2f, false);
                for (float x = XW + 1f; x <= XE - 0.9f; x += 3f)
                    Slab("FramePost", mech, new Vector3(x, h * 0.5f, HZ - 0.24f), new Vector3(0.3f, h, 0.24f), K.beam, 1.2f, false);
                foreach (float y in new[] { 2.2f, cat - 0.15f, 6.8f, h - 0.8f })
                    Slab("FrameRail", mech, new Vector3(CX, y, HZ - 0.2f), new Vector3(W - 0.8f, 0.24f, 0.16f), K.beam, 1.2f, false);
                var pile = new GameObject("GearPile").transform; pile.SetParent(mech, false);
                var gs = new[]
                {   // alt kat (y 0.6..4.0)
                    new Vector4(-5.6f, 2.6f, 1.3f, 20), new Vector4(-3.6f, 3.3f, 0.8f, 14), new Vector4(-6.6f, 1.2f, 0.55f, 10), new Vector4(-2.2f, 1.6f, 0.7f, 12),
                    new Vector4(4.6f, 2.5f, 1.4f, 22), new Vector4(6.8f, 3.2f, 0.8f, 14), new Vector4(8.6f, 1.6f, 0.75f, 12), new Vector4(2.9f, 1.3f, 0.55f, 10),
                    // üst kat (y 5.0..8.6)
                    new Vector4(-6.0f, 6.6f, 1.2f, 20), new Vector4(-3.4f, 7.6f, 0.9f, 14), new Vector4(-1.4f, 5.8f, 0.6f, 10), new Vector4(1.0f, 7.0f, 1.5f, 24),
                    new Vector4(3.6f, 5.6f, 0.7f, 12), new Vector4(5.2f, 7.4f, 1.1f, 18), new Vector4(7.6f, 6.0f, 0.8f, 14), new Vector4(8.8f, 8.0f, 0.55f, 10),
                };
                for (int i = 0; i < gs.Length; i++)
                    MeshObj("Gear" + i, pile, new Vector3(gs[i].x, gs[i].y, HZ - (i % 2 == 0 ? 0.09f : 0.2f)), GearMesh((int)gs[i].w, gs[i].z, 0.14f, gs[i].z * 0.2f, 0.12f), i % 4 == 0 ? K.bronze : gearIron)
                        .transform.localRotation = Quaternion.Euler(0, 0, i * 37f);
                foreach (float y in new[] { 3.85f, 8.4f })
                {
                    Rod("Shaft", pile, new Vector3(XW + 1f, y, HZ - 0.44f), new Vector3(XE - 1f, y, HZ - 0.44f), 0.07f, K.iron);
                    foreach (float x in new[] { -5f, -1.5f, 2.4f, 6.2f })
                        Prim("Pulley", pile, PrimitiveType.Cylinder, new Vector3(x, y, HZ - 0.44f), new Vector3(0.5f, 0.04f, 0.5f), K.iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
                for (int k = 0; k < 3; k++) // kayış: üst mil → alt mil
                    Rod("Belt", pile, new Vector3(-1.5f + k * 3.9f, 8.4f, HZ - 0.46f), new Vector3(-1.5f + k * 3.9f, 3.85f, HZ - 0.46f), 0.05f, K.leather);
                ProbeLit(pile.gameObject);
                var spins = new List<Spin>();
                void Front(string n, int teeth, float r, Vector2 p, float speed)
                {
                    var g = MeshObj(n, mech, new Vector3(p.x, p.y, HZ - 0.38f), GearMesh(teeth, r, 0.16f, r * 0.22f, 0.1f), K.brass);
                    Prim("Axle", g.transform, PrimitiveType.Cylinder, new Vector3(0, 0, 0.12f), new Vector3(r * 0.3f, 0.2f, r * 0.3f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    AddSpin(g, speed);
                    var sp = g.GetComponent<Spin>(); sp.enabled = false; spins.Add(sp);
                }
                Front("GearFrontA", 18, 1.2f, new Vector2(0.4f, 2.4f), 10f);
                Front("GearFrontB", 10, 0.65f, new Vector2(2.0f, 3.2f), -16f);
                Front("GearFrontC", 16, 1.0f, new Vector2(-0.6f, 6.4f), -12f);
                Front("GearFrontD", 8, 0.5f, new Vector2(0.9f, 7.5f), 20f);
                var ps = mech.gameObject.AddComponent<PuzzleSpin>();
                var pso = new SerializedObject(ps);
                pso.FindProperty("puzzleId").intValue = 1;
                SetRefArray(pso, "targets", spins.ToArray());
                pso.ApplyModifiedPropertiesWithoutUndo();
                foreach (var r in mech.GetComponentsInChildren<Renderer>())
                    if (r.transform.parent == pile || r.GetComponentInParent<Spin>() != null) r.probeAnchor = anchor;
                // İskele: tahta döşeme + demir korkuluk (köprü ağzı açık) + görünmez engel
                Deck(root, "Catwalk", new Vector3(CX, cat - 0.08f, HZ - 0.95f), W - 0.4f, 1.9f, 0.16f, K.beam, 3, 1);
                for (float x = XW + 0.6f; x < XE - 0.4f; x += 1.5f)
                    Rod("CatBracket", root, new Vector3(x, cat - 1.2f, HZ - 0.05f), new Vector3(x, cat - 0.16f, HZ - 1.5f), 0.12f, K.beam);
                var rail = new GameObject("CatRail").transform; rail.SetParent(root, false);
                float gap0 = 2.4f, gap1 = 3.6f, rz = HZ - 1.9f;
                foreach (var (a, b) in new[] { (XW + 0.3f, gap0), (gap1, XE - 0.3f) })
                {
                    for (float x = a; x <= b + 0.01f; x += 0.3f) Rod("Bar", rail, new Vector3(x, cat, rz), new Vector3(x, cat + 1f, rz), 0.025f, K.iron);
                    Rod("Top", rail, new Vector3(a, cat + 1.02f, rz), new Vector3(b, cat + 1.02f, rz), 0.06f, K.iron);
                    var c = new GameObject("RailBlock").AddComponent<BoxCollider>(); c.transform.SetParent(root, false);
                    c.transform.localPosition = new Vector3((a + b) * 0.5f, cat + 0.65f, rz); c.size = new Vector3(b - a, 1.3f, 0.25f);
                }
                CombineChildren(rail, "tower_catrail", K.iron);
                ProbeLit(rail.gameObject, false);
            }

            // --- Ortada serbest L merdiven: doğuda +z kolu → sahanlık → batıya kol → köprü ile iskeleye ---
            {
                const float sw = 1.3f, mid = 2.2f;
                WoodStair(root, "StairA", new Vector3(7.4f, 0, -1.6f), mid, 3.6f, 1f, sw);           // x 7.4, z -1.6 → 2.0
                Deck(root, "Landing", new Vector3(7.4f, mid - 0.08f, 2.7f), 1.6f, 1.4f, 0.16f, K.beam, 1, 1);
                void Block(Vector3 c, Vector3 sz) { var b = new GameObject("RailBlock").AddComponent<BoxCollider>(); b.transform.SetParent(root, false); b.transform.localPosition = c; b.size = sz; }
                Rod("LandingRailE", root, new Vector3(8.2f, mid + 0.95f, 2.0f), new Vector3(8.2f, mid + 0.95f, 3.4f), 0.05f, K.beam);
                Rod("LandingRailN", root, new Vector3(6.6f, mid + 0.95f, 3.4f), new Vector3(8.2f, mid + 0.95f, 3.4f), 0.05f, K.beam);
                foreach (var p in new[] { new Vector3(8.2f, 0, 2.7f), new Vector3(7.4f, 0, 3.4f), new Vector3(8.2f, 0, 3.4f) })
                    Rod("LandingBaluster", root, p + Vector3.up * mid, p + Vector3.up * (mid + 0.95f), 0.05f, K.beam);
                Block(new Vector3(8.25f, mid + 0.6f, 2.7f), new Vector3(0.15f, 1.2f, 1.5f));
                Block(new Vector3(7.4f, mid + 0.6f, 3.45f), new Vector3(1.7f, 1.2f, 0.15f));
                foreach (float x in new[] { 6.7f, 8.1f }) Slab("LandingPost", root, new Vector3(x, (mid - 0.16f) * 0.5f, 3.3f), new Vector3(0.14f, mid - 0.16f, 0.14f), K.beam, 1f);
                var pivot = new GameObject("StairBPivot").transform; pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(6.6f, mid, 2.75f); pivot.localRotation = Quaternion.Euler(0, -90, 0); // yerel +z = dünya -x
                WoodStair(pivot, "StairB", Vector3.zero, cat - mid, 2.95f, 1f, sw);                 // x 6.6 → 3.65 (köprü kenarı)
                Deck(root, "Bridge", new Vector3(3.0f, cat - 0.08f, (2.75f + rzBridge()) * 0.5f), 1.3f, rzBridge() - 2.05f, 0.16f, K.beam, 1, 1);
                foreach (float x in new[] { 2.36f, 3.64f })
                {
                    float z0 = x > 3f ? 3.5f : 2.1f; // doğu korkuluğu merdiven ağzında başlamaz
                    Rod("BridgeRail", root, new Vector3(x, cat + 1f, z0), new Vector3(x, cat + 1f, rzBridge()), 0.05f, K.iron);
                    for (float z = z0; z < rzBridge(); z += 0.6f) Rod("BridgeBar", root, new Vector3(x, cat, z), new Vector3(x, cat + 1f, z), 0.025f, K.iron);
                    var c = new GameObject("RailBlock").AddComponent<BoxCollider>(); c.transform.SetParent(root, false);
                    c.transform.localPosition = new Vector3(x, cat + 0.65f, (z0 + rzBridge()) * 0.5f); c.size = new Vector3(0.22f, 1.3f, rzBridge() - z0);
                }
                float BridgeBack() => 2.05f;
                var back = new GameObject("RailBlock").AddComponent<BoxCollider>(); back.transform.SetParent(root, false); // köprünün güney ucu (merdiven ağzı dışı)
                back.transform.localPosition = new Vector3(3.0f, cat + 0.65f, BridgeBack()); back.size = new Vector3(1.4f, 1.3f, 0.2f);
                Rod("BridgeRailS", root, new Vector3(2.36f, cat + 1f, BridgeBack()), new Vector3(3.64f, cat + 1f, BridgeBack()), 0.05f, K.iron);
            }

            // --- Batı: merdiven kapısı, sivri vitraylar (yüksek), aplikler, harita panosu ---
            foreach (int s in new[] { -1, 1 })
            {
                KitR("WindowLancet", root, new Vector3(XW, 4.8f, s * 5.8f), Quaternion.Euler(0, 90, 0), 1.5f);
                KitR("WallSconce", root, new Vector3(XW + 0.05f, 2.6f, s * 2.4f), Quaternion.Euler(0, 90, 0));
                Kit("L_Candle", root, new Vector3(XW + 0.7f, 2.7f, s * 2.4f));
            }
            KitR("PaintingLandscape", root, new Vector3(XW + 0.28f, 0.45f, 7.4f), Quaternion.Euler(-14, 90, 0));
            Kit("Chest", root, new Vector3(XW + 0.8f, 0, 4.4f), 90f);
            Kit("BookStack", root, new Vector3(XW + 1.6f, 0, 3.6f), 40f);
            Kit("Crate", root, new Vector3(XW + 0.7f, 0, -4.4f), 12f);
            Kit("Bucket", root, new Vector3(XW + 0.6f, 0, -3.4f));
            Kit("Barrel", root, new Vector3(XW + 0.8f, 0, -5.8f), 70f);
            Kit("BarrelLying", root, new Vector3(XW + 2.0f, 0, -6.6f), 10f);

            // --- Doğu: saatçinin tezgahı (Detay 1: yarım kalmış onarım), alet panosu, dolaplar ---
            {
                var wb = new Vector3(XE - 0.7f, 0, -4.5f);
                Kit("Workbench", root, wb, -90f);
                Kit("L_Lantern", root, wb + new Vector3(-0.1f, 0.82f, 0.95f));
                MeshObj("OpenGear", det, wb + new Vector3(-0.2f, 0.87f, -0.2f), GearMesh(14, 0.32f, 0.08f, 0.07f, 0.04f), K.brass).transform.localRotation = Quaternion.Euler(90, 20, 0);
                for (int k = 0; k < 5; k++) Prim("Pinion", det, PrimitiveType.Cylinder, wb + new Vector3(-0.15f + k * 0.07f, 0.85f, 0.25f), new Vector3(0.05f, 0.01f, 0.05f), K.brass);
                Rod("Caliper", det, wb + new Vector3(-0.3f, 0.86f, 0.5f), wb + new Vector3(-0.05f, 0.86f, 0.62f), 0.012f, K.iron);
                Kit("Spectacles", root, wb + new Vector3(-0.25f, 0.84f, 0.05f), 120f);
                Kit("Goblet2", root, wb + new Vector3(-0.3f, 0.82f, -0.7f));
                KCandle(root, wb + new Vector3(-0.35f, 0.82f, -0.45f), 0.05f, 0.02f);
                Kit("Stool", root, wb + new Vector3(-0.9f, 0, 0.1f), 20f);
                Slab("ToolBoard", root, new Vector3(XE - 0.07f, 1.5f, wb.z), new Vector3(0.03f, 0.8f, 2.0f), K.furniture, 1.2f, false);
                var onWall = Quaternion.Euler(0, 0, 90);
                KitR("Wrench", root, new Vector3(XE - 0.1f, 1.55f, wb.z - 0.55f), onWall * Quaternion.Euler(0, 15, 0));
                KitR("Hammer", root, new Vector3(XE - 0.1f, 1.52f, wb.z), onWall * Quaternion.Euler(0, -10, 0));
                KitR("Mallet", root, new Vector3(XE - 0.1f, 1.52f, wb.z + 0.55f), onWall * Quaternion.Euler(0, 8, 0));
                KitR("PaperA", root, new Vector3(XE - 0.07f, 1.6f, wb.z + 1.4f), onWall * Quaternion.Euler(0, 6, 0));
                KitR("PaperC", root, new Vector3(XE - 0.07f, 1.45f, wb.z + 1.75f), onWall * Quaternion.Euler(0, -9, 0));
                KitR("WallSconce", root, new Vector3(XE - 0.05f, 2.6f, wb.z - 1.5f), Quaternion.Euler(0, -90, 0));
                Kit("L_Candle", root, new Vector3(XE - 0.7f, 2.7f, wb.z - 1.5f));
            }
            Kit("Cabinet", root, new Vector3(XE - 0.45f, 0, 0.6f), -90f);
            Books(root, new Vector3(XE - 0.4f, 0, -1.4f), -90f);
            Books(root, new Vector3(XE - 0.4f, 0, 6.4f), -90f);
            KitR("PaintingKeep", root, new Vector3(XE - 0.04f, 4.0f, 0.6f), Quaternion.Euler(0, -90, 0), 1.6f);
            Kit("Ladder", root, new Vector3(XE - 0.45f, 0, 4.5f), 90f);
            Kit("CrateStack", root, new Vector3(XE - 0.9f, 0, 8.6f), -100f);
            {
                var spare = new GameObject("SpareGears").transform; spare.SetParent(root, false);
                MeshObj("Lean", spare, new Vector3(XE - 0.24f, 0.69f, 2.4f), GearMesh(12, 0.7f, 0.14f, 0.14f, 0.1f), K.bronze).transform.localRotation = Quaternion.Euler(0, 0, -12) * Quaternion.Euler(0, 90, 0);
                MeshObj("FlatA", spare, new Vector3(6.0f, 0.05f, -2.4f), GearMesh(10, 0.45f, 0.12f, 0.1f, 0.1f), K.bronze).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("FlatB", spare, new Vector3(-2.4f, 0.04f, 6.2f), GearMesh(8, 0.3f, 0.1f, 0.07f, 0.08f), K.brass).transform.localRotation = Quaternion.Euler(90, 25, 0);
                MeshObj("FlatC", spare, new Vector3(-4.6f, 0.05f, -1.2f), GearMesh(16, 0.6f, 0.12f, 0.12f, 0.1f), K.iron).transform.localRotation = Quaternion.Euler(90, 50, 0);
                ProbeLit(spare.gameObject);
            }

            // --- Kahraman 2: zincirli saat ağırlıkları (iskeleden yere iner, zemin kuyusuna) ---
            foreach (var (x, y0) in new[] { (-3.2f, 0.9f), (-1.6f, 2.1f), (-4.8f, 1.5f) })
            {
                Rod("WeightChain", det, new Vector3(x, h - 0.4f, HZ - 2.6f), new Vector3(x, y0 + 0.7f, HZ - 2.6f), 0.035f, K.iron);
                Prim("Weight", det, PrimitiveType.Cylinder, new Vector3(x, y0 + 0.35f, HZ - 2.6f), new Vector3(0.36f, 0.35f, 0.36f), K.iron);
                MeshObj("WeightRing", det, new Vector3(x, y0 + 0.72f, HZ - 2.6f), RingMesh(0.1f, 0.025f, 0.025f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Slab("WeightPitRim", det, new Vector3(-3.2f, 0.02f, HZ - 2.6f), new Vector3(4.2f, 0.04f, 1.0f), K.iron, 1f, false);

            // --- Güney: giriş kapısı (holden), yazı masası, sarkaçlı saat, komodin ---
            {
                var door = Kit("Door", root, new Vector3(1f, 0, -HZ + 0.12f), 0f, 1.1f);
                var da = new GameObject("DoorProbeAnchor").transform; da.SetParent(root, false); da.localPosition = new Vector3(1f, 1.4f, -HZ + 0.9f);
                foreach (var r in door.GetComponentsInChildren<Renderer>()) r.probeAnchor = da;
                foreach (int s in new[] { -1, 1 })
                {
                    Kit("WallSconce", root, new Vector3(1f + s * 1.7f, 2.6f, -HZ + 0.05f));
                    Kit("L_Candle", root, new Vector3(1f + s * 1.7f, 2.7f, -HZ + 0.7f));
                }
            }
            Kit("StudyTable", root, new Vector3(-3.6f, 0, -HZ + 0.9f), 0f);
            Kit("Stool", root, new Vector3(-3.4f, 0, -HZ + 1.9f), -15f);
            Kit("L_Candelabra", root, new Vector3(-3.2f, 1.8f, -HZ + 1.3f));
            Kit("GrandfatherClock", root, new Vector3(5.2f, 0, -HZ + 0.4f), 0f);
            Kit("Commode", root, new Vector3(7.2f, 0, -HZ + 0.45f), 0f);
            Kit("Armillary", root, new Vector3(7.2f, 0.95f, -HZ + 0.45f), 30f);
            Kit("PaperScatter", root, new Vector3(-1.2f, 0, -6.8f), 20f);
            Kit("PaperScatter", root, new Vector3(3.6f, 0, 4.0f), 160f);

            // --- Detay 3: iskelede güvercin yuvası + tüyler; Detay 4: dikmeye kazınmış çan saatleri; Detay 5: şafttan damlayan su ---
            {
                var np = new Vector3(8.6f, cat, HZ - 0.7f);
                for (int k = 0; k < 10; k++)
                {
                    float a = k * 36f * Mathf.Deg2Rad;
                    Rod("Twig", det, np + new Vector3(Mathf.Cos(a) * 0.25f, 0.04f, Mathf.Sin(a) * 0.25f), np + new Vector3(Mathf.Cos(a + 1.4f) * 0.25f, 0.07f, Mathf.Sin(a + 1.4f) * 0.25f), 0.02f, K.furniture);
                }
                Prim("Egg", det, PrimitiveType.Sphere, np + new Vector3(0.03f, 0.06f, 0), new Vector3(0.05f, 0.04f, 0.04f), feather);
                for (int k = 0; k < 7; k++)
                    QuadObj("Feather", det, new Vector3(5.5f + k * 0.5f, 0.012f + (k % 2) * cat, HZ - 1.2f - (k % 3) * 0.3f), new Vector2(0.06f, 0.14f), feather, Quaternion.Euler(90, k * 47f, 0));
                for (int l = 0; l < 12; l++) // dikmede kazıntı sayım çizgileri
                    Slab("Notch", det, new Vector3(XE - 0.38f + 0.18f, 1.2f + (l / 6) * 0.3f, -HZ + 0.2f + 0.19f), new Vector3(0.005f, 0.16f, 0.012f), chalk, 1f, false)
                        .transform.localPosition += new Vector3(0, 0, -(l % 6) * 0.03f);
                Kit("Bucket2", root, new Vector3(CX + 0.6f, 0, bellZ + 0.9f), 0f);
                Prim("Puddle", det, PrimitiveType.Cylinder, new Vector3(CX + 0.35f, 0.004f, bellZ + 0.7f), new Vector3(0.8f, 0.002f, 0.55f), water);
                QuadObj("Drip", det, new Vector3(CX + 0.6f, h * 0.5f, bellZ + 0.9f), new Vector2(0.02f, h - 1f), K.shaft, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            // --- Orta: halat çıkrığı (çanı kurmak için; ip şafta çıkar) + plan masası ---
            {
                var cp = new Vector3(-2.6f, 0, 1.2f);
                Prim("Drum", det, PrimitiveType.Cylinder, cp + Vector3.up * 0.65f, new Vector3(1.1f, 0.55f, 1.1f), K.beam);
                Prim("DrumCap", det, PrimitiveType.Cylinder, cp + Vector3.up * 1.22f, new Vector3(1.3f, 0.04f, 1.3f), K.iron);
                Prim("DrumBase", det, PrimitiveType.Cylinder, cp + Vector3.up * 0.06f, new Vector3(1.5f, 0.06f, 1.5f), K.iron);
                for (int k = 0; k < 4; k++)
                {
                    var d = Quaternion.Euler(0, k * 90f + 20f, 0) * Vector3.forward;
                    Rod("PushBar", det, cp + Vector3.up * 1.05f + d * 0.5f, cp + Vector3.up * 1.05f + d * 1.6f, 0.08f, K.beam);
                }
                for (int k = 0; k < 6; k++) MeshObj("Wound", det, cp + Vector3.up * (0.25f + k * 0.14f), RingMesh(0.57f, 0.04f, 0.05f), K.furniture);
                Rod("DrumRope", det, cp + new Vector3(0.55f, 0.95f, 0.2f), new Vector3(CX - 0.6f, h - 0.4f, bellZ - 0.6f), 0.035f, K.furniture);
                Prim("CeilPulley", det, PrimitiveType.Cylinder, new Vector3(CX - 0.6f, h - 0.4f, bellZ - 0.6f), new Vector3(0.4f, 0.05f, 0.4f), K.iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
                var tp = new Vector3(-4.8f, 0, -3.4f);
                Kit("Table", root, tp, 0f, 1.1f);
                float ty = 0.82f * 1.1f;
                foreach (var (o, sz, yw) in new[] { (new Vector3(-0.3f, 0, 0), new Vector3(0.9f, 0.004f, 0.6f), 4f), (new Vector3(0.45f, 0.004f, 0.05f), new Vector3(0.6f, 0.004f, 0.45f), -9f) })
                    Slab("Blueprint", det, tp + o + Vector3.up * (ty + 0.003f), sz, K.paperC, 1f, false).transform.localRotation = Quaternion.Euler(0, yw, 0);
                Kit("Compass", root, tp + new Vector3(0.2f, ty, -0.15f), 30f);
                Kit("Magnifier", root, tp + new Vector3(-0.6f, ty, 0.18f), 60f);
                Kit("Candelabra", root, tp + new Vector3(0.85f, ty, 0.1f), 0f, 0.9f);
                Kit("L_Candelabra", root, tp + new Vector3(0.85f, ty + 0.45f, 0.1f));
                Kit("ChairWood", root, tp + new Vector3(0, 0, -0.8f), 5f);
                Letter(root, tp + new Vector3(-1.15f, ty + 0.002f, 0.05f), 20f, 2);
                Kit("Rug", root, tp + new Vector3(0, 0, -0.2f), 0f, 0.95f);
                Kit("CrateLong", root, new Vector3(-0.6f, 0, -2.0f), 35f);
                Kit("Basket", root, new Vector3(-1.4f, 0, 3.0f), 20f);
                foreach (var p in new[] { new Vector3(CX - 4f, 4.2f, -2f), new Vector3(CX + 3.5f, 4.2f, -1f), new Vector3(CX, 4.6f, 5.5f), new Vector3(5.0f, 7.0f, 3.5f), new Vector3(CX, 4.6f, -7.5f) })
                {
                    var f = LightGo("TowerFill", LightType.Point, Pal.Candle, 3.6f, 10f, true);
                    f.transform.SetParent(root, false); f.transform.localPosition = p;
                }
            }

            // --- Orta: avizeler, ağlar ---
            Kit("ChandelierIron", root, new Vector3(CX, h - 2.2f, -5.5f), 0f, 1.4f);
            Rod("ChandChain", root, new Vector3(CX, h - 1.5f, -5.5f), new Vector3(CX, h - 0.3f, -5.5f), 0.04f, K.iron);
            Kit("L_Chandelier", root, new Vector3(CX, h - 3.0f, -5.5f));
            foreach (int s in new[] { -1, 1 })
            {
                Kit("CandelabraFloor", root, new Vector3(CX + s * 5.5f, 0, 6.6f), s * 30f);
                Kit("L_Candelabra", root, new Vector3(CX + s * 5.1f, 1.9f, 6.1f));
            }
            Kit("L_Lantern", root, new Vector3(-6.4f, cat, HZ - 1.0f), 0f);
            Kit("Lantern", root, new Vector3(-6.4f, cat, HZ - 1.0f), 0f);
            KitR("Cobweb", root, new Vector3(XW + 0.03f, h - 0.25f, -HZ + 0.03f), Quaternion.Euler(0, 180, 180), 2f);
            KitR("Cobweb", root, new Vector3(XE - 0.03f, h - 0.25f, HZ - 0.03f), Quaternion.Euler(0, 0, 180), 1.8f);
            KitR("Cobweb", root, new Vector3(XW + 0.03f, h - 0.25f, HZ - 0.03f), Quaternion.Euler(0, 0, -90), 2f);
            KitR("Cobweb", root, new Vector3(XW + 0.03f, 0, -HZ + 0.03f), Quaternion.Euler(0, 90, 0), 1.2f);
            KitR("Cobweb", root, new Vector3(sx0 + 0.1f, h - 0.3f, sz0 + 0.1f), Quaternion.Euler(0, 45, 180), 1.4f);

            // --- Işık altyapısı ---
            var skips = new[] { new Bounds(new Vector3(7.4f, 1.1f, 0.4f), new Vector3(1.6f, 2.2f, 3.6f)) };
            KitProbes(root, new Vector3(CX, h * 0.5f, 0), new Vector3(W, h, HZ * 2f), 6, 4, 6, skips);
            KitReflection(root, new Vector3(CX, 2.5f, 0), new Vector3(W, h, HZ * 2f));
            return root;
        }

        static float rzBridge() => TwZ - 1.9f; // köprünün iskeleye değdiği z (iskele korkuluk hattı)
    }
}
