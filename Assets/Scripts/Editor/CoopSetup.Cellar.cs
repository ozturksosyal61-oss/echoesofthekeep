using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun bulmaca 2 odası: şarap mahzeni (ref: Docs/Reference/mahzen.png). Tuğla beşik tonoz + taş kaburgalar, ıslak zemin,
    /// fıçı istifleri, şarap rafı, masa. Batı duvarındaki sembol paneli (6 madalyon: sembol + Roma rakamı) bulmacanın okuma yüzeyi.
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

        static void BuildCellar(Transform roomO, out LiftDoor gate, out LiftDoor rewardGate, out Transform[] syms, out Transform[] nums, out GameObject litGo)
        {
            // İç: x -24.75..-15.25, z -5..5. Üzengi 2.4, tonoz tepe ~4.3. Doğu: giriş (kasa odası), batı: ödül kapısı (sarnıç).
            const float spring = 2.4f, endH = 4.6f;
            var brick = K.cellarBrick; var stone = K.castleStone;
            var arch = new Opening(0f, 3f, 0f, 3f);

            Deck(roomO, "CryptCorridorFloor", new Vector3(-13.9f, -0.25f, 0), 2.3f, 3.5f, 0.5f, K.cellarFloor, 1, 1);
            Deck(roomO, "CryptCorridorCeil", new Vector3(-13.9f, 3.25f, 0), 2.3f, 3.5f, 0.5f, brick, 1, 1);
            Wall(roomO, "CryptCorridorN", new Vector3(-13.9f, 0, 1.75f), true, 2.3f, 3f, brick, 2f);
            Wall(roomO, "CryptCorridorS", new Vector3(-13.9f, 0, -1.75f), true, 2.3f, 3f, brick, 2f);

            Deck(roomO, "CryptFloor", new Vector3(-20f, -0.25f, 0), 10.5f, 10.5f, 0.5f, K.cellarFloor, 2, 2);
            Wall(roomO, "CryptWallE", new Vector3(-15f, 0, 0), false, 10.5f, endH, brick, 2f, arch);
            Wall(roomO, "CryptWallW", new Vector3(-25f, 0, 0), false, 10.5f, endH, brick, 2f, arch);
            Wall(roomO, "CryptWallN", new Vector3(-20f, 0, 5.25f), true, 10.5f, spring + 0.3f, brick, 2f);
            Wall(roomO, "CryptWallS", new Vector3(-20f, 0, -5.25f), true, 10.5f, spring + 0.3f, brick, 2f);
            VaultStrip(roomO, "CellarVault", -20f, 10.5f, 5.12f, spring, 1.95f, 0.3f, brick, 12, 2f);
            foreach (float x in new[] { -22.75f, -17.75f })
            {
                VaultStrip(roomO, "CellarRib" + x, x, 0.5f, 4.98f, spring, 1.74f, 0.28f, stone, 12, 1.2f);
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("Pilaster", roomO, new Vector3(x, spring * 0.5f, s * 4.86f), new Vector3(0.5f, spring, 0.28f), stone, 1.2f);
                    Slab("PilasterCap", roomO, new Vector3(x, spring, s * 4.82f), new Vector3(0.64f, 0.16f, 0.4f), stone, 1.2f, false);
                    Slab("PilasterBase", roomO, new Vector3(x, 0.12f, s * 4.82f), new Vector3(0.64f, 0.24f, 0.4f), stone, 1.2f, false);
                }
            }
            // Kapı söveleri (taş)
            foreach (float wx in new[] { -24.69f, -15.31f })
            {
                foreach (int s in new[] { -1, 1 })
                    Slab("DoorJamb", roomO, new Vector3(wx, 1.5f, s * 1.64f), new Vector3(0.14f, 3f, 0.28f), stone, 1.2f, false);
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

            // --- Sembol paneli (batı duvarı, kapının kuzeyi): yerel x = duvar boyunca (kuzey +), -z = odaya ---
            syms = new Transform[6];
            nums = new Transform[6];
            {
                var panel = new GameObject("SymbolPanel").transform;
                panel.SetParent(roomO, false);
                panel.localPosition = new Vector3(-24.75f, 0, 3.35f);
                panel.localRotation = Quaternion.Euler(0, -90, 0);
                Slab("Back", panel, new Vector3(0, 1.625f, -0.04f), new Vector3(2.8f, 2.45f, 0.08f), stone, 1.4f, false);
                Slab("FrameT", panel, new Vector3(0, 2.92f, -0.07f), new Vector3(3.08f, 0.14f, 0.14f), stone, 1f, false);
                Slab("FrameB", panel, new Vector3(0, 0.33f, -0.07f), new Vector3(3.08f, 0.14f, 0.14f), stone, 1f, false);
                foreach (int s in new[] { -1, 1 })
                    Slab("FrameSide", panel, new Vector3(s * 1.47f, 1.625f, -0.07f), new Vector3(0.14f, 2.45f, 0.14f), stone, 1f, false);
                var inset = MakeMat("K_CellarInset", new Color(0.075f, 0.065f, 0.06f));
                for (int i = 0; i < 6; i++)
                {
                    float cx = (i % 3 - 1) * 0.92f, cy = i < 3 ? 2.35f : 1.2f;
                    MeshObj("Ring" + i, panel, new Vector3(cx, cy, -0.08f), RingMesh(0.4f, 0.05f, 0.05f), K.bronze).transform.localRotation = Quaternion.Euler(-90, 0, 0);
                    Prim("Disc" + i, panel, PrimitiveType.Cylinder, new Vector3(cx, cy, -0.084f), new Vector3(0.72f, 0.005f, 0.72f), inset).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Slab("Plaque" + i, panel, new Vector3(cx, cy - 0.57f, -0.086f), new Vector3(0.56f, 0.3f, 0.012f), inset, 1f, false);
                    syms[i] = new GameObject("CellarSym" + i).transform;
                    syms[i].SetParent(panel, false);
                    syms[i].localPosition = new Vector3(cx, cy, -0.094f);
                    nums[i] = new GameObject("CellarNum" + i).transform;
                    nums[i].SetParent(panel, false);
                    nums[i].localPosition = new Vector3(cx, cy - 0.57f, -0.096f);
                }
            }

            // --- Batı duvarı: kapının iki yanında askılı fener; güneyde fıçı + kadeh ---
            foreach (int s in new[] { -1, 1 })
            {
                Rod("LanternArm", roomO, new Vector3(-24.75f, 2.32f, s * 1.74f), new Vector3(-24.33f, 2.32f, s * 1.74f), 0.03f, K.iron);
                Rod("LanternBrace", roomO, new Vector3(-24.75f, 2.02f, s * 1.74f), new Vector3(-24.45f, 2.3f, s * 1.74f), 0.022f, K.iron);
                Rod("LanternHook", roomO, new Vector3(-24.35f, 2.32f, s * 1.74f), new Vector3(-24.35f, 2.2f, s * 1.74f), 0.015f, K.iron);
                DarkLantern(roomO, lit, new Vector3(-24.35f, 1.78f, s * 1.74f), s * 20f);
            }
            Kit("Barrel", roomO, new Vector3(-24.2f, 0, -2.75f), 40f);
            Kit("Goblet1", roomO, new Vector3(-24.28f, 1.0f, -2.68f));
            Kit("Bottle1", roomO, new Vector3(-24.1f, 1.0f, -2.86f), 70f);

            // --- Güney duvarı: sehpa üstünde fıçı istifi (4 + 3), iki büyük fıçı ---
            foreach (float z in new[] { -4.4f, -3.75f })
                Slab("BarrelRail", roomO, new Vector3(-21.6f, 0.05f, z), new Vector3(6.3f, 0.1f, 0.12f), K.beam, 1.2f, false);
            for (int i = 0; i < 4; i++) Kit("BarrelLying", roomO, new Vector3(-24.2f + i * 0.84f, 0.1f, -4.08f), 0f);
            for (int i = 0; i < 3; i++) Kit("BarrelLying", roomO, new Vector3(-23.78f + i * 0.84f, 0.78f, -4.08f), 0f);
            Kit("BarrelLying", roomO, new Vector3(-20.05f, 0.1f, -3.85f), 0f, 1.55f);
            Kit("BarrelLying", roomO, new Vector3(-18.72f, 0.1f, -3.85f), 0f, 1.55f);
            Kit("Bucket", roomO, new Vector3(-19.4f, 0, -2.75f));
            Kit("Crate", roomO, new Vector3(-21.3f, 0, -2.9f), 22f);
            Kit("Bottle3", roomO, new Vector3(-21.35f, 0.6f, -2.95f));
            Kit("Bowl", roomO, new Vector3(-21.18f, 0.6f, -2.8f));
            Kit("Stool", roomO, new Vector3(-22.4f, 0, -2.95f), 15f);

            // --- Kuzey duvarı: şarap rafı + şişeler, merdiven, köşede fıçı + amfora ---
            {
                const float rx = -20.25f, rw = 4f, rh = 2.1f, rd = 0.42f, zf = 5f - rd;
                var rack = new GameObject("WineRack").transform;
                rack.SetParent(roomO, false);
                var bc = rack.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(rx, rh * 0.5f, 5f - rd * 0.5f); bc.size = new Vector3(rw, rh, rd);
                Slab("Back", rack, new Vector3(rx, rh * 0.5f, 4.985f), new Vector3(rw, rh, 0.03f), K.beam, 1.5f, false);
                for (int i = 0; i <= 4; i++)
                    Slab("Upright" + i, rack, new Vector3(rx - rw * 0.5f + 0.03f + (rw - 0.06f) * i / 4f, rh * 0.5f, 5f - rd * 0.5f), new Vector3(0.06f, rh, rd), K.furniture, 1.2f, false);
                float[] ys = { 0.12f, 0.45f, 0.78f, 1.11f, 1.44f, 1.77f, rh };
                foreach (float y in ys)
                    Slab("Shelf", rack, new Vector3(rx, y - 0.025f, 5f - rd * 0.5f), new Vector3(rw, 0.05f, rd), K.furniture, 1.2f, false);
                var bottleMesh = LatheMesh("kit_rackbottle", new[]
                {
                    new Vector2(0f, 0f), new Vector2(0.038f, 0f), new Vector2(0.038f, 0.18f), new Vector2(0.015f, 0.235f), new Vector2(0.015f, 0.3f), new Vector2(0f, 0.3f),
                }, 8);
                var rnd = new System.Random(7);
                float bay = (rw - 0.06f) / 4f;
                for (int row = 0; row < 6; row++)
                    for (int b = 0; b < 4; b++)
                    {
                        int n = row == 5 ? rnd.Next(0, 3) : rnd.Next(2, 6);
                        float x0 = rx - rw * 0.5f + 0.03f + bay * b + 0.14f + (float)rnd.NextDouble() * 0.08f;
                        for (int k = 0; k < n; k++)
                        {
                            // Raf şişesi: torna ağ (~100 üçgen); kit şişe modeli 6-8 bin üçgen, onlarcası mobil bütçeyi aşar
                            var bo = MeshObj("RackBottle", rack, new Vector3(x0 + k * 0.15f, ys[row] + 0.04f, zf + 0.04f + (float)rnd.NextDouble() * 0.04f), bottleMesh, K.bottleGlass);
                            bo.transform.localRotation = Quaternion.Euler(90, 0, 0);
                            bo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                            bo.isStatic = true;
                        }
                    }
                KitR("PaperB", roomO, new Vector3(-18.2f, 1.5f, 4.97f), Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(0, 6, 0)); // envanter notu
            }
            Kit("Ladder", roomO, new Vector3(-19.1f, 0, 4.3f), 0f);
            Kit("Barrel", roomO, new Vector3(-23.3f, 0, 4.4f), 110f);
            Kit("Amphora", roomO, new Vector3(-23.3f, 1.0f, 4.4f), 0f, 0.72f);
            Kit("Amphora", roomO, new Vector3(-17.1f, 0, 4.35f), 60f);
            Kit("CrateStack", roomO, new Vector3(-16.2f, 0, 3.9f), 180f);
            Kit("Amphora", roomO, new Vector3(-15.85f, 0, 2.35f), 200f, 0.8f);

            // --- Doğu duvarı (girişin güneyi): masa + bordo örtü + mum + kadeh + şişe + not ---
            {
                const float th = 0.82f;
                Kit("Table", roomO, new Vector3(-15.8f, 0, -3.4f), 90f);
                Kit("TableCloth", roomO, new Vector3(-15.86f, th, -3.1f), -90f);
                Prim("CandleDish", roomO, PrimitiveType.Cylinder, new Vector3(-15.7f, th + 0.02f, -3.0f), new Vector3(0.11f, 0.008f, 0.11f), K.brass);
                KCandle(roomO, new Vector3(-15.7f, th + 0.028f, -3.0f), 0.2f, 0.02f, false);
                CellarFlame(lit, new Vector3(-15.7f, th + 0.232f, -3.0f), 0.024f);
                Kit("Goblet0", roomO, new Vector3(-15.95f, th + 0.012f, -3.2f));
                Kit("Goblet2", roomO, new Vector3(-15.72f, th + 0.012f, -3.32f));
                Kit("Bottle2", roomO, new Vector3(-15.62f, th, -3.75f), 30f);
                Kit("Bottle0", roomO, new Vector3(-15.78f, th, -3.95f), 140f);
                Kit("Bowl", roomO, new Vector3(-16.0f, th, -3.7f));
                Kit("PaperA", roomO, new Vector3(-15.95f, th + 0.004f, -4.05f), 25f);
                Kit("Dagger", roomO, new Vector3(-15.6f, th + 0.004f, -4.1f), -40f);
                Kit("Stool", roomO, new Vector3(-16.8f, 0, -3.3f), 35f);
                Kit("Chest", roomO, new Vector3(-15.8f, 0, -1.95f), -90f);
                Kit("CrateLong", roomO, new Vector3(-16.9f, 0, -4.5f), 8f);
            }

            // --- Orta: zincirli fener, zemin ızgarası ---
            Rod("LanternChain", roomO, new Vector3(-20f, 2.9f, 0), new Vector3(-20f, 4.3f, 0), 0.02f, K.iron);
            DarkLantern(roomO, lit, new Vector3(-20f, 2.48f, 0));
            {
                var drain = new GameObject("Drain").transform;
                drain.SetParent(roomO, false);
                drain.localPosition = new Vector3(-20f, 0, 0.9f);
                Prim("Hole", drain, PrimitiveType.Cylinder, new Vector3(0, 0.003f, 0), new Vector3(0.88f, 0.003f, 0.88f), MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f)));
                MeshObj("Rim", drain, new Vector3(0, 0, 0), RingMesh(0.48f, 0.07f, 0.02f), K.iron);
                for (int i = -2; i <= 2; i++)
                {
                    float x = i * 0.15f, l = Mathf.Round(2f * Mathf.Sqrt(0.42f * 0.42f - x * x) * 100f) / 100f;
                    Slab("Bar", drain, new Vector3(x, 0.012f, 0), new Vector3(0.04f, 0.016f, l), K.iron, 1f, false);
                }
                Slab("Cross", drain, new Vector3(0, 0.012f, 0), new Vector3(0.84f, 0.016f, 0.04f), K.iron, 1f, false);
            }

            // --- Çözülünce: 2 realtime ışık + alevler (yukarıda) + örümcek ağları (ışıksız malzeme, karanlıkta görünmesin) ---
            void LitLight(string n, Vector3 p, float intensity, float range)
            {
                var l = PointLight(lit, n, p, Pal.Lantern, intensity, range);
                var fl = l.gameObject.AddComponent<LightFlicker>();
                var so = new SerializedObject(fl);
                SetRef(so, "target", l);
                SetFlt(so, "amount", 0.12f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            LitLight("CenterLight", new Vector3(-20f, 1.95f, 0), 6.5f, 13f);
            LitLight("PanelLight", new Vector3(-23.9f, 2.0f, 2.3f), 4.5f, 7f);
            KitR("Cobweb", lit, new Vector3(-24.72f, 0, 4.97f), Quaternion.identity, 1.1f);
            KitR("Cobweb", lit, new Vector3(-15.28f, 0, -4.97f), Quaternion.Euler(0, 180, 0), 1.2f);
            KitR("Cobweb", lit, new Vector3(-22.2f, 2.1f, 4.6f), Quaternion.identity, 0.9f);
            KitR("Cobweb", lit, new Vector3(-17.5f, 2.4f, -4.7f), Quaternion.Euler(0, 180, 180), 1.3f);
            lit.gameObject.SetActive(false);
        }
    }
}
