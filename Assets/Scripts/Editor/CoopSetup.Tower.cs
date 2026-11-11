using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// X'in ilk odası: çan kulesi mekanizma odası (ref: Docs/Reference/mekanizma.jpg). Lambri + sıva + kirişli tavan + parke,
    /// kuzey duvarında dişli yığını, kirişten asılı çan, doğuda tezgah. Yalnızca Art kit; ışık baked (2 realtime fener).
    /// Bulmaca nesneleri (kollar) BuildPuzzle1'de eklenir.
    /// </summary>
    public static partial class CoopSetup
    {
        /// <summary>ExcludeFromBake'in dokunmayacağı (kit + baked ışıklı) nesneler.</summary>
        static readonly HashSet<GameObject> bakeKeep = new HashSet<GameObject>();

        static void KeepBaked(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) bakeKeep.Add(t.gameObject);
        }

        static Transform BuildTower(Vector3 center)
        {
            bakeKeep.Clear();
            var root = new GameObject("RoomA_Mekanizma").transform;
            root.position = center;
            const float S = 16f, hs = 8f, h = 5.6f; // iç ölçü 16 x 16, tavan 5.6

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), 17, 17, 0.5f, K.parquet, 3, 3);
            Wall(root, "WallN", new Vector3(0, 0, hs + 0.25f), true, 17, h, K.plaster, 2.5f);
            Wall(root, "WallS", new Vector3(0, 0, -hs - 0.25f), true, 17, h, K.plaster, 2.5f);
            Wall(root, "WallE", new Vector3(hs + 0.25f, 0, 0), false, 17, h, K.plaster, 2.5f);
            var stairDoor = new Opening(0f, 3f, 0f, 3f); // Saat Odası merdiveni (PuzzleDoor)
            Wall(root, "WallW", new Vector3(-hs - 0.25f, 0, 0), false, 17, h, K.plaster, 2.5f, stairDoor);
            BeamCeiling(root, "Ceil", new Vector3(0, h, 0), S, S, 1.6f);
            // Çatı: tek parça gölge örtüsü (ince, parçalı tavanın derzlerinden ay ışığı sızmasın)
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, h + 0.6f, 0), new Vector3(18f, 0.6f, 18f), K.beam, 2f, false), 0);
            Wainscot(root, "WainN", new Vector3(0, 0, hs), true, S, Vector3.back, 1.3f);
            Wainscot(root, "WainS", new Vector3(0, 0, -hs), true, S, Vector3.forward, 1.8f);
            Wainscot(root, "WainE", new Vector3(hs, 0, 0), false, S, Vector3.left, 1.8f);
            Wainscot(root, "WainW", new Vector3(-hs, 0, 0), false, S, Vector3.right, 1.8f, stairDoor);
            foreach (int s in new[] { -1, 1 })
                Slab("DoorJamb", root, new Vector3(-hs + 0.08f, 1.55f, s * 1.6f), new Vector3(0.16f, 3.1f, 0.22f), K.beam, 1.2f, false);
            Slab("DoorLintel", root, new Vector3(-hs + 0.08f, 3.12f, 0), new Vector3(0.16f, 0.24f, 3.42f), K.beam, 1.2f, false);

            // Karkas: köşe dikmeleri + payandalar, çan kirişi
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    Slab("Post", root, new Vector3(sx * 7.82f, h * 0.5f, sz * 7.82f), new Vector3(0.36f, h, 0.36f), K.beam, 1.2f, false);
                    Rod("Brace", root, new Vector3(sx * 7.8f, h - 1.5f, sz * 7.82f), new Vector3(sx * 6.4f, h - 0.3f, sz * 7.82f), 0.2f, K.beam);
                }
            const float bellZ = 3f;
            Slab("BellBeam", root, new Vector3(0, h - 0.56f, bellZ), new Vector3(S, 0.44f, 0.44f), K.beam, 1.2f, false);
            foreach (int s in new[] { -1, 1 })
                Rod("BellBrace", root, new Vector3(s * 7.9f, h - 2.0f, bellZ), new Vector3(s * 6.3f, h - 0.7f, bellZ), 0.22f, K.beam);

            // --- Çan (kit, manuel model): kirişten boyunduruk + demir kuşakla asılı ---
            {
                const float bellH = 1.65f; float top = h - 1.0f;
                Kit("Bell", root, new Vector3(0, top - bellH, bellZ), 0f, bellH / 1.1f);
                var rig = new GameObject("BellRig").transform;
                rig.SetParent(root, false);
                Slab("Yoke", rig, new Vector3(0, top + 0.1f, bellZ), new Vector3(1.3f, 0.26f, 0.3f), K.furniture, 1.2f, false);
                foreach (int s in new[] { -1, 1 })
                    Slab("Strap", rig, new Vector3(s * 0.5f, top + 0.25f, bellZ), new Vector3(0.07f, 0.62f, 0.5f), K.iron, 1f, false);
                Rod("Rope", rig, new Vector3(0, top - bellH + 0.3f, bellZ), new Vector3(0, 2.05f, bellZ), 0.035f, K.furniture);
                ProbeLit(rig.gameObject);
            }

            // --- Kuzey duvarı: ahşap karkas içinde dişli yığını (statik, tek malzeme) + önde bulmaca çözülünce dönen dişliler ---
            {
                var mech = new GameObject("Mechanism").transform;
                mech.SetParent(root, false);
                var anchor = new GameObject("ProbeAnchor").transform; // duvara yapışık parçalar probe gövdesinin dışında kalmasın
                anchor.SetParent(mech, false);
                anchor.localPosition = new Vector3(0, 3f, 6.4f);

                Slab("Back", mech, new Vector3(0, 3.45f, hs - 0.015f), new Vector3(12.6f, 4.3f, 0.03f), K.beam, 2f, false);
                foreach (float x in new[] { -6.15f, -2.05f, 2.05f, 6.15f })
                    Slab("FramePost", mech, new Vector3(x, h * 0.5f, hs - 0.24f), new Vector3(0.3f, h, 0.24f), K.beam, 1.2f, false);
                foreach (float y in new[] { 3.35f, 5.25f })
                    Slab("FrameRail", mech, new Vector3(0, y, hs - 0.2f), new Vector3(12.3f, 0.24f, 0.16f), K.beam, 1.2f, false);

                var pile = new GameObject("GearPile").transform;
                pile.SetParent(mech, false);
                // x, y, yarıçap, diş
                var gs = new[]
                {
                    new Vector4(-4.6f, 2.75f, 1.2f, 20), new Vector4(-2.9f, 4.0f, 0.9f, 14), new Vector4(-5.2f, 4.55f, 0.7f, 12), new Vector4(-3.1f, 2.05f, 0.6f, 10),
                    new Vector4(-1.0f, 4.45f, 0.8f, 14), new Vector4(0.9f, 2.3f, 0.9f, 14), new Vector4(2.7f, 2.5f, 0.7f, 12), new Vector4(4.4f, 2.95f, 1.25f, 20),
                    new Vector4(3.0f, 4.45f, 0.75f, 12), new Vector4(5.35f, 4.6f, 0.6f, 10), new Vector4(5.5f, 1.85f, 0.5f, 8), new Vector4(-5.65f, 1.85f, 0.5f, 8),
                    new Vector4(1.35f, 4.7f, 0.55f, 10), new Vector4(-0.9f, 2.2f, 0.6f, 10),
                };
                for (int i = 0; i < gs.Length; i++)
                {
                    var g = MeshObj("Gear" + i, pile, new Vector3(gs[i].x, gs[i].y, hs - (i % 2 == 0 ? 0.09f : 0.2f)), GearMesh((int)gs[i].w, gs[i].z, 0.14f, gs[i].z * 0.2f, 0.12f), K.iron);
                    g.transform.localRotation = Quaternion.Euler(0, 0, i * 37f);
                }
                // Mil + kasnaklar
                Rod("Shaft", pile, new Vector3(-5.9f, 4.95f, hs - 0.44f), new Vector3(5.9f, 4.95f, hs - 0.44f), 0.07f, K.iron);
                foreach (float x in new[] { -4.4f, -1.2f, 2.9f, 4.9f })
                    Prim("Pulley", pile, PrimitiveType.Cylinder, new Vector3(x, 4.95f, hs - 0.44f), new Vector3(0.5f, 0.04f, 0.5f), K.iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
                ProbeLit(pile.gameObject);

                // Öndeki dişliler (orta bölme): hareketli, bulmaca 1 çözülünce döner
                var spins = new List<Spin>();
                void Front(string n, int teeth, float r, Vector2 p, float speed)
                {
                    var g = MeshObj(n, mech, new Vector3(p.x, p.y, hs - 0.38f), GearMesh(teeth, r, 0.16f, r * 0.22f, 0.1f), K.brass);
                    Prim("Axle", g.transform, PrimitiveType.Cylinder, new Vector3(0, 0, 0.12f), new Vector3(r * 0.3f, 0.2f, r * 0.3f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    AddSpin(g, speed);
                    var sp = g.GetComponent<Spin>();
                    sp.enabled = false;
                    spins.Add(sp);
                }
                Front("GearFrontA", 16, 1.0f, new Vector2(-0.75f, 2.6f), 10f);
                Front("GearFrontB", 10, 0.6f, new Vector2(0.55f, 3.22f), -16f);
                Front("GearFrontC", 8, 0.5f, new Vector2(1.15f, 3.94f), 20f);
                var ps = mech.gameObject.AddComponent<PuzzleSpin>();
                var pso = new SerializedObject(ps);
                pso.FindProperty("puzzleId").intValue = 1;
                SetRefArray(pso, "targets", spins.ToArray());
                pso.ApplyModifiedPropertiesWithoutUndo();

                foreach (var r in mech.GetComponentsInChildren<Renderer>())
                    if (r.transform.parent == pile || r.GetComponentInParent<Spin>() != null) r.probeAnchor = anchor;
            }
            foreach (int s in new[] { -1, 1 })
            {
                Kit("CandelabraFloor", root, new Vector3(s * 6.6f, 0, 6.3f), s * 30f);
                Kit("L_Candelabra", root, new Vector3(s * 6.2f, 1.9f, 5.8f));
            }
            Kit("L_Candelabra", root, new Vector3(0, 2.4f, 5.4f)); // kolların önü: semboller okunsun

            // --- Batı: vitraylar (ay), merdiven kapısı, aplikler ---
            foreach (int s in new[] { -1, 1 })
            {
                KitR("WindowLancet", root, new Vector3(-hs, 1.95f, s * 4.75f), Quaternion.Euler(0, 90, 0), 1.3f);
                KitR("WallSconce", root, new Vector3(-hs + 0.05f, 2.4f, s * 2.3f), Quaternion.Euler(0, 90, 0));
                Kit("L_Candle", root, new Vector3(-hs + 0.7f, 2.5f, s * 2.3f));
            }
            KitR("PaintingLandscape", root, new Vector3(-hs + 0.28f, 0.45f, 6.3f), Quaternion.Euler(-14, 90, 0)); // yere dayalı tablo
            Kit("L_Lantern", root, new Vector3(-6.9f, 0, 5.2f));
            Kit("Chest", root, new Vector3(-7.2f, 0, 4.2f), 90f);
            Kit("BookStack", root, new Vector3(-6.4f, 0, 3.4f), 40f);
            Kit("PaperScatter", root, new Vector3(-5.6f, 0, 4.6f), 70f);
            Kit("Crate", root, new Vector3(-7.3f, 0, -4.2f), 12f);
            Kit("Stool", root, new Vector3(-6.5f, 0, -5.3f), 50f);
            Kit("Bucket", root, new Vector3(-7.4f, 0, -3.2f));

            // --- Doğu: tezgah + aletler + fener, alet panosu, notlar, merdiven, dolaplar ---
            Kit("Workbench", root, new Vector3(7.3f, 0, 3.0f), -90f);
            Kit("L_Lantern", root, new Vector3(7.4f, 0.82f, 3.95f));
            Slab("ToolBoard", root, new Vector3(hs - 0.07f, 1.4f, 3.0f), new Vector3(0.03f, 0.7f, 1.9f), K.furniture, 1.2f, false);
            var onWall = Quaternion.Euler(0, 0, 90); // yatık nesneyi doğu duvarına asar (üstü odaya bakar)
            KitR("Wrench", root, new Vector3(hs - 0.1f, 1.45f, 2.45f), onWall * Quaternion.Euler(0, 15, 0));
            KitR("Hammer", root, new Vector3(hs - 0.1f, 1.42f, 3.0f), onWall * Quaternion.Euler(0, -10, 0));
            KitR("Mallet", root, new Vector3(hs - 0.1f, 1.42f, 3.55f), onWall * Quaternion.Euler(0, 8, 0));
            KitR("PaperA", root, new Vector3(hs - 0.07f, 1.5f, 4.5f), onWall * Quaternion.Euler(0, 6, 0));
            KitR("PaperC", root, new Vector3(hs - 0.07f, 1.35f, 4.85f), onWall * Quaternion.Euler(0, -9, 0));
            KitR("PaperB", root, new Vector3(hs - 0.07f, 1.45f, 1.6f), onWall * Quaternion.Euler(0, -5, 0));
            KitR("WallSconce", root, new Vector3(hs - 0.05f, 2.5f, 1.0f), Quaternion.Euler(0, -90, 0));
            Kit("L_Candle", root, new Vector3(hs - 0.7f, 2.6f, 1.0f));
            Kit("Stool", root, new Vector3(6.2f, 0, 2.6f), 20f);
            Kit("PaperScatter", root, new Vector3(5.6f, 0, 4.4f), 160f);
            Kit("CrateStack", root, new Vector3(6.9f, 0, 6.9f), -100f);
            Kit("Ladder", root, new Vector3(7.55f, 0, -0.4f), 90f);
            KitR("PaintingKeep", root, new Vector3(hs - 0.04f, 3.5f, -2.9f), Quaternion.Euler(0, -90, 0), 1.4f);
            Kit("Cabinet", root, new Vector3(hs - 0.45f, 0, -2.9f), -90f);
            Kit("Bookshelf", root, new Vector3(hs - 0.4f, 0, -4.6f), -90f);
            Kit("BookRow", root, new Vector3(hs - 0.4f, 0.98f, -4.6f), -90f);
            Kit("Barrel", root, new Vector3(7.2f, 0, -6.2f), 25f);
            {
                // Yedek dişliler: duvara dayalı + yerde
                var spare = new GameObject("SpareGears").transform;
                spare.SetParent(root, false);
                MeshObj("Lean", spare, new Vector3(7.76f, 0.69f, 0.75f), GearMesh(12, 0.7f, 0.14f, 0.14f, 0.1f), K.bronze).transform.localRotation = Quaternion.Euler(0, 0, -12) * Quaternion.Euler(0, 90, 0);
                MeshObj("FlatA", spare, new Vector3(5.3f, 0.05f, 1.1f), GearMesh(10, 0.45f, 0.12f, 0.1f, 0.1f), K.bronze).transform.localRotation = Quaternion.Euler(90, 0, 0);
                MeshObj("FlatB", spare, new Vector3(4.9f, 0.04f, 5.5f), GearMesh(8, 0.3f, 0.1f, 0.07f, 0.08f), K.brass).transform.localRotation = Quaternion.Euler(90, 25, 0);
                ProbeLit(spare.gameObject);
            }
            Kit("CrateLong", root, new Vector3(-3.4f, 0, 1.2f), 35f);
            Kit("Bucket", root, new Vector3(-2.6f, 0, 0.5f));
            Kit("Barrel", root, new Vector3(-7.2f, 0, -6.9f), 70f);
            Kit("BarrelLying", root, new Vector3(-6.0f, 0, -7.2f), 10f);

            // --- Güney: giriş kapısı (holden gelen merdiven), yazı masası, sarkaçlı saat ---
            {
                var door = Kit("Door", root, new Vector3(0, 0, -hs + 0.12f), 0f, 1.1f);
                var da = new GameObject("DoorProbeAnchor").transform; // kapı duvara yapışık: probe gövdesinin dışında kararmasın
                da.SetParent(root, false);
                da.localPosition = new Vector3(0, 1.4f, -hs + 0.9f);
                foreach (var r in door.GetComponentsInChildren<Renderer>()) r.probeAnchor = da;
            }
            foreach (int s in new[] { -1, 1 })
            {
                Kit("WallSconce", root, new Vector3(s * 1.7f, 2.4f, -hs + 0.05f));
                Kit("L_Candle", root, new Vector3(s * 1.7f, 2.5f, -hs + 0.7f));
            }
            Kit("StudyTable", root, new Vector3(-4.4f, 0, -7.2f), 0f);
            Kit("Stool", root, new Vector3(-4.2f, 0, -6.2f), -15f);
            Kit("L_Candelabra", root, new Vector3(-4.0f, 1.8f, -6.6f));
            Kit("GrandfatherClock", root, new Vector3(3.6f, 0, -hs + 0.4f), 0f);
            Kit("Commode", root, new Vector3(5.6f, 0, -hs + 0.45f), 0f);
            Kit("Armillary", root, new Vector3(5.6f, 0.95f, -hs + 0.45f), 30f);
            Kit("PaperScatter", root, new Vector3(-2.2f, 0, -5.2f), 20f);

            // --- Orta: avize, örümcek ağları ---
            Kit("ChandelierIron", root, new Vector3(0, h - 0.2f, -2.6f), 0f, 1.2f);
            Kit("L_Chandelier", root, new Vector3(0, 3.0f, -2.6f));
            KitR("Cobweb", root, new Vector3(-hs + 0.03f, h - 0.25f, -hs + 0.03f), Quaternion.Euler(0, 180, 180), 1.8f);
            KitR("Cobweb", root, new Vector3(hs - 0.03f, h - 0.25f, hs - 0.03f), Quaternion.Euler(0, 0, 180), 1.6f);
            KitR("Cobweb", root, new Vector3(-hs + 0.03f, h - 0.25f, hs - 0.03f), Quaternion.Euler(0, 0, -90), 2f);
            KitR("Cobweb", root, new Vector3(-hs + 0.03f, 0, hs - 0.03f), Quaternion.identity, 1.1f);

            // --- Işık altyapısı ---
            KitProbes(root, new Vector3(0, h * 0.5f, 0), new Vector3(S, h, S), 5, 3, 5);
            KitReflection(root, new Vector3(0, 2.2f, 0), new Vector3(S, h, S));
            return root;
        }
    }
}
