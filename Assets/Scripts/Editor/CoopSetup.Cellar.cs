using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun bulmaca 2 odası: şarap mahzeni v2 (ref: Docs/Reference/mahzen.png). 10x14, 2x3 çapraz tonoz hücresi + kaburgalar, orta sütun çifti,
    /// ıslak zemin, kemerli şişe rafı nişleri, şarap presi (kahraman eşya), musluklu fıçılar. Batı duvarı kuzey hücresindeki sembol paneli
    /// (6 madalyon: sembol + Roma rakamı) bulmacanın okuma yüzeyi; girişten sütun arkasında kalır.
    /// Bulmaca karanlık ister: oda bake DIŞI, fenerler sönük; alevler + 2 realtime ışık + ağlar "CellarLit" altında, çözülünce açılır.
    /// </summary>
    public static partial class CoopSetup
    {
        /// <summary>z ekseninde yarım elips kesitli, x boyunca uzanan tonoz şeridi (parçalı levhalar).</summary>
        static void VaultStrip(Transform root, string name, float xc, float xLen, float R, float spring, float rise, float thick, Material mat, int segs, float tile)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(root, false);
            for (int i = 0; i < segs; i++)
            {
                float t0 = Mathf.PI * (i / (float)segs - 0.5f), t1 = Mathf.PI * ((i + 1) / (float)segs - 0.5f);
                var a = new Vector2(R * Mathf.Sin(t0), spring + rise * Mathf.Cos(t0));
                var b = new Vector2(R * Mathf.Sin(t1), spring + rise * Mathf.Cos(t1));
                var d = b - a;
                float len = Mathf.Round((d.magnitude + 0.04f) * 100f) / 100f;
                var s = Slab(name + i, holder, new Vector3(xc, (a.y + b.y) * 0.5f, (a.x + b.x) * 0.5f), new Vector3(xLen, thick, len), mat, tile, false);
                s.transform.localRotation = Quaternion.Euler(Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg, 0, 0);
            }
        }

        /// <summary>Sönük fener (cam ışımaz); alevi lit grubuna eklenir.</summary>
        static void DarkLantern(Transform root, Transform lit, Vector3 pos, float yaw = 0f)
        {
            var go = Kit("Lantern", root, pos, yaw);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) if (ms[i] == K.lanternGlass) ms[i] = K.clearGlass;
                r.sharedMaterials = ms;
            }
            CellarFlame(lit, pos + new Vector3(0, 0.13f, 0), 0.05f);
        }

        static void CellarFlame(Transform lit, Vector3 pos, float r)
        {
            var fl = MeshObj("Flame", lit, pos, FlameMesh(), K.flame);
            fl.transform.localScale = new Vector3(r, r * 2.2f, r);
            fl.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Çapraz (groin) tonoz hücresi: iki beşik tonozun birleşimi, merkez (0,0), aşağı bakan. Köşelerde üzengi, kenar ortalarında tepe.</summary>
        static Mesh GroinMesh(float sx, float sz, float s, float r, int n)
        {
            string key = "groin_" + F(sx) + "_" + F(sz) + "_" + F(s) + "_" + F(r) + "_" + n;
            if (meshCache.TryGetValue(key, out var cached)) return cached;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    float u = 2f * i / n - 1f, w = 2f * j / n - 1f;
                    float y = s + r * Mathf.Max(Mathf.Sqrt(Mathf.Max(0f, 1f - w * w)), Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
                    var p = new Vector3(u * sx * 0.5f, y, w * sz * 0.5f);
                    v.Add(p);
                    // UV: yatay konum + yükseklik (dik kenarlarda tuğla uzamasın)
                    bool alongX = Mathf.Abs(u) > Mathf.Abs(w);
                    uv.Add(new Vector2((p.x + (alongX ? Mathf.Sign(u) * (s + r - y) : 0f)) / 2f, (p.z + (alongX ? 0f : Mathf.Sign(w) * (s + r - y))) / 2f));
                }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    int a = i * (n + 1) + j, b = a + 1, c = a + n + 1, d = c + 1;
                    bool main = (i < n / 2) == (j < n / 2); // kıvrım köşegenleri köşe noktalarından geçsin
                    if (main) { Tri(tris, v, a, d, b, Vector3.down); Tri(tris, v, a, c, d, Vector3.down); }
                    else { Tri(tris, v, a, c, b, Vector3.down); Tri(tris, v, b, c, d, Vector3.down); }
                }
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            return SaveMesh(m, key);
        }

        /// <summary>Kemer kaburgası: a→b yatay, ortası rise kadar yüksek (yarım elips), taş çubuklar.</summary>
        static void ArchRib(Transform root, Vector3 a, Vector3 b, float rise, float thick, Material mat, int segs = 8)
        {
            Vector3 P(float t) { var p = Vector3.Lerp(a, b, t); float q = 2f * t - 1f; p.y += rise * Mathf.Sqrt(Mathf.Max(0f, 1f - q * q)); return p; }
            for (int k = 0; k < segs; k++)
            {
                float t0 = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * k / segs), t1 = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * (k + 1) / segs);
                Rod("Rib", root, P(t0), P(t1), thick, mat);
            }
        }

        /// <summary>Şarap şişesi rafı nişi (duvarda girinti): rafa yatık torna şişeler (tek ağ) + çapraz çıtalar.</summary>
        static void BottleBay(Transform root, string name, Vector3 baseCenter, Vector3 inward, float w, float h, float depth, System.Random rnd)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = baseCenter;
            t.localRotation = Quaternion.LookRotation(inward);
            Slab("Back", t, new Vector3(0, h * 0.5f, -depth - 0.03f), new Vector3(w + 0.1f, h, 0.06f), K.beam, 1.5f, false);
            var bottleMesh = LatheMesh("kit_rackbottle", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.038f, 0f), new Vector2(0.038f, 0.18f), new Vector2(0.015f, 0.235f), new Vector2(0.015f, 0.3f), new Vector2(0f, 0.3f),
            }, 8);
            var bottles = new GameObject("Bottles").transform; bottles.SetParent(t, false);
            int rows = Mathf.FloorToInt((h - 0.15f) / 0.3f);
            for (int r = 0; r < rows; r++)
            {
                float y = 0.1f + r * 0.3f;
                Slab("Shelf", t, new Vector3(0, y - 0.02f, -depth * 0.5f), new Vector3(w, 0.04f, depth), K.furniture, 1.2f, false);
                int n = Mathf.FloorToInt(w / 0.105f);
                for (int k = 0; k < n; k++)
                {
                    if (rnd.NextDouble() < 0.14) continue; // eksik şişeler
                    var bo = MeshObj("B", bottles, new Vector3(-w * 0.5f + 0.06f + k * 0.105f, y + 0.04f, -depth + 0.04f + (float)rnd.NextDouble() * 0.05f), bottleMesh, K.bottleGlass);
                    bo.transform.localRotation = Quaternion.Euler(-90, 0, 0); // dipler odaya
                }
            }
            CombineChildren(bottles, "cellar_bottles_" + name, K.bottleGlass);
            bottles.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var lat = new GameObject("Lattice").transform; lat.SetParent(t, false);
            for (float x = -w * 0.5f; x < w * 0.5f - 0.1f; x += 0.6f)
            {
                float x2 = Mathf.Min(x + 0.6f, w * 0.5f);
                Rod("X", lat, new Vector3(x, 0.05f, -0.03f), new Vector3(x2, h - 0.05f, -0.03f), 0.035f, K.furniture);
                Rod("X", lat, new Vector3(x2, 0.05f, -0.03f), new Vector3(x, h - 0.05f, -0.03f), 0.035f, K.furniture);
            }
            CombineChildren(lat, "cellar_lattice_" + F(w) + "_" + F(h), K.furniture);
        }

        /// <summary>Kahraman eşya: vidalı şarap presi (ahşap çerçeve, demir vida, fıçı tekne, oluk).</summary>
        static void WinePress(Transform root, Vector3 pos, float yaw, Material wineStain)
        {
            var t = new GameObject("WinePress").transform;
            t.SetParent(root, false); t.localPosition = pos; t.localRotation = Quaternion.Euler(0, yaw, 0);
            Prim("Basin", t, PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), new Vector3(1.6f, 0.18f, 1.6f), K.beam);
            Prim("BasinRim", t, PrimitiveType.Cylinder, new Vector3(0, 0.37f, 0), new Vector3(1.66f, 0.02f, 1.66f), K.iron);
            Prim("Must", t, PrimitiveType.Cylinder, new Vector3(0, 0.36f, 0), new Vector3(1.4f, 0.01f, 1.4f), wineStain);
            Prim("Cage", t, PrimitiveType.Cylinder, new Vector3(0, 0.72f, 0), new Vector3(0.9f, 0.35f, 0.9f), K.furniture);
            foreach (float y in new[] { 0.45f, 0.95f }) Prim("Hoop", t, PrimitiveType.Cylinder, new Vector3(0, y, 0), new Vector3(0.94f, 0.03f, 0.94f), K.iron);
            Prim("Plate", t, PrimitiveType.Cylinder, new Vector3(0, 1.12f, 0), new Vector3(0.86f, 0.06f, 0.86f), K.beam);
            foreach (int sg in new[] { -1, 1 })
                Slab("Post", t, new Vector3(sg * 0.95f, 1.2f, 0), new Vector3(0.22f, 2.4f, 0.22f), K.beam, 1f);
            Slab("Beam", t, new Vector3(0, 2.3f, 0), new Vector3(2.3f, 0.3f, 0.3f), K.beam, 1f, false);
            Prim("Screw", t, PrimitiveType.Cylinder, new Vector3(0, 1.75f, 0), new Vector3(0.14f, 0.6f, 0.14f), K.iron);
            for (int k = 0; k < 9; k++) MeshObj("Thread", t, new Vector3(0, 1.25f + k * 0.11f, 0), RingMesh(0.085f, 0.02f, 0.03f), K.iron);
            Rod("Bar", t, new Vector3(-0.8f, 1.3f, 0.05f), new Vector3(0.8f, 1.3f, -0.05f), 0.07f, K.beam);
            Slab("Spout", t, new Vector3(0, 0.2f, 0.95f), new Vector3(0.16f, 0.05f, 0.5f), K.beam, 1f, false).transform.localRotation = Quaternion.Euler(12, 0, 0);
            Kit("Bucket2", t, new Vector3(0, 0, 1.3f), 20f);
            Prim("Spill", t, PrimitiveType.Cylinder, new Vector3(0.1f, 0.004f, 1.25f), new Vector3(0.9f, 0.002f, 0.6f), wineStain);
            var col = t.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 1f, 0); col.radius = 0.85f; col.height = 2.4f;
        }

        static void BuildCellar(Transform roomO, out LiftDoor gate, out LiftDoor rewardGate, out Transform[] syms, out Transform[] nums, out GameObject litGo)
        {
            // İç: x -24.75..-15.25, z -7..7 (2 x 3 çapraz tonoz hücresi, ortada x=-20 sütun çifti z=±2.33). Üzengi 2.2, tepe 3.8.
            // Doğu: giriş (kasa geçidi), batı: ödül kapısı (sarnıç). Bulmaca karanlık: oda bake DIŞI; alevler/ışıklar/ağlar "CellarLit" altında.
            const float spring = 2.2f, rise = 1.6f, H = spring + rise + 0.25f, HZ = 7f, X0 = -24.75f, X1 = -15.25f, XC = -20f;
            const float cx = (X1 - X0) * 0.5f, cz = HZ * 2f / 3f; // hücre 4.75 x 4.67
            var brick = K.cellarBrick; var stone = K.castleStone;
            var floorM = KTex("K_WineFloor", "slab_tiles", new Color(0.4f, 0.42f, 0.48f), 0.78f, 1.2f); // ıslak, derzler soğuk (turuncu parlamasın)
            var stain = MakeMat("K_WineStain", new Color(0.07f, 0.008f, 0.012f)); stain.SetFloat("_Smoothness", 0.9f);
            var chalk = MakeMat("K_Chalk", new Color(0.78f, 0.76f, 0.7f));
            var bread = MakeMat("K_Bread", new Color(0.52f, 0.36f, 0.2f));
            var arch = new Opening(0f, 3f, 0f, 3f);
            var rnd = new System.Random(7);

            Deck(roomO, "CryptCorridorFloor", new Vector3(-13.9f, -0.25f, 0), 2.3f, 3.5f, 0.5f, K.cellarFloor, 1, 1);
            Deck(roomO, "CryptCorridorCeil", new Vector3(-13.9f, 3.25f, 0), 2.3f, 3.5f, 0.5f, brick, 1, 1);
            Wall(roomO, "CryptCorridorN", new Vector3(-13.9f, 0, 1.75f), true, 2.3f, 3f, brick, 2f);
            Wall(roomO, "CryptCorridorS", new Vector3(-13.9f, 0, -1.75f), true, 2.3f, 3f, brick, 2f);

            // --- Kabuk ---
            Deck(roomO, "CryptFloor", new Vector3(XC, -0.25f, 0), 10.5f, HZ * 2 + 0.5f, 0.5f, floorM, 2, 3);
            Wall(roomO, "CryptWallE", new Vector3(X1 + 0.25f, 0, 0), false, HZ * 2 + 0.5f, H, brick, 2f, arch);
            Wall(roomO, "CryptWallW", new Vector3(X0 - 0.25f, 0, 0), false, HZ * 2 + 0.5f, H, brick, 2f, arch);
            Wall(roomO, "CryptWallN", new Vector3(XC, 0, HZ + 0.25f), true, 10.5f, H, brick, 2f,
                new Opening(-cx * 0.5f, 3.2f, 0.1f, 2.45f), new Opening(cx * 0.5f, 3.2f, 0.1f, 2.45f));
            Wall(roomO, "CryptWallS", new Vector3(XC, 0, -HZ - 0.25f), true, 10.5f, H, brick, 2f);
            var vault = new GameObject("CellarVault").transform; vault.SetParent(roomO, false);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                {
                    var c = new Vector3(X0 + cx * (i + 0.5f), 0, -HZ + cz * (j + 0.5f));
                    MeshObj("Groin", vault, c, GroinMesh(cx + 0.02f, cz + 0.02f, spring, rise, 16), brick).isStatic = true;
                    foreach (int sg in new[] { -1, 1 }) // köşegen kaburgalar
                        ArchRib(roomO, c + new Vector3(-cx * 0.5f, spring, -sg * cz * 0.5f), c + new Vector3(cx * 0.5f, spring, sg * cz * 0.5f), rise, 0.16f, stone, 10);
                }
            // Kuşak kemerleri (sütun hatları) + duvar kemerleri
            for (int j = 0; j < 3; j++)
            {
                float z0 = -HZ + cz * j, z1 = z0 + cz;
                foreach (float x in new[] { X0 + 0.1f, XC, X1 - 0.1f })
                    ArchRib(roomO, new Vector3(x, spring, z0), new Vector3(x, spring, z1), rise, x == XC ? 0.34f : 0.22f, stone, 10);
            }
            for (int i = 0; i < 2; i++)
            {
                float x0 = X0 + cx * i, x1 = x0 + cx;
                foreach (float z in new[] { -HZ + 0.1f, -HZ + cz, HZ - cz, HZ - 0.1f })
                    ArchRib(roomO, new Vector3(x0, spring, z), new Vector3(x1, spring, z), rise, Mathf.Abs(z) > HZ - 0.5f ? 0.22f : 0.34f, stone, 10);
            }
            // Sütunlar (ortada) + duvar ayakları
            foreach (float z in new[] { -HZ + cz, HZ - cz })
            {
                Slab("Column", roomO, new Vector3(XC, spring * 0.5f, z), new Vector3(0.55f, spring, 0.55f), stone, 1.2f);
                Slab("ColumnBase", roomO, new Vector3(XC, 0.15f, z), new Vector3(0.8f, 0.3f, 0.8f), stone, 1.2f, false);
                Slab("ColumnCap", roomO, new Vector3(XC, spring - 0.08f, z), new Vector3(0.82f, 0.2f, 0.82f), stone, 1.2f, false);
                foreach (int sg in new[] { -1, 1 })
                {
                    float wx = sg < 0 ? X0 + 0.14f : X1 - 0.14f;
                    Slab("Pilaster", roomO, new Vector3(wx, spring * 0.5f, z), new Vector3(0.28f, spring, 0.5f), stone, 1.2f);
                    Slab("PilasterCap", roomO, new Vector3(wx, spring - 0.06f, z), new Vector3(0.4f, 0.16f, 0.64f), stone, 1.2f, false);
                }
            }
            foreach (int sg in new[] { -1, 1 })
            {
                Slab("Pilaster", roomO, new Vector3(XC, spring * 0.5f, sg * (HZ - 0.14f)), new Vector3(0.5f, spring, 0.28f), stone, 1.2f);
                Slab("PilasterCap", roomO, new Vector3(XC, spring - 0.06f, sg * (HZ - 0.14f)), new Vector3(0.64f, 0.16f, 0.4f), stone, 1.2f, false);
            }
            // Kapı söveleri (taş)
            foreach (float wx in new[] { X0 + 0.06f, X1 - 0.06f })
            {
                foreach (int sg in new[] { -1, 1 })
                    Slab("DoorJamb", roomO, new Vector3(wx, 1.5f, sg * 1.64f), new Vector3(0.14f, 3f, 0.28f), stone, 1.2f, false);
                Slab("DoorLintel", roomO, new Vector3(wx, 3.16f, 0), new Vector3(0.14f, 0.32f, 3.56f), stone, 1.2f, false);
            }

            // Batı nişi (sarnıç geçidi)
            Deck(roomO, "NicheFloor", new Vector3(-26.4f, -0.25f, 0), 2.3f, 3.5f, 0.5f, K.cellarFloor, 1, 1);
            Deck(roomO, "NicheCeil", new Vector3(-26.4f, 3.25f, 0), 2.3f, 3.5f, 0.5f, brick, 1, 1);
            Wall(roomO, "NicheW", new Vector3(-27.5f, 0, 0), false, 3.5f, 3f, brick, 2f, arch);
            Wall(roomO, "NicheN", new Vector3(-26.4f, 0, 1.75f), true, 2.3f, 3f, brick, 2f);
            Wall(roomO, "NicheS", new Vector3(-26.4f, 0, -1.75f), true, 2.3f, 3f, brick, 2f);

            gate = MakeDoor("CryptGate", roomO, new Vector3(-15f, 1.5f, 0), false, K.furniture);
            rewardGate = MakeDoor("CryptRewardGate", roomO, new Vector3(-25f, 1.5f, 0), false, K.furniture);
            foreach (var d in new[] { gate, rewardGate })
                foreach (var r in d.GetComponentsInChildren<Renderer>()) if (r.sharedMaterial == A.iron) r.sharedMaterial = K.iron;

            var lit = new GameObject("CellarLit").transform;
            lit.SetParent(roomO, false);
            litGo = lit.gameObject;

            // --- Sembol paneli (batı duvarı, kuzey hücresi: girişten sütun arkasında kalır) ---
            syms = new Transform[6];
            nums = new Transform[6];
            const float PZ = HZ - cz * 0.5f;
            {
                var panel = new GameObject("SymbolPanel").transform;
                panel.SetParent(roomO, false);
                panel.localPosition = new Vector3(X0, 0, PZ);
                panel.localRotation = Quaternion.Euler(0, -90, 0);
                Slab("Back", panel, new Vector3(0, 1.625f, -0.04f), new Vector3(2.8f, 2.45f, 0.08f), stone, 1.4f, false);
                Slab("FrameT", panel, new Vector3(0, 2.92f, -0.07f), new Vector3(3.08f, 0.14f, 0.14f), stone, 1f, false);
                Slab("FrameB", panel, new Vector3(0, 0.33f, -0.07f), new Vector3(3.08f, 0.14f, 0.14f), stone, 1f, false);
                foreach (int sg in new[] { -1, 1 })
                    Slab("FrameSide", panel, new Vector3(sg * 1.47f, 1.625f, -0.07f), new Vector3(0.14f, 2.45f, 0.14f), stone, 1f, false);
                var inset = MakeMat("K_CellarInset", new Color(0.075f, 0.065f, 0.06f));
                for (int i = 0; i < 6; i++)
                {
                    float px = (i % 3 - 1) * 0.92f, py = i < 3 ? 2.35f : 1.2f;
                    MeshObj("Ring" + i, panel, new Vector3(px, py, -0.08f), RingMesh(0.4f, 0.05f, 0.05f), K.bronze).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                    Prim("Disc" + i, panel, PrimitiveType.Cylinder, new Vector3(px, py, -0.084f), new Vector3(0.72f, 0.005f, 0.72f), inset).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Slab("Plaque" + i, panel, new Vector3(px, py - 0.57f, -0.086f), new Vector3(0.56f, 0.3f, 0.012f), inset, 1f, false);
                    syms[i] = new GameObject("CellarSym" + i).transform;
                    syms[i].SetParent(panel, false);
                    syms[i].localPosition = new Vector3(px, py, -0.094f);
                    nums[i] = new GameObject("CellarNum" + i).transform;
                    nums[i].SetParent(panel, false);
                    nums[i].localPosition = new Vector3(px, py - 0.57f, -0.096f);
                }
                // Panelin altında: mum dipleri, düşmüş kibrit kutusu, yanık kibritler (önceki ziyaretçi)
                for (int k = 0; k < 3; k++)
                {
                    var cp = new Vector3(X0 + 0.25f, 0, PZ - 0.9f + k * 0.8f);
                    KCandle(roomO, cp, 0.05f + k * 0.03f, 0.025f, false);
                    CellarFlame(lit, cp + Vector3.up * (0.08f + k * 0.03f), 0.02f);
                }
                Slab("MatchBox", roomO, new Vector3(X0 + 0.7f, 0.015f, PZ + 0.3f), new Vector3(0.09f, 0.03f, 0.06f), K.leatherRed, 1f, false).transform.localRotation = Quaternion.Euler(0, 33, 0);
                for (int k = 0; k < 4; k++)
                    Rod("BurntMatch", roomO, new Vector3(X0 + 0.5f + k * 0.07f, 0.006f, PZ - 0.2f + k * 0.05f), new Vector3(X0 + 0.55f + k * 0.07f, 0.006f, PZ - 0.25f + k * 0.09f), 0.008f, K.iron);
            }

            // --- Batı duvarı: kapı yanlarında askılı fener; güney hücresinde musluklu fıçılar ---
            foreach (int sg in new[] { -1, 1 })
            {
                Rod("LanternArm", roomO, new Vector3(X0, 2.32f, sg * 1.74f), new Vector3(X0 + 0.42f, 2.32f, sg * 1.74f), 0.03f, K.iron);
                Rod("LanternBrace", roomO, new Vector3(X0, 2.02f, sg * 1.74f), new Vector3(X0 + 0.3f, 2.3f, sg * 1.74f), 0.022f, K.iron);
                Rod("LanternHook", roomO, new Vector3(X0 + 0.4f, 2.32f, sg * 1.74f), new Vector3(X0 + 0.4f, 2.2f, sg * 1.74f), 0.015f, K.iron);
                DarkLantern(roomO, lit, new Vector3(X0 + 0.4f, 1.78f, sg * 1.74f), sg * 20f);
            }
            for (int k = 0; k < 2; k++)
            {
                float z = -4.0f - k * 1.05f;
                Slab("Cradle", roomO, new Vector3(X0 + 0.55f, 0.2f, z), new Vector3(0.9f, 0.4f, 0.9f), K.beam, 1f);
                Kit("BarrelLying", roomO, new Vector3(X0 + 0.55f, 0.4f, z), 90f, 1.1f);
                Rod("Tap", roomO, new Vector3(X0 + 1.08f, 0.82f, z), new Vector3(X0 + 1.22f, 0.82f, z), 0.03f, K.brass);
                Rod("TapSpout", roomO, new Vector3(X0 + 1.22f, 0.82f, z), new Vector3(X0 + 1.22f, 0.72f, z), 0.025f, K.brass);
                for (int l = 0; l < 3 + k; l++) // tebeşir sayım çizgileri
                    Slab("Tally", roomO, new Vector3(X0 + 1.02f, 0.95f, z - 0.15f + l * 0.06f), new Vector3(0.005f, 0.16f, 0.012f), chalk, 1f, false);
            }
            Kit("Bucket", roomO, new Vector3(X0 + 1.3f, 0, -4.0f), 30f);
            Kit("Goblet1", roomO, new Vector3(X0 + 1.35f, 0, -5.05f));
            Kit("Jug", roomO, new Vector3(X0 + 0.5f, 0, -6.4f), 40f);

            // --- Güney duvarı: fıçı istifi (rayda 4 + 3), iki dev fıçı, sandıklar ---
            foreach (float z in new[] { -HZ + 0.6f, -HZ + 1.25f })
                Slab("BarrelRail", roomO, new Vector3(-20.6f, 0.05f, z), new Vector3(5.5f, 0.1f, 0.12f), K.beam, 1.2f, false);
            for (int i = 0; i < 4; i++) Kit("BarrelLying", roomO, new Vector3(-22.6f + i * 0.84f, 0.1f, -HZ + 0.92f), 0f);
            for (int i = 0; i < 3; i++) Kit("BarrelLying", roomO, new Vector3(-22.18f + i * 0.84f, 0.78f, -HZ + 0.92f), 0f);
            Kit("BarrelLying", roomO, new Vector3(-17.4f, 0.1f, -HZ + 1.15f), 0f, 1.55f);
            Kit("BarrelLying", roomO, new Vector3(-16.05f, 0.1f, -HZ + 1.15f), 0f, 1.55f);
            for (int l = 0; l < 4; l++) Slab("Tally", roomO, new Vector3(-17.6f + l * 0.07f, 0.95f, -HZ + 1.92f), new Vector3(0.012f, 0.18f, 0.005f), chalk, 1f, false);
            Slab("TallyCross", roomO, new Vector3(-17.5f, 0.95f, -HZ + 1.925f), new Vector3(0.34f, 0.012f, 0.005f), chalk, 1f, false).transform.localRotation = Quaternion.Euler(0, 0, 22);
            Kit("Crate", roomO, new Vector3(-19.2f, 0, -HZ + 0.7f), 12f);
            Kit("Crate", roomO, new Vector3(-19.15f, 0.6f, -HZ + 0.72f), 40f, 0.8f);
            Kit("Bottle3", roomO, new Vector3(-19.15f, 1.08f, -HZ + 0.7f));
            Kit("BarrelRack", roomO, new Vector3(-23.9f, 0, -HZ + 2.6f), 90f, 0.9f);

            // --- Güneybatı: kahraman eşya şarap presi (dökülmüş şıra) ---
            WinePress(roomO, new Vector3(-21.6f, 0, -3.6f), 15f, stain);
            Kit("Basket", roomO, new Vector3(-20.5f, 0, -2.6f), 30f);
            Kit("Basket", roomO, new Vector3(-22.9f, 0, -2.3f), 70f, 0.9f);
            Kit("Spade", roomO, new Vector3(-23.4f, 0, -1.9f), 200f);

            // --- Kuzey duvarı: iki kemerli şişe rafı nişi, merdiven, amforalar ---
            foreach (float bx in new[] { XC - cx * 0.5f, XC + cx * 0.5f })
            {
                BottleBay(roomO, "BottleBay" + (bx < XC ? "W" : "E"), new Vector3(bx, 0.1f, HZ), Vector3.back, 3.15f, 2.3f, 0.45f, rnd);
                Slab("BayLintel", roomO, new Vector3(bx, 2.56f, HZ - 0.06f), new Vector3(3.5f, 0.22f, 0.14f), stone, 1f, false);
                foreach (int sg in new[] { -1, 1 }) Slab("BayJamb", roomO, new Vector3(bx + sg * 1.68f, 1.28f, HZ - 0.06f), new Vector3(0.16f, 2.56f, 0.14f), stone, 1f, false);
            }
            var shelfBlock = new GameObject("RackBlock").AddComponent<BoxCollider>();
            shelfBlock.transform.SetParent(roomO, false); shelfBlock.transform.localPosition = new Vector3(XC, 1.2f, HZ - 0.05f); shelfBlock.size = new Vector3(9.5f, 2.4f, 0.1f);
            Kit("Ladder", roomO, new Vector3(-18.4f, 0, HZ - 0.55f), 0f);
            Kit("Amphora", roomO, new Vector3(XC, 0, HZ - 0.5f), 60f);
            Kit("Amphora", roomO, new Vector3(X0 + 0.5f, 0, HZ - 0.5f), 110f, 0.85f);
            Kit("CeramicVase3", roomO, new Vector3(X1 - 1.2f, 0, HZ - 0.4f), 0f);

            // --- Kuzeydoğu: mahzen ustasının masası (defter, gözlük, mum) + raf ---
            {
                var dp = new Vector3(X1 - 0.6f, 0, 5.0f);
                Kit("SmallTable", roomO, dp, -90f);
                float ty = 0.62f;
                Kit("BookStack", roomO, dp + new Vector3(0.1f, ty, 0.2f), 20f, 0.8f);
                Kit("PaperA", roomO, dp + new Vector3(-0.05f, ty + 0.004f, -0.15f), 60f);
                Kit("Spectacles", roomO, dp + new Vector3(0.12f, ty + 0.01f, -0.2f), 140f);
                KCandle(roomO, dp + new Vector3(-0.15f, ty, 0.25f), 0.12f, 0.02f, false);
                CellarFlame(lit, dp + new Vector3(-0.15f, ty + 0.14f, 0.25f), 0.022f);
                Kit("Stool", roomO, dp + new Vector3(-0.75f, 0, 0), 20f);
                KitR("PaperB", roomO, new Vector3(X1 - 0.03f, 1.5f, 5.0f), Quaternion.Euler(0, -90, 0) * Quaternion.Euler(-90, 0, 0)); // envanter notu (duvarda)
                Kit("Cabinet", roomO, new Vector3(X1 - 0.35f, 0, 2.9f), -90f, 0.85f);
                Kit("Pot", roomO, new Vector3(X1 - 0.6f, 0, 3.9f), 0f, 0.8f);
                Kit("Jug", roomO, new Vector3(X1 - 0.5f, 0, 2.0f), 10f);
                Kit("Broom", roomO, new Vector3(X1 - 0.25f, 0, 6.5f), -60f);
            }

            // --- Güneydoğu (girişin güneyi): tadım masası, bordo örtü, mum, kadeh, şişe, ekmek ---
            {
                const float th = 0.82f;
                var tp = new Vector3(-15.8f, 0, -3.4f);
                Kit("Table", roomO, tp, 90f);
                Kit("TableCloth", roomO, tp + new Vector3(-0.06f, th, 0.3f), -90f);
                Prim("CandleDish", roomO, PrimitiveType.Cylinder, tp + new Vector3(0.1f, th + 0.02f, 0.4f), new Vector3(0.11f, 0.008f, 0.11f), K.brass);
                KCandle(roomO, tp + new Vector3(0.1f, th + 0.028f, 0.4f), 0.2f, 0.02f, false);
                CellarFlame(lit, tp + new Vector3(0.1f, th + 0.232f, 0.4f), 0.024f);
                Kit("Goblet0", roomO, tp + new Vector3(-0.15f, th + 0.012f, 0.2f));
                Kit("Goblet2", roomO, tp + new Vector3(0.08f, th + 0.012f, 0.08f));
                Kit("Bottle2", roomO, tp + new Vector3(0.18f, th, -0.35f), 30f);
                Kit("Bottle0", roomO, tp + new Vector3(0.02f, th, -0.55f), 140f);
                Kit("Bowl", roomO, tp + new Vector3(-0.2f, th, -0.3f));
                Kit("PaperA", roomO, tp + new Vector3(-0.15f, th + 0.004f, -0.65f), 25f);
                Kit("Dagger", roomO, tp + new Vector3(0.2f, th + 0.004f, -0.7f), -40f);
                Prim("Bread", roomO, PrimitiveType.Sphere, tp + new Vector3(-0.1f, th + 0.04f, 0.6f), new Vector3(0.22f, 0.08f, 0.12f), bread);
                Kit("Stool", roomO, tp + new Vector3(-1.1f, 0.2f, 0.2f), 35f).transform.localRotation = Quaternion.Euler(0, 35, 88); // devrilmiş tabure
                Kit("Chest", roomO, new Vector3(X1 - 0.5f, 0, -1.95f), -90f);
                Kit("CrateLong", roomO, new Vector3(-16.9f, 0, -HZ + 0.5f), 8f);
            }

            // --- Orta: zincirli fener, zemin ızgarası, kırık şişe + dökülmüş şarap ---
            Rod("LanternChain", roomO, new Vector3(XC, 2.9f, 0), new Vector3(XC, spring + rise, 0), 0.02f, K.iron);
            DarkLantern(roomO, lit, new Vector3(XC, 2.48f, 0));
            {
                var drain = new GameObject("Drain").transform;
                drain.SetParent(roomO, false);
                drain.localPosition = new Vector3(XC, 0, 0.9f);
                Prim("Hole", drain, PrimitiveType.Cylinder, new Vector3(0, 0.003f, 0), new Vector3(0.88f, 0.003f, 0.88f), MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f)));
                MeshObj("Rim", drain, new Vector3(0, 0, 0), RingMesh(0.48f, 0.07f, 0.02f), K.iron);
                for (int i = -2; i <= 2; i++)
                {
                    float x = i * 0.15f, l = Mathf.Round(2f * Mathf.Sqrt(0.42f * 0.42f - x * x) * 100f) / 100f;
                    Slab("Bar", drain, new Vector3(x, 0.012f, 0), new Vector3(0.04f, 0.016f, l), K.iron, 1f, false);
                }
                Slab("Cross", drain, new Vector3(0, 0.012f, 0), new Vector3(0.84f, 0.016f, 0.04f), K.iron, 1f, false);
                foreach (var (px, pz, sx, sz, yw) in new[] { (0.9f, 1.6f, 0.7f, 0.4f, 30f), (1.25f, 1.85f, 0.45f, 0.3f, 70f), (0.6f, 1.35f, 0.35f, 0.22f, 10f), (0.5f, 1.05f, 0.18f, 0.5f, 25f) }) // düzensiz birikinti, ızgaraya akar
                    Prim("WinePool", roomO, PrimitiveType.Cylinder, new Vector3(XC + px, 0.002f + px * 0.001f, pz), new Vector3(sx, 0.001f, sz), stain).transform.localRotation = Quaternion.Euler(0, yw, 0);
                for (int k = 0; k < 6; k++)
                {
                    var sh = Slab("Shard", roomO, new Vector3(XC + 1.2f + (float)rnd.NextDouble() * 0.5f, 0.008f, 1.8f + (float)rnd.NextDouble() * 0.5f), new Vector3(0.06f, 0.008f, 0.04f), K.bottleGlass, 1f, false);
                    sh.transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0);
                }
                Kit("Bottle1", roomO, new Vector3(XC + 1.5f, 0.04f, 2.0f), 0f).transform.localRotation = Quaternion.Euler(0, 40, 90);
            }

            // --- Zemin dağınıklığı: fareler, kapan, sandıklar, sepetler, fıçılar ---
            Kit("Rat", roomO, new Vector3(-16.1f, 0, -2.8f), 120f);
            Kit("Rat", roomO, new Vector3(X0 + 0.6f, 0, 2.9f), 300f, 0.9f);
            Kit("Mousetrap", roomO, new Vector3(-18.0f, 0, HZ - 1.2f), 40f);
            Kit("CrateStack", roomO, new Vector3(X1 - 0.8f, 0, HZ - 0.8f), 180f);
            Kit("Barrel", roomO, new Vector3(XC + 0.75f, 0, HZ - cz + 0.1f), 110f);
            Kit("Barrel", roomO, new Vector3(XC - 0.75f, 0, -HZ + cz - 0.1f), 30f);
            Kit("Goblet2", roomO, new Vector3(XC - 0.75f, 1.0f, -HZ + cz - 0.1f));
            KCandle(roomO, new Vector3(XC - 0.6f, 1.0f, -HZ + cz), 0.1f, 0.022f, false);
            CellarFlame(lit, new Vector3(XC - 0.6f, 1.12f, -HZ + cz), 0.022f);
            KCandle(roomO, new Vector3(XC + 0.6f, 1.0f, HZ - cz + 0.2f), 0.08f, 0.022f, false);
            CellarFlame(lit, new Vector3(XC + 0.6f, 1.1f, HZ - cz + 0.2f), 0.022f);
            Kit("Basket", roomO, new Vector3(-16.4f, 0, 2.1f), 10f);
            Kit("Bucket2", roomO, new Vector3(-23.5f, 0, 2.4f), 0f);
            Kit("Barrel", roomO, new Vector3(-23.9f, 0, 3.4f), 200f);
            Kit("CuttingBoard", roomO, new Vector3(-23.9f, 1.0f, 3.4f), 30f);
            Kit("Bottle3", roomO, new Vector3(-23.75f, 1.0f, 3.5f));

            // --- Çözülünce: 2 realtime ışık + alevler (yukarıda) + örümcek ağları ---
            void LitLight(string n, Vector3 p, float intensity, float range)
            {
                var l = PointLight(lit, n, p, Pal.Lantern, intensity, range);
                var fl = l.gameObject.AddComponent<LightFlicker>();
                var so = new SerializedObject(fl);
                SetRef(so, "target", l);
                SetFlt(so, "amount", 0.12f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            LitLight("CenterLight", new Vector3(XC, 1.95f, -0.5f), 7f, 15f);
            LitLight("PanelLight", new Vector3(X0 + 1.0f, 2.0f, PZ - 0.6f), 4.5f, 8f);
            KitR("Cobweb", lit, new Vector3(X0 + 0.03f, 0, HZ - 0.03f), Quaternion.identity, 1.1f);
            KitR("Cobweb", lit, new Vector3(X1 - 0.03f, 0, -HZ + 0.03f), Quaternion.Euler(0, 180, 0), 1.2f);
            KitR("Cobweb", lit, new Vector3(XC - 0.3f, spring - 0.1f, HZ - cz + 0.3f), Quaternion.identity, 0.9f);
            KitR("Cobweb", lit, new Vector3(XC + 0.3f, spring - 0.1f, -HZ + cz - 0.3f), Quaternion.Euler(0, 180, 180), 1.1f);
            KitR("Cobweb", lit, new Vector3(X0 + 0.03f, spring, -HZ + 0.03f), Quaternion.Euler(0, 90, 180), 1.0f);
            KitR("Cobweb", lit, new Vector3(X1 - 0.03f, spring, HZ - 0.03f), Quaternion.Euler(0, -90, 180), 1.0f);
            lit.gameObject.SetActive(false);
        }
    }
}
