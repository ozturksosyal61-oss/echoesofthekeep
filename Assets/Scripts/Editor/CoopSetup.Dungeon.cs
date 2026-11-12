using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun ilk odası: zindan v3 "Muhafız Koğuşu". İç x ±8, z ±13; kuzey-güney ikiz tuğla beşik tonoz (tepe 7.2),
    /// ortada sütunlu kemer dizisi (x=0). Kuzeyde üç parmaklıklı hücre (ortadaki açık: kaçış tüneli hikâyesi),
    /// güneybatıda ahşap gözcü platformu + serbest merdiven, güneydoğuda gardiyan masası, ortada asılı gibbet kafesi.
    /// Fresk (bulmaca 1) doğudaki Kayıt Şapeli'nde (x 8.5..13, z ±5.5): girişten görünmez. Batıda yuvarlak kasa kapısı.
    /// Baked; 2 realtime fener. Arkada kasa odası (x -8.5..-12.75, z ±3).
    /// </summary>
    public static partial class CoopSetup
    {
        const float DgSpring = 4.6f, DgRise = 2.6f, DgHX = 8f, DgHZ = 13f;

        /// <summary>Serbest ahşap merdiven: alt uçtan üst uca z boyunca (dz yönü), genişlik w. Çarpışma görünmez rampa.</summary>
        static void WoodStair(Transform root, string name, Vector3 bottom, float rise, float run, float dz, float w)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = bottom;
            t.localRotation = Quaternion.Euler(0, dz > 0 ? 0 : 180, 0);
            int n = Mathf.Max(4, Mathf.RoundToInt(rise / 0.2f));
            float sr = run / n, sh = rise / n;
            for (int i = 0; i < n; i++)
                Slab("Tread" + i, t, new Vector3(0, sh * (i + 1) - 0.03f, sr * (i + 0.5f)), new Vector3(w, 0.06f, sr + 0.04f), K.beam, 1f, false);
            foreach (int s in new[] { -1, 1 })
            {
                Rod("Stringer", t, new Vector3(s * (w * 0.5f + 0.04f), 0, 0), new Vector3(s * (w * 0.5f + 0.04f), rise, run), 0.12f, K.beam);
                Rod("HandRail", t, new Vector3(s * (w * 0.5f + 0.04f), 0.95f, 0), new Vector3(s * (w * 0.5f + 0.04f), rise + 0.95f, run), 0.06f, K.beam);
                for (int k = 0; k <= 3; k++)
                {
                    float f = k / 3f;
                    Rod("Baluster", t, new Vector3(s * (w * 0.5f + 0.04f), rise * f, run * f), new Vector3(s * (w * 0.5f + 0.04f), rise * f + 0.95f, run * f), 0.05f, K.beam);
                }
            }
            float ang = Mathf.Atan2(rise, run);
            var ramp = Slab("Ramp", t, Vector3.zero, new Vector3(w, 0.3f, Mathf.Sqrt(run * run + rise * rise) + 0.3f), K.beam, 1f);
            ramp.GetComponent<MeshRenderer>().enabled = false;
            GameObjectUtility.SetStaticEditorFlags(ramp, 0);
            ramp.transform.localPosition = new Vector3(0, rise * 0.5f - 0.15f * Mathf.Cos(ang), run * 0.5f + 0.15f * Mathf.Sin(ang));
            ramp.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0, 0);
            ProbeLit(t.gameObject);
        }

        /// <summary>Parmaklık dizisi (x boyunca a→b, z sabit). gapC/gapW: kapı boşluğu. Çarpışma kutu.</summary>
        static void BarRow(Transform root, float x0, float x1, float z, float h, float gapC, float gapW)
        {
            var t = new GameObject("Bars").transform;
            t.SetParent(root, false);
            void Seg(float a, float b)
            {
                if (b - a < 0.1f) return;
                for (float x = a + 0.09f; x < b; x += 0.17f) Rod("Bar", t, new Vector3(x, 0, z), new Vector3(x, h, z), 0.035f, K.iron);
                foreach (float y in new[] { 0.12f, h * 0.55f, h - 0.08f }) Slab("Rail", t, new Vector3((a + b) * 0.5f, y, z), new Vector3(b - a, 0.07f, 0.06f), K.iron, 1f, false);
                var c = new GameObject("Block").AddComponent<BoxCollider>();
                c.transform.SetParent(t, false);
                c.transform.localPosition = new Vector3((a + b) * 0.5f, h * 0.5f, z);
                c.size = new Vector3(b - a, h, 0.12f);
            }
            Seg(x0, gapC - gapW * 0.5f);
            Seg(gapC + gapW * 0.5f, x1);
            ProbeLit(t.gameObject);
        }

        static Transform BuildDungeon(Vector3 center)
        {
            var root = new GameObject("RoomB_Sarnic").transform; // ad korunuyor (Shots/HUD bulur)
            root.position = center;
            float top = DgSpring + DgRise;
            var stone = K.castleStone; var brick = K.cellarBrick;
            var bone = MakeMat("K_Bone", new Color(0.6f, 0.55f, 0.46f));
            var voidM = MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f));
            var straw = KTex("K_Straw", "dirt_floor", new Color(0.55f, 0.48f, 0.36f), 0.05f, 1.2f); // düz turuncu yüzey yerine dokulu kirli saman
            var dgFloor = KTex("K_DungeonFloor", "cobblestone_floor_08", new Color(0.5f, 0.5f, 0.52f), 0.35f, 1.3f); // mahzenden farklı; derzler turuncu parlamasın
            var burlap = MakeMat("K_Burlap", new Color(0.36f, 0.29f, 0.2f)); burlap.SetFloat("_Smoothness", 0.05f);
            var scratch = MakeMat("K_Scratch", new Color(0.62f, 0.58f, 0.5f));
            var rust = MakeMat("K_RustStain", new Color(0.2f, 0.1f, 0.06f)); rust.SetFloat("_Smoothness", 0.4f);
            const float HX = DgHX, HZ = DgHZ;
            const float ChX0 = 8.5f, ChX1 = 13f, ChZ = 5.5f, ChSpring = 3.8f, ChRise = 1.4f; // Kayıt Şapeli

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), HX * 2 + 1, HZ * 2 + 1, 0.5f, dgFloor, 3, 5);
            Wall(root, "WallE", new Vector3(HX + 0.25f, 0, 0), false, HZ * 2 + 1, DgSpring + 0.6f, stone, 2f, new Opening(0f, 7f, 0f, 4.4f)); // şapel kemeri
            Wall(root, "WallW", new Vector3(-HX - 0.25f, 0, 0), false, HZ * 2 + 1, DgSpring + 0.6f, stone, 2f, new Opening(0f, 3f, 0f, 3f)); // kasa kapısı
            Wall(root, "WallN", new Vector3(0, 0, HZ + 0.25f), true, HX * 2 + 1, top + 0.3f, stone, 2f);
            Wall(root, "WallS", new Vector3(0, 0, -HZ - 0.25f), true, HX * 2 + 1, top + 0.3f, stone, 2f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, top + 0.6f, 0), new Vector3(HX * 2 + 2, 0.5f, HZ * 2 + 2), stone, 2f, false), 0);
            float[] colZ = { -7.6f, -2.6f, 2.6f, 7.6f };
            foreach (int sx in new[] { -1, 1 })
            {
                var bay = new GameObject("Bay" + sx).transform;
                bay.SetParent(root, false);
                bay.localPosition = new Vector3(sx * 4f, 0, 0);
                bay.localRotation = Quaternion.Euler(0, 90, 0);
                VaultStrip(bay, "Vault", 0, HZ * 2 + 0.5f, 4.1f, DgSpring, DgRise, 0.3f, brick, 14, 2f);
                foreach (float z in colZ)
                    VaultStrip(bay, "Rib" + z, -z, 0.5f, 3.96f, DgSpring, DgRise - 0.16f, 0.28f, stone, 14, 1.2f);
            }
            Slab("Architrave", root, new Vector3(0, DgSpring - 0.2f, 0), new Vector3(0.6f, 0.45f, HZ * 2), stone, 1.5f, false);
            foreach (float z in colZ)
            {
                var col = new GameObject("Column").transform;
                col.SetParent(root, false);
                col.localPosition = new Vector3(0, 0, z);
                var bc = col.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0, DgSpring * 0.5f, 0); bc.size = new Vector3(0.8f, DgSpring, 0.8f);
                Slab("Base", col, new Vector3(0, 0.22f, 0), new Vector3(1.1f, 0.44f, 1.1f), stone, 1.2f, false);
                Slab("Plinth", col, new Vector3(0, 0.51f, 0), new Vector3(0.9f, 0.14f, 0.9f), stone, 1.2f, false);
                float sh = DgSpring - 1.2f;
                ProbeLit(Prim("Shaft", col, PrimitiveType.Cylinder, new Vector3(0, 0.58f + sh * 0.5f, 0), new Vector3(0.68f, sh * 0.5f, 0.68f), stone));
                Slab("Neck", col, new Vector3(0, DgSpring - 0.58f, 0), new Vector3(0.82f, 0.12f, 0.82f), stone, 1.2f, false);
                Slab("Cap", col, new Vector3(0, DgSpring - 0.37f, 0), new Vector3(1.1f, 0.32f, 1.1f), stone, 1.2f, false);
            }
            foreach (int s in new[] { -1, 1 }) Slab("Corbel", root, new Vector3(0, DgSpring - 0.55f, s * (HZ - 0.15f)), new Vector3(0.6f, 0.4f, 0.3f), stone, 1.2f, false);
            // Kasa kapısı çerçevesi
            foreach (int s in new[] { -1, 1 })
                Slab("VaultJamb", root, new Vector3(-HX + 0.08f, 1.6f, s * 1.72f), new Vector3(0.18f, 3.2f, 0.44f), stone, 1.2f, false);
            Slab("VaultLintel", root, new Vector3(-HX + 0.08f, 3.25f, 0), new Vector3(0.18f, 0.3f, 3.9f), stone, 1.2f, false);

            // --- Kayıt Şapeli (doğu; fresk arka duvarda) ---
            {
                float cx = (ChX0 + ChX1) * 0.5f, cw = ChX1 - ChX0 + 0.5f;
                Deck(root, "ChapelFloor", new Vector3(cx - 0.25f, -0.25f, 0), cw, ChZ * 2 + 1, 0.5f, K.cellarFloor, 1, 2);
                Wall(root, "ChapelN", new Vector3(cx - 0.25f, 0, ChZ + 0.25f), true, cw, ChSpring + ChRise + 0.4f, stone, 2f);
                Wall(root, "ChapelS", new Vector3(cx - 0.25f, 0, -ChZ - 0.25f), true, cw, ChSpring + ChRise + 0.4f, stone, 2f);
                Wall(root, "ChapelE", new Vector3(ChX1 + 0.25f, 0, 0), false, ChZ * 2 + 1, ChSpring + ChRise + 0.4f, stone, 2f);
                var cv = new GameObject("ChapelVault").transform;
                cv.SetParent(root, false);
                cv.localPosition = new Vector3(cx - 0.25f, 0, 0);
                cv.localRotation = Quaternion.Euler(0, 90, 0);
                VaultStrip(cv, "Vault", 0, ChZ * 2 + 0.6f, (cw + 0.1f) * 0.5f, ChSpring, ChRise, 0.3f, brick, 10, 2f);
                GameObjectUtility.SetStaticEditorFlags(Slab("ChapelRoof", root, new Vector3(cx, ChSpring + ChRise + 0.5f, 0), new Vector3(cw + 1f, 0.4f, ChZ * 2 + 1.5f), stone, 2f, false), 0);
                ArchTrim(root, "ChapelArch", new Vector3(HX, 0, 0), Vector3.left, 7.3f, 6.2f, false);
                // Kemer alnı: kafatası + yazıt levhası
                Slab("ArchPlaque", root, new Vector3(HX - 0.06f, 4.95f, 0), new Vector3(0.08f, 0.55f, 1.8f), K.iron, 1f, false);
                Skull(root, new Vector3(HX - 0.2f, 5.3f, 0), -90f, 1.5f, bone, voidM);
                // İki açık demir kapı kanadı (duvara yaslı)
                KitR("IronGate", root, new Vector3(HX - 0.35f, 0, 3.6f + 1.4f), Quaternion.Euler(0, 90, 0), 0.5f);
                KitR("IronGate", root, new Vector3(HX - 0.35f, 0, -3.6f - 1.4f), Quaternion.Euler(0, 90, 0), 0.5f);
                // Fresk önünde mum sırası + diz çökme sırası + kürsü
                for (int i = 0; i < 6; i++) Kit(i % 2 == 0 ? "CandleCluster" : "Candle", root, new Vector3(ChX1 - 0.35f, 0, -4.2f + i * 1.7f), i * 40f);
                Slab("Kneeler", root, new Vector3(ChX1 - 1.9f, 0.22f, 0), new Vector3(0.45f, 0.12f, 6f), K.beam, 1f);
                foreach (float z in new[] { -2.8f, 0f, 2.8f }) Slab("KneelerLeg", root, new Vector3(ChX1 - 1.9f, 0.08f, z), new Vector3(0.4f, 0.16f, 0.12f), K.beam, 1f, false);
                Kit("CandelabraFloor", root, new Vector3(ChX1 - 0.6f, 0, ChZ - 0.5f), 200f);
                Kit("CandelabraFloor", root, new Vector3(ChX1 - 0.6f, 0, -ChZ + 0.5f), 20f);
                Kit("L_Candelabra", root, new Vector3(ChX1 - 1.3f, 1.9f, ChZ - 0.9f));
                Kit("L_Candelabra", root, new Vector3(ChX1 - 1.3f, 1.9f, -ChZ + 0.9f));
                // Kuzey duvar: kemik nişi; güney duvar: kayıt rafı
                ArchTrim(root, "BoneNiche", new Vector3(cx, 0.6f, ChZ), Vector3.back, 1.6f, 2.0f, true);
                for (int i = 0; i < 3; i++) Skull(root, new Vector3(cx - 0.45f + i * 0.45f, 0.62f, ChZ - 0.25f), 180f + (i - 1) * 15f, 1f, bone, voidM);
                for (int i = 0; i < 5; i++) Prim("Femur", root, PrimitiveType.Capsule, new Vector3(cx - 0.6f + i * 0.3f, 0.65f, ChZ - 0.15f), new Vector3(0.05f, 0.22f, 0.05f), bone).transform.localRotation = Quaternion.Euler(0, 0, 90);
                Kit("Bookshelf", root, new Vector3(cx, 0, -ChZ + 0.25f), 0f, 0.85f);
                Books(root, new Vector3(cx, 0, -ChZ + 0.25f), 0f, 0.85f);
                Kit("PaperScatter", root, new Vector3(ChX1 - 2.6f, 0, -1.6f), 70f);
                KitR("Cobweb", root, new Vector3(ChX1 - 0.03f, ChSpring + 0.2f, ChZ - 0.03f), Quaternion.Euler(0, 0, 180), 1.2f);
                KitR("Cobweb", root, new Vector3(ChX1 - 0.03f, 0, -ChZ + 0.03f), Quaternion.Euler(0, 180, 0), 0.9f);
                KitProbes(root, new Vector3(cx, (ChSpring + ChRise) * 0.5f, 0), new Vector3(ChX1 - ChX0, ChSpring + ChRise, ChZ * 2), 3, 3, 4);
            }

            // --- Kasa odası (kapının arkası; mahzen geçidine açılır) ---
            Deck(root, "VaultFloor", new Vector3(-10.625f, -0.25f, 0), 4.25f, 7f, 0.5f, K.cellarFloor, 1, 1);
            Deck(root, "VaultCeiling", new Vector3(-10.625f, 4.25f, 0), 4.25f, 7f, 0.5f, brick, 1, 1);
            Wall(root, "VaultWallW", new Vector3(-12.75f, 0, 0), false, 7f, 4f, stone, 2f, new Opening(0f, 3f, 0f, 3f)); // mahzen geçidi (bulmaca 2)
            Wall(root, "VaultWallS", new Vector3(-10.625f, 0, -3.25f), true, 4.25f, 4f, stone, 2f);
            Wall(root, "VaultWallN", new Vector3(-10.625f, 0, 3.25f), true, 4.25f, 4f, stone, 2f);
            Kit("Chest", root, new Vector3(-11.9f, 0, -2.3f), 90f);
            Kit("CandleCluster", root, new Vector3(-12.3f, 0, 2.5f));
            Kit("WallSconce", root, new Vector3(-10.6f, 2.2f, -2.95f), 0f);
            Kit("L_Candle", root, new Vector3(-10.6f, 2.3f, -2.4f));
            KitR("Cobweb", root, new Vector3(-12.47f, 3.95f, 2.97f), Quaternion.Euler(0, 0, -90), 1.2f);
            Kit("Crate", root, new Vector3(-9.3f, 0, 2.6f), 20f, 0.8f);
            Kit("Goblet1", root, new Vector3(-9.3f, 0.48f, 2.6f));
            Kit("Bottle2", root, new Vector3(-9.1f, 0.48f, 2.5f));

            var det = new GameObject("Details").transform;
            det.SetParent(root, false);

            // --- Kuzey: üç hücre (x -8..-2.7, -2.7..2.7, 2.7..8; z 9.2..13). Ortadaki açık. ---
            const float CellZ = 9.2f, CellH = 3.3f;
            foreach (float x in new[] { -2.7f, 2.7f })
            {
                Slab("CellWall", root, new Vector3(x, CellH * 0.5f, (CellZ + HZ) * 0.5f), new Vector3(0.4f, CellH, HZ - CellZ), stone, 1.5f);
                Slab("CellPost", root, new Vector3(x, CellH * 0.5f, CellZ), new Vector3(0.5f, CellH, 0.5f), stone, 1.2f);
            }
            Slab("CellLintel", root, new Vector3(0, CellH + 0.2f, CellZ), new Vector3(HX * 2, 0.4f, 0.5f), stone, 1.5f);
            BarRow(root, -HX, -2.95f, CellZ, CellH, -5.4f, 0f);
            BarRow(root, 2.95f, HX, CellZ, CellH, 5.4f, 0f);
            BarRow(root, -2.45f, 2.45f, CellZ, CellH, 0f, 1.3f);
            // Açık hücre kapısı (menteşede dönmüş)
            {
                var g = new GameObject("CellDoor").transform;
                g.SetParent(det, false);
                g.localPosition = new Vector3(0.65f, 0, CellZ);
                g.localRotation = Quaternion.Euler(0, -110f, 0);
                for (int i = 0; i < 7; i++) Rod("Bar", g, new Vector3(-0.1f - i * 0.17f, 0.05f, 0), new Vector3(-0.1f - i * 0.17f, CellH - 0.2f, 0), 0.035f, K.iron);
                foreach (float y in new[] { 0.15f, 1.6f, CellH - 0.3f }) Slab("Rail", g, new Vector3(-0.6f, y, 0), new Vector3(1.25f, 0.07f, 0.06f), K.iron, 1f, false);
                Prim("Lock", g, PrimitiveType.Cube, new Vector3(-1.15f, 1.1f, 0), new Vector3(0.14f, 0.2f, 0.1f), K.iron);
            }
            for (int c = 0; c < 3; c++)
            {
                float cx = (c - 1) * 5.4f;
                Slab("Straw", det, new Vector3(cx + 0.6f, 0.015f, 11.6f), new Vector3(2.6f, 0.03f, 2f), straw, 1f, false);
                Kit("PrisonCot", root, new Vector3(cx - 1.2f, 0, 11.6f), 90f + c * 3f, 0.85f);
                Kit("Bucket", root, new Vector3(cx + 1.8f, 0, 12.4f), c * 70f);
                // Prangalar
                foreach (float dx in new[] { -0.4f, 0.4f })
                {
                    Rod("Ring", det, new Vector3(cx + dx, 2.1f, HZ - 0.02f), new Vector3(cx + dx, 2.1f, HZ - 0.14f), 0.04f, K.iron);
                    Rod("Chain", det, new Vector3(cx + dx, 2.1f, HZ - 0.12f), new Vector3(cx + dx * 1.3f, 1.0f + c * 0.1f, HZ - 0.18f), 0.03f, K.iron);
                    MeshObj("Cuff", det, new Vector3(cx + dx * 1.3f, 0.92f + c * 0.1f, HZ - 0.18f), RingMesh(0.09f, 0.025f, 0.05f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                Kit("Bowl2", root, new Vector3(cx + 0.9f, 0.02f, 10.2f), c * 50f);
            }
            // Detay 1: hücre duvarına kazınmış çentikler (5'li gruplar)
            for (int g = 0; g < 7; g++)
                for (int k = 0; k < 5; k++)
                {
                    float z = 10.0f + g * 0.38f, y = 1.4f + (g % 3) * 0.28f;
                    if (k < 4) Slab("Tally", det, new Vector3(-2.49f, y, z + k * 0.06f), new Vector3(0.01f, 0.22f, 0.015f), scratch, 1f, false);
                    else Slab("TallyX", det, new Vector3(-2.49f, y, z + 0.09f), new Vector3(0.01f, 0.03f, 0.3f), scratch, 1f, false).transform.localRotation = Quaternion.Euler(35, 0, 0);
                }
            // Detay 2: ortadaki hücrede kaçış tüneli (sökülmüş taş, moloz, kaşık)
            Slab("TunnelHole", det, new Vector3(0.9f, 0.4f, HZ - 0.01f), new Vector3(0.8f, 0.6f, 0.02f), voidM, 1f, false);
            for (int i = 0; i < 9; i++)
                ProbeLit(Prim("Rubble", det, PrimitiveType.Cube, new Vector3(0.4f + (i % 3) * 0.35f, 0.06f + (i / 3) * 0.02f, HZ - 0.4f - (i / 3) * 0.25f), new Vector3(0.2f + (i % 2) * 0.08f, 0.12f, 0.16f), stone));
            Slab("PulledStone", det, new Vector3(1.6f, 0.15f, HZ - 0.9f), new Vector3(0.55f, 0.3f, 0.35f), stone, 1f, false);
            Kit("Spoon", root, new Vector3(1.2f, 0.01f, HZ - 1.1f), 40f);
            Kit("Rat", root, new Vector3(-4.2f, 0, 10.5f), 150f);
            Kit("Mousetrap", root, new Vector3(4.6f, 0, 10.0f), 20f);
            // Detay 3: hücre önünde fare + ekmek kabuğu
            Kit("Rat", root, new Vector3(2.0f, 0, 8.3f), -60f);
            Prim("Bread", det, PrimitiveType.Sphere, new Vector3(2.25f, 0.04f, 8.15f), new Vector3(0.16f, 0.07f, 0.1f), MakeMat("K_Bread", new Color(0.45f, 0.3f, 0.15f)));
            Kit("WindowLancet", root, new Vector3(-4f, 4.6f, HZ), 180f, 1.0f);
            Kit("WindowLancet", root, new Vector3(4f, 4.6f, HZ), 180f, 1.0f);

            // --- Güneybatı: gözcü platformu (y 2.6) + serbest merdiven ---
            const float PlY = 2.6f, PlX1 = -4.2f, PlZ1 = -9.6f;
            Deck(root, "WatchDeck", new Vector3((-HX + PlX1) * 0.5f, PlY - 0.1f, (-HZ + PlZ1) * 0.5f), PlX1 + HX, PlZ1 + HZ, 0.2f, K.beam, 1, 1);
            foreach (var p in new[] { new Vector3(PlX1 + 0.15f, 0, PlZ1 + 0.15f), new Vector3(PlX1 + 0.15f, 0, -HZ + 0.2f), new Vector3(-HX + 0.2f, 0, PlZ1 + 0.15f) })
                Slab("WatchPost", root, p + Vector3.up * (PlY * 0.5f), new Vector3(0.26f, PlY, 0.26f), K.beam, 1f);
            Slab("WatchBeam", root, new Vector3((-HX + PlX1) * 0.5f, PlY - 0.3f, PlZ1 + 0.15f), new Vector3(PlX1 + HX, 0.25f, 0.22f), K.beam, 1f, false);
            Slab("WatchBeam", root, new Vector3(PlX1 + 0.15f, PlY - 0.3f, (-HZ + PlZ1) * 0.5f), new Vector3(0.22f, 0.25f, PlZ1 + HZ), K.beam, 1f, false);
            {
                // Korkuluk (merdiven ağzı x -6.05..-4.75 açık)
                void Rail(Vector3 a, Vector3 b)
                {
                    var d = b - a; float len = d.magnitude;
                    Rod("Rail", det, a + Vector3.up * (PlY + 1f), b + Vector3.up * (PlY + 1f), 0.08f, K.beam);
                    Rod("MidRail", det, a + Vector3.up * (PlY + 0.5f), b + Vector3.up * (PlY + 0.5f), 0.05f, K.beam);
                    int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.9f));
                    for (int i = 0; i <= n; i++) Rod("Post", det, a + d * (i / (float)n) + Vector3.up * PlY, a + d * (i / (float)n) + Vector3.up * (PlY + 1f), 0.07f, K.beam);
                    var c = new GameObject("RailBlock").AddComponent<BoxCollider>();
                    c.transform.SetParent(root, false);
                    c.transform.localPosition = (a + b) * 0.5f + Vector3.up * (PlY + 0.55f);
                    c.transform.localRotation = Quaternion.FromToRotation(Vector3.right, d.normalized);
                    c.size = new Vector3(len, 1.1f, 0.12f);
                }
                Rail(new Vector3(-HX + 0.1f, 0, PlZ1), new Vector3(-6.1f, 0, PlZ1));
                Rail(new Vector3(-4.7f, 0, PlZ1), new Vector3(PlX1, 0, PlZ1));
                Rail(new Vector3(PlX1, 0, PlZ1), new Vector3(PlX1, 0, -HZ + 0.1f));
            }
            WoodStair(root, "WatchStair", new Vector3(-5.4f, 0, -4.6f), PlY, 5.0f, -1f, 1.25f);
            Kit("ChairWood", root, new Vector3(-6.6f, PlY, -11.6f), 20f, 0.95f);
            Kit("Barrel3", root, new Vector3(-7.4f, PlY, -12.4f), 0f, 0.85f);
            Kit("Crate", root, new Vector3(-5.3f, PlY, -12.5f), 10f, 0.8f);
            Kit("Lantern", root, new Vector3(-5.3f, PlY + 0.48f, -12.5f), 30f);
            Kit("Shield", root, new Vector3(-6.4f, PlY + 2.0f, -HZ + 0.05f), 0f);
            KitR("Sword", root, new Vector3(-7.7f, PlY + 0.05f, -10.4f), Quaternion.Euler(0, 90, 12), 1f);
            Kit("Goblet2", root, new Vector3(-7.4f, PlY + 0.93f, -12.4f));
            // Platformun altı: depo
            Kit("CrateStack", root, new Vector3(-7.0f, 0, -12.0f), 0f);
            Kit("Barrel4", root, new Vector3(-5.2f, 0, -12.4f), 30f);
            Kit("Broom", root, new Vector3(-4.6f, 0, -12.8f), 10f);
            Kit("Spade", root, new Vector3(-7.75f, 0, -10.0f), 90f);
            for (int i = 0; i < 4; i++) ProbeLit(Prim("Sack", det, PrimitiveType.Sphere, new Vector3(-7.4f + (i % 2) * 0.55f, 0.25f + (i / 2) * 0.38f, -8.6f - (i % 2) * 0.2f), new Vector3(0.55f, 0.45f, 0.48f), burlap));

            // --- Güney girişi (O buradan girer) ---
            Kit("Door", root, new Vector3(0, 0, -HZ + 0.12f), 0f, 1.2f);
            ArchTrim(root, "EntryArch", new Vector3(0, 0, -HZ), Vector3.forward, 3.1f, 4.5f, true);
            Kit("WallSconce", root, new Vector3(-2.3f, 2.4f, -HZ + 0.03f));
            Kit("WallSconce", root, new Vector3(2.3f, 2.4f, -HZ + 0.03f));
            Kit("L_Candle", root, new Vector3(-2.3f, 2.5f, -HZ + 0.7f));
            Kit("L_Candle", root, new Vector3(2.3f, 2.5f, -HZ + 0.7f));
            Kit("Banner", root, new Vector3(4.5f, 6.2f, -HZ), 0f, 1.6f);

            // --- Güneydoğu: gardiyan masası (Detay 4: kayıt defteri, gözlük, köstekli saat, anahtar panosu) ---
            Kit("DiningTable", root, new Vector3(5.2f, 0, -10.6f), 0f, 0.8f);
            Kit("ChairWood", root, new Vector3(5.0f, 0, -11.7f), 10f, 0.95f);
            Kit("BookStack", root, new Vector3(4.5f, 0.71f, -10.5f), 15f);
            Kit("PaperPile", root, new Vector3(5.4f, 0.71f, -10.4f), 80f);
            Kit("Spectacles", root, new Vector3(5.6f, 0.71f, -10.9f), 200f);
            Kit("PocketWatch", root, new Vector3(5.9f, 0.71f, -10.5f), 30f);
            Kit("CandlestickWood", root, new Vector3(6.1f, 0.71f, -10.2f));
            Kit("L_Candle", root, new Vector3(6.1f, 1.3f, -10.2f));
            Kit("Jug", root, new Vector3(4.2f, 0.71f, -10.1f), 60f);
            Kit("Goblet0", root, new Vector3(4.0f, 0.71f, -10.5f));
            Prim("Quill", det, PrimitiveType.Cylinder, new Vector3(5.1f, 0.74f, -10.6f), new Vector3(0.008f, 0.12f, 0.008f), scratch).transform.localRotation = Quaternion.Euler(70, 30, 0);
            Prim("Inkwell", det, PrimitiveType.Cylinder, new Vector3(5.0f, 0.74f, -10.3f), new Vector3(0.06f, 0.03f, 0.06f), K.bottleGlass);
            Kit("Nightstand", root, new Vector3(7.5f, 0, -12.4f), -45f, 0.9f);
            Kit("OilLamp", root, new Vector3(7.5f, 0.63f, -12.4f), 0f);
            Kit("Cabinet", root, new Vector3(HX - 0.45f, 0, -8.3f), -90f, 0.9f);
            Kit("Rug", root, new Vector3(5.2f, 0, -10.8f), 0f, 0.6f);
            {   // Anahtar panosu (doğu duvarı)
                Slab("KeyBoard", det, new Vector3(HX - 0.04f, 2.0f, -10.8f), new Vector3(0.05f, 0.7f, 1.4f), K.beam, 1f, false);
                for (int i = 0; i < 6; i++)
                {
                    float z = -11.35f + i * 0.22f;
                    Rod("Peg", det, new Vector3(HX - 0.06f, 2.15f, z), new Vector3(HX - 0.16f, 2.15f, z), 0.02f, K.iron);
                    if (i == 3) continue; // eksik anahtar (kaçan mahkûm)
                    MeshObj("KeyRing", det, new Vector3(HX - 0.15f, 2.02f, z), RingMesh(0.05f, 0.012f, 0.012f), K.brass).transform.localRotation = Quaternion.Euler(0, 0, 90);
                    Slab("Key", det, new Vector3(HX - 0.15f, 1.88f, z), new Vector3(0.015f, 0.18f, 0.02f), K.brass, 1f, false);
                }
            }
            Kit("PaintingPortrait", root, new Vector3(HX - 0.04f, 2.6f, -7.0f), -90f, 0.8f);
            // Masa köşesini doldur: duvar rafı, sandık, erzak, yer kâğıtları
            Kit("ShelfTall", root, new Vector3(3.6f, 0, -HZ + 0.35f), 0f, 0.9f);
            Kit("Bottle0", root, new Vector3(3.3f, 1.05f, -HZ + 0.35f), 20f);
            Kit("Bottle2", root, new Vector3(3.7f, 1.05f, -HZ + 0.35f), 70f);
            Kit("BookSet", root, new Vector3(3.6f, 1.55f, -HZ + 0.35f), 0f);
            Kit("MetalJug", root, new Vector3(3.9f, 0.55f, -HZ + 0.35f), 0f);
            Kit("Chest", root, new Vector3(6.8f, 0, -HZ + 0.5f), 0f, 0.85f);
            Kit("Lantern", root, new Vector3(6.8f, 0.55f, -HZ + 0.5f), 30f);
            Kit("CrateStack", root, new Vector3(HX - 0.7f, 0, -HZ + 0.7f), 15f, 0.8f);
            Kit("Stool", root, new Vector3(6.3f, 0, -9.6f), 40f);
            Kit("Tray", root, new Vector3(6.3f, 0.45f, -9.6f), 10f);
            Kit("Goblet1", root, new Vector3(6.35f, 0.47f, -9.55f));
            Kit("PaperScatter", root, new Vector3(4.6f, 0.01f, -9.4f), 120f);
            Kit("Mousetrap", root, new Vector3(7.9f, 0, -10.2f), 60f);
            Kit("Rat", root, new Vector3(HX - 0.6f, 0, -10.0f), 200f);
            Kit("CandleCluster", root, new Vector3(HX - 0.4f, 0, -11.1f), 0f);

            // --- Doğu (şapelin kuzeyi): sorgu köşesi — sandalye, mangal, aletler ---
            {
                var chair = new GameObject("InterrogationChair").transform;
                chair.SetParent(det, false);
                chair.localPosition = new Vector3(6.2f, 0, 7.4f);
                chair.localRotation = Quaternion.Euler(0, -120f, 0);
                Slab("Seat", chair, new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0.08f, 0.6f), K.beam, 1f, false);
                Slab("Back", chair, new Vector3(0, 1.1f, 0.27f), new Vector3(0.6f, 1.2f, 0.08f), K.beam, 1f, false);
                foreach (var (x, z) in new[] { (-0.26f, -0.26f), (0.26f, -0.26f), (-0.26f, 0.26f), (0.26f, 0.26f) }) Slab("Leg", chair, new Vector3(x, 0.25f, z), new Vector3(0.07f, 0.5f, 0.07f), K.beam, 1f, false);
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("Arm", chair, new Vector3(s * 0.3f, 0.75f, 0), new Vector3(0.08f, 0.06f, 0.6f), K.beam, 1f, false);
                    Slab("Strap", chair, new Vector3(s * 0.3f, 0.79f, -0.08f), new Vector3(0.1f, 0.03f, 0.08f), K.leather, 1f, false);
                    Slab("AnkleStrap", chair, new Vector3(s * 0.26f, 0.15f, -0.27f), new Vector3(0.1f, 0.06f, 0.03f), K.leather, 1f, false);
                }
                Slab("NeckStrap", chair, new Vector3(0, 1.45f, 0.22f), new Vector3(0.45f, 0.06f, 0.03f), K.leather, 1f, false);
                Slab("Stain", det, new Vector3(6.2f, 0.004f, 7.4f), new Vector3(1.1f, 0.006f, 0.9f), rust, 1f, false);
            }
            Brazier(root, new Vector3(4.6f, 0, 6.2f), 0.8f, true);
            Kit("Workbench", root, new Vector3(HX - 0.5f, 0, 5.6f + 1.4f), -90f, 0.9f);
            Kit("Hammer", root, new Vector3(HX - 0.55f, 0.88f, 6.6f), 30f);
            Kit("HandSaw", root, new Vector3(HX - 0.6f, 0.88f, 7.3f), 80f);
            Kit("Hatchet", root, new Vector3(HX - 0.45f, 0.88f, 7.8f), 160f);
            Kit("Wrench", root, new Vector3(HX - 0.6f, 0.88f, 7.0f), 10f);
            Kit("BullHead", root, new Vector3(HX - 0.05f, 3.3f, 7.1f), -90f);
            for (int i = 0; i < 4; i++)
            {   // duvarda asılı zincir + kancalar
                float z = 6.0f + i * 0.5f;
                Rod("Hook", det, new Vector3(HX - 0.03f, 2.6f, z), new Vector3(HX - 0.2f, 2.55f, z), 0.03f, K.iron);
                Rod("HangChain", det, new Vector3(HX - 0.2f, 2.55f, z), new Vector3(HX - 0.22f, 1.7f - (i % 2) * 0.3f, z), 0.025f, K.iron);
            }

            // --- Doğu (şapelin güneyi): silah rafı ---
            {
                Slab("RackBoard", det, new Vector3(HX - 0.05f, 1.6f, -5.6f), new Vector3(0.06f, 1.6f, 2.4f), K.beam, 1f, false);
                Slab("RackShelf", det, new Vector3(HX - 0.25f, 0.4f, -5.6f), new Vector3(0.4f, 0.06f, 2.4f), K.beam, 1f, false);
                KitR("Sword", root, new Vector3(HX - 0.2f, 0.45f, -6.5f), Quaternion.Euler(0, -90, -6), 1f);
                KitR("Axe", root, new Vector3(HX - 0.2f, 0.45f, -5.9f), Quaternion.Euler(0, -90, 4), 1f);
                KitR("Mace", root, new Vector3(HX - 0.2f, 0.45f, -5.3f), Quaternion.Euler(0, -90, -4), 1f);
                KitR("WarHammer", root, new Vector3(HX - 0.2f, 0.45f, -4.7f), Quaternion.Euler(0, -90, 6), 1f);
                Kit("Shield", root, new Vector3(HX - 0.05f, 3.2f, -5.6f), -90f, 0.9f);
                Kit("Barrel", root, new Vector3(HX - 0.6f, 0, -3.9f), 40f, 0.9f);
            }

            // --- Batı (kapının kuzeyi): erzak — fıçılar, çuvallar, su teknesi ---
            Kit("BarrelRack", root, new Vector3(-HX + 0.6f, 0, 6.2f), 90f);
            Kit("Barrel2", root, new Vector3(-HX + 0.6f, 0, 4.2f), 0f);
            Kit("Barrel3", root, new Vector3(-6.4f, 0, 4.0f), 50f);
            Kit("CrateLong", root, new Vector3(-6.6f, 0, 7.6f), 90f);
            Kit("Crate", root, new Vector3(-HX + 0.5f, 0, 8.3f), 15f);
            Kit("Crate", root, new Vector3(-HX + 0.5f, 0.6f, 8.3f), 50f, 0.8f);
            for (int i = 0; i < 3; i++) ProbeLit(Prim("Sack", det, PrimitiveType.Sphere, new Vector3(-5.4f - i * 0.5f, 0.24f, 8.4f + (i % 2) * 0.2f), new Vector3(0.55f, 0.48f, 0.5f), burlap));
            {
                Slab("TroughBase", det, new Vector3(-HX + 0.55f, 0.3f, 2.4f), new Vector3(0.8f, 0.6f, 1.7f), stone, 1f, false);
                Slab("TroughWater", det, new Vector3(-HX + 0.55f, 0.58f, 2.4f), new Vector3(0.6f, 0.02f, 1.5f), MakeMat("K_DarkWater", new Color(0.02f, 0.03f, 0.035f)), 1f, false);
                Kit("Bucket2", root, new Vector3(-6.9f, 0, 2.6f), 20f);
                Kit("MetalJug", root, new Vector3(-HX + 0.55f, 0.6f, 1.45f), 0f);
            }
            Kit("WallSconce", root, new Vector3(-HX + 0.05f, 2.3f, 3.0f), 90f);
            Kit("WallSconce", root, new Vector3(-HX + 0.05f, 2.3f, -3.0f), 90f);
            Kit("L_Candle", root, new Vector3(-HX + 0.7f, 2.4f, 3.0f));
            Kit("L_Candle", root, new Vector3(-HX + 0.7f, 2.4f, -3.0f));
            // Batı (kapının güneyi): çöp, kemik yığını, kırık tabure
            Kit("FoldStool", root, new Vector3(-6.8f, 0, -2.9f), 110f);
            Kit("Basket", root, new Vector3(-7.4f, 0, -4.0f), 0f);
            Kit("Pot", root, new Vector3(-7.5f, 0, -5.2f), 0f);
            Kit("VaseCeramic", root, new Vector3(-7.6f, 0, -6.0f), 40f);

            // --- Orta: Detay 5: asılı gibbet kafesi (içinde iskelet kalıntısı) ---
            {
                var g = new GameObject("Gibbet").transform;
                g.SetParent(det, false);
                g.localPosition = new Vector3(-4f, 1.4f, -0.2f);
                g.localRotation = Quaternion.Euler(0, 20, 6);
                const float r = 0.45f, h = 1.8f;
                MeshObj("RingBottom", g, Vector3.zero, RingMesh(r, 0.03f, 0.04f), K.iron);
                MeshObj("RingMid", g, Vector3.up * h * 0.5f, RingMesh(r, 0.03f, 0.04f), K.iron);
                MeshObj("RingTop", g, Vector3.up * h, RingMesh(r * 0.7f, 0.03f, 0.04f), K.iron);
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI * 2f / 10f;
                    var p0 = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                    var p1 = new Vector3(Mathf.Cos(a) * r, h * 0.5f, Mathf.Sin(a) * r);
                    var p2 = new Vector3(Mathf.Cos(a) * r * 0.7f, h, Mathf.Sin(a) * r * 0.7f);
                    Rod("Bar", g, p0, p1, 0.025f, K.iron);
                    Rod("Bar", g, p1, p2, 0.025f, K.iron);
                }
                Slab("CageFloor", g, new Vector3(0, -0.02f, 0), new Vector3(r * 1.6f, 0.04f, r * 1.6f), K.iron, 1f, false);
                Skull(g, new Vector3(0.1f, 0.02f, 0.05f), 40f, 1f, bone, voidM);
                for (int i = 0; i < 4; i++) Prim("Bone", g, PrimitiveType.Capsule, new Vector3(-0.12f + i * 0.07f, 0.04f, -0.1f), new Vector3(0.035f, 0.16f, 0.035f), bone).transform.localRotation = Quaternion.Euler(90, i * 40f, 0);
                float ceil = DgSpring + DgRise * Mathf.Sqrt(Mathf.Max(0f, 1f - 0.04f / (4.1f * 4.1f))) - 0.1f;
                Rod("GibbetChain", det, new Vector3(-4f, 1.4f + h, -0.2f), new Vector3(-4f, ceil, -0.2f), 0.04f, K.iron);
                Slab("Drip", det, new Vector3(-4.05f, 0.004f, -0.25f), new Vector3(0.7f, 0.006f, 0.6f), rust, 1f, false);
            }
            // Gider ızgaraları + dağınık saman/kağıt
            foreach (var p in new[] { new Vector3(3.6f, 0, 2.0f), new Vector3(-2.2f, 0, -6.2f) })
            {
                Slab("DrainPit", det, p + new Vector3(0, 0.002f, 0), new Vector3(0.7f, 0.004f, 0.5f), voidM, 1f, false);
                for (int i = 0; i < 6; i++) Rod("DrainBar", det, p + new Vector3(-0.3f + i * 0.12f, 0.012f, -0.22f), p + new Vector3(-0.3f + i * 0.12f, 0.012f, 0.22f), 0.025f, K.iron);
            }
            Kit("PaperScatter", root, new Vector3(1.6f, 0, -3.6f), 40f);
            Kit("PaperScatter", root, new Vector3(4.2f, 0, -8.6f), 200f);
            Kit("PaperA", root, new Vector3(-1.2f, 0.005f, 5.2f), 30f);
            Slab("StrawPatch", det, new Vector3(-3.0f, 0.012f, 6.6f), new Vector3(1.4f, 0.02f, 0.9f), straw, 1f, false).transform.localRotation = Quaternion.Euler(0, 25, 0);
            Slab("StrawPatch", det, new Vector3(3.4f, 0.012f, -4.8f), new Vector3(1.0f, 0.02f, 0.7f), straw, 1f, false).transform.localRotation = Quaternion.Euler(0, -40, 0);
            Kit("Rat", root, new Vector3(-6.6f, 0, -9.0f), 80f);
            // Sütunlarda zincirli halka + meşale yuvası
            foreach (float z in new[] { -2.6f, 2.6f })
                foreach (int s in new[] { -1, 1 })
                    MeshObj("PillarRing", det, new Vector3(s * 0.4f, 1.9f, z), RingMesh(0.08f, 0.02f, 0.02f), K.iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
            ProbeLit(det.gameObject);

            // --- Işık: asılı fenerler (2 realtime) + ortada demir avize (baked) ---
            foreach (var p in new[] { new Vector3(4.4f, 3.0f, 1.2f), new Vector3(-3.8f, 3.1f, -7.2f) })
            {
                float off = Mathf.Abs(Mathf.Abs(p.x) - 4f);
                float ceil = DgSpring + DgRise * Mathf.Sqrt(Mathf.Max(0f, 1f - off * off / (4.1f * 4.1f))) - 0.15f;
                Rod("LanternChain", root, p + Vector3.up * 0.5f, new Vector3(p.x, ceil, p.z), 0.022f, K.iron);
                Kit("L_Lantern", root, p, 15f);
            }
            Kit("ChandelierIron", root, new Vector3(4f, 5.2f, -5.6f), 0f, 1.3f);
            Rod("ChandelierChain", root, new Vector3(4f, 5.9f, -5.6f), new Vector3(4f, top - 0.1f, -5.6f), 0.04f, K.iron);
            Kit("L_Chandelier", root, new Vector3(4f, 4.9f, -5.6f));
            Kit("L_Candelabra", root, new Vector3(0, 2.2f, 10.4f));
            Kit("CandleCluster", root, new Vector3(-0.5f, 0, 10.0f));

            // --- Ağlar ---
            KitR("Cobweb", root, new Vector3(HX - 0.03f, DgSpring + 0.25f, HZ - 0.03f), Quaternion.Euler(0, 0, 180), 1.6f);
            KitR("Cobweb", root, new Vector3(-HX + 0.03f, DgSpring + 0.25f, -HZ + 0.03f), Quaternion.Euler(0, 180, 180), 1.6f);
            KitR("Cobweb", root, new Vector3(HX - 0.03f, 0, -HZ + 0.03f), Quaternion.Euler(0, 180, 0), 1.1f);
            KitR("Cobweb", root, new Vector3(-HX + 0.03f, 0, HZ - 0.03f), Quaternion.identity, 1.0f);
            KitR("Cobweb", root, new Vector3(-HX + 0.03f, DgSpring + 0.25f, HZ - 0.03f), Quaternion.Euler(0, 0, -90), 1.4f);

            var skips = new System.Collections.Generic.List<Bounds>();
            foreach (float z in colZ) skips.Add(new Bounds(new Vector3(0, DgSpring * 0.5f, z), new Vector3(1.2f, DgSpring, 1.2f)));
            KitProbes(root, new Vector3(0, top * 0.5f, 0), new Vector3(HX * 2, top, HZ * 2), 5, 4, 8, skips.ToArray());
            KitProbes(root, new Vector3(-10.625f, 2f, 0), new Vector3(4.25f, 4f, 6f), 3, 3, 3);
            KitReflection(root, new Vector3(0, 2.4f, 0), new Vector3(HX * 2, top, HZ * 2));
            return root;
        }

        /// <summary>Bulmaca 1'in O tarafı: freski boyalı panoya, kasa kapısını yuvarlak perçinli kapıya çevirir.</summary>
        static void DressPuzzle1O(Transform roomO, GameObject board, LiftDoor vaultDoor)
        {
            var stone = K.castleStone;
            var fresco = MakeMat("K_Fresco", Color.Lerp(Pal.Bordo, Color.black, 0.35f));
            board.GetComponent<Renderer>().sharedMaterial = fresco;
            foreach (var n in new[] { "HintBoardFrameTop", "HintBoardFrameBottom" })
            {
                var t = roomO.Find(n);
                if (t != null) t.GetComponent<Renderer>().sharedMaterial = K.brass;
            }
            Slab("FrescoCornice", roomO, new Vector3(12.86f, 3.32f, 0), new Vector3(0.3f, 0.14f, 10.1f), stone, 1.2f, false);
            Slab("FrescoSill", roomO, new Vector3(12.86f, 0.7f, 0), new Vector3(0.3f, 0.12f, 10.1f), stone, 1.2f, false);
            foreach (float z in new[] { 2.2f, 0f, -2.2f, 4.85f, -4.85f })
                Slab("FrescoColumn", roomO, new Vector3(12.8f, 2f, z), new Vector3(0.12f, 2.3f, 0.16f), K.brass, 1f, false);

            // Kasa kapısı: gövde koyu demir, önünde yuvarlak perçinli kapak + çark (kapıyla birlikte kalkar)
            vaultDoor.GetComponent<Renderer>().sharedMaterial = K.iron;
            var dt = vaultDoor.transform;
            foreach (var r in dt.GetComponentsInChildren<Renderer>()) r.sharedMaterial = K.iron;
            var bronze = K.bronze;
            var axisX = Quaternion.Euler(0, 0, 90);
            Prim("Disc", dt, PrimitiveType.Cylinder, new Vector3(0.2f, 0, 0), new Vector3(2.9f, 0.05f, 2.9f), K.iron).transform.localRotation = axisX;
            MeshObj("RivetRing", dt, new Vector3(0.26f, 0, 0), RingMesh(1.32f, 0.06f, 0.05f), bronze).transform.localRotation = axisX;
            MeshObj("InnerRing", dt, new Vector3(0.26f, 0, 0), RingMesh(0.95f, 0.04f, 0.04f), K.iron).transform.localRotation = axisX;
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                Prim("Rivet", dt, PrimitiveType.Sphere, new Vector3(0.27f, Mathf.Sin(a) * 1.18f, Mathf.Cos(a) * 1.18f), Vector3.one * 0.07f, bronze);
            }
            foreach (float y in new[] { -0.5f, 0.5f })
                Slab("Strap", dt, new Vector3(0.25f, y, -0.3f), new Vector3(0.04f, 0.14f, 2.2f), K.iron, 1f, false);
            foreach (float y in new[] { -0.9f, 0.9f })
                Slab("Hinge", dt, new Vector3(0.24f, y, 1.35f), new Vector3(0.08f, 0.22f, 0.5f), K.iron, 1f, false);
            var wheel = new GameObject("Wheel").transform;
            wheel.SetParent(dt, false);
            wheel.localPosition = new Vector3(0.42f, 0, 0);
            MeshObj("Rim", wheel, Vector3.zero, RingMesh(0.42f, 0.05f, 0.05f), bronze).transform.localRotation = axisX;
            for (int i = 0; i < 3; i++)
                Slab("Spoke", wheel, Vector3.zero, new Vector3(0.04f, 0.8f, 0.04f), bronze, 1f, false).transform.localRotation = Quaternion.Euler(i * 60f, 0, 0);
            Prim("Hub", wheel, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.16f, K.brass);
            Rod("Stem", dt, new Vector3(0.2f, 0, 0), new Vector3(0.42f, 0, 0), 0.05f, K.iron);
            var anchor = new GameObject("DoorProbeAnchor").transform;
            anchor.SetParent(roomO, false);
            anchor.localPosition = new Vector3(-6.8f, 1.5f, 0);
            foreach (var t in dt.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                var r = t.GetComponent<Renderer>();
                if (r != null) r.probeAnchor = anchor;
                var c = t.GetComponent<Collider>();
                if (c != null && t != dt) Object.DestroyImmediate(c);
            }
        }
    }
}
