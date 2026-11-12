using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun bulmaca 3 odası: sarnıç v3 (ref: Docs/Reference/sarnic2.png). Bazilika tipi: 19.5x18, 4x4 yuvarlak sütun ormanı,
    /// 25 yelken kubbe + kemerler, 3 oculus (ay huzmesi, damla), batı vana rıhtımı, kuzey savağı, kuzeydoğu kuru sekide bakımcı köşesi,
    /// suya yıkılmış sütun. Baked (KeepBaked), 2 realtime fener. Kök "Cistern": OWest yerel (-36,0,0).
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

        static CisternValve MakeValve(Transform parent, int index, Vector3 pos, CisternPuzzle cistern, float yaw = -90f)
        {
            var root = new GameObject("Valve" + index).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, yaw, 0); // varsayılan: yüz (-Z) doğuya; duvar yüzü yerel z = +0.1
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

        // Sarnıç v3 iç sınırları (Cistern yerel): doğu duvarı mahzen geçidine bağlı kalır (x 5.75), oda batıya ve kuzey-güneye büyür.
        const float CsXW = -13.75f, CsXE = 5.75f, CsZ = 9f, CsNicheX = -4f, CsColH = 4.6f, CsRise = 2.2f;

        /// <summary>Yelken kubbe (sail vault) hücresi: köşelerde üzengi, kenar ortası rise/2, tepe rise (eliptik), aşağı bakan.</summary>
        static Mesh SailMesh(float sx, float sz, float s, float rise, int n)
        {
            string key = "sail_" + F(sx) + "_" + F(sz) + "_" + F(s) + "_" + F(rise) + "_" + n;
            if (meshCache.TryGetValue(key, out var cached)) return cached;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    float u = 2f * i / n - 1f, w = 2f * j / n - 1f;
                    float y = s + rise * 0.5f * (Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) + Mathf.Sqrt(Mathf.Max(0f, 1f - w * w)));
                    var p = new Vector3(u * sx * 0.5f, y, w * sz * 0.5f);
                    v.Add(p);
                    uv.Add(new Vector2((p.x + Mathf.Sign(u) * (s + rise - y) * 0.5f) / 2f, (p.z + Mathf.Sign(w) * (s + rise - y) * 0.5f) / 2f));
                }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    int a = i * (n + 1) + j, b = a + 1, c = a + n + 1, d = c + 1;
                    Tri(tris, v, a, d, b, Vector3.down); Tri(tris, v, a, c, d, Vector3.down);
                }
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m);
            return SaveMesh(m, key);
        }

        /// <summary>Döner gövde, metre cinsinden UV (taş dokusu sütunda uzamasın). Profil (yarıçap, y) alttan üste.</summary>
        static Mesh MetricLathe(string name, Vector2[] prof, int seg, float tile)
        {
            if (meshCache.TryGetValue(name, out var cached)) return cached;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            int rows = prof.Length;
            float[] acc = new float[rows];
            for (int k = 1; k < rows; k++) acc[k] = acc[k - 1] + (prof[k] - prof[k - 1]).magnitude;
            float rMax = 0f; foreach (var q in prof) rMax = Mathf.Max(rMax, q.x);
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                for (int k = 0; k < rows; k++)
                {
                    v.Add(new Vector3(Mathf.Cos(a) * prof[k].x, prof[k].y, Mathf.Sin(a) * prof[k].x));
                    uv.Add(new Vector2(a * rMax / tile, acc[k] / tile));
                }
            }
            for (int i = 0; i < seg; i++)
                for (int k = 0; k < rows - 1; k++)
                {
                    int a0 = i * rows + k, a1 = a0 + 1, b0 = a0 + rows, b1 = b0 + 1;
                    var outward = new Vector3(v[a0].x + v[a1].x, 0.0001f, v[a0].z + v[a1].z);
                    Tri(tris, v, a0, a1, b1, outward); Tri(tris, v, a0, b1, b0, outward);
                }
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m);
            return SaveMesh(m, name);
        }

        static void BuildCistern(Transform roomO, out CisternPuzzle cistern, out CisternValve[] valves, out LiftDoor gate, out LiftDoor nextGate, out GameObject water)
        {
            var cis = new GameObject("Cistern").transform;
            cis.SetParent(roomO, false);
            cis.localPosition = new Vector3(-36f, 0, 0);
            const float XW = CsXW, XE = CsXE, HZ = CsZ, XC = (CsXW + CsXE) * 0.5f, spring = CsColH, rise = CsRise, H = CsColH + CsRise + 0.4f;
            const int NX = 5, NZ = 5;
            float bx = (XE - XW) / NX, bz = HZ * 2f / NZ;                       // bölme 3.9 x 3.6
            var wall = K.cisternWall; var stone = K.cisternWall; var brick = K.cellarBrick;
            var vaultM = KTex("K_CisternVault", "castle_wall_slates", new Color(0.52f, 0.56f, 0.56f), 0.3f, 1.2f);
            var moss = MakeMat("K_WetBand", new Color(0.07f, 0.08f, 0.06f)); moss.SetFloat("_Smoothness", 0.7f); // su izi bandı
            var lily = MakeMat("K_Lily", new Color(0.12f, 0.17f, 0.09f)); lily.SetFloat("_Smoothness", 0.5f);
            var chalk = MakeMat("K_Chalk", new Color(0.78f, 0.76f, 0.7f));
            var voidMat = MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f));
            var arch = new Opening(0f, 3f, 0f, 3f);
            var rnd = new System.Random(31);
            float ColX(int i) => XW + bx * (i + 1); // 4 sütun hattı (i 0..3)
            float ColZ(int j) => -HZ + bz * (j + 1);

            // --- Kabuk ---
            Deck(cis, "CisternFloor", new Vector3(XC, -0.25f, 0), XE - XW + 0.5f, HZ * 2 + 0.5f, 0.5f, K.cisternFloor, 4, 4);
            Wall(cis, "CisternWallE", new Vector3(XE + 0.25f, 0, 0), false, HZ * 2 + 0.5f, H, wall, 2f, arch);
            Wall(cis, "CisternWallW", new Vector3(XW - 0.25f, 0, 0), false, HZ * 2 + 0.5f, H, wall, 2f);
            Wall(cis, "CisternWallN", new Vector3(XC, 0, HZ + 0.25f), true, XE - XW + 0.5f, H, wall, 2f, new Opening(-0.5f - XC, 2.2f, 0.9f, 2.4f)); // savak ağzı
            Wall(cis, "CisternWallS", new Vector3(XC, 0, -HZ - 0.25f), true, XE - XW + 0.5f, H, wall, 2f, new Opening(CsNicheX - XC, 3f, 0f, 3f));
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", cis, new Vector3(XC, H + 0.4f, 0), new Vector3(XE - XW + 1.5f, 0.5f, HZ * 2 + 1.5f), stone, 2f, false), 0);
            // Yelken kubbeler (sıra başına tek ağ)
            for (int i = 0; i < NX; i++)
            {
                var row = new GameObject("VaultRow" + i).transform; row.SetParent(cis, false);
                for (int j = 0; j < NZ; j++)
                {
                    var c = new Vector3(XW + bx * (i + 0.5f), 0, -HZ + bz * (j + 0.5f));
                    MeshObj("Sail", row, c, SailMesh(bx + 0.02f, bz + 0.02f, spring, rise, 12), vaultM);
                }
                CombineChildren(row, "cistern_vault_row" + i, vaultM).isStatic = true;
            }
            // Kemerler: sütun hatları boyunca iki yönde (tek ağ)
            var ribs = new GameObject("Arches").transform; ribs.SetParent(cis, false);
            for (int i = 0; i <= NX; i++)
            {
                float x = XW + bx * i + (i == 0 ? 0.12f : i == NX ? -0.12f : 0f);
                for (int j = 0; j < NZ; j++) ArchRib(ribs, new Vector3(x, spring, -HZ + bz * j), new Vector3(x, spring, -HZ + bz * (j + 1)), rise * 0.5f, i == 0 || i == NX ? 0.25f : 0.42f, stone, 8);
            }
            for (int j = 0; j <= NZ; j++)
            {
                float z = -HZ + bz * j + (j == 0 ? 0.12f : j == NZ ? -0.12f : 0f);
                for (int i = 0; i < NX; i++) ArchRib(ribs, new Vector3(XW + bx * i, spring, z), new Vector3(XW + bx * (i + 1), spring, z), rise * 0.5f, j == 0 || j == NZ ? 0.25f : 0.42f, stone, 8);
            }
            CombineChildren(ribs, "cistern_arches", stone).isStatic = true;

            // Sütunlar (4x4): kaide + yosunlu taban + yuvarlak gövde + bilezik + başlık
            var colShaft = MetricLathe("cistern_shaft2", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0.4f, 0.12f), new Vector2(0.34f, 0.2f), new Vector2(0.33f, CsColH - 0.75f),
                new Vector2(0.38f, CsColH - 0.65f), new Vector2(0.38f, CsColH - 0.58f), new Vector2(0.33f, CsColH - 0.5f), new Vector2(0f, CsColH - 0.5f),
            }, 16, 1.6f);
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    var col = new GameObject("Column").transform;
                    col.SetParent(cis, false);
                    col.localPosition = new Vector3(ColX(i), 0, ColZ(j));
                    var bc = col.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, CsColH * 0.5f, 0); bc.size = new Vector3(0.72f, CsColH, 0.72f);
                    Slab("Base", col, new Vector3(0, 0.2f, 0), new Vector3(1.0f, 0.4f, 1.0f), stone, 1.2f, false);
                    MeshObj("Shaft", col, new Vector3(0, 0.38f, 0), colShaft, stone);
                    Slab("Cap", col, new Vector3(0, CsColH - 0.25f, 0), new Vector3(1.05f, 0.5f, 1.05f), stone, 1.2f, false);
                    Slab("Abacus", col, new Vector3(0, CsColH - 0.03f, 0), new Vector3(1.2f, 0.06f, 1.2f), stone, 1.2f, false);
                }
            // Duvar ayakları (sütun hatlarının duvara değdiği yerler)
            for (int i = 0; i < 4; i++)
                foreach (int s in new[] { -1, 1 })
                    Slab("Pilaster", cis, new Vector3(ColX(i), spring * 0.5f, s * (HZ - 0.14f)), new Vector3(0.6f, spring, 0.28f), stone, 1.2f);
            for (int j = 0; j < 4; j++)
                foreach (float x in new[] { XW + 0.14f, XE - 0.14f })
                    Slab("Pilaster", cis, new Vector3(x, spring * 0.5f, ColZ(j)), new Vector3(0.28f, spring, 0.6f), stone, 1.2f);

            // Yürüme yolları (suyun üstünde 0.1): batı vana rıhtımı, doğu kapısından orta geçit, güney kapısına kol, kuzeydoğu kuru seki
            Slab("QuayW", cis, new Vector3(XW + 1.2f, 0.05f, 0), new Vector3(2.4f, 0.1f, HZ * 2f), K.cisternFloor, 1.5f);
            Slab("QuayEdgeW", cis, new Vector3(XW + 2.42f, 0.06f, 0), new Vector3(0.12f, 0.12f, HZ * 2f), stone, 1f, false);
            Slab("Causeway", cis, new Vector3((XW + 2.4f + XE) * 0.5f, 0.05f, 0), new Vector3(XE - XW - 2.4f, 0.1f, 2.2f), K.cisternFloor, 1.5f);
            Slab("CausewayS", cis, new Vector3(CsNicheX, 0.05f, -HZ * 0.5f), new Vector3(2.2f, 0.1f, HZ - 1.1f), K.cisternFloor, 1.5f);
            Slab("Dais", cis, new Vector3(XE - 2.1f, 0.15f, HZ - 1.9f), new Vector3(4.2f, 0.3f, 3.8f), K.cisternFloor, 1.5f);
            Slab("DaisStep", cis, new Vector3(XE - 2.1f, 0.075f, HZ - 4.0f), new Vector3(1.6f, 0.15f, 0.45f), stone, 1f);

            // Doğu geçidi (mahzen nişine) + güney nişi (bulmaca 4 geçidi)
            Deck(cis, "CisternCorridorFloor", new Vector3(7.1f, -0.25f, 0), 2.3f, 3.5f, 0.5f, K.cisternFloor, 1, 1);
            Deck(cis, "CisternCorridorCeil", new Vector3(7.1f, 3.25f, 0), 2.3f, 3.5f, 0.5f, brick, 1, 1);
            Wall(cis, "CisternCorridorN", new Vector3(7.1f, 0, 1.75f), true, 2.3f, 3f, wall, 2f);
            Wall(cis, "CisternCorridorS", new Vector3(7.1f, 0, -1.75f), true, 2.3f, 3f, wall, 2f);
            float nz = -HZ - 1.4f;
            Deck(cis, "CisternNicheFloor", new Vector3(CsNicheX, -0.25f, nz), 3.5f, 2.3f, 0.5f, K.cisternFloor, 1, 1);
            Deck(cis, "CisternNicheCeil", new Vector3(CsNicheX, 3.25f, nz), 3.5f, 2.3f, 0.5f, brick, 1, 1);
            Wall(cis, "CisternNicheS", new Vector3(CsNicheX, 0, nz - 1.1f), true, 3.5f, 3f, wall, 2f);
            Wall(cis, "CisternNicheE", new Vector3(CsNicheX + 1.75f, 0, nz), false, 2.3f, 3f, wall, 2f);
            Wall(cis, "CisternNicheW", new Vector3(CsNicheX - 1.75f, 0, nz), false, 2.3f, 3f, wall, 2f);
            Slab("BakeBlock", cis, new Vector3(8.2f, 1.5f, 0), new Vector3(0.1f, 3f, 3.5f), stone, 1f, false).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            foreach (int s in new[] { -1, 1 })
            {
                Slab("GateJambE", cis, new Vector3(XE - 0.06f, 1.5f, s * 1.64f), new Vector3(0.14f, 3f, 0.28f), stone, 1.2f, false);
                Slab("GateJambS", cis, new Vector3(CsNicheX + s * 1.64f, 1.5f, -HZ + 0.06f), new Vector3(0.28f, 3f, 0.14f), stone, 1.2f, false);
            }
            Slab("GateLintelE", cis, new Vector3(XE - 0.06f, 3.16f, 0), new Vector3(0.14f, 0.32f, 3.56f), stone, 1.2f, false);
            Slab("GateLintelS", cis, new Vector3(CsNicheX, 3.16f, -HZ + 0.06f), new Vector3(3.56f, 0.32f, 0.14f), stone, 1.2f, false);
            var gaE = new GameObject("GateAnchorE").transform; gaE.SetParent(cis, false); gaE.localPosition = new Vector3(XE - 1.1f, 1.5f, 0);
            var gaS = new GameObject("GateAnchorS").transform; gaS.SetParent(cis, false); gaS.localPosition = new Vector3(CsNicheX, 1.5f, -HZ + 1.2f);
            gate = BarGate("CisternGate", cis, new Vector3(7.1f, 1.5f, 0), false, gaE);
            nextGate = BarGate("CisternNextGate", cis, new Vector3(CsNicheX, 1.5f, -HZ - 0.25f), true, gaS);

            // --- Su (seviyeyle yükselen; çocuklar birlikte yükselir) ---
            water = Slab("CisternWater", cis, new Vector3(XC, 0.02f, 0), new Vector3(XE - XW, 0.04f, HZ * 2f), MakeMat("K_CisternWater", new Color(0.1f, 0.2f, 0.2f)), 1f, false);
            water.GetComponent<Renderer>().sharedMaterial.SetFloat("_Smoothness", 0.88f);
            water.GetComponent<Renderer>().sharedMaterial.SetFloat("_Metallic", 0.1f);
            water.isStatic = false;
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            water.GetComponent<Renderer>().sharedMaterial.color = new Color(0.1f, 0.19f, 0.19f);
            {
                var flo = water.transform;
                Transform Boat(string name, Vector3 p, float yaw, float tilt)
                {
                    var boat = new GameObject(name).transform;
                    boat.SetParent(flo, false);
                    boat.localPosition = p - new Vector3(XC, 0, 0);
                    boat.localRotation = Quaternion.Euler(tilt, yaw, tilt * 0.5f);
                    Slab("Hull", boat, new Vector3(0, -0.06f, 0), new Vector3(0.8f, 0.06f, 2.2f), K.beam, 1f, false);
                    foreach (int s in new[] { -1, 1 })
                    {
                        Slab("Side", boat, new Vector3(s * 0.45f, 0.08f, 0), new Vector3(0.06f, 0.32f, 2.3f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, -s * 14f);
                        Slab("Gunwale", boat, new Vector3(s * 0.49f, 0.24f, 0), new Vector3(0.08f, 0.04f, 2.32f), K.furniture, 1f, false);
                        Slab("End", boat, new Vector3(0, 0.08f, s * 1.15f), new Vector3(0.95f, 0.32f, 0.06f), K.beam, 1f, false);
                        Slab("Seat", boat, new Vector3(0, 0.14f, s * 0.45f), new Vector3(0.86f, 0.04f, 0.25f), K.furniture, 1f, false);
                    }
                    return boat;
                }
                var b1 = Boat("Boat", new Vector3(ColX(2) + 1.0f, -0.02f, ColZ(2) + 0.9f), 75f, 0f);
                Rod("Oar", b1, new Vector3(-0.3f, 0.2f, -0.8f), new Vector3(0.25f, 0.18f, 0.9f), 0.04f, K.furniture);
                Kit("Lantern", b1, new Vector3(0.1f, 0.12f, 0.5f), 40f);           // Detay: sönmüş fener (bakıcı nerede?)
                Kit("Bucket2", b1, new Vector3(-0.15f, 0.0f, -0.4f), 10f);
                Boat("SunkBoat", new Vector3(ColX(0) - 1.6f, -0.32f, ColZ(0) - 0.4f), -30f, 18f); // yarı batık kayık
                var wm = water.GetComponent<Renderer>().sharedMaterial;
                Slab("WaterCorridor", flo, new Vector3(6.975f - XC, 0, 0), new Vector3(2.5f, 0.04f, 3.5f), wm, 1f, false);
                Slab("WaterNiche", flo, new Vector3(CsNicheX - XC, 0, nz + 0.15f), new Vector3(3.5f, 0.04f, 2.5f), wm, 1f, false);
                var deb = new GameObject("Debris").transform;
                deb.SetParent(flo, false);
                deb.localPosition = new Vector3(-XC, 0, 0);
                for (int k = 0; k < 9; k++)
                {
                    var p = new Vector3(XW + 3f + (float)rnd.NextDouble() * (XE - XW - 5f), 0.01f, -HZ + 1f + (float)rnd.NextDouble() * (HZ * 2f - 2f));
                    if (Mathf.Abs(p.z) < 1.4f) p.z += 2.2f;
                    Slab("Plank", deb, p, new Vector3(0.8f + (float)rnd.NextDouble() * 0.6f, 0.04f, 0.22f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0);
                }
                Kit("BarrelLying", deb, new Vector3(-6.6f, -0.22f, -6.2f), 30f);
                Kit("BarrelLying", deb, new Vector3(1.0f, -0.25f, 6.4f), 100f);
                Kit("Crate", deb, new Vector3(-8.4f, -0.3f, 3.1f), 25f, 0.8f);
                for (int k = 0; k < 22; k++) // nilüfer yaprakları (duvar diplerinde kümeler)
                {
                    var p = new Vector3(XW + 2.7f + (float)rnd.NextDouble() * 3f, 0.025f, (rnd.NextDouble() < 0.5 ? -1f : 1f) * (2.5f + (float)rnd.NextDouble() * 5.5f));
                    if (k > 11) p = new Vector3(XE - 4.5f + (float)rnd.NextDouble() * 3.5f, 0.025f, -HZ + 0.8f + (float)rnd.NextDouble() * 3f);
                    Prim("Lily", deb, PrimitiveType.Cylinder, p, new Vector3(0.22f + (float)rnd.NextDouble() * 0.15f, 0.003f, 0.2f + (float)rnd.NextDouble() * 0.12f), lily).transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0);
                }
                foreach (var t in flo.GetComponentsInChildren<Transform>(true))
                {
                    if (t == flo) continue;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    var c = t.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
                }
            }
            // Bağlama halatı: sütuna sarılı, kayığa iner
            for (int i = 0; i < 3; i++)
                MeshObj("MooringCoil", cis, new Vector3(ColX(2), 0.72f + i * 0.06f, ColZ(2)), RingMesh(0.42f, 0.045f, 0.05f), K.beam);
            Rod("MooringRope", cis, new Vector3(ColX(2) + 0.3f, 0.76f, ColZ(2) + 0.3f), new Vector3(ColX(2) + 0.9f, 0.4f, ColZ(2) + 0.8f), 0.035f, K.beam);

            // --- Kahraman: suya yıkılmış sütun ---
            {
                // Yıkılmış sütun: kırık kaide yerinde, gövde tamburları ve başlık suya devrilmiş (girişten ve geçitten görünür)
                var fc = new GameObject("FallenColumn").transform; fc.SetParent(cis, false); fc.localPosition = new Vector3(CsNicheX, 0, 3.6f);
                Slab("BrokenBase", fc, new Vector3(-1.2f, 0.2f, -0.2f), new Vector3(1.0f, 0.4f, 1.0f), stone, 1.2f);
                var stump = MeshObj("Stump", fc, new Vector3(-1.2f, 0.38f, -0.2f), colShaft, stone); stump.transform.localScale = new Vector3(1f, 0.26f, 1f);
                for (int k = 0; k < 3; k++)
                {
                    var drum = MeshObj("Drum", fc, new Vector3(-0.3f + k * 1.15f, 0.18f - k * 0.05f, 0.1f + k * 0.35f), colShaft, stone);
                    drum.transform.localScale = new Vector3(1f, 0.25f, 1f);
                    drum.transform.localRotation = Quaternion.Euler(0, 15f + k * 9f, 90f + k * 4f);
                }
                var capt = Slab("FallenCap", fc, new Vector3(3.3f, 0.2f, 1.4f), new Vector3(1.05f, 0.5f, 1.05f), stone, 1.2f, false);
                capt.transform.localRotation = Quaternion.Euler(18, 30, 12);
            }

            // Tavan açıklıkları: ay huzmesi + baked spot (3 bölme tepesinde)
            {
                var glow = GlowMat("K_Oculus", Pal.Moon * 0.4f, Pal.Moon * 1.1f);
                foreach (var o in new[] { new Vector2(XW + bx * 1.5f, -HZ + bz * 3.5f), new Vector2(XW + bx * 3.5f, -HZ + bz * 1.5f), new Vector2(XW + bx * 2.5f, -HZ + bz * 4.5f) })
                {
                    float cy = spring + rise - 0.15f, L = cy - 0.1f;
                    var oc = new GameObject("Oculus").transform;
                    oc.SetParent(cis, false);
                    oc.localPosition = new Vector3(o.x, 0, o.y);
                    MeshObj("Ring", oc, new Vector3(0, cy, 0), RingMesh(0.5f, 0.1f, 0.16f), stone);
                    Prim("Sky", oc, PrimitiveType.Cylinder, new Vector3(0, cy + 0.06f, 0), new Vector3(0.9f, 0.01f, 0.9f), glow);
                    QuadObj("Shaft", oc, new Vector3(0, cy - L * 0.5f, 0), new Vector2(0.9f, L), K.shaft, Quaternion.LookRotation(Vector3.right, Vector3.up));
                    QuadObj("ShaftB", oc, new Vector3(0, cy - L * 0.5f, 0), new Vector2(0.7f, L), K.shaft, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                    var ml = LightGo("OculusMoon", LightType.Spot, Pal.Moon, 16f, 10f, true);
                    ml.transform.SetParent(oc, false);
                    ml.transform.localPosition = new Vector3(0, cy - 0.1f, 0);
                    ml.transform.localRotation = Quaternion.LookRotation(Vector3.down);
                    ml.GetComponent<Light>().spotAngle = 34f; ml.GetComponent<Light>().innerSpotAngle = 18f;
                    for (int k = 0; k < 3; k++) // damlayan su ipleri
                        QuadObj("Drip", oc, new Vector3(-0.3f + k * 0.28f, cy - 1.2f - k * 0.3f, 0.1f * k), new Vector2(0.025f, 2.2f + k * 0.5f), K.shaft, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                }
            }

            // --- Bulmaca: batı duvarında 4 vana ---
            cistern = new GameObject("Puzzle3_Cistern").AddComponent<CisternPuzzle>();
            cistern.transform.SetParent(cis, false);
            valves = new CisternValve[4];
            // Vanalar girişte görünmesin: 4 farklı sütunun batı yüzünde (doğu kapısından sütun gövdesi saklar), sütun ormanında aranır
            var vSpots = new[] { (3, 3), (1, 0), (2, 2), (0, 1) };
            for (int i = 0; i < 4; i++)
            {
                var (ci, cj) = vSpots[i];
                var vp = new Vector3(ColX(ci) - 0.43f, 0, ColZ(cj));
                valves[i] = MakeValve(cis, i, vp, cistern, 90f); // yüz batıya, sırt sütuna
            }

            // --- Küçük ayrıntılar (probe ışıklı) ---
            var det = new GameObject("Details").transform;
            det.SetParent(cis, false);
            foreach (float z in new[] { -4f, 4f }) // batı duvarında sahte (sökülmüş) vana kalıntıları: asıl vanalar sütunlarda
            {
                Prim("DeadPipe", det, PrimitiveType.Cylinder, new Vector3(XW + 0.25f, 1.6f, z), new Vector3(0.2f, 1.6f, 0.2f), K.iron);
                MeshObj("DeadFlange", det, new Vector3(XW + 0.25f, 1.25f, z), RingMesh(0.17f, 0.08f, 0.07f), K.iron);
                Prim("DeadStem", det, PrimitiveType.Cylinder, new Vector3(XW + 0.42f, 1.25f, z), new Vector3(0.07f, 0.1f, 0.07f), K.iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
                MeshObj("BrokenWheel", det, new Vector3(XW + 1.0f, 0.13f, z + 0.4f), RingMesh(0.42f, 0.06f, 0.05f), K.bronze).transform.localRotation = Quaternion.Euler(4, 30, 0);
            }
            void Chain(float x, float z, float top, float bottom)
            {
                Rod("Chain", det, new Vector3(x, top, z), new Vector3(x, bottom, z), 0.022f, K.iron);
                Rod("HookA", det, new Vector3(x, bottom, z), new Vector3(x + 0.08f, bottom - 0.13f, z), 0.02f, K.iron);
                Rod("HookB", det, new Vector3(x + 0.08f, bottom - 0.13f, z), new Vector3(x - 0.02f, bottom - 0.2f, z), 0.02f, K.iron);
            }
            Vector3 Bracket(Vector3 wallPt, Vector3 inward)
            {
                var tip = wallPt + inward * 0.42f;
                Rod("LanternArm", det, wallPt, tip, 0.03f, K.iron);
                Rod("LanternBrace", det, wallPt + Vector3.down * 0.3f, wallPt + inward * 0.3f, 0.022f, K.iron);
                Rod("LanternHook", det, tip - inward * 0.02f, tip - inward * 0.02f + Vector3.down * 0.12f, 0.015f, K.iron);
                return tip - inward * 0.02f + Vector3.down * 0.54f;
            }
            void BakedLantern(Vector3 p, float yaw)
            {
                var go = Kit("Lantern", cis, p, yaw);
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, GameObjectUtility.GetStaticEditorFlags(t.gameObject) & ~StaticEditorFlags.ContributeGI);
                Kit("L_Candelabra", cis, p + new Vector3(0, 0.2f, 0));
            }
            for (int k = 0; k < 8; k++) // tavandan sarkan zincirler (kancalı)
                Chain(XW + bx * (0.5f + (k * 3 % NX)), -HZ + bz * (0.5f + (k * 2 % NZ)) + 0.6f, spring + rise - 0.3f, 2.6f + (k % 3) * 0.4f);
            // Seviye cetveli (batı duvarı): çentik aralığı = su seviyesi adımı
            Slab("GaugeBoard", det, new Vector3(XW + 0.02f, 1.2f, -2.65f), new Vector3(0.04f, 2.4f, 0.16f), K.furniture, 1f, false);
            for (int i = 1; i <= 10; i++)
                Slab("GaugeNotch", det, new Vector3(XW + 0.045f, 0.02f + i * CisternPuzzle.LevelHeight, -2.65f), new Vector3(0.012f, 0.02f, i % 2 == 0 ? 0.16f : 0.09f), K.brass, 1f, false);
            // Detay: sütunda tebeşirle eski su seviyesi çizgileri (önceki taşkınlar)
            foreach (var (y, w) in new[] { (1.1f, 0.3f), (1.45f, 0.22f), (1.9f, 0.36f), (2.3f, 0.18f) })
                Slab("FloodMark", det, new Vector3(ColX(3) - 0.345f, y, ColZ(1)), new Vector3(0.006f, 0.02f, w), chalk, 1f, false);
            // Halat kangalları + duvar askısı
            for (int i = 0; i < 3; i++)
                MeshObj("RopeCoil", det, new Vector3(XE - 1.6f, 0.62f + i * 0.045f, -HZ + 0.9f), RingMesh(0.2f - i * 0.012f, 0.05f, 0.045f), K.beam);
            MeshObj("RopeHang", det, new Vector3(XE - 0.01f, 1.55f, -3.4f), RingMesh(0.22f, 0.05f, 0.06f), K.beam).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Rod("RopePeg", det, new Vector3(XE, 1.76f, -3.4f), new Vector3(XE - 0.15f, 1.8f, -3.4f), 0.03f, K.iron);
            // Kuzey savağı: tahta kapak + çark + akan su
            {
                float sx = -0.5f;
                Slab("SluiceFrameL", det, new Vector3(sx - 1.2f, 1.6f, HZ - 0.05f), new Vector3(0.2f, 1.8f, 0.2f), K.beam, 1f, false);
                Slab("SluiceFrameR", det, new Vector3(sx + 1.2f, 1.6f, HZ - 0.05f), new Vector3(0.2f, 1.8f, 0.2f), K.beam, 1f, false);
                Slab("SluiceTop", det, new Vector3(sx, 2.55f, HZ - 0.05f), new Vector3(2.6f, 0.22f, 0.22f), K.beam, 1f, false);
                Slab("SluiceGate", det, new Vector3(sx, 1.95f, HZ + 0.05f), new Vector3(2.2f, 1.0f, 0.08f), K.furniture, 1f, false);
                Prim("SluiceVoid", det, PrimitiveType.Cube, new Vector3(sx, 1.2f, HZ + 0.2f), new Vector3(2.2f, 0.6f, 0.1f), voidMat);
                MeshObj("SluiceWheel", det, new Vector3(sx + 1.55f, 2.1f, HZ - 0.12f), RingMesh(0.32f, 0.05f, 0.05f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                Rod("SluiceRod", det, new Vector3(sx, 2.45f, HZ - 0.12f), new Vector3(sx, 3.3f, HZ - 0.12f), 0.06f, K.iron);
                QuadObj("Spill", det, new Vector3(sx, 0.55f, HZ - 0.3f), new Vector2(2.0f, 1.1f), K.shaft, Quaternion.Euler(30, 180, 0));
            }
            // Yan duvar boruları (vana duvarı yanında paslı demir hat)
            Rod("PipeRun", det, new Vector3(XW + 0.25f, 3.6f, -7.5f), new Vector3(XW + 0.25f, 3.6f, 7.5f), 0.16f, K.iron);
            foreach (float z in new[] { -6f, -2.65f, 2.65f, 6f }) MeshObj("PipeClamp", det, new Vector3(XW + 0.25f, 3.6f, z), RingMesh(0.11f, 0.04f, 0.08f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
            foreach (int s in new[] { -1, 1 }) Rod("PipeDown", det, new Vector3(XW + 0.25f, 3.6f, s * 7.5f), new Vector3(XW + 0.25f, 0.1f, s * 7.5f), 0.16f, K.iron);
            var da = new GameObject("WallDetailAnchor").transform; da.SetParent(det, false); da.localPosition = new Vector3(XW + 1.2f, 1.5f, -2.65f);
            ProbeLit(det.gameObject);
            foreach (var r in det.GetComponentsInChildren<Renderer>()) if (r.name.StartsWith("Gauge")) r.probeAnchor = da;

            // --- Işık: vana duvarında 2 realtime fener, gerisi baked ---
            foreach (int s in new[] { -1, 1 })
                Kit("L_Lantern", cis, Bracket(new Vector3(XW, 2.4f, s * 2.65f), Vector3.right), s * 25f);
            foreach (int s in new[] { -1, 1 })
            {
                var lp = Bracket(new Vector3(XW, 2.4f, s * 6.5f), Vector3.right);
                Kit("Lantern", cis, lp, s * 10f);
            }
            {
                foreach (var p in new[] { new Vector3(ColX(0) + 1.95f, 2.6f, ColZ(1) - 1.8f), new Vector3(ColX(2) + 1.95f, 2.6f, ColZ(2) + 1.8f), new Vector3(ColX(1) + 1.95f, 2.6f, ColZ(3) + 1.8f) })
                {   // sıcak dolgu (fenerlerin çevresi)
                    var w = LightGo("WarmFill", LightType.Point, Pal.Lantern, 2.4f, 8f, true);
                    w.transform.SetParent(cis, false); w.transform.localPosition = p;
                }
                foreach (var (i, j) in new[] { (1, 1), (2, 2), (0, 3), (3, 0) }) // sütunlardan sarkan fenerler
                {
                    var p = new Vector3(ColX(i) + 0.62f, 0, ColZ(j));
                    Rod("ColArm", det, new Vector3(ColX(i) + 0.33f, 2.9f, ColZ(j)), new Vector3(p.x, 2.9f, p.z), 0.03f, K.iron);
                    BakedLantern(new Vector3(p.x, 2.3f, p.z), 0f);
                }
                foreach (var (i, j) in new[] { (3, 3), (1, 0), (2, 2), (0, 1) }) // vana sütunlarının arka yüzünde loş fener (girişten görünmez)
                {
                    var p = new Vector3(ColX(i) - 0.62f, 0, ColZ(j) + 0.55f);
                    Rod("ColArm", det, new Vector3(ColX(i) - 0.33f, 2.9f, ColZ(j) + 0.3f), new Vector3(p.x, 2.9f, p.z), 0.03f, K.iron);
                    BakedLantern(new Vector3(p.x, 2.3f, p.z), 0f);
                }
                BakedLantern(Bracket(new Vector3(XE, 2.4f, 3.4f), Vector3.left), 40f);
                BakedLantern(Bracket(new Vector3(CsNicheX + 2.3f, 2.4f, -HZ), Vector3.forward), -30f);
                var teal = Color.Lerp(Pal.Moon, Pal.Vitray[1], 0.55f); // soğuk dolgu (sarnıç havası)
                foreach (var p in new[] { new Vector3(XC + 3f, 2f, 4.5f), new Vector3(XC - 3f, 2f, -4.5f), new Vector3(XC - 5f, 3.5f, 5f), new Vector3(XC + 5f, 3.5f, -5f) })
                {
                    var f = LightGo("TealFill", LightType.Point, teal, 4f, 13f, true);
                    f.transform.SetParent(cis, false);
                    f.transform.localPosition = p;
                }
            }

            // --- Kuzeydoğu kuru seki: bakımcının köşesi (masa, notlar, alet, sandık, yatak) ---
            {
                float dy = 0.3f;
                var tp = new Vector3(XE - 1.4f, dy, HZ - 1.0f);
                const float th = 0.82f;
                Kit("Table", cis, tp, 0f);
                Prim("CandleDish", cis, PrimitiveType.Cylinder, tp + new Vector3(0.5f, th + 0.02f, 0.1f), new Vector3(0.11f, 0.008f, 0.11f), K.brass);
                KCandle(cis, tp + new Vector3(0.5f, th + 0.028f, 0.1f), 0.18f, 0.02f);
                Kit("L_Candle", cis, tp + new Vector3(0.3f, th + 0.5f, -0.4f));
                Kit("PaperA", cis, tp + new Vector3(-0.25f, th + 0.004f, -0.1f), 20f);
                Kit("PaperC", cis, tp + new Vector3(0f, th + 0.006f, -0.2f), -35f);
                Kit("Wrench", cis, tp + new Vector3(-0.45f, th + 0.004f, 0.15f), 30f);
                Kit("Hammer", cis, tp + new Vector3(0.15f, th + 0.004f, 0.2f), -60f);
                Kit("Bottle1", cis, tp + new Vector3(0.35f, th, -0.2f), 50f);
                Kit("Goblet0", cis, tp + new Vector3(0.2f, th + 0.012f, -0.05f));
                KitR("PaperB", cis, new Vector3(tp.x, dy + 1.55f, HZ - 0.03f), Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(0, 6, 0));
                Kit("ChairWood", cis, tp + new Vector3(0, 0, -0.8f), 10f);
                Kit("PrisonCot", cis, new Vector3(XE - 0.5f, dy, HZ - 3.0f), 0f, 0.85f);
                Kit("Chest", cis, new Vector3(XE - 3.4f, dy, HZ - 0.55f), 180f);
                Kit("Ladder", cis, new Vector3(XE - 4.0f, dy, HZ - 0.4f), 0f);
                Kit("Bucket", cis, new Vector3(XE - 2.6f, dy, HZ - 2.9f));
                Kit("Pot", cis, new Vector3(XE - 3.1f, dy, HZ - 2.6f));
                Kit("Broom", cis, new Vector3(XE - 0.25f, dy, HZ - 1.9f), -70f);
                Kit("Basket", cis, new Vector3(XE - 3.6f, dy, HZ - 3.3f), 30f);
            }
            // --- Güneydoğu: erzak istifi (rıhtım ucunda) ---
            Slab("DockSE", cis, new Vector3(XE - 1.6f, 0.1f, -HZ + 1.6f), new Vector3(3.2f, 0.2f, 3.2f), K.cisternFloor, 1.5f);
            Kit("Barrel", cis, new Vector3(XE - 0.6f, 0.2f, -HZ + 0.6f), 25f);
            Kit("Barrel", cis, new Vector3(XE - 0.65f, 0.2f, -HZ + 1.6f), 130f);
            Kit("Crate", cis, new Vector3(XE - 1.8f, 0.2f, -HZ + 0.6f), 15f);
            Kit("CrateStack", cis, new Vector3(XE - 2.9f, 0.2f, -HZ + 0.65f), 180f);
            Kit("CrateLong", cis, new Vector3(XE - 1.9f, 0.2f, -HZ + 2.2f), 70f);
            Kit("Mallet", cis, new Vector3(XE - 1.85f, 0.62f, -HZ + 2.2f), 110f);
            Kit("Lantern", cis, new Vector3(XE - 1.8f, 0.8f, -HZ + 0.6f), 20f);
            // --- Vana rıhtımı köşeleri + boyunca ---
            Kit("Barrel", cis, new Vector3(XW + 0.6f, 0.1f, HZ - 0.55f), 70f);
            Kit("Pot", cis, new Vector3(XW + 1.3f, 0.1f, HZ - 0.5f));
            Kit("Bucket", cis, new Vector3(XW + 0.6f, 0.1f, -HZ + 0.55f));
            Kit("Amphora", cis, new Vector3(XW + 1.4f, 0.1f, -HZ + 0.5f), 200f, 0.8f);
            Kit("Crate", cis, new Vector3(XW + 0.6f, 0.1f, 7.2f), 10f);
            Kit("Bucket2", cis, new Vector3(XW + 1.8f, 0.1f, -6.2f), 40f);
            Kit("Wrench", cis, new Vector3(XW + 1.6f, 0.11f, 2.3f), 80f);
            Kit("Rat", cis, new Vector3(XW + 1.9f, 0.1f, 5.6f), 220f);
            Kit("Spade", cis, new Vector3(XW + 0.4f, 0.1f, -7.3f), 10f);
            // Ağlar
            KitR("Cobweb", cis, new Vector3(XE - 0.03f, 0, -HZ + 0.03f), Quaternion.Euler(0, 180, 0), 1.2f);
            KitR("Cobweb", cis, new Vector3(XW + 0.03f, 0.1f, HZ - 0.03f), Quaternion.identity, 1.0f);
            KitR("Cobweb", cis, new Vector3(XW + 0.03f, spring - 0.2f, -HZ + 0.03f), Quaternion.Euler(0, 90, 180), 1.4f);
            KitR("Cobweb", cis, new Vector3(XE - 0.03f, spring - 0.2f, HZ - 0.03f), Quaternion.Euler(0, -90, 180), 1.4f);
            KitR("Cobweb", cis, new Vector3(ColX(0) + 0.4f, spring - 0.3f, ColZ(2) + 0.4f), Quaternion.Euler(0, 45, 180), 1.0f);
            KitR("Cobweb", cis, new Vector3(ColX(3) - 0.4f, spring - 0.3f, ColZ(0) - 0.4f), Quaternion.Euler(0, 225, 180), 1.0f);

            var skips = new List<Bounds>();
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) skips.Add(new Bounds(new Vector3(ColX(i), 2.3f, ColZ(j)), new Vector3(1.1f, 4.6f, 1.1f)));
            KitProbes(cis, new Vector3(XC, 2.9f, 0), new Vector3(XE - XW, 5.6f, HZ * 2f), 7, 3, 7, skips.ToArray());
            KitReflection(cis, new Vector3(XC, 2.5f, 0), new Vector3(XE - XW, 6f, HZ * 2f));
            KeepBaked(cis);
        }
    }
}
