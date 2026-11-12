using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun bulmaca 3 odası: sarnıç (ref: Docs/Reference/sarnic.jpg). Yosunlu taş duvar, üç tuğla beşik tonoz + taş kaburgalar,
    /// sütunlar, sığ su + taş yürüme yolu, batı duvarında 4 paslı vana (renk panosu + sembol plakası), parmaklıklı kapılar.
    /// Baked (KeepBaked), 2 realtime fener (vana duvarı). Kök "Cistern": roomO yerel (-36,0,0); iç x ±5.75, z ±6.
    /// </summary>
    public static partial class CoopSetup
    {
        /// <summary>Parmaklıklı demir kapı (LiftDoor): gövde görünmez çarpışma kutusu, çubuklar çocuk.</summary>
        static LiftDoor BarGate(string name, Transform parent, Vector3 pos, bool alongX, Transform probeAnchor)
        {
            var d = MakeDoor(name, parent, pos, alongX, K.iron);
            var iron = MakeMat("K_GateIron", new Color(0.1f, 0.095f, 0.09f));
            iron.SetFloat("_Metallic", 0.6f); iron.SetFloat("_Smoothness", 0.35f);
            d.GetComponent<MeshRenderer>().enabled = false;
            for (int i = d.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(d.transform.GetChild(i).gameObject);
            Vector3 P(float a, float y) => alongX ? new Vector3(a, y, 0) : new Vector3(0, y, a);
            for (int i = -3; i <= 3; i++) Rod("Bar", d.transform, P(i * 0.44f, -1.5f), P(i * 0.44f, 1.5f), 0.05f, iron);
            foreach (float y in new[] { -1.38f, 0f, 1.38f }) Rod("Rail", d.transform, P(-1.5f, y), P(1.5f, y), 0.08f, iron);
            Rod("BraceA", d.transform, P(-1.45f, -1.3f), P(1.45f, 1.3f), 0.045f, iron);
            Rod("BraceB", d.transform, P(-1.45f, 1.3f), P(1.45f, -1.3f), 0.045f, iron);
            foreach (var r in d.GetComponentsInChildren<Renderer>())
            {
                r.gameObject.isStatic = false;
                r.probeAnchor = probeAnchor;
            }
            return d;
        }

        static CisternValve MakeValve(Transform parent, int index, Vector3 pos, CisternPuzzle cistern)
        {
            var root = new GameObject("Valve" + index).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, -90, 0); // yüz (-Z) doğuya; duvar yüzü yerel z = +0.1
            const float wy = 1.25f, py = 1.9f;
            var horiz = Quaternion.Euler(90, 0, 0);
            var face = Quaternion.Euler(-90, 0, 0);
            Slab("Plate", root, new Vector3(0, wy, 0.06f), new Vector3(0.9f, 0.9f, 0.08f), K.iron, 1f); // etkileşim çarpışması
            var colorMat = MakeMat("ValvePipe" + index, Pal.Puzzle[index]); // çalışma anında tohumdan boyanır
            colorMat.SetFloat("_Metallic", 0.5f); colorMat.SetFloat("_Smoothness", 0.35f);
            const float pz = -0.25f, ey = 3.5f; // boru duvardan açıkta; üstte dirsekle duvara girer
            var pipe = Prim("Pipe", root, PrimitiveType.Cylinder, new Vector3(0, ey * 0.5f, pz), new Vector3(0.2f, ey * 0.5f, 0.2f), colorMat);
            var elbow = Prim("Elbow", root, PrimitiveType.Sphere, new Vector3(0, ey, pz), Vector3.one * 0.24f, colorMat);
            var stub = Prim("PipeIn", root, PrimitiveType.Cylinder, new Vector3(0, ey, (pz + 0.1f) * 0.5f), new Vector3(0.2f, (0.1f - pz) * 0.5f, 0.2f), colorMat);
            stub.transform.localRotation = horiz;
            MeshObj("WallFlange", root, new Vector3(0, ey, 0.06f), RingMesh(0.18f, 0.08f, 0.06f), K.iron).transform.localRotation = horiz;
            foreach (float y in new[] { 0.12f, 0.45f, 2.05f, 3.15f })
                MeshObj("Flange", root, new Vector3(0, y, pz), RingMesh(0.17f, 0.08f, 0.07f), K.iron);
            var collar = MeshObj("Collar", root, new Vector3(0, 2.7f, pz), RingMesh(0.15f, 0.06f, 0.16f), colorMat);
            Prim("Body", root, PrimitiveType.Cylinder, new Vector3(0, wy, -0.32f), new Vector3(0.36f, 0.1f, 0.36f), K.iron).transform.localRotation = horiz;
            Prim("Stem", root, PrimitiveType.Cylinder, new Vector3(0, wy, -0.48f), new Vector3(0.07f, 0.12f, 0.07f), K.iron).transform.localRotation = horiz;

            var wheel = new GameObject("Wheel").transform;
            wheel.SetParent(root, false);
            wheel.localPosition = new Vector3(0, wy, -0.6f);
            MeshObj("Rim", wheel, new Vector3(0, 0, 0.025f), RingMesh(0.42f, 0.06f, 0.05f), K.bronze).transform.localRotation = face;
            for (int i = 0; i < 3; i++)
                Slab("Spoke" + i, wheel, Vector3.zero, new Vector3(0.76f, 0.05f, 0.035f), K.bronze, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, i * 60f);
            Prim("Hub", wheel, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.14f, K.brass);

            // Renk panosu (güney yanı): ahşap çerçeve + boyalı iç
            Slab("ColorFrame", root, new Vector3(-0.58f, py, 0.07f), new Vector3(0.5f, 0.62f, 0.05f), K.furniture, 1f, false);
            var inset = Slab("ColorInset", root, new Vector3(-0.58f, py, 0.04f), new Vector3(0.36f, 0.48f, 0.02f), colorMat, 1f, false);
            // Sembol plakası (kuzey yanı): bronz çerçeve + koyu iç
            Slab("SymbolFrame", root, new Vector3(0.62f, py, 0.075f), new Vector3(0.66f, 0.66f, 0.04f), K.bronze, 1f, false);
            Slab("SymbolPlate", root, new Vector3(0.62f, py, 0.05f), new Vector3(0.58f, 0.58f, 0.02f), MakeMat("K_CellarInset", new Color(0.075f, 0.065f, 0.06f)), 1f, false);
            var anchor = new GameObject("Symbol").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0.62f, py, 0.03f);

            // Hareketli/boyanan parçalar: statik değil, probe çapası duvarın önünde
            var pa = new GameObject("ProbeAnchor").transform;
            pa.SetParent(root, false);
            pa.localPosition = new Vector3(0, 1.5f, -0.8f);
            foreach (var t in root.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.probeAnchor = pa;

            var it = root.gameObject.AddComponent<Interactable>();
            it.Label = "Vanayı çevir";
            var v = root.gameObject.AddComponent<CisternValve>();
            var so = new SerializedObject(v);
            so.FindProperty("puzzleId").intValue = 3;
            so.FindProperty("nodeId").intValue = 27 + index;
            so.FindProperty("valveIndex").intValue = index;
            SetRef(so, "cistern", cistern);
            SetRef(so, "wheel", wheel);
            SetRef(so, "symbolAnchor", anchor);
            SetRefArray(so, "pipe", new Object[] { collar.GetComponent<Renderer>(), inset.GetComponent<Renderer>(), pipe.GetComponent<Renderer>(), elbow.GetComponent<Renderer>(), stub.GetComponent<Renderer>() });
            SetRef(so, "symbolMat", A.gold);
            so.ApplyModifiedPropertiesWithoutUndo();
            return v;
        }

        static void BuildCistern(Transform roomO, out CisternPuzzle cistern, out CisternValve[] valves, out LiftDoor gate, out LiftDoor nextGate, out GameObject water)
        {
            var cis = new GameObject("Cistern").transform;
            cis.SetParent(roomO, false);
            cis.localPosition = new Vector3(-36f, 0, 0);
            const float spring = 3.3f, rise = 1.8f, endH = 5.4f; // ref sarnic2: yüksek, ferah
            var wall = K.cisternWall; var stone = K.cisternWall; var brick = K.cellarBrick;
            var arch = new Opening(0f, 3f, 0f, 3f);

            // --- Kabuk ---
            Deck(cis, "CisternFloor", new Vector3(0, -0.25f, 0), 12.5f, 12.5f, 0.5f, K.cisternFloor, 3, 3);
            Wall(cis, "CisternWallE", new Vector3(6f, 0, 0), false, 12.5f, endH, wall, 2f, arch);
            Wall(cis, "CisternWallW", new Vector3(-6f, 0, 0), false, 12.5f, endH, wall, 2f);
            Wall(cis, "CisternWallN", new Vector3(0, 0, 6.25f), true, 12.5f, spring + 0.3f, wall, 2f);
            Wall(cis, "CisternWallS", new Vector3(0, 0, -6.25f), true, 12.5f, spring + 0.3f, wall, 2f, arch);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", cis, new Vector3(0, endH + 0.4f, 0), new Vector3(13.5f, 0.5f, 13.5f), stone, 2f, false), 0);
            foreach (int b in new[] { -1, 0, 1 })
            {
                var bay = new GameObject("Bay" + b).transform;
                bay.SetParent(cis, false);
                bay.localPosition = new Vector3(0, 0, b * 4f);
                VaultStrip(bay, "Vault", 0, 12.5f, 2.12f, spring, rise, 0.3f, brick, 8, 2f);
                foreach (float x in new[] { -2.5f, 2.5f })
                    VaultStrip(bay, "Rib" + (x < 0 ? "W" : "E"), x, 0.5f, 1.98f, spring, rise - 0.2f, 0.28f, stone, 8, 1.2f);
            }
            foreach (int s in new[] { -1, 1 })
            {
                Slab("Architrave", cis, new Vector3(0, spring - 0.05f, s * 2f), new Vector3(11.5f, 0.5f, 0.5f), stone, 1.5f, false);
                Slab("Corbel", cis, new Vector3(5.6f, spring - 0.48f, s * 2f), new Vector3(0.3f, 0.36f, 0.6f), stone, 1.2f, false);
                foreach (float x in new[] { -2.5f, 2.5f })
                {
                    {   // Sütun: kare kaide + yuvarlak gövde + başlık
                        var col = new GameObject("Column").transform;
                        col.SetParent(cis, false);
                        col.localPosition = new Vector3(x, 0, s * 2f);
                        col.localScale = new Vector3(1.2f, spring / 2.6f, 1.2f);
                        col.gameObject.AddComponent<BoxCollider>().center = new Vector3(0, 1.15f, 0);
                        col.GetComponent<BoxCollider>().size = new Vector3(0.6f, 2.3f, 0.6f);
                        Slab("Base", col, new Vector3(0, 0.2f, 0), new Vector3(0.84f, 0.4f, 0.84f), stone, 1.2f, false);
                        Slab("Plinth", col, new Vector3(0, 0.46f, 0), new Vector3(0.68f, 0.12f, 0.68f), stone, 1.2f, false);
                        ProbeLit(Prim("Shaft", col, PrimitiveType.Cylinder, new Vector3(0, 1.21f, 0), new Vector3(0.5f, 0.69f, 0.5f), stone));
                        Slab("Neck", col, new Vector3(0, 1.95f, 0), new Vector3(0.64f, 0.1f, 0.64f), stone, 1.2f, false);
                        Slab("Cap", col, new Vector3(0, 2.15f, 0), new Vector3(0.84f, 0.3f, 0.84f), stone, 1.2f, false);
                    }
                    Slab("Pilaster", cis, new Vector3(x, spring * 0.5f, s * 5.86f), new Vector3(0.5f, spring, 0.28f), stone, 1.2f);
                    Slab("PilasterBase", cis, new Vector3(x, 0.14f, s * 5.82f), new Vector3(0.64f, 0.28f, 0.4f), stone, 1.2f, false);
                }
            }
            // Yürüme yolu (suyun üstünde): batı rıhtımı, doğu kapısından geçit, güney kapısına kol
            Slab("QuayW", cis, new Vector3(-5.0f, 0.05f, 0), new Vector3(1.5f, 0.1f, 12f), K.cisternFloor, 1.5f);
            Slab("Causeway", cis, new Vector3(0.75f, 0.05f, 0), new Vector3(10f, 0.1f, 2.2f), K.cisternFloor, 1.5f);
            Slab("CausewayS", cis, new Vector3(0, 0.05f, -3.55f), new Vector3(2.2f, 0.1f, 4.9f), K.cisternFloor, 1.5f);

            // Doğu geçidi (mahzen nişine) + güney nişi (bulmaca 4 geçidi, şimdilik kapalı)
            Deck(cis, "CisternCorridorFloor", new Vector3(7.1f, -0.25f, 0), 2.3f, 3.5f, 0.5f, K.cisternFloor, 1, 1);
            Deck(cis, "CisternCorridorCeil", new Vector3(7.1f, 3.25f, 0), 2.3f, 3.5f, 0.5f, brick, 1, 1);
            Wall(cis, "CisternCorridorN", new Vector3(7.1f, 0, 1.75f), true, 2.3f, 3f, wall, 2f);
            Wall(cis, "CisternCorridorS", new Vector3(7.1f, 0, -1.75f), true, 2.3f, 3f, wall, 2f);
            Deck(cis, "CisternNicheFloor", new Vector3(0, -0.25f, -7.4f), 3.5f, 2.3f, 0.5f, K.cisternFloor, 1, 1);
            Deck(cis, "CisternNicheCeil", new Vector3(0, 3.25f, -7.4f), 3.5f, 2.3f, 0.5f, brick, 1, 1);
            Wall(cis, "CisternNicheS", new Vector3(0, 0, -8.5f), true, 3.5f, 3f, wall, 2f);
            Wall(cis, "CisternNicheE", new Vector3(1.75f, 0, -7.4f), false, 2.3f, 3f, wall, 2f);
            Wall(cis, "CisternNicheW", new Vector3(-1.75f, 0, -7.4f), false, 2.3f, 3f, wall, 2f);
            // Bake'te mahzen tarafı görünmez: geçit ucundan ortam ışığı sızmasın (yalnızca gölge)
            Slab("BakeBlock", cis, new Vector3(8.2f, 1.5f, 0), new Vector3(0.1f, 3f, 3.5f), stone, 1f, false).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            foreach (int s in new[] { -1, 1 })
            {
                Slab("GateJambE", cis, new Vector3(5.69f, 1.5f, s * 1.64f), new Vector3(0.14f, 3f, 0.28f), stone, 1.2f, false);
                Slab("GateJambS", cis, new Vector3(s * 1.64f, 1.5f, -5.94f), new Vector3(0.28f, 3f, 0.14f), stone, 1.2f, false);
            }
            var gaE = new GameObject("GateAnchorE").transform; gaE.SetParent(cis, false); gaE.localPosition = new Vector3(4.6f, 1.5f, 0);
            var gaS = new GameObject("GateAnchorS").transform; gaS.SetParent(cis, false); gaS.localPosition = new Vector3(0, 1.5f, -4.8f);
            gate = BarGate("CisternGate", cis, new Vector3(7.1f, 1.5f, 0), false, gaE);
            nextGate = BarGate("CisternNextGate", cis, new Vector3(0, 1.5f, -6.25f), true, gaS);

            // --- Su ---
            water = Slab("CisternWater", cis, new Vector3(0, 0.02f, 0), new Vector3(11.5f, 0.04f, 12f), MakeMat("K_CisternWater", new Color(0.05f, 0.15f, 0.15f)), 1f, false);
            water.GetComponent<Renderer>().sharedMaterial.SetFloat("_Smoothness", 0.93f);
            water.isStatic = false;
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            water.GetComponent<Renderer>().sharedMaterial.color = new Color(0.03f, 0.09f, 0.1f);
            {
                var flo = water.transform; // çocuklar su seviyesiyle birlikte yükselir
                var boat = new GameObject("Boat").transform;
                boat.SetParent(flo, false);
                boat.localPosition = new Vector3(3.9f, -0.02f, 3.35f);
                boat.localRotation = Quaternion.Euler(0, 75f, 0);
                Slab("Hull", boat, new Vector3(0, -0.06f, 0), new Vector3(0.8f, 0.06f, 2.2f), K.beam, 1f, false);
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("Side", boat, new Vector3(s * 0.45f, 0.08f, 0), new Vector3(0.06f, 0.32f, 2.3f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, -s * 14f);
                    Slab("Gunwale", boat, new Vector3(s * 0.49f, 0.24f, 0), new Vector3(0.08f, 0.04f, 2.32f), K.furniture, 1f, false);
                    Slab("End", boat, new Vector3(0, 0.08f, s * 1.15f), new Vector3(0.95f, 0.32f, 0.06f), K.beam, 1f, false);
                    Slab("Seat", boat, new Vector3(0, 0.14f, s * 0.45f), new Vector3(0.86f, 0.04f, 0.25f), K.furniture, 1f, false);
                }
                Rod("Oar", boat, new Vector3(-0.3f, 0.2f, -0.8f), new Vector3(0.25f, 0.18f, 0.9f), 0.04f, K.furniture);
                // Su geçitlere de taşar: kapı ağzında kesik "kalıp" kenarı görünmesin (kenarlar duvar içinde kalır)
                var wm = water.GetComponent<Renderer>().sharedMaterial;
                Slab("WaterCorridor", flo, new Vector3(6.975f, 0, 0), new Vector3(2.5f, 0.04f, 3.5f), wm, 1f, false);
                Slab("WaterNiche", flo, new Vector3(0, 0, -7.25f), new Vector3(3.5f, 0.04f, 2.5f), wm, 1f, false);
                var deb = new GameObject("Debris").transform;
                deb.SetParent(flo, false);
                Slab("Plank", deb, new Vector3(-2.8f, 0.01f, 2.8f), new Vector3(1.2f, 0.04f, 0.24f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(0, 20, 0);
                Slab("Plank", deb, new Vector3(-2.1f, 0.01f, 3.3f), new Vector3(0.9f, 0.04f, 0.22f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(0, -15, 0);
                Slab("Plank", deb, new Vector3(-3.0f, 0.01f, -3.4f), new Vector3(1.1f, 0.04f, 0.24f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(0, 60, 0);
                Kit("BarrelLying", deb, new Vector3(-2.6f, -0.22f, -4.6f), 30f);
                foreach (var t in flo.GetComponentsInChildren<Transform>(true))
                {
                    if (t == flo) continue;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    var c = t.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
                }
            }
            // Bağlama halatı: sütuna sarılı, kayığın kıçına iner
            for (int i = 0; i < 3; i++)
                MeshObj("MooringCoil", cis, new Vector3(2.5f, 0.72f + i * 0.06f, 2f), RingMesh(0.36f, 0.045f, 0.05f), K.beam);
            Rod("MooringRope", cis, new Vector3(2.75f, 0.76f, 2.25f), new Vector3(2.85f, 0.45f, 3.0f), 0.035f, K.beam);
            // Tavan açıklıkları: ay ışığı huzmesi + baked spot
            {
                var glow = GlowMat("K_Oculus", Pal.Moon * 0.4f, Pal.Moon * 1.1f);
                foreach (var o in new[] { new Vector2(1.5f, 0f), new Vector2(-1.2f, 4f) })
                {
                    float cy = spring + rise - 0.18f, L = cy - 0.1f;
                    var oc = new GameObject("Oculus").transform;
                    oc.SetParent(cis, false);
                    oc.localPosition = new Vector3(o.x, 0, o.y);
                    MeshObj("Ring", oc, new Vector3(0, cy, 0), RingMesh(0.46f, 0.1f, 0.14f), stone);
                    Prim("Sky", oc, PrimitiveType.Cylinder, new Vector3(0, cy + 0.05f, 0), new Vector3(0.8f, 0.01f, 0.8f), glow);
                    QuadObj("Shaft", oc, new Vector3(0, cy - L * 0.5f, 0), new Vector2(0.8f, L), K.shaft, Quaternion.LookRotation(Vector3.right, Vector3.up));
                    QuadObj("ShaftB", oc, new Vector3(0, cy - L * 0.5f, 0), new Vector2(0.65f, L), K.shaft, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                    var ml = LightGo("OculusMoon", LightType.Spot, Pal.Moon, 12f, 8f, true);
                    ml.transform.SetParent(oc, false);
                    ml.transform.localPosition = new Vector3(0, cy - 0.1f, 0);
                    ml.transform.localRotation = Quaternion.LookRotation(Vector3.down);
                    ml.GetComponent<Light>().spotAngle = 34f; ml.GetComponent<Light>().innerSpotAngle = 18f;
                }
            }

            // --- Bulmaca: batı duvarında 4 vana ---
            cistern = new GameObject("Puzzle3_Cistern").AddComponent<CisternPuzzle>();
            cistern.transform.SetParent(cis, false);
            valves = new CisternValve[4];
            float[] vz = { -4f, -1f, 1f, 4f };
            for (int i = 0; i < 4; i++) valves[i] = MakeValve(cis, i, new Vector3(-5.65f, 0, vz[i]), cistern);

            // --- Küçük ayrıntılar (probe ışıklı) ---
            var det = new GameObject("Details").transform;
            det.SetParent(cis, false);
            var voidMat = MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f));
            void Chain(float x, float z, float top, float bottom)
            {
                Rod("Chain", det, new Vector3(x, top, z), new Vector3(x, bottom, z), 0.022f, K.iron);
                Rod("HookA", det, new Vector3(x, bottom, z), new Vector3(x + 0.08f, bottom - 0.13f, z), 0.02f, K.iron);
                Rod("HookB", det, new Vector3(x + 0.08f, bottom - 0.13f, z), new Vector3(x - 0.02f, bottom - 0.2f, z), 0.02f, K.iron);
            }
            // Duvar kolu: ucunda asılı fener; dönen değer fenerin tabanı
            Vector3 Bracket(Vector3 wallPt, Vector3 inward)
            {
                var tip = wallPt + inward * 0.42f;
                Rod("LanternArm", det, wallPt, tip, 0.03f, K.iron);
                Rod("LanternBrace", det, wallPt + Vector3.down * 0.3f, wallPt + inward * 0.3f, 0.022f, K.iron);
                Rod("LanternHook", det, tip - inward * 0.02f, tip - inward * 0.02f + Vector3.down * 0.12f, 0.015f, K.iron);
                return tip - inward * 0.02f + Vector3.down * 0.54f;
            }
            // Baked fener: model bake'e gölge düşürmez (tonozda leke olmasın), ışık camın içinde
            void BakedLantern(Vector3 p, float yaw)
            {
                var go = Kit("Lantern", cis, p, yaw);
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, GameObjectUtility.GetStaticEditorFlags(t.gameObject) & ~StaticEditorFlags.ContributeGI);
                Kit("L_Candelabra", cis, p + new Vector3(0, 0.2f, 0));
            }
            Chain(2.5f, -4f, 4.95f, 3.6f);
            Chain(-2.5f, 4f, 4.95f, 3.9f);
            Chain(0.4f, 4.3f, 4.9f, 3.4f);
            Chain(-2.2f, -3.6f, 4.9f, 4.0f);
            // Seviye cetveli (batı duvarı): çentik aralığı = su seviyesi adımı
            Slab("GaugeBoard", det, new Vector3(-5.73f, 1.2f, -2.6f), new Vector3(0.04f, 2.4f, 0.16f), K.furniture, 1f, false);
            for (int i = 1; i <= 10; i++)
                Slab("GaugeNotch", det, new Vector3(-5.705f, 0.02f + i * CisternPuzzle.LevelHeight, -2.6f), new Vector3(0.012f, 0.02f, i % 2 == 0 ? 0.16f : 0.09f), K.brass, 1f, false);
            // Halat kangalları: sandık üstü + doğu duvarı askısı
            for (int i = 0; i < 3; i++)
                MeshObj("RopeCoil", det, new Vector3(4.0f, 0.6f + i * 0.045f, -5.2f), RingMesh(0.2f - i * 0.012f, 0.05f, 0.045f), K.beam);
            MeshObj("RopeHang", det, new Vector3(5.74f, 1.55f, -2.6f), RingMesh(0.22f, 0.05f, 0.06f), K.beam).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Rod("RopePeg", det, new Vector3(5.75f, 1.76f, -2.6f), new Vector3(5.6f, 1.8f, -2.6f), 0.03f, K.iron);
            // Besleme borusu (kuzey duvarı)
            MeshObj("InflowFlange", det, new Vector3(-1.5f, 2.0f, 5.42f), RingMesh(0.32f, 0.09f, 0.07f), K.iron).transform.localRotation = Quaternion.Euler(-90, 0, 0);
            ProbeLit(det.gameObject);
            Prim("InflowPipe", det, PrimitiveType.Cylinder, new Vector3(-1.5f, 2.0f, 5.7f), new Vector3(0.5f, 0.35f, 0.5f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim("InflowHole", det, PrimitiveType.Cylinder, new Vector3(-1.5f, 2.0f, 5.345f), new Vector3(0.44f, 0.004f, 0.44f), voidMat).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var da = new GameObject("WallDetailAnchor").transform; da.SetParent(det, false); da.localPosition = new Vector3(-4.6f, 1.5f, -2.6f);
            foreach (var r in det.GetComponentsInChildren<Renderer>()) if (r.name.StartsWith("Gauge")) r.probeAnchor = da;

            // --- Işık: vana duvarında 2 realtime fener, gerisi baked ---
            foreach (int s in new[] { -1, 1 })
                Kit("L_Lantern", cis, Bracket(new Vector3(-5.75f, 2.4f, s * 2.6f), Vector3.right), s * 25f);
            {
                Rod("LanternChain", det, new Vector3(0, 3.5f, 0), new Vector3(0, 4.95f, 0), 0.02f, K.iron);
                BakedLantern(new Vector3(0, 3.08f, 0), 0f);
                var pe = Bracket(new Vector3(5.75f, 2.4f, 2.6f), Vector3.left);
                BakedLantern(pe, 40f);
                var ps = Bracket(new Vector3(2.3f, 2.4f, -6f), Vector3.forward);
                BakedLantern(ps, -30f);
                var teal = Color.Lerp(Pal.Moon, Pal.Vitray[1], 0.55f); // soğuk dolgu (sarnıç havası)
                foreach (var p in new[] { new Vector3(2.6f, 1.5f, 4f), new Vector3(-1.4f, 1.5f, -4f) })
                {
                    var f = LightGo("TealFill", LightType.Point, teal, 2.4f, 11f, true);
                    f.transform.SetParent(cis, false);
                    f.transform.localPosition = p;
                }
            }

            // --- Kuzeydoğu: bakımcının masası (vana notları, aletler), merdiven, sandık ---
            {
                const float th = 0.82f;
                Kit("Table", cis, new Vector3(4.3f, 0, 5.2f), 0f);
                Prim("CandleDish", cis, PrimitiveType.Cylinder, new Vector3(4.8f, th + 0.02f, 5.3f), new Vector3(0.11f, 0.008f, 0.11f), K.brass);
                KCandle(cis, new Vector3(4.8f, th + 0.028f, 5.3f), 0.18f, 0.02f);
                Kit("L_Candle", cis, new Vector3(4.6f, th + 0.5f, 4.8f));
                Kit("PaperA", cis, new Vector3(4.05f, th + 0.004f, 5.1f), 20f);
                Kit("PaperC", cis, new Vector3(4.3f, th + 0.006f, 5.0f), -35f);
                Kit("Wrench", cis, new Vector3(3.85f, th + 0.004f, 5.35f), 30f);
                Kit("Hammer", cis, new Vector3(4.45f, th + 0.004f, 5.4f), -60f);
                Kit("Bottle1", cis, new Vector3(4.65f, th, 5.0f), 50f);
                Kit("Goblet0", cis, new Vector3(4.5f, th + 0.012f, 5.15f));
                KitR("PaperB", cis, new Vector3(4.3f, 1.55f, 5.97f), Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(0, 6, 0));
                Kit("Stool", cis, new Vector3(3.9f, 0, 4.35f), 25f);
                Kit("Ladder", cis, new Vector3(2.4f, 0, 5.3f), 0f);
                    Kit("Chest", cis, new Vector3(5.2f, 0, 2.2f), -90f);
                Kit("Bucket", cis, new Vector3(1.4f, 0, 5.4f));
            }
            // --- Güneydoğu: erzak (fıçı, sandıklar, halat, kova) ---
            Kit("Barrel", cis, new Vector3(5.1f, 0, -5.3f), 25f);
            Kit("Barrel", cis, new Vector3(5.2f, 0, -4.3f), 130f);
            Kit("Crate", cis, new Vector3(4.0f, 0, -5.2f), 15f);
            Kit("CrateStack", cis, new Vector3(2.9f, 0, -5.35f), 180f);
            Kit("CrateLong", cis, new Vector3(3.9f, 0, -3.8f), 70f);
            Kit("Bucket", cis, new Vector3(4.9f, 0, -3.3f));
            Kit("Mallet", cis, new Vector3(3.85f, 0.42f, -3.8f), 110f);
            // --- Rıhtım köşeleri ---
            Kit("Barrel", cis, new Vector3(-5.2f, 0.1f, 5.55f), 70f);
            Kit("Pot", cis, new Vector3(-4.6f, 0.1f, 5.6f));
            Kit("Bucket", cis, new Vector3(-5.25f, 0.1f, -5.55f));
            Kit("Amphora", cis, new Vector3(-4.7f, 0.1f, -5.6f), 200f, 0.8f);
            KitR("Cobweb", cis, new Vector3(5.72f, 0, -5.97f), Quaternion.Euler(0, 180, 0), 1.2f);
            KitR("Cobweb", cis, new Vector3(-3.0f, 2.1f, 5.6f), Quaternion.identity, 1.0f);
            KitR("Cobweb", cis, new Vector3(3.4f, 2.4f, -5.7f), Quaternion.Euler(0, 180, 180), 1.2f);
            KitR("Cobweb", cis, new Vector3(-5.72f, 0.1f, 5.97f), Quaternion.identity, 0.9f);

            KitProbes(cis, new Vector3(0, 2.3f, 0), new Vector3(11.5f, 4.4f, 11.2f), 4, 3, 5);
            KitReflection(cis, new Vector3(0, 2.3f, 0), new Vector3(11.5f, 5f, 12f));
            KeepBaked(cis);
        }
    }
}
