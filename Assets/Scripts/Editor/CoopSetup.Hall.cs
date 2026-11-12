using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Lobi Holü (ref: Docs/Reference/lobi_hol.png): oyunun başladığı gotik giriş holü. Büyük merdiven sahanlığındaki X plakası
    /// yukarıya (çan kulesi), doğu duvarındaki mahzen nişindeki O plakası aşağıya götürür (HallGate). Yalnızca Art kit kullanır.
    /// </summary>
    public static partial class CoopSetup
    {
        const int HallPuzzleId = 0, PlateXNode = 90, PlateONode = 91, HallGateNode = 92; // 35-58 bulmacalarda dolu

        static Material voidMat, inlayMat;

        /// <summary>Kit dekoru gibi: ışık haritası yemez, probe kullanır; baked gölge verir.</summary>
        static GameObject ProbeLit(GameObject go, bool gi = true)
        {
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (gi) flags |= StaticEditorFlags.ContributeGI;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                var mr = t.GetComponent<MeshRenderer>();
                if (mr != null) mr.receiveGI = ReceiveGI.LightProbes;
            }
            return go;
        }

        /// <summary>Kit öncesi (gerçek zamanlı ışıklı) odalar: bake dışında kalır, probe yerine sahne ortam ışığını kullanır.</summary>
        static void ExcludeFromBake()
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (bakeKeep.Contains(t.gameObject)) continue;
                    var f = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    if ((f & StaticEditorFlags.ContributeGI) != 0) GameObjectUtility.SetStaticEditorFlags(t.gameObject, f & ~StaticEditorFlags.ContributeGI);
                    var r = t.GetComponent<Renderer>();
                    if (r != null) r.lightProbeUsage = LightProbeUsage.Off;
                }
        }

        static void Pillar(Transform root, string name, Vector3 basePos, float h, float w = 0.8f)
        {
            Slab(name + "_Base", root, basePos + Vector3.up * 0.25f, new Vector3(w + 0.3f, 0.5f, w + 0.3f), K.castleStone, 1.5f);
            Slab(name + "_Shaft", root, basePos + Vector3.up * (h * 0.5f), new Vector3(w, h, w), K.castleStone, 2f, false);
            Slab(name + "_Ring", root, basePos + Vector3.up * (h - 1.0f), new Vector3(w + 0.14f, 0.16f, w + 0.14f), K.castleStone, 1.5f, false);
            Slab(name + "_Cap", root, basePos + Vector3.up * (h - 0.3f), new Vector3(w + 0.4f, 0.6f, w + 0.4f), K.castleStone, 1.5f, false);
        }

        /// <summary>Sivri kemer çerçevesi (duvar yüzeyinde). recess: arkasına karanlık boşluk (sahte derinlik).</summary>
        static Transform ArchTrim(Transform root, string name, Vector3 baseCenter, Vector3 inward, float w, float h, bool recess)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = baseCenter;
            t.localRotation = Quaternion.LookRotation(inward, Vector3.up);
            float hs = h - w * 0.62f;
            const float th = 0.26f;
            foreach (int s in new[] { -1, 1 })
            {
                Slab("Jamb", t, new Vector3(s * w * 0.5f, hs * 0.5f, 0.1f), new Vector3(th, hs, 0.3f), K.castleStone, 1.5f, false);
                var rib = Rod("Rib", t, new Vector3(s * w * 0.5f, hs - 0.05f, 0.1f), new Vector3(0, h, 0.1f), th, K.castleStone);
                rib.transform.localScale = new Vector3(1f, 1.04f, 1.15f);
            }
            if (recess)
            {
                if (voidMat == null) { voidMat = MakeMat("K_Void", new Color(0.012f, 0.012f, 0.016f)); voidMat.SetFloat("_Smoothness", 0f); } // hol öncesi odalar da kullanır
                Slab("Void", t, new Vector3(0, hs * 0.5f, 0.02f), new Vector3(w, hs, 0.03f), voidMat, 1f, false);
                for (int i = 0; i < 4; i++)
                {
                    float k0 = i / 4f, k1 = (i + 1) / 4f;
                    Slab("Void" + i, t, new Vector3(0, hs + (h - hs) * (k0 + k1) * 0.5f, 0.02f), new Vector3(w * (1f - k1 * 0.9f), (h - hs) * 0.25f, 0.03f), voidMat, 1f, false);
                }
            }
            return t;
        }

        /// <summary>Korkuluk: a→b arası tırabzan + dikmeler + babalar. collide: göğüs hizasında görünmez engel.</summary>
        static void Balustrade(Transform root, string name, Vector3 a, Vector3 b, bool collide)
        {
            var mesh = LatheMesh("kit_baluster", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.07f, 0f), new Vector2(0.07f, 0.06f), new Vector2(0.035f, 0.12f), new Vector2(0.065f, 0.3f),
                new Vector2(0.04f, 0.5f), new Vector2(0.03f, 0.66f), new Vector2(0.06f, 0.74f), new Vector2(0.06f, 0.8f), new Vector2(0f, 0.8f),
            }, 8);
            var holder = new GameObject(name).transform;
            holder.SetParent(root, false);
            var d = b - a; float len = d.magnitude;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.42f));
            for (int i = 0; i < n; i++)
                MeshObj("Baluster", holder, a + d * ((i + 0.5f) / n), mesh, K.castleStone);
            Rod("Rail", holder, a + Vector3.up * 0.86f, b + Vector3.up * 0.86f, 0.14f, K.castleStone);
            Rod("Plinth", holder, a + Vector3.up * 0.03f, b + Vector3.up * 0.03f, 0.16f, K.castleStone);
            foreach (var p in new[] { a, b })
                Slab("Newel", holder, p + Vector3.up * 0.55f, new Vector3(0.24f, 1.1f, 0.24f), K.castleStone, 1.5f, false);
            ProbeLit(holder.gameObject);
            if (collide)
            {
                var c = new GameObject("Block").AddComponent<BoxCollider>();
                c.transform.SetParent(holder, false);
                c.transform.localPosition = (a + b) * 0.5f + Vector3.up * 0.5f;
                c.transform.localRotation = Quaternion.FromToRotation(Vector3.right, d.normalized);
                c.size = new Vector3(len, 1f, 0.16f);
            }
        }

        /// <summary>Pirinç kakma sembol (X ya da O), yerel XY düzleminde, boyut s. Dönen: parlatılacak renderer'lar.</summary>
        static List<Renderer> Symbol(Transform parent, Vector3 pos, Quaternion rot, bool isX, float s)
        {
            var t = new GameObject(isX ? "SymbolX" : "SymbolO").transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localRotation = rot;
            var list = new List<Renderer>();
            if (isX)
            {
                float k = s * 0.5f;
                list.Add(Rod("A", t, new Vector3(-k, -k, 0), new Vector3(k, k, 0), s * 0.13f, inlayMat).GetComponent<Renderer>());
                list.Add(Rod("B", t, new Vector3(-k, k, 0), new Vector3(k, -k, 0), s * 0.13f, inlayMat).GetComponent<Renderer>());
            }
            else
            {
                var ring = MeshObj("Ring", t, Vector3.zero, RingMesh(0.5f, 0.11f, 0.12f), inlayMat);
                ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
                ring.transform.localScale = new Vector3(s * 1.3f, s * 0.6f, s * 1.3f);
                list.Add(ring.GetComponent<Renderer>());
            }
            foreach (var r in list) { r.shadowCastingMode = ShadowCastingMode.Off; r.gameObject.isStatic = false; }
            return list;
        }

        static PressurePlate MakePlate(Transform root, string name, Vector3 pos, int nodeId, bool isX, List<Renderer> glow)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            ProbeLit(Slab("Rim", go.transform, new Vector3(0, 0.02f, 0), new Vector3(2.1f, 0.04f, 2.1f), K.iron, 1f, false));
            var top = Slab("Top", go.transform, new Vector3(0, 0.05f, 0), new Vector3(1.8f, 0.07f, 1.8f), K.bronze, 1.2f, false);
            top.isStatic = false;
            glow.AddRange(Symbol(top.transform, new Vector3(0, 0.04f, 0), Quaternion.Euler(90, 0, 0), isX, 1.1f));
            var plate = go.AddComponent<PressurePlate>();
            var so = new SerializedObject(plate);
            so.FindProperty("puzzleId").intValue = HallPuzzleId;
            so.FindProperty("nodeId").intValue = nodeId;
            so.FindProperty("size").vector2Value = new Vector2(1.8f, 1.8f);
            SetFlt(so, "height", 1.2f);
            SetRef(so, "top", top.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
            return plate;
        }

        /// <summary>Kapı üstü levha: koyu taş + pirinç sembol.</summary>
        static void SymbolPlaque(Transform root, Vector3 pos, Vector3 inward, bool isX, List<Renderer> glow)
        {
            var t = new GameObject(isX ? "PlaqueX" : "PlaqueO").transform;
            t.SetParent(root, false);
            t.localPosition = pos; t.localRotation = Quaternion.LookRotation(inward, Vector3.up);
            ProbeLit(Slab("Plate", t, new Vector3(0, 0, 0.03f), new Vector3(0.9f, 0.9f, 0.06f), K.iron, 1f, false));
            glow.AddRange(Symbol(t, new Vector3(0, 0, 0.09f), Quaternion.identity, isX, 0.5f));
        }

        static Transform BuildHall(Vector3 center)
        {
            {
                matDir = KitMatDir;
                voidMat = MakeMat("K_Void", new Color(0.012f, 0.012f, 0.016f));
                voidMat.SetFloat("_Smoothness", 0f);
                inlayMat = GlowMat("K_PlateInlay", new Color(0.8f, 0.6f, 0.3f), new Color(1f, 0.69f, 0.4f) * 0.25f, 0.6f);
                inlayMat.SetFloat("_Metallic", 0.6f);
                inlayMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                matDir = "Assets/Materials";
            }

            var root = new GameObject("Hall_Lobi").transform;
            root.position = center;
            const float SX = 18f, SZ = 22f, H = 9f;      // iç ölçü
            const float hx = SX * 0.5f, hz = SZ * 0.5f;
            const float LandY = 3f, LandZ = 8f;          // sahanlık: z 8..11, y 3
            const float StairZ0 = 2.5f, StairW = 4.5f;   // merdiven: z 2.5 → 8
            const float AlcZ = 3f, AlcW = 3.4f, AlcH = 3.8f, AlcD = 3.2f; // mahzen nişi (doğu)

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), SX, SZ, 0.5f, K.hallFloor, 3, 4);
            Wall(root, "WallN", new Vector3(0, 0, hz + 0.25f), true, SX + 1f, H, K.castleStone, 2.2f);
            Wall(root, "WallS", new Vector3(0, 0, -hz - 0.25f), true, SX + 1f, H, K.castleStone, 2.2f);
            Wall(root, "WallW", new Vector3(-hx - 0.25f, 0, 0), false, SZ, H, K.castleStone, 2.2f);
            Wall(root, "WallE", new Vector3(hx + 0.25f, 0, 0), false, SZ, H, K.castleStone, 2.2f, new Opening(AlcZ, AlcW, 0f, AlcH));
            BeamCeiling(root, "Ceil", new Vector3(0, H, 0), SX, SZ, 2.2f);
            foreach (float x in new[] { -5.5f, 5.5f })
                foreach (float z in new[] { -7f, -1f })
                    Pillar(root, "Pillar", new Vector3(x, 0, z), H);

            // --- Büyük merdiven + sahanlık (X: yukarı) ---
            const float run = LandZ - StairZ0;
            int steps = 15; float sr = run / steps, sh = LandY / steps;
            for (int i = 0; i < steps; i++)
                Slab("Step" + i, root, new Vector3(0, sh * (i + 1) * 0.5f, StairZ0 + sr * (i + 0.5f)), new Vector3(StairW, sh * (i + 1), sr), K.castleStone, 1.5f, false);
            {
                float ang = Mathf.Atan2(LandY, run);
                var ramp = Slab("StairRamp", root, Vector3.zero, new Vector3(StairW, 0.3f, Mathf.Sqrt(run * run + LandY * LandY) + 0.3f), K.castleStone, 1f);
                ramp.GetComponent<MeshRenderer>().enabled = false;
                GameObjectUtility.SetStaticEditorFlags(ramp, 0);
                ramp.transform.localPosition = new Vector3(0, LandY * 0.5f - 0.15f * Mathf.Cos(ang), StairZ0 + run * 0.5f + 0.15f * Mathf.Sin(ang));
                ramp.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
            }
            Deck(root, "Landing", new Vector3(0, LandY - 0.2f, (LandZ + hz) * 0.5f), SX, hz - LandZ, 0.4f, K.hallFloor, 3, 1);
            Slab("LandingFascia", root, new Vector3(-(hx + StairW * 0.5f) * 0.5f, LandY - 0.45f, LandZ + 0.12f), new Vector3(hx - StairW * 0.5f, 0.9f, 0.24f), K.castleStone, 1.5f, false);
            Slab("LandingFascia", root, new Vector3((hx + StairW * 0.5f) * 0.5f, LandY - 0.45f, LandZ + 0.12f), new Vector3(hx - StairW * 0.5f, 0.9f, 0.24f), K.castleStone, 1.5f, false);
            foreach (int s in new[] { -1, 1 })
            {
                float sx = s * (StairW * 0.5f - 0.1f);
                Balustrade(root, "StairRail", new Vector3(sx, 0, StairZ0 + 0.1f), new Vector3(sx, LandY, LandZ), false);
                Balustrade(root, "LandRail", new Vector3(s * StairW * 0.5f, LandY, LandZ + 0.12f), new Vector3(s * (hx - 0.1f), LandY, LandZ + 0.12f), true);
                Pillar(root, "LandPost", new Vector3(s * 5.5f, 0, LandZ + 0.5f), LandY - 0.4f, 0.7f);
                ArchTrim(root, "UnderArch", new Vector3(s * 5.6f, 0, hz), Vector3.back, 2.6f, 2.5f, true);
                Slab("StairSide", root, new Vector3(s * (StairW * 0.5f + 0.02f), 0.2f, StairZ0 + run * 0.5f), new Vector3(0.05f, 0.4f, run), K.castleStone, 1.5f, false);
            }

            var glow = new List<Renderer>();
            // Kule kapısı (sahanlıkta, kuzey duvarı) + X plakası
            Kit("Door", root, new Vector3(0, LandY, hz - 0.12f), 180f);
            ArchTrim(root, "DoorArchX", new Vector3(0, LandY, hz), Vector3.back, 2.5f, 3.6f, true);
            SymbolPlaque(root, new Vector3(0, LandY + 4.15f, hz), Vector3.back, true, glow);
            var plateX = MakePlate(root, "PlateX", new Vector3(0, LandY, LandZ + 1.55f), PlateXNode, true, glow);
            Kit("WallSconce", root, new Vector3(-2.1f, LandY + 2.1f, hz - 0.03f), 180f);
            Kit("WallSconce", root, new Vector3(2.1f, LandY + 2.1f, hz - 0.03f), 180f);
            Kit("L_Candle", root, new Vector3(-2.1f, LandY + 2.2f, hz - 0.7f));
            Kit("L_Candle", root, new Vector3(2.1f, LandY + 2.2f, hz - 0.7f));
            Kit("WindowRose", root, new Vector3(0, 7.25f, hz), 180f, 0.95f);
            Kit("Banner", root, new Vector3(-4.6f, 8.2f, hz), 180f, 2f);
            Kit("Banner", root, new Vector3(4.6f, 8.2f, hz), 180f, 2f);
            Kit("Amphora", root, new Vector3(-7.9f, LandY, hz - 0.9f), 30f);
            Kit("CandelabraFloor", root, new Vector3(7.6f, LandY, hz - 0.9f), 0f);
            Kit("L_Candelabra", root, new Vector3(7.2f, LandY + 1.9f, hz - 1.3f));

            // --- Mahzen nişi (doğu) + O plakası ---
            {
                float ax = hx + 0.5f + AlcD * 0.5f; // niş merkezi
                Deck(root, "AlcoveFloor", new Vector3(ax, -0.25f, AlcZ), AlcD, AlcW, 0.5f, K.cellarFloor, 1, 1);
                Deck(root, "AlcoveCeil", new Vector3(ax, AlcH + 0.25f, AlcZ), AlcD, AlcW, 0.5f, K.cellarBrick, 1, 1);
                Slab("AlcoveN", root, new Vector3(ax, AlcH * 0.5f, AlcZ + AlcW * 0.5f + 0.25f), new Vector3(AlcD, AlcH, 0.5f), K.cellarBrick, 2f);
                Slab("AlcoveS", root, new Vector3(ax, AlcH * 0.5f, AlcZ - AlcW * 0.5f - 0.25f), new Vector3(AlcD, AlcH, 0.5f), K.cellarBrick, 2f);
                Slab("AlcoveE", root, new Vector3(hx + 0.5f + AlcD + 0.25f, AlcH * 0.5f, AlcZ), new Vector3(0.5f, AlcH, AlcW + 1f), K.cellarBrick, 2f);
                ArchTrim(root, "AlcoveArch", new Vector3(hx, 0, AlcZ), Vector3.left, AlcW + 0.1f, AlcH + 1.5f, false);
                KitR("Door", root, new Vector3(hx + 0.5f + AlcD - 0.12f, 0, AlcZ), Quaternion.Euler(0, -90, 0), 0.92f);
                SymbolPlaque(root, new Vector3(hx + 0.5f + AlcD, 2.95f, AlcZ), Vector3.left, false, glow);
                Kit("Barrel", root, new Vector3(ax + 0.9f, 0, AlcZ + 1.15f), 40f, 0.9f);
                Kit("L_Lantern", root, new Vector3(ax + 0.9f, 0.9f, AlcZ + 1.15f));
                Kit("Bucket", root, new Vector3(ax + 1.0f, 0, AlcZ - 1.2f));
                KitR("Cobweb", root, new Vector3(hx + 0.55f, AlcH - 0.02f, AlcZ + AlcW * 0.5f - 0.03f), Quaternion.Euler(0, 0, -90), 0.9f);
                KitProbes(root, new Vector3(ax, AlcH * 0.5f, AlcZ), new Vector3(AlcD, AlcH, AlcW), 2, 2, 2);
            }
            var plateO = MakePlate(root, "PlateO", new Vector3(hx + 0.5f + 1.35f, 0, AlcZ), PlateONode, false, glow);
            Kit("CrateStack", root, new Vector3(hx - 0.9f, 0, AlcZ + 3.2f), -90f);
            Kit("Barrel", root, new Vector3(hx - 0.8f, 0, AlcZ - 2.6f), 15f);
            Kit("BarrelLying", root, new Vector3(hx - 1.0f, 0, AlcZ - 3.7f), 80f);

            // Kapı mantığı
            var gate = new GameObject("HallGate").AddComponent<HallGate>();
            gate.transform.SetParent(root, false);
            var gso = new SerializedObject(gate);
            gso.FindProperty("puzzleId").intValue = HallPuzzleId;
            gso.FindProperty("nodeId").intValue = HallGateNode;
            SetRef(gso, "plateX", plateX);
            SetRef(gso, "plateO", plateO);
            SetRefArray(gso, "glow", glow.ToArray());
            gso.ApplyModifiedPropertiesWithoutUndo();

            // --- Batı duvarı: yüksek vitraylar (ay huzmeleri), heykel nişi, çalışma masası ---
            foreach (float z in new[] { -6.2f, 0f, 5.6f })
                KitR("WindowLancet", root, new Vector3(-hx, 1.7f, z), Quaternion.Euler(0, 90, 0), 2f);
            ArchTrim(root, "StatueNiche", new Vector3(-hx, 0, -3.1f), Vector3.right, 2.3f, 4.4f, true);
            Slab("StatuePlinth", root, new Vector3(-hx + 0.75f, 0.35f, -3.1f), new Vector3(1.3f, 0.7f, 1.3f), K.castleStone, 1.5f);
            Kit("StatueAngel", root, new Vector3(-hx + 0.75f, 0.7f, -3.1f), 90f, 1.25f);
            Kit("CandelabraFloor", root, new Vector3(-hx + 0.8f, 0, -4.7f), 30f);
            Kit("L_Candelabra", root, new Vector3(-hx + 1.4f, 1.9f, -4.6f));
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, 4.2f, -4.2f), Quaternion.Euler(0, -90, -90), 1.4f);
            Kit("Table", root, new Vector3(-6.6f, 0, -8.6f), 90f);
            Kit("BookStack", root, new Vector3(-6.7f, 0.82f, -9.1f), 20f);
            Kit("Armillary", root, new Vector3(-6.6f, 0.82f, -8.5f), 0f);
            Kit("Candelabra", root, new Vector3(-6.75f, 0.82f, -7.9f), 80f);
            Kit("L_Candelabra", root, new Vector3(-6.0f, 1.8f, -8.0f));
            Kit("PaperPile", root, new Vector3(-6.45f, 0.82f, -8.95f), 60f);
            Letter(root, new Vector3(-6.45f, 0.83f, -9.5f), 75f, 0); // kâhyanın notu
            Kit("Chest", root, new Vector3(-7.9f, 0, -10.0f), 160f, 1.2f);
            Kit("PaperScatter", root, new Vector3(-5.2f, 0, -7.4f), 40f);
            Kit("PaperScatter", root, new Vector3(2.6f, 0, 1.2f), 200f);
            Kit("Amphora", root, new Vector3(-hx + 0.7f, 0, 2.6f), 70f);
            Kit("BrassCandleholder0", root, new Vector3(-hx + 0.8f, 0, 3.6f), 20f, 1.6f);
            Kit("Cabinet", root, new Vector3(-hx + 0.45f, 0, 8.6f), 90f);
            Books(root, new Vector3(-hx + 0.4f, 0, 10.0f), 90f);
            Kit("Stool", root, new Vector3(-5.6f, 0, -8.9f), 30f);

            // --- Doğu duvarı: şömine köşesi ---
            {
                float fx = hx, fz = -4.2f;
                var f = new GameObject("Fireplace").transform;
                f.SetParent(root, false);
                f.localPosition = new Vector3(fx, 0, fz);
                f.localRotation = Quaternion.Euler(0, -90, 0); // +Z odaya (batıya)
                foreach (int s in new[] { -1, 1 }) Slab("Jamb", f, new Vector3(s * 1.15f, 0.8f, 0.35f), new Vector3(0.45f, 1.6f, 0.7f), K.castleStone, 1.5f);
                Slab("Lintel", f, new Vector3(0, 1.8f, 0.35f), new Vector3(2.75f, 0.4f, 0.7f), K.castleStone, 1.5f, false);
                Slab("Mantel", f, new Vector3(0, 2.05f, 0.4f), new Vector3(3.1f, 0.1f, 0.9f), K.castleStone, 1.5f, false);
                Slab("Hood", f, new Vector3(0, 3.3f, 0.2f), new Vector3(2.2f, 2.4f, 0.4f), K.castleStone, 1.5f, false);
                Slab("Back", f, new Vector3(0, 0.8f, 0.03f), new Vector3(1.9f, 1.6f, 0.06f), voidMat, 1f, false);
                Slab("Hearth", f, new Vector3(0, 0.05f, 0.5f), new Vector3(2.3f, 0.1f, 1.0f), K.castleStone, 1.5f, false);
                for (int i = 0; i < 3; i++)
                {
                    var log = Prim("Log" + i, f, PrimitiveType.Cylinder, new Vector3(-0.35f + 0.35f * i, 0.18f + 0.05f * (i % 2), 0.35f), new Vector3(0.14f, 0.38f, 0.14f), K.furniture);
                    log.transform.localRotation = Quaternion.Euler(0, 20f * (i - 1), 90f);
                    var fl = MeshObj("Ember" + i, f, new Vector3(-0.3f + 0.3f * i, 0.24f, 0.36f), FlameMesh(), K.flame);
                    fl.transform.localScale = new Vector3(0.12f, 0.2f + 0.08f * (i % 2), 0.12f);
                    fl.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                Kit("CandleCluster", f, new Vector3(-1.05f, 2.1f, 0.45f));
                Kit("Candle", f, new Vector3(0.9f, 2.1f, 0.5f));
                Kit("Candle", f, new Vector3(1.2f, 2.1f, 0.4f));
                Kit("Vase", f, new Vector3(0.2f, 2.1f, 0.45f));
                Kit("L_Candelabra", f, new Vector3(0, 0.7f, 1.0f));
                Kit("L_Candle", f, new Vector3(0, 2.6f, 1.1f));
                KitR("PaintingKeep", f, new Vector3(0, 4.1f, 0.42f), Quaternion.identity, 2.2f);
            }
            Kit("Rug", root, new Vector3(6.0f, 0, -4.2f), 90f, 0.75f);
            Kit("Armchair", root, new Vector3(6.3f, 0, -2.5f), -150f);
            Kit("Armchair", root, new Vector3(6.2f, 0, -5.9f), -40f);
            Kit("Stool", root, new Vector3(7.0f, 0, -4.2f), 0f, 0.9f);
            Kit("Goblet0", root, new Vector3(7.0f, 0.45f, -4.2f));
            Kit("GrandfatherClock", root, new Vector3(hx - 0.4f, 0, -8.2f), -90f);
            Kit("Commode", root, new Vector3(hx - 0.45f, 0, -9.9f), -90f);
            Kit("Bust", root, new Vector3(hx - 0.5f, 0.95f, -9.9f), -90f);
            KitR("PaintingLandscape", root, new Vector3(hx - 0.04f, 2.6f, -9.2f), Quaternion.Euler(0, -90, 0), 1.6f);
            foreach (float z in new[] { -8.5f, 0.2f }) KitR("WindowLancet", root, new Vector3(hx, 4.9f, z), Quaternion.Euler(0, -90, 0), 1.1f);

            // --- Giriş (güney) ---
            Kit("Door", root, new Vector3(0, 0, -hz + 0.12f), 0f, 1.35f);
            ArchTrim(root, "EntryArch", new Vector3(0, 0, -hz), Vector3.forward, 3.4f, 4.9f, true);
            Kit("WallSconce", root, new Vector3(-2.7f, 2.4f, -hz + 0.03f));
            Kit("WallSconce", root, new Vector3(2.7f, 2.4f, -hz + 0.03f));
            Kit("L_Candle", root, new Vector3(-2.7f, 2.5f, -hz + 0.7f));
            Kit("L_Candle", root, new Vector3(2.7f, 2.5f, -hz + 0.7f));
            Kit("Banner", root, new Vector3(-5.6f, 7.6f, -hz), 0f, 2f);
            Kit("Banner", root, new Vector3(5.6f, 7.6f, -hz), 0f, 2f);
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, H - 0.4f, -hz + 0.03f), Quaternion.Euler(0, 180, 180), 2f);
            KitR("Cobweb", root, new Vector3(hx - 0.03f, H - 0.4f, hz - 0.03f), Quaternion.Euler(0, 0, 180), 2.2f);
            KitR("Cobweb", root, new Vector3(-hx + 0.03f, H - 0.4f, hz - 0.03f), Quaternion.Euler(0, 0, -90), 2f);

            // --- Orta: halı yolu, avizeler, sütun flamaları ---
            Kit("Rug", root, new Vector3(0, 0, -3.6f), 90f, 2.1f);
            Kit("ChandelierOrnate", root, new Vector3(0, 4.9f, -2.5f), 0f, 2.3f);
            Rod("ChandelierChain", root, new Vector3(0, 8.3f, -2.5f), new Vector3(0, H - 0.1f, -2.5f), 0.05f, K.iron);
            Kit("L_Chandelier", root, new Vector3(0, 4.6f, -2.5f));
            Kit("L_Chandelier", root, new Vector3(0, 6.2f, -2.5f));
            Kit("ChandelierIron", root, new Vector3(0, H - 0.45f, 5.2f), 0f, 1.5f);
            Kit("L_Chandelier", root, new Vector3(0, 5.6f, 5.2f));
            foreach (float x in new[] { -5.5f, 5.5f })
                foreach (float z in new[] { -7f, -1f })
                    KitR("Banner", root, new Vector3(x - Mathf.Sign(x) * 0.41f, 6.6f, z), Quaternion.Euler(0, x < 0 ? 90 : -90, 0), 1.5f);

            // --- Işık altyapısı ---
            KitProbes(root, new Vector3(0, H * 0.5f, 0), new Vector3(SX, H, SZ), 5, 4, 6,
                      new Bounds(new Vector3(0, LandY * 0.45f, StairZ0 + run * 0.5f), new Vector3(StairW + 0.6f, LandY * 0.9f, run + 0.4f)));
            KitReflection(root, new Vector3(0, 2.2f, -2f), new Vector3(SX, H, SZ));
            return root;
        }
    }
}
