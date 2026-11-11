using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun ilk odası: zindan (ref: Docs/Reference/zindan.png). Taş duvar, kuzey-güney yönlü iki tuğla beşik tonoz,
    /// ortada sütunlu kemer dizisi (x=0, z ±4), doğu duvarında fresk (bulmaca 1), batıda yuvarlak kasa kapısı.
    /// Baked; 2 realtime asılı fener. İç ±8, tonoz tepe ~4.9. Arkada kasa odası (x -8.5..-12.75, z ±3).
    /// </summary>
    public static partial class CoopSetup
    {
        const float DgSpring = 3.2f, DgRise = 1.7f;

        static Transform BuildDungeon(Vector3 center)
        {
            var root = new GameObject("RoomB_Sarnic").transform; // ad korunuyor (Shots/HUD bulur)
            root.position = center;
            float top = DgSpring + DgRise;
            var stone = K.castleStone; var brick = K.cellarBrick;

            // --- Kabuk ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), 17, 17, 0.5f, K.cellarFloor, 3, 3);
            Wall(root, "WallE", new Vector3(8.25f, 0, 0), false, 17, DgSpring + 0.6f, stone, 2f);
            Wall(root, "WallW", new Vector3(-8.25f, 0, 0), false, 17, DgSpring + 0.6f, stone, 2f, new Opening(0f, 3f, 0f, 3f)); // kasa kapısı
            Wall(root, "WallN", new Vector3(0, 0, 8.25f), true, 17, top + 0.3f, stone, 2f);
            Wall(root, "WallS", new Vector3(0, 0, -8.25f), true, 17, top + 0.3f, stone, 2f);
            GameObjectUtility.SetStaticEditorFlags(Slab("Roof", root, new Vector3(0, top + 0.6f, 0), new Vector3(18f, 0.5f, 18f), stone, 2f, false), 0);
            foreach (int sx in new[] { -1, 1 })
            {
                var bay = new GameObject("Bay" + sx).transform;
                bay.SetParent(root, false);
                bay.localPosition = new Vector3(sx * 4f, 0, 0);
                bay.localRotation = Quaternion.Euler(0, 90, 0); // tonoz z boyunca uzanır
                VaultStrip(bay, "Vault", 0, 17f, 4.1f, DgSpring, DgRise, 0.3f, brick, 12, 2f);
                foreach (float z in new[] { -4f, 4f })
                    VaultStrip(bay, "Rib" + z, -z, 0.5f, 3.96f, DgSpring, DgRise - 0.16f, 0.28f, stone, 12, 1.2f);
            }
            // Orta kemer dizisi: kiriş + iki yuvarlak sütun + duvar konsolları
            Slab("Architrave", root, new Vector3(0, DgSpring - 0.2f, 0), new Vector3(0.6f, 0.45f, 16f), stone, 1.5f, false);
            foreach (int s in new[] { -1, 1 })
            {
                var col = new GameObject("Column").transform;
                col.SetParent(root, false);
                col.localPosition = new Vector3(0, 0, s * 4f);
                var bc = col.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0, DgSpring * 0.5f, 0); bc.size = new Vector3(0.75f, DgSpring, 0.75f);
                Slab("Base", col, new Vector3(0, 0.2f, 0), new Vector3(1.0f, 0.4f, 1.0f), stone, 1.2f, false);
                Slab("Plinth", col, new Vector3(0, 0.47f, 0), new Vector3(0.82f, 0.14f, 0.82f), stone, 1.2f, false);
                float sh = DgSpring - 1.15f;
                ProbeLit(Prim("Shaft", col, PrimitiveType.Cylinder, new Vector3(0, 0.54f + sh * 0.5f, 0), new Vector3(0.62f, sh * 0.5f, 0.62f), stone));
                Slab("Neck", col, new Vector3(0, DgSpring - 0.56f, 0), new Vector3(0.76f, 0.12f, 0.76f), stone, 1.2f, false);
                Slab("Cap", col, new Vector3(0, DgSpring - 0.36f, 0), new Vector3(1.0f, 0.3f, 1.0f), stone, 1.2f, false);
                Slab("Corbel", root, new Vector3(0, DgSpring - 0.55f, s * 7.85f), new Vector3(0.6f, 0.4f, 0.3f), stone, 1.2f, false);
            }
            // Kasa kapısı çerçevesi
            foreach (int s in new[] { -1, 1 })
                Slab("VaultJamb", root, new Vector3(-7.92f, 1.6f, s * 1.72f), new Vector3(0.18f, 3.2f, 0.44f), stone, 1.2f, false);
            Slab("VaultLintel", root, new Vector3(-7.92f, 3.25f, 0), new Vector3(0.18f, 0.3f, 3.9f), stone, 1.2f, false);

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

            var det = new GameObject("Details").transform;
            det.SetParent(root, false);
            // Zincirler + prangalar (kuzey ve güney duvarları)
            void Shackle(float x, float z, float dz, float top0, float len)
            {
                Rod("Ring", det, new Vector3(x, top0, z), new Vector3(x, top0, z + dz * 0.12f), 0.04f, K.iron);
                Rod("Chain", det, new Vector3(x, top0, z + dz * 0.1f), new Vector3(x + 0.05f, top0 - len, z + dz * 0.14f), 0.03f, K.iron);
                MeshObj("Cuff", det, new Vector3(x + 0.05f, top0 - len - 0.1f, z + dz * 0.14f), RingMesh(0.09f, 0.025f, 0.05f), K.iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Shackle(2.2f, 7.98f, -1, 2.4f, 1.1f); Shackle(3.0f, 7.98f, -1, 2.4f, 0.9f);
            Shackle(-2.6f, -7.98f, 1, 2.5f, 1.2f); Shackle(-3.3f, -7.98f, 1, 2.5f, 0.8f);
            // Yer ızgarası (gider)
            Slab("DrainPit", det, new Vector3(-2.6f, 0.002f, 3.0f), new Vector3(0.7f, 0.004f, 0.5f), MakeMat("K_CellarVoid", new Color(0.012f, 0.01f, 0.01f)), 1f, false);
            for (int i = 0; i < 6; i++)
                Rod("DrainBar", det, new Vector3(-2.9f + i * 0.12f, 0.012f, 2.78f), new Vector3(-2.9f + i * 0.12f, 0.012f, 3.22f), 0.025f, K.iron);
            ProbeLit(det.gameObject);

            // --- Işık: fresk iki yanında aplik (baked), iki asılı fener (realtime), batı aplikleri ---
            foreach (int s in new[] { -1, 1 })
            {
                Kit("WallSconce", root, new Vector3(7.95f, 2.3f, s * 5.7f), -90f);
                Kit("L_Candelabra", root, new Vector3(7.3f, 2.4f, s * 5.7f));
                Kit("WallSconce", root, new Vector3(-7.95f, 2.3f, s * 3.0f), 90f);
                Kit("L_Candle", root, new Vector3(-7.3f, 2.4f, s * 3.0f));
            }
            foreach (var p in new[] { new Vector3(5.4f, 2.45f, 0), new Vector3(-4.6f, 2.6f, -1.2f) })
            {
                float off = Mathf.Abs(Mathf.Abs(p.x) - 4f);
                float ceil = DgSpring + DgRise * Mathf.Sqrt(Mathf.Max(0f, 1f - off * off / (4.1f * 4.1f))) - 0.15f;
                Rod("LanternChain", root, p + Vector3.up * 0.5f, new Vector3(p.x, ceil, p.z), 0.022f, K.iron);
                Kit("L_Lantern", root, p, 15f);
            }

            // --- Dekor ---
            // Güneybatı: masa (mektuplar, mum, kadeh), tabure, sandık
            Kit("StudyTable", root, new Vector3(-5.2f, 0, -7.1f), 0f);
            Kit("Stool", root, new Vector3(-4.7f, 0, -6.2f), 40f);
            Kit("L_Candle", root, new Vector3(-4.9f, 1.3f, -6.8f));
            Kit("Chest", root, new Vector3(-7.25f, 0, -5.2f), 90f);
            Kit("BookStack", root, new Vector3(-7.3f, 0, -6.6f), 20f);
            // Güneydoğu: fıçılar, sandıklar
            Kit("Barrel", root, new Vector3(7.0f, 0, -7.0f), 25f);
            Kit("BarrelLying", root, new Vector3(5.5f, 0, -7.3f), 10f);
            Kit("CrateStack", root, new Vector3(7.1f, 0, -5.6f), -90f);
            // Kuzeydoğu: testi, sandık, kova
            Kit("Amphora", root, new Vector3(6.9f, 0, 7.0f), 30f);
            Kit("Crate", root, new Vector3(5.8f, 0, 7.2f), 20f);
            Kit("Bucket", root, new Vector3(7.2f, 0, 6.0f));
            // Kuzeybatı: fıçılar, şamdan
            Kit("Barrel", root, new Vector3(-7.0f, 0, 7.0f), 0f);
            Kit("Barrel", root, new Vector3(-5.9f, 0, 7.25f), 80f);
            Kit("Crate", root, new Vector3(-7.2f, 0, 5.9f), -15f);
            Kit("CandelabraFloor", root, new Vector3(-4.6f, 0, 7.4f), 180f);
            Kit("L_Candelabra", root, new Vector3(-4.6f, 1.9f, 6.9f));
            // Ortada dağınık kağıtlar; köşelerde ağlar
            Kit("PaperScatter", root, new Vector3(1.6f, 0, -2.6f), 40f);
            Kit("PaperScatter", root, new Vector3(-5.6f, 0, -5.4f), 200f);
            KitR("Cobweb", root, new Vector3(7.97f, DgSpring + 0.25f, 7.97f), Quaternion.Euler(0, 0, 180), 1.6f);
            KitR("Cobweb", root, new Vector3(-7.97f, DgSpring + 0.25f, -7.97f), Quaternion.Euler(0, 180, 180), 1.6f);
            KitR("Cobweb", root, new Vector3(7.97f, 0, -7.97f), Quaternion.Euler(0, 180, 0), 1.1f);
            KitR("Cobweb", root, new Vector3(-7.97f, 0, 7.97f), Quaternion.identity, 1.0f);

            var colSkip = new[] { new Bounds(new Vector3(0, 1.6f, -4f), new Vector3(1.1f, 3.4f, 1.1f)), new Bounds(new Vector3(0, 1.6f, 4f), new Vector3(1.1f, 3.4f, 1.1f)) };
            KitProbes(root, new Vector3(0, 2.4f, 0), new Vector3(16f, 4.6f, 16f), 5, 3, 5, colSkip);
            KitProbes(root, new Vector3(-10.625f, 2f, 0), new Vector3(4.25f, 4f, 6f), 3, 3, 3);
            KitReflection(root, new Vector3(0, 2.2f, 0), new Vector3(16f, 4.8f, 16f));
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
            Slab("FrescoCornice", roomO, new Vector3(7.86f, 3.32f, 0), new Vector3(0.3f, 0.14f, 10.1f), stone, 1.2f, false);
            Slab("FrescoSill", roomO, new Vector3(7.86f, 0.7f, 0), new Vector3(0.3f, 0.12f, 10.1f), stone, 1.2f, false);
            foreach (float z in new[] { 2.2f, 0f, -2.2f, 4.85f, -4.85f })
                Slab("FrescoColumn", roomO, new Vector3(7.8f, 2f, z), new Vector3(0.12f, 2.3f, 0.16f), K.brass, 1f, false);

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
