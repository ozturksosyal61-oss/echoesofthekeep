using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Bulmaca 6 "Ay aynaları" (ref cati.png, kuyu.png). Simge seti: ay evreleri (Assets/Generated/Moon, 8 doku).
    /// X: Çatı terası, kule yerel (110,35,-90): kayrak çatılar, sivri kuleler, mazgallı kuzey surları, ahşap geçitler; 3 döner ayna + kuyu ağzı.
    /// O: Kuyu odası, roomO yerel (90,0,0): yuvarlak taş oda (R 8), ortada parmaklıklı kuyu, tepede göğe açılan şaft, zeminde 8 ay taşı.
    /// Girişler: harita odası kürsüsündeki çatı merdiveni / katakomb çıkış galerisi kapısı (bulmaca 5 çözülünce).
    /// Düğümler: 50 tohum, 51 tur, 52 evre, 53 tutma, 54-56 aynalar, 57-58 geçitler.
    /// </summary>
    public static partial class CoopSetup
    {
        static void BuildPuzzle6(Transform roomX, Transform roomO)
        {
            var def = new GameObject("Puzzle6").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 6;
            dso.FindProperty("title").stringValue = "Bulmaca 6 (ay aynaları)";
            dso.FindProperty("startsActive").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();
            foreach (var d in Object.FindObjectsByType<PuzzleDef>(FindObjectsSortMode.None))
                if (d.Id == 5)
                {
                    var so = new SerializedObject(d);
                    var up = so.FindProperty("unlocks"); up.arraySize = 1; up.GetArrayElementAtIndex(0).intValue = 6;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

            MoonMats(out var skyMats, out var tileMats);
            var roof = BuildRoof(roomX, out var mirrorSpots, out var mouth, out var moonR, out var roofArrive);
            var well = BuildWell(roomO, out var tiles, out var wellTop, out var spot, out var spotLight, out var wellArrive);
            for (int k = 0; k < tiles.Length; k++) tiles[k].sharedMaterial = tileMats[k];

            var moonGo = new GameObject("Puzzle6_Moon");
            moonGo.transform.SetParent(roof, false);
            var mp = moonGo.AddComponent<MoonPuzzle>();
            var mirrors = new MoonMirror[3];
            for (int i = 0; i < 3; i++)
            {
                mirrors[i] = mirrorSpots[i].GetComponent<MoonMirror>();
                var mso = new SerializedObject(mirrors[i]);
                mso.FindProperty("puzzleId").intValue = 6;
                mso.FindProperty("nodeId").intValue = 54 + i;
                mso.ApplyModifiedPropertiesWithoutUndo();
            }
            var pso = new SerializedObject(mp);
            pso.FindProperty("puzzleId").intValue = 6;
            pso.FindProperty("nodeId").intValue = 50;
            SetRefArray(pso, "mirrors", mirrors);
            SetRef(pso, "roof", roof);
            pso.ApplyModifiedPropertiesWithoutUndo();

            var view = moonGo.AddComponent<MoonView>();
            var vso = new SerializedObject(view);
            SetRef(vso, "puzzle", mp);
            SetRef(vso, "moon", moonR);
            SetRefArray(vso, "skyMats", skyMats);
            SetRefArray(vso, "tileMats", tileMats);
            SetRefArray(vso, "tiles", tiles);
            var beamM = MakeMat("K_MoonBeam", new Color(0.22f, 0.34f, 0.62f), "CoopGame/MoonBeam");
            beamM.SetColor("_Core", new Color(1.1f, 1.35f, 1.9f));
            SetRef(vso, "beamMat", beamM);
            SetRef(vso, "flareMat", MakeMat("K_MoonFlare", new Color(0.9f, 1.15f, 1.7f), "CoopGame/MoonFlare"));
            SetRef(vso, "mouth", mouth);
            SetRef(vso, "wellTop", wellTop);
            SetRef(vso, "spot", spot);
            SetRef(vso, "spotLight", spotLight);
            vso.ApplyModifiedPropertiesWithoutUndo();

            var hud = moonGo.AddComponent<MoonHud>();
            var hso = new SerializedObject(hud);
            SetRef(hso, "puzzle", mp);
            SetRef(hso, "font", U.body);
            hso.FindProperty("roofCenter").vector3Value = roof.TransformPoint(new Vector3(0, 2.5f, 0));
            hso.FindProperty("roofSize").vector3Value = new Vector3(18f, 7f, 32f);
            hso.FindProperty("wellCenter").vector3Value = well.TransformPoint(new Vector3(0, 2f, 0));
            hso.FindProperty("wellSize").vector3Value = new Vector3(17f, 5f, 17f);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Geçitler (bulmaca 5 çözülünce): harita odası kürsüsünde çatı merdiveni, katakomb çıkış galerisi kapısı
            var map = roomX.Find("RoomA5_Harita");
            if (map != null)
            {
                const float PY = 3.6f, TOP = 8.6f, lz = 5.7f;
                foreach (int s in new[] { -1, 1 })
                    Rod("RoofLadderRail", map, new Vector3(s * 0.32f, PY, lz), new Vector3(s * 0.32f, TOP, lz + 0.25f), 0.07f, K.beam);
                for (float y = PY + 0.3f; y < TOP; y += 0.32f)
                    Slab("RoofLadderRung", map, new Vector3(0, y, lz + (y - PY) / (TOP - PY) * 0.25f), new Vector3(0.64f, 0.05f, 0.05f), K.beam, 1f, false);
                Slab("RoofHatch", map, new Vector3(0, TOP - 0.02f, lz + 0.1f), new Vector3(1.1f, 0.06f, 1.0f), MakeMat("K_HatchVoid", new Color(0.02f, 0.025f, 0.04f)), 1f, false);
                MakePassage("PassageX6", map, 57, map.TransformPoint(new Vector3(0, PY + 1f, lz - 0.35f)), new Vector3(1.4f, 2f, 0.9f), roofArrive, 5);
            }
            var cata = roomO.Find("RoomB5_Katakomb");
            if (cata != null)
            {
                float h = LabyrinthPuzzle.N * LabyrinthPuzzle.Cell * 0.5f, doorZ = h + 0.5f + 2.6f - 0.12f;
                MakePassage("PassageO6", cata, 58, cata.TransformPoint(new Vector3(0, 1f, doorZ - 0.6f)), new Vector3(2.4f, 2f, 1.0f), wellArrive, 5);
            }
            MakeCheckpoint(roomX, 6, Roles.X, roofArrive.position);
            MakeCheckpoint(roomO, 6, Roles.O, wellArrive.position);
            KeepBaked(roof);
            KeepBaked(well);
        }

        // ---------------- Ay evresi dokuları + malzemeleri ----------------

        static void MoonMats(out Material[] sky, out Material[] tile)
        {
            const string dir = "Assets/Generated/Moon";
            Directory.CreateDirectory(dir);
            sky = new Material[8]; tile = new Material[8];
            const int S = 128;
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var sun = new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)); // i=0 yeni ay, 4 dolunay, 2 ilk dördün (sağ yarı aydınlık)
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f, r2 = u * u + v * v;
                        if (r2 > 0.92f * 0.92f) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                        float uu = u / 0.92f, vv = v / 0.92f;
                        var p = new Vector3(uu, vv, Mathf.Sqrt(Mathf.Max(0f, 1f - uu * uu - vv * vv)));
                        float lit = Mathf.Clamp01(Vector3.Dot(p, sun) * 6f + 0.5f);
                        float crater = 0.86f + 0.14f * Mathf.PerlinNoise(uu * 3.1f + 7f, vv * 3.1f + 3f) - 0.1f * Mathf.PerlinNoise(uu * 9f, vv * 9f);
                        var litC = new Color(0.93f, 0.91f, 0.84f) * crater;
                        var darkC = new Color(0.07f, 0.075f, 0.09f);
                        bool rim = r2 > 0.86f * 0.86f;
                        var c = Color.Lerp(darkC, litC, lit);
                        if (rim) c = Color.Lerp(c, new Color(0.45f, 0.44f, 0.42f), 0.5f);
                        c.a = 1f;
                        tex.SetPixel(x, y, c);
                    }
                tex.Apply();
                string path = dir + "/moon_" + i + ".png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                var sm = MakeMat("K_MoonSky" + i, Color.white, "Universal Render Pipeline/Unlit");
                Cutout(sm, t);
                sm.SetColor("_BaseColor", new Color(1.08f, 1.08f, 1.12f)); // kraterler seçilsin, hafif bloom
                sky[i] = sm;
                var tm = MakeMat("K_MoonTile" + i, Color.white);
                Cutout(tm, t);
                tm.SetFloat("_Smoothness", 0.35f);
                tm.EnableKeyword("_EMISSION");
                tm.SetTexture("_EmissionMap", t);
                tm.SetColor("_EmissionColor", new Color(0.16f, 0.17f, 0.2f)); // loş ışıkta da seçilsin
                tm.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                tile[i] = tm;
            }
        }

        static void Cutout(Material m, Texture2D t)
        {
            m.SetTexture("_BaseMap", t); m.mainTexture = t;
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(m);
        }

        static Mesh SpireMesh(float r, float h, int seg) => LatheMesh("kit_spire_" + F(r) + "_" + F(h) + "_" + seg, new[] { new Vector2(r, 0f), new Vector2(0f, h) }, seg);

        /// <summary>Sivri kule: kare gövde + piramit külah + tepede demir uç.</summary>
        static void Spire(Transform root, Vector3 basePos, float w, float bodyH, float capH, Material stone, Material slate)
        {
            if (bodyH > 0f) Slab("SpireBody", root, basePos + Vector3.up * (bodyH * 0.5f), new Vector3(w, bodyH, w), stone, 1.5f, false);
            var cap = MeshObj("SpireCap", root, basePos + Vector3.up * bodyH, SpireMesh(w * 0.72f, capH, 4), slate);
            cap.transform.localRotation = Quaternion.Euler(0, 45, 0);
            Rod("Finial", root, basePos + Vector3.up * (bodyH + capH - 0.2f), basePos + Vector3.up * (bodyH + capH + 1.2f), 0.06f, K.iron);
        }

        // ---------------- Çatı terası (X) ----------------

        static Transform BuildRoof(Transform roomX, out Transform[] mirrorNodes, out Transform mouth, out Renderer moon, out Transform arrive)
        {
            const float hx = 7f, hz = 14f;
            var root = new GameObject("RoomA6_Cati").transform;
            root.SetParent(roomX, false);
            root.localPosition = new Vector3(110f, 35f, -90f);
            var stone = K.castleStone; var wood = K.beam;
            var slate = KTex("K_RoofSlate", "roof_slates_02", new Color(0.5f, 0.53f, 0.6f), 0.35f);
            var cobble = KTex("K_RoofCobble", "cobblestone_large_01", new Color(0.55f, 0.58f, 0.6f), 0.3f);
            var puddle = MakeMat("K_Puddle", new Color(0.04f, 0.05f, 0.07f)); puddle.SetFloat("_Smoothness", 0.96f); puddle.SetFloat("_Metallic", 0.2f);
            var moss = MakeMat("K_Moss", new Color(0.16f, 0.21f, 0.12f)); moss.SetFloat("_Smoothness", 0.1f);
            var silver = MakeMat("K_MirrorSilver", new Color(0.78f, 0.84f, 0.92f)); silver.SetFloat("_Smoothness", 0.97f); silver.SetFloat("_Metallic", 1f);

            // --- Zemin: arnavut kaldırımı + ahşap geçitler + su birikintileri + yosun ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), hx * 2f + 2f, hz * 2f, 0.5f, cobble, 2, 4); // çatı eteklerinin altına taşar
            Slab("WalkA", root, new Vector3(0.6f, 0.03f, 0), new Vector3(2.2f, 0.06f, hz * 2f - 1f), wood, 1.2f, false);
            Slab("WalkB", root, new Vector3(0, 0.035f, 4.5f), new Vector3(hx * 2f - 1.5f, 0.06f, 1.8f), wood, 1.2f, false);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(-5.5f + Hash01(i, 1) * 11f, 0.008f, -12f + Hash01(i, 2) * 24f);
                if (Mathf.Abs(p.x - 0.6f) < 1.4f) p.x += 2.6f;
                Slab(i % 3 == 0 ? "Moss" : "Puddle", root, p, new Vector3(0.6f + Hash01(i, 3) * 1.3f, 0.01f, 0.5f + Hash01(i, 4) * 1.4f), i % 3 == 0 ? moss : puddle, 1f, false)
                    .transform.localRotation = Quaternion.Euler(0, Hash01(i, 5) * 180f, 0);
            }

            // --- Batı: mazgallı taş korkuluk, dışında dik kayrak çatı ---
            Slab("ParapetW", root, new Vector3(-hx - 0.3f, 0.55f, 0), new Vector3(0.6f, 1.1f, hz * 2f), stone, 1.5f);
            for (float z = -hz + 0.6f; z < hz; z += 2.0f)
                Slab("MerlonW", root, new Vector3(-hx - 0.3f, 1.45f, z), new Vector3(0.6f, 0.7f, 1.0f), stone, 1.2f, false);
            // --- Doğu: alçak taş bordür + dövme demir parmaklık ---
            Slab("CurbE", root, new Vector3(hx + 0.25f, 0.25f, 0), new Vector3(0.5f, 0.5f, hz * 2f), stone, 1.5f);
            Slab("FenceTopE", root, new Vector3(hx + 0.25f, 1.45f, 0), new Vector3(0.08f, 0.06f, hz * 2f), K.iron, 1f, false);
            Slab("FenceMidE", root, new Vector3(hx + 0.25f, 0.9f, 0), new Vector3(0.05f, 0.05f, hz * 2f), K.iron, 1f, false);
            Slab("FenceColE", root, new Vector3(hx + 0.25f, 1.0f, 0), new Vector3(0.3f, 1.0f, hz * 2f), K.iron, 1f).GetComponent<MeshRenderer>().enabled = false;
            for (float z = -hz + 0.15f; z < hz; z += 0.16f)
            {
                Rod("FenceBar", root, new Vector3(hx + 0.25f, 0.5f, z), new Vector3(hx + 0.25f, 1.6f, z), 0.025f, K.iron);
                if (Mathf.Abs(z % 1.6f) < 0.08f) Prim("FenceSpike", root, PrimitiveType.Sphere, new Vector3(hx + 0.25f, 1.62f, z), new Vector3(0.06f, 0.12f, 0.06f), K.iron);
            }
            foreach (int s in new[] { -1, 1 }) // dik kayrak çatılar (dış manzarayı kapatır)
            {
                var rf = new GameObject("RoofSlope").transform;
                rf.SetParent(root, false);
                rf.localPosition = new Vector3(s * (hx + 0.7f), 0.2f, 0);
                rf.localRotation = Quaternion.Euler(0, 0, s * -55f);
                Slab("Slates", rf, new Vector3(0, 6f, 0), new Vector3(0.4f, 12f, hz * 2f + 6f), slate, 3f, false);
                Slab("Ridge", root, new Vector3(s * (hx + 0.7f + Mathf.Sin(55f * Mathf.Deg2Rad) * 12f), 0.2f + Mathf.Cos(55f * Mathf.Deg2Rad) * 12f, 0), new Vector3(0.6f, 0.4f, hz * 2f + 6f), K.iron, 2f, false);
                for (int k = 0; k < 3; k++) // çatı penceresi (dormer) ve bacalar
                {
                    float z = -9f + k * 9f;
                    Slab("Dormer", rf, new Vector3(-s * 0.9f, 3.8f, z), new Vector3(1.6f, 1.8f, 1.8f), stone, 1f, false);
                    Slab("DormerGlass", rf, new Vector3(-s * 1.71f, 3.7f, z), new Vector3(0.04f, 0.9f, 0.7f), K.vitray, 1f, false);
                }
            }
            // --- Kuzey: mazgallı sur + köşe kuleleri (ay bu yönde) ---
            Slab("WallN", root, new Vector3(0, 0.6f, hz + 0.3f), new Vector3(hx * 2f + 1.2f, 1.2f, 0.6f), stone, 1.5f);
            for (float x = -hx + 0.5f; x < hx; x += 1.8f)
                Slab("MerlonN", root, new Vector3(x, 1.55f, hz + 0.3f), new Vector3(0.9f, 0.7f, 0.6f), stone, 1.2f, false);
            foreach (int s in new[] { -1, 1 })
            {
                Spire(root, new Vector3(s * (hx + 0.6f), 0, hz + 0.6f), 2.6f, 9f, 7f, stone, slate);
                Spire(root, new Vector3(s * (hx + 0.6f), 0, -hz - 0.6f), 2.2f, 4f, 5f, stone, slate);
                Spire(root, new Vector3(s * 16f, -10f, 34f), 5f, 24f, 12f, stone, slate);           // uzak kuleler (sis içinde)
                Spire(root, new Vector3(s * 28f, -14f, 18f), 6f, 22f, 10f, stone, slate);
            }
            Spire(root, new Vector3(-17f, -12f, 46f), 7f, 30f, 16f, stone, slate); // ayın arkasında kalmasın
            Slab("CurtainWallFar", root, new Vector3(0, -6f, 40f), new Vector3(50f, 16f, 2f), stone, 4f, false);

            // --- Güney: merdiven kulesi duvarı (geliş kapısı), sancaklar, aslan başları ---
            Slab("TowerWallS", root, new Vector3(0, 4f, -hz - 0.4f), new Vector3(hx * 2f + 1.4f, 8f, 0.8f), stone, 2f);
            Spire(root, new Vector3(0, 8f, -hz - 0.4f), 3.2f, 2f, 8f, stone, slate);
            Kit("Door", root, new Vector3(0.6f, 0, -hz + 0.02f), 0f, 1.1f);
            ArchTrim(root, "DoorArch", new Vector3(0.6f, 0, -hz), Vector3.forward, 2.6f, 3.6f, false);
            foreach (int s in new[] { -1, 1 })
            {
                Kit("Banner", root, new Vector3(s * 4.2f, 6.6f, -hz + 0.02f), 0f, 1.7f);
                Kit("WallSconce", root, new Vector3(0.6f + s * 1.9f, 2.5f, -hz + 0.03f), 0f);
                KitR("LionHead", root, new Vector3(-hx - 0.05f, 1.3f, s * 6f), Quaternion.Euler(0, 90, 0), 1.2f);
                KitR("LionHead", root, new Vector3(hx + 0.5f, 0.5f, s * 9.5f), Quaternion.Euler(0, 90, 0), 1.0f);
            }
            Kit("L_Candle", root, new Vector3(2.5f, 2.6f, -hz + 0.6f));
            Kit("L_Candle", root, new Vector3(-1.3f, 2.6f, -hz + 0.6f));

            // --- Aynalar: A1 (kuzeybatı, ayı yakalar), A2 (kuzeydoğu), A3 (kuyu ağzı, batı ortası) ---
            mirrorNodes = new Transform[3];
            var mpos = new[] { new Vector3(-4f, 0, 10f), new Vector3(4f, 0, 10f), new Vector3(-4f, 0, 2f) }; // A1→A2 doğu (90°), A2→A3 güneybatı (225°): hiçbir ay yönüne zıt değil
            for (int i = 0; i < 3; i++)
            {
                var n = new GameObject("Mirror" + (i + 1)).transform;
                n.SetParent(root, false); n.localPosition = mpos[i];
                var bc = n.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.2f, 0); bc.size = new Vector3(1.6f, 2.4f, 1.6f);
                n.gameObject.AddComponent<Interactable>().Label = "Aynayı çevir";
                var mm = n.gameObject.AddComponent<MoonMirror>();
                // ayak: üç kıvrık demir bacak + direk + bilezik
                foreach (float a in new[] { 0f, 120f, 240f })
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                    Rod("Leg", n, d * 0.75f, Vector3.up * 0.9f, 0.07f, K.iron);
                    Prim("Foot", n, PrimitiveType.Sphere, d * 0.75f + Vector3.up * 0.04f, Vector3.one * 0.12f, K.iron);
                }
                Rod("Post", n, Vector3.up * 0.8f, Vector3.up * 1.2f, 0.1f, K.iron);
                Prim("Collar", n, PrimitiveType.Cylinder, Vector3.up * 1.0f, new Vector3(0.2f, 0.05f, 0.2f), K.brass);
                var pv = new GameObject("Pivot").transform;
                pv.SetParent(n, false);
                var head = new GameObject("Head").transform;
                head.SetParent(pv, false); head.localPosition = Vector3.up * 1.6f;
                if (i == 2) head.localRotation = Quaternion.Euler(40f, 0, 0); // kuyu aynası aşağı eğik
                MeshObj("Ring", head, Vector3.zero, RingMesh(0.62f, 0.07f, 0.1f), K.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                Prim("Glass", head, PrimitiveType.Cylinder, new Vector3(0, 0, -0.02f), new Vector3(1.12f, 0.012f, 1.12f), silver).transform.localRotation = Quaternion.Euler(90, 0, 0);
                Prim("Back", head, PrimitiveType.Cylinder, new Vector3(0, 0, -0.05f), new Vector3(1.16f, 0.015f, 1.16f), K.bronze).transform.localRotation = Quaternion.Euler(90, 0, 0);
                Rod("Fork", pv, new Vector3(-0.68f, 1.15f, 0), new Vector3(-0.68f, 1.6f, 0), 0.05f, K.iron);
                Rod("Fork", pv, new Vector3(0.68f, 1.15f, 0), new Vector3(0.68f, 1.6f, 0), 0.05f, K.iron);
                Rod("ForkBase", pv, new Vector3(-0.68f, 1.15f, 0), new Vector3(0.68f, 1.15f, 0), 0.06f, K.iron);
                Prim("Pointer", pv, PrimitiveType.Cube, new Vector3(0, 1.17f, 0.35f), new Vector3(0.05f, 0.03f, 0.5f), K.brass); // yön oku
                var so = new SerializedObject(mm);
                so.FindProperty("steps").intValue = i == 2 ? MoonPuzzle.Lenses : MoonPuzzle.MirrorSteps;
                so.FindProperty("baseAz").floatValue = 0f;
                SetRef(so, "pivot", pv);
                so.ApplyModifiedPropertiesWithoutUndo();
                mirrorNodes[i] = n;
                // ayna çevresine pusula gülü (yönleri saymak için 16 çentik)
                for (int k = 0; k < 16; k++)
                {
                    var dd = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward;
                    Slab("Notch", n, dd * 1.05f + Vector3.up * 0.045f, new Vector3(0.05f, 0.02f, k % 4 == 0 ? 0.3f : 0.16f), K.brass, 1f, false).transform.localRotation = Quaternion.Euler(0, k * 22.5f, 0);
                }
                Slab("Plinth", n, Vector3.up * 0.015f, new Vector3(2.3f, 0.03f, 2.3f), stone, 1f, false);
            }
            // kuyu ağzı (A3 altında): taş bilezik, ızgara, 8 mercek
            mouth = new GameObject("WellMouth").transform;
            mouth.SetParent(root, false); mouth.localPosition = mpos[2] + Vector3.up * 0.35f;
            MeshObj("Curb", root, mpos[2], RingMesh(1.25f, 0.3f, 0.35f), stone);
            Prim("Grate", root, PrimitiveType.Cylinder, mpos[2] + Vector3.up * 0.3f, new Vector3(1.9f, 0.01f, 1.9f), MakeMat("K_WellVoid", new Color(0.01f, 0.012f, 0.02f)));
            for (int k = 0; k < 8; k++)
            {
                var dd = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward;
                Prim("Lens", root, PrimitiveType.Sphere, mpos[2] + Vector3.up * 0.38f + dd * 0.95f, new Vector3(0.2f, 0.1f, 0.2f), K.lanternGlass);
                Rod("GrateBar", root, mpos[2] + Vector3.up * 0.32f + dd * 0.95f, mpos[2] + Vector3.up * 0.32f - dd * 0.95f, 0.03f, K.iron);
            }

            // ay kartı (görünüm MoonView'da konumlanır)
            var mq = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(mq.GetComponent<Collider>());
            mq.name = "Moon"; mq.transform.SetParent(root, false);
            mq.transform.localPosition = new Vector3(0, 21f, 40f);
            moon = mq.GetComponent<Renderer>();
            moon.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(mq, 0);

            // --- Gökbilimci köşesi, eşyalar ---
            Kit("GothicTable", root, new Vector3(5.0f, 0, -1.0f), 15f);
            Kit("PaperPile", root, new Vector3(4.8f, 0.56f, -1.1f), 20f);
            Letter(root, new Vector3(5.45f, 0.565f, -1.35f), 30f, 12);
            Kit("Compass", root, new Vector3(5.3f, 0.56f, -0.8f), 60f);
            Kit("Magnifier", root, new Vector3(4.6f, 0.56f, -0.7f), 130f);
            Kit("Stool", root, new Vector3(4.0f, 0, -1.9f), 30f);
            Kit("Chest", root, new Vector3(6.0f, 0, 12.4f), -90f);
            Kit("CrateLong", root, new Vector3(-5.8f, 0, -2.4f), 90f);
            Kit("BarrelLying", root, new Vector3(-5.8f, 0, -8.6f), 10f);
            Kit("Bucket", root, new Vector3(-2.4f, 0, -7.4f), 0f);
            Kit("Barrel2", root, new Vector3(5.9f, 0, -11.6f), 30f);
            Kit("CrateStack", root, new Vector3(-5.8f, 0, -12.2f), 0f);
            Kit("Ladder", root, new Vector3(-5.6f, 0, -13.2f), 0f);
            Kit("Cannon", root, new Vector3(-3.5f, 0, 13.2f), 180f);
            Kit("Cannon", root, new Vector3(2.5f, 0, 13.2f), 175f);
            foreach (var (x, z) in new[] { (-0.6f, -10f), (1.9f, -4f), (-0.6f, 1f), (1.9f, 7.5f), (-0.6f, 12f), (3.5f, 4.5f), (-3.2f, 4.5f) })
                Kit("Lantern", root, new Vector3(x, 0.06f, z), x * 20f);
            Kit("L_Lantern", root, new Vector3(-0.6f, 0.06f, -10f), 0f);
            Kit("L_Lantern", root, new Vector3(1.9f, 0.06f, 7.5f), 0f);
            Slab("BellFramePostA", root, new Vector3(5.0f, 1.4f, -6.9f), new Vector3(0.22f, 2.8f, 0.22f), wood, 1f);
            Slab("BellFramePostB", root, new Vector3(5.0f, 1.4f, -9.1f), new Vector3(0.22f, 2.8f, 0.22f), wood, 1f);
            Slab("BellFrameBeam", root, new Vector3(5.0f, 2.85f, -8.0f), new Vector3(0.26f, 0.26f, 2.6f), wood, 1f, false);
            Kit("Bell", root, new Vector3(5.0f, 1.55f, -8.0f), 90f, 1.1f);
            Kit("StatueAngel", root, new Vector3(-6.2f, 0, 13.0f), 160f, 0.8f);
            Kit("StatueAngel", root, new Vector3(6.2f, 0, 13.0f), 200f, 0.8f);
            Kit("BrassVase", root, new Vector3(5.9f, 0, 1.0f), 0f);
            Kit("Amphora", root, new Vector3(-6.0f, 0, -5.0f), 0f, 1.1f);
            Kit("Rug", root, new Vector3(4.8f, 0.04f, -1.0f), 15f, 0.9f);
            Kit("CandleCluster", root, new Vector3(-6.3f, 0, 9.2f));
            Kit("CandleCluster", root, new Vector3(6.2f, 0, -8.6f));

            // --- v2: 5 özgün detay (gökbilimcinin son gecesi) ---
            {
                var dd = new GameObject("StoryDetails").transform; dd.SetParent(root, false);
                var crow = MakeMat("K_Crow", new Color(0.05f, 0.05f, 0.06f)); crow.SetFloat("_Smoothness", 0.4f);
                // 1: mazgallarda tüneyen kargalar
                foreach (var (x, z, yw) in new[] { (-5.8f, 13.3f, 160f), (2.6f, 13.3f, 200f), (6.6f, 9.0f, 250f) })
                {
                    var c = new Vector3(x, 1.25f, z);
                    var q = Quaternion.Euler(0, yw, 0);
                    Prim("CrowBody", dd, PrimitiveType.Sphere, c, new Vector3(0.16f, 0.14f, 0.26f), crow).transform.localRotation = Quaternion.Euler(-15, yw, 0);
                    Prim("CrowHead", dd, PrimitiveType.Sphere, c + q * new Vector3(0, 0.08f, 0.13f), Vector3.one * 0.1f, crow);
                    Prim("Beak", dd, PrimitiveType.Cube, c + q * new Vector3(0, 0.07f, 0.2f), new Vector3(0.02f, 0.02f, 0.07f), crow).transform.localRotation = q;
                }
                // 2: taşlarla sabitlenmiş yıldız haritası
                Slab("StarChart", dd, new Vector3(-1.8f, 0.012f, 4.6f), new Vector3(0.8f, 0.006f, 0.6f), MakeMat("K_StarChart", new Color(0.1f, 0.12f, 0.2f)), 1f, false).transform.localRotation = Quaternion.Euler(0, 12, 0);
                Prim("Weight", dd, PrimitiveType.Sphere, new Vector3(-2.15f, 0.05f, 4.35f), new Vector3(0.12f, 0.08f, 0.12f), stone);
                Prim("Weight", dd, PrimitiveType.Sphere, new Vector3(-1.45f, 0.05f, 4.9f), new Vector3(0.12f, 0.08f, 0.12f), stone);
                // 3: kırık teleskop merceği
                for (int k = 0; k < 6; k++)
                    Slab("LensShard", dd, new Vector3(5.0f + (k % 3) * 0.12f, 0.12f, 6.2f + (k / 3) * 0.1f), new Vector3(0.05f, 0.005f, 0.04f), K.clearGlass, 1f, false).transform.localRotation = Quaternion.Euler(0, k * 50f, 0);
                // 4: mazgala bırakılmış söndürülmüş mum halkası (ay için adak)
                for (int k = 0; k < 7; k++) Kit("Candle", root, new Vector3(1.6f, 1.2f, hz + 0.3f) + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * 0.24f, 0f, 0.6f);
                // 5: köşeye takılmış pelerin
                Slab("Cloak", dd, new Vector3(hx - 0.5f, 0.1f, -9.5f), new Vector3(0.9f, 0.08f, 0.7f), K.velvet, 1f, false).transform.localRotation = Quaternion.Euler(0, 40, 12);
                ProbeLit(dd.gameObject);
            }

            // --- Gözlemevi genişletmesi (ref cati.png): teleskoplar, küreler, çalışma masaları, notlar, kitaplar ---
            var navy = MakeMat("K_CelestialGlobe", new Color(0.09f, 0.12f, 0.2f)); navy.SetFloat("_Smoothness", 0.45f);
            // doğu gözlem platformu: büyük teleskop aya doğru
            Slab("ObsDeck", root, new Vector3(5.2f, 0.06f, 6.7f), new Vector3(2.8f, 0.12f, 2.4f), wood, 1.2f, false);
            Telescope(root, new Vector3(5.3f, 0.12f, 6.8f), -30f, 34f, 1.25f);
            Kit("Stool", root, new Vector3(4.4f, 0.12f, 5.9f), 20f);
            Kit("Crate", root, new Vector3(6.3f, 0.12f, 5.8f), 10f);
            Kit("BookStack", root, new Vector3(6.3f, 0.72f, 5.8f), 40f);
            Kit("PaperC", root, new Vector3(6.2f, 0.75f, 5.7f), 70f);
            Kit("Lantern", root, new Vector3(4.1f, 0.12f, 7.6f), 0f);
            // gökbilimci masası (doğu) zenginleşir
            Kit("BookSet", root, new Vector3(5.45f, 0.56f, -1.35f), 105f, 0.8f);
            Kit("OilLamp", root, new Vector3(4.35f, 0.56f, -1.3f), 0f);
            Kit("Goblet1", root, new Vector3(5.6f, 0.56f, -0.55f), 0f);
            Kit("PaperScatter", root, new Vector3(4.3f, 0.01f, -2.4f), 40f);
            Globe(root, new Vector3(6.2f, 0, -2.5f), 1.0f, navy);
            Kit("Commode", root, new Vector3(6.45f, 0, -4.4f), -90f);
            Kit("BookStack", root, new Vector3(6.45f, 0.95f, -4.75f), 15f);
            Kit("Jug", root, new Vector3(6.45f, 0.95f, -4.0f), 0f);
            Kit("Candlestick", root, new Vector3(6.5f, 0.95f, -4.4f), 0f);
            // batı çalışma köşesi: masa + notlar + orta teleskop
            Kit("Rug", root, new Vector3(-5.3f, 0.04f, 6.3f), 90f, 0.8f);
            Kit("StudyTable", root, new Vector3(-5.8f, 0, 6.3f), 90f);
            Kit("Stool", root, new Vector3(-4.7f, 0, 5.7f), 10f);
            Kit("PaperScatter", root, new Vector3(-4.9f, 0.05f, 7.2f), 200f);
            Kit("BookStack", root, new Vector3(-6.5f, 0.05f, 7.3f), 70f);
            Telescope(root, new Vector3(-2.7f, 0, 6.3f), 20f, 40f, 1.0f);
            Kit("Lantern", root, new Vector3(-3.3f, 0.05f, 5.4f), 0f);
            // güneybatı harita masası: küre + harita + mumlar
            Kit("RoundTable", root, new Vector3(-3.6f, 0, -4.6f), 0f);
            Globe(root, new Vector3(-3.75f, 0.8f, -4.45f), 0.45f, navy);
            Kit("PaperA", root, new Vector3(-3.3f, 0.8f, -4.85f), 30f);
            Kit("Compass", root, new Vector3(-3.25f, 0.8f, -4.4f), 0f);
            Kit("Candlestick", root, new Vector3(-3.95f, 0.8f, -4.95f), 0f);
            Kit("Stool", root, new Vector3(-2.7f, 0, -5.1f), 40f);
            Kit("Stool", root, new Vector3(-4.4f, 0, -3.7f), 200f);
            Kit("Basket", root, new Vector3(-4.6f, 0, -5.6f), 0f);
            // batı onarım tezgâhı (mercek/ayna parçaları)
            Kit("Workbench", root, new Vector3(-6.2f, 0, -6.9f), 90f);
            Kit("Crate", root, new Vector3(-5.9f, 0, -10.6f), 25f);
            Kit("Lantern", root, new Vector3(-5.9f, 0.6f, -10.6f), 0f);
            Kit("Bottle1", root, new Vector3(-5.2f, 0, -9.4f), 0f);
            // güneydoğu küçük teleskop + sepet
            Telescope(root, new Vector3(3.3f, 0, -12.1f), 0f, 28f, 0.85f);
            Kit("Basket", root, new Vector3(4.2f, 0, -12.9f), 0f);
            Kit("PaperB", root, new Vector3(2.7f, 0.01f, -11.4f), 10f);
            // batı: kaide üstünde armiller küre
            Slab("ArmPlinth", root, new Vector3(-6.0f, 0.45f, 0.0f), new Vector3(0.8f, 0.9f, 0.8f), stone, 1f);
            Kit("Armillary", root, new Vector3(-6.0f, 0.9f, 0.0f), 30f, 1.3f);
            // korkuluklarda fenerler + mumlar
            foreach (float z in new[] { -6.4f, 3.6f, 11.6f }) Kit("Lantern", root, new Vector3(-hx - 0.3f, 1.1f, z), 0f);
            foreach (float z in new[] { -5f, 3f, 9f }) Kit("Lantern", root, new Vector3(hx + 0.25f, 0.5f, z), 0f);
            Kit("CandleCluster", root, new Vector3(-6.4f, 0, -1.4f));
            Kit("CandleCluster", root, new Vector3(6.4f, 0, 2.6f));
            Kit("L_Candle", root, new Vector3(-5.6f, 1.15f, 6.3f));
            Kit("L_Candle", root, new Vector3(5.0f, 0.9f, -1.0f));

            // Işık: yukarıdan baked ay dolgu ışığı (kuzeyden)
            var fill = LightGo("MoonFill", LightType.Spot, Pal.Moon, 30f, 60f, true);
            fill.transform.SetParent(root, false);
            fill.transform.localPosition = new Vector3(0, 22f, 26f);
            fill.transform.localRotation = Quaternion.LookRotation(new Vector3(0, -22f, -28f));
            var fl = fill.GetComponent<Light>(); fl.spotAngle = 70f; fl.innerSpotAngle = 40f;

            arrive = new GameObject("RoofArrive").transform;
            arrive.SetParent(root, false);
            arrive.localPosition = new Vector3(0.6f, 0.1f, -hz + 1.4f);

            KitProbes(root, new Vector3(0, 2.2f, 0), new Vector3(hx * 2f, 4f, hz * 2f), 5, 2, 9);
            KitReflection(root, new Vector3(0, 2f, 0), new Vector3(hx * 2f, 6f, hz * 2f));
            return root;
        }

        /// <summary>Silindir boru (yuvarlak; Rod kare kesitli).</summary>
        static GameObject Tube(string name, Transform parent, Vector3 a, Vector3 b, float r, Material mat)
        {
            var go = Prim(name, parent, PrimitiveType.Cylinder, (a + b) * 0.5f, new Vector3(r * 2f, (b - a).magnitude * 0.5f, r * 2f), mat);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        /// <summary>Ahşap tripod üstünde pirinç teleskop. yaw: bakış yönü, pitch: yükselme açısı. pos: zemin.</summary>
        static void Telescope(Transform parent, Vector3 pos, float yaw, float pitch, float scale)
        {
            var t = new GameObject("Telescope").transform;
            t.SetParent(parent, false); t.localPosition = pos; t.localRotation = Quaternion.Euler(0, yaw, 0); t.localScale = Vector3.one * scale;
            var head = new Vector3(0, 1.25f, 0);
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                var foot = Quaternion.Euler(0, a + 60f, 0) * Vector3.forward * 0.55f;
                Tube("Leg", t, foot, head, 0.03f, K.beam);
                Prim("Shoe", t, PrimitiveType.Sphere, foot + Vector3.up * 0.02f, Vector3.one * 0.07f, K.brass);
                Tube("Brace", t, Vector3.Lerp(foot, head, 0.35f), new Vector3(0, 0.45f, 0), 0.012f, K.brass);
            }
            Prim("Hub", t, PrimitiveType.Cylinder, head, new Vector3(0.16f, 0.05f, 0.16f), K.brass);
            Prim("Yoke", t, PrimitiveType.Cube, head + Vector3.up * 0.1f, new Vector3(0.05f, 0.18f, 0.14f), K.bronze);
            var dir = Quaternion.Euler(-pitch, 0, 0) * Vector3.forward;
            var p = head + Vector3.up * 0.18f;
            var back = p - dir * 0.55f; var front = p + dir * 0.95f;
            Tube("TubeMain", t, back, front, 0.075f, K.brass);
            Tube("TubeLeather", t, Vector3.Lerp(back, front, 0.25f), Vector3.Lerp(back, front, 0.6f), 0.08f, K.leather);
            Tube("Dew", t, front, front + dir * 0.28f, 0.095f, K.brass);
            Tube("Draw", t, back - dir * 0.3f, back, 0.05f, K.brass);
            Tube("Eyepiece", t, back - dir * 0.42f, back - dir * 0.3f, 0.028f, K.iron);
            foreach (float k in new[] { 0.05f, 0.62f, 0.98f })
                Tube("Band", t, Vector3.Lerp(back, front, k) - dir * 0.015f, Vector3.Lerp(back, front, k) + dir * 0.015f, 0.088f, K.bronze);
            Tube("Lens", t, front + dir * 0.26f, front + dir * 0.275f, 0.08f, K.lanternGlass);
            Tube("Finder", t, Vector3.Lerp(back, front, 0.3f) + Vector3.up * 0.12f, Vector3.Lerp(back, front, 0.75f) + Vector3.up * 0.12f, 0.022f, K.brass);
            var col = t.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.8f, 0); col.radius = 0.35f; col.height = 1.6f;
        }

        /// <summary>Gök küresi: koyu lacivert küre, eğik pirinç meridyen halkası, ekvator halkası, ahşap ayak. size 1 = yer küresi (~1.2 m).</summary>
        static void Globe(Transform parent, Vector3 pos, float size, Material sphere)
        {
            var g = new GameObject("Globe").transform;
            g.SetParent(parent, false); g.localPosition = pos; g.localScale = Vector3.one * size;
            bool floor = size > 0.7f;
            float cy = floor ? 0.85f : 0.42f;
            if (floor)
            {
                foreach (float a in new[] { 0f, 90f, 180f, 270f })
                {
                    var d = Quaternion.Euler(0, a + 45f, 0) * Vector3.forward;
                    Tube("Leg", g, d * 0.38f, d * 0.2f + Vector3.up * 0.62f, 0.03f, K.furniture);
                }
                MeshObj("Horizon", g, Vector3.up * 0.62f, RingMesh(0.42f, 0.08f, 0.04f), K.furniture);
            }
            else
            {
                Prim("Foot", g, PrimitiveType.Cylinder, Vector3.up * 0.02f, new Vector3(0.36f, 0.02f, 0.36f), K.furniture);
                Tube("Stem", g, Vector3.up * 0.04f, Vector3.up * 0.14f, 0.03f, K.brass);
            }
            var c = Vector3.up * cy;
            Prim("Sphere", g, PrimitiveType.Sphere, c, Vector3.one * (floor ? 0.62f : 0.5f), sphere);
            float rr = floor ? 0.35f : 0.29f;
            var mer = MeshObj("Meridian", g, c, RingMesh(rr, 0.025f, 0.03f), K.brass).transform;
            mer.localRotation = Quaternion.Euler(0, 0, 23.5f) * Quaternion.Euler(90, 0, 0);
            if (!floor) Tube("Arm", g, Vector3.up * 0.14f, c + Quaternion.Euler(0, 0, 23.5f) * Vector3.down * rr, 0.015f, K.brass);
            MeshObj("Equator", g, c, RingMesh(rr * 0.98f, 0.015f, 0.02f), K.brass).transform.localRotation = Quaternion.Euler(0, 0, 23.5f);
            if (floor) { var col = g.gameObject.AddComponent<SphereCollider>(); col.center = c; col.radius = 0.4f; }
        }

        // ---------------- Kuyu odası (O) ----------------

        static Transform BuildWell(Transform roomO, out Renderer[] tiles, out Transform wellTop, out Transform spot, out Light spotLight, out Transform arrive)
        {
            const float R = 8f, H = 7f, SH = 3f; const int segs = 20;
            var root = new GameObject("RoomB6_Kuyu").transform;
            root.SetParent(roomO, false);
            root.localPosition = new Vector3(90f, 0, 0);
            var brick = KTex("K_WellBrick", "castle_brick_07", new Color(0.62f, 0.64f, 0.6f), 0.25f);
            var stone = K.castleStone;
            var floorM = KTex("K_WellFloor", "stone_tiles_02", new Color(0.5f, 0.5f, 0.52f), 0.4f, 1.1f); // lobi zemini tekrar etmesin
            var moss = MakeMat("K_Moss", new Color(0.16f, 0.21f, 0.12f));
            var voidM = MakeMat("K_WellVoid", new Color(0.01f, 0.012f, 0.02f));
            var bone = MakeMat("K_Bone", new Color(0.6f, 0.55f, 0.46f));

            Deck(root, "Floor", new Vector3(0, -0.25f, 0), R * 2f + 1f, R * 2f + 1f, 0.5f, floorM, 3, 3);
            // yuvarlak duvar: 20 parça, aralarda gömme sütunlar, her ikinci bölmede kemerli niş
            float segLen = 2f * R * Mathf.Tan(Mathf.PI / segs) + 0.12f;
            for (int i = 0; i < segs; i++)
            {
                float a = i * 360f / segs;
                var seg = new GameObject("WallSeg" + i).transform;
                seg.SetParent(root, false); seg.localRotation = Quaternion.Euler(0, a, 0);
                Wall(seg, "W", new Vector3(0, 0, R + 0.25f), true, segLen, H + 0.4f, brick, 2f);
                var colA = Quaternion.Euler(0, a + 180f / segs, 0);
                if (i % 2 == 0)
                {
                    var c = new GameObject("Column").transform; c.SetParent(root, false); c.localRotation = colA;
                    Slab("Shaft", c, new Vector3(0, H * 0.5f, R - 0.15f), new Vector3(0.5f, H, 0.4f), stone, 1.5f, false);
                    Slab("Base", c, new Vector3(0, 0.25f, R - 0.2f), new Vector3(0.7f, 0.5f, 0.5f), stone, 1f, false);
                }
                else if (i != 15) ArchTrim(seg, "Niche", new Vector3(0, 0.9f, R), Vector3.back, 1.2f, 1.6f, true);
            }
            for (int i = 0; i < segs; i++)
            {
                var c = new GameObject("CorniceSeg").transform; c.SetParent(root, false); c.localRotation = Quaternion.Euler(0, i * 360f / segs, 0);
                Slab("C", c, new Vector3(0, 4.6f, R - 0.2f), new Vector3(segLen, 0.3f, 0.5f), stone, 1f, false);
                Slab("C2", c, new Vector3(0, H - 0.2f, R - 0.25f), new Vector3(segLen, 0.4f, 0.6f), stone, 1f, false);
            }
            // tavan: ortada göğe açılan kare şaft
            foreach (var (c, sx, sz) in new[] { (new Vector3(0, 0, (SH * 0.5f + R + 0.5f) * 0.5f), R * 2f + 1f, R + 0.5f - SH * 0.5f), (new Vector3(0, 0, -(SH * 0.5f + R + 0.5f) * 0.5f), R * 2f + 1f, R + 0.5f - SH * 0.5f),
                                                 (new Vector3((SH * 0.5f + R + 0.5f) * 0.5f, 0, 0), R + 0.5f - SH * 0.5f, SH), (new Vector3(-(SH * 0.5f + R + 0.5f) * 0.5f, 0, 0), R + 0.5f - SH * 0.5f, SH) })
                Deck(root, "Ceil", c + Vector3.up * (H + 0.25f), sx, sz, 0.5f, stone, 2, 1);
            Wall(root, "ShaftN", new Vector3(0, H, SH * 0.5f + 0.25f), true, SH + 1f, 10f, brick, 2f);
            Wall(root, "ShaftS", new Vector3(0, H, -SH * 0.5f - 0.25f), true, SH + 1f, 10f, brick, 2f);
            Wall(root, "ShaftE", new Vector3(SH * 0.5f + 0.25f, H, 0), false, SH, 10f, brick, 2f);
            Wall(root, "ShaftW", new Vector3(-SH * 0.5f - 0.25f, H, 0), false, SH, 10f, brick, 2f);
            wellTop = new GameObject("WellTop").transform;
            wellTop.SetParent(root, false); wellTop.localPosition = new Vector3(0, H + 9f, 0);
            Slab("ShaftSkyGlow", root, new Vector3(0, H + 9.8f, 0), new Vector3(SH, 0.05f, SH), GlowMat("K_SkyGlow", new Color(0.1f, 0.13f, 0.2f), new Color(0.25f, 0.33f, 0.55f)), 1f, false);
            // yukarıdan sarkan zincirler
            for (int k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, 45f + k * 90f, 0) * Vector3.forward * 5.2f;
                Rod("Chain", root, d + Vector3.up * H, d + Vector3.up * (H - 2.2f - k * 0.4f), 0.04f, K.iron);
                Prim("Hook", root, PrimitiveType.Sphere, d + Vector3.up * (H - 2.25f - k * 0.4f), new Vector3(0.1f, 0.14f, 0.1f), K.iron);
            }

            // --- Orta: kuyu (taş bilezik, kara boşluk, parmaklık, kapılar, zincir, makara) ---
            MeshObj("WellCurb", root, Vector3.zero, RingMesh(1.4f, 0.35f, 0.75f), stone);
            Prim("WellVoid", root, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(2.1f, 0.01f, 2.1f), voidM);
            Slab("WellBlock", root, new Vector3(0, 0.6f, 0), new Vector3(2.8f, 1.2f, 2.8f), stone, 1f).GetComponent<MeshRenderer>().enabled = false;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, 22.5f + k * 45f, 0) * Vector3.forward * 1.6f;
                Rod("FencePost", root, d, d + Vector3.up * 1.7f, 0.08f, K.iron);
                Prim("PostCap", root, PrimitiveType.Sphere, d + Vector3.up * 1.75f, Vector3.one * 0.13f, K.iron);
                var d2 = Quaternion.Euler(0, 22.5f + (k + 1) * 45f, 0) * Vector3.forward * 1.6f;
                if (k == 1 || k == 5) // iki kapı kanadı (ref: açık parmaklık kapısı)
                {
                    for (int b = 1; b < 5; b++) Rod("GateBar", root, Vector3.Lerp(d, d2, b / 5f), Vector3.Lerp(d, d2, b / 5f) + Vector3.up * 2.0f, 0.025f, K.iron);
                    Rod("GateTop", root, d + Vector3.up * 1.9f, d2 + Vector3.up * 1.9f, 0.03f, K.iron);
                }
                for (int c = 0; c < 6; c++) // sarkan zincir
                {
                    float t0 = c / 6f, t1 = (c + 1) / 6f;
                    float s0 = Mathf.Sin(t0 * Mathf.PI) * 0.25f, s1 = Mathf.Sin(t1 * Mathf.PI) * 0.25f;
                    Rod("FenceChain", root, Vector3.Lerp(d, d2, t0) + Vector3.up * (1.2f - s0), Vector3.Lerp(d, d2, t1) + Vector3.up * (1.2f - s1), 0.03f, K.iron);
                }
            }
            // makara kirişi + kova
            Rod("PulleyBeam", root, new Vector3(-1.9f, 2.9f, 0), new Vector3(1.9f, 2.9f, 0), 0.18f, K.beam);
            foreach (int s in new[] { -1, 1 }) Rod("PulleyPost", root, new Vector3(s * 1.9f, 0, 0), new Vector3(s * 1.9f, 3.0f, 0), 0.16f, K.beam);
            MeshObj("Pulley", root, new Vector3(0, 2.7f, 0), RingMesh(0.22f, 0.08f, 0.06f), K.beam).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Rod("Rope", root, new Vector3(0.2f, 2.7f, 0), new Vector3(0.2f, 1.1f, 0), 0.02f, K.paperC);
            Kit("Bucket", root, new Vector3(0.2f, 0.75f, 0), 0f, 0.8f);

            // --- Zemin: ay taşları halkası (8 döşeme), bronz kakma halkalar ---
            MeshObj("InlayOuter", root, Vector3.up * 0.005f, RingMesh(4.9f, 0.12f, 0.01f), K.bronze);
            MeshObj("InlayInner", root, Vector3.up * 0.005f, RingMesh(2.75f, 0.1f, 0.01f), K.bronze);
            tiles = new Renderer[8];
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var p = dir * 3.8f;
                Slab("TileStone", root, p + Vector3.up * 0.02f, new Vector3(1.35f, 0.04f, 1.35f), stone, 1f, false).transform.localRotation = Quaternion.Euler(0, a, 0);
                var q = QuadObj("MoonTile" + k, root, p + Vector3.up * 0.045f, new Vector2(1.1f, 1.1f), null, Quaternion.Euler(90, a + 180f, 0));
                GameObjectUtility.SetStaticEditorFlags(q, 0);
                tiles[k] = q.GetComponent<Renderer>();
                var rot = Quaternion.Euler(0, a, 0);
                foreach (var (o, sz) in new[] { (new Vector3(0, 0, 0.66f), new Vector3(1.38f, 0.05f, 0.06f)), (new Vector3(0, 0, -0.66f), new Vector3(1.38f, 0.05f, 0.06f)), (new Vector3(0.66f, 0, 0), new Vector3(0.06f, 0.05f, 1.38f)), (new Vector3(-0.66f, 0, 0), new Vector3(0.06f, 0.05f, 1.38f)) })
                    Slab("TileFrame", root, p + rot * o + Vector3.up * 0.04f, sz, K.bronze, 1f, false).transform.localRotation = rot;
                var div = Quaternion.Euler(0, a + 22.5f, 0) * Vector3.forward;
                Rod("Divider", root, div * 2.85f + Vector3.up * 0.008f, div * 4.8f + Vector3.up * 0.008f, 0.06f, K.bronze);
            }
            var spotGo = Prim("BeamSpot", root, PrimitiveType.Cylinder, new Vector3(0, 0.06f, 3.8f), new Vector3(1.3f, 0.005f, 1.3f), GlowMat("K_BeamSpot", new Color(0.5f, 0.6f, 0.8f), new Color(0.9f, 1.05f, 1.5f)));
            spotGo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(spotGo, 0);
            spot = spotGo.transform;
            var sl = LightGo("BeamLight", LightType.Point, Pal.Moon, 2.2f, 6f, false);
            sl.transform.SetParent(root, false);
            spotLight = sl.GetComponent<Light>();

            // --- Çevre: nişlerde mumlar, aplikler, kemik yığınları, melek heykeli, eşyalar ---
            for (int i = 1; i < segs; i += 2)
            {
                if (i == 15) continue;
                var d = Quaternion.Euler(0, i * 360f / segs, 0) * Vector3.forward;
                Kit("CandleCluster", root, d * (R - 0.25f) + Vector3.up * 0.9f, i * 360f / segs + 180f);
            }
            foreach (int i in new[] { 4, 8, 12 })
            {
                float a = i * 360f / segs + 9f;
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Kit("WallSconce", root, d * (R - 0.05f) + Vector3.up * 2.6f, a + 180f);
                Kit("L_Candle", root, d * (R - 0.7f) + Vector3.up * 2.7f);
            }
            Kit("L_Candelabra", root, new Vector3(0, 3.4f, -4.5f));
            void Skull(Vector3 p, float yaw, float s = 1f)
            {
                var t = new GameObject("Skull").transform;
                t.SetParent(root, false); t.localPosition = p; t.localRotation = Quaternion.Euler(0, yaw, 0); t.localScale = Vector3.one * s;
                Prim("Cranium", t, PrimitiveType.Sphere, new Vector3(0, 0.1f, 0.01f), new Vector3(0.17f, 0.17f, 0.2f), bone);
                Prim("Face", t, PrimitiveType.Sphere, new Vector3(0, 0.06f, -0.07f), new Vector3(0.13f, 0.12f, 0.1f), bone);
                foreach (int k in new[] { -1, 1 })
                    Prim("Eye", t, PrimitiveType.Sphere, new Vector3(k * 0.035f, 0.08f, -0.115f), new Vector3(0.04f, 0.035f, 0.02f), voidM);
            }
            var rnd = new System.Random(6060);
            foreach (var (cx, cz) in new[] { (-5.6f, -4.2f), (5.4f, -4.8f), (6.2f, 2.6f), (-6.4f, 3.0f), (-2.6f, 6.6f) })
            {
                for (int k = 0; k < 7; k++)
                {
                    var p = new Vector3(cx + (float)rnd.NextDouble() * 1.4f - 0.7f, 0.03f, cz + (float)rnd.NextDouble() * 1.2f - 0.6f);
                    float ang = rnd.Next(360) * Mathf.Deg2Rad; var dd = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (0.25f + 0.1f * k % 3);
                    Rod("Bone", root, p - dd + Vector3.up * 0.04f, p + dd + Vector3.up * 0.05f, 0.05f, bone);
                }
                Skull(new Vector3(cx, 0.03f, cz), rnd.Next(360), 1.3f);
                Skull(new Vector3(cx + 0.4f, 0.03f, cz - 0.3f), rnd.Next(360), 1.0f);
                Kit("Candle", root, new Vector3(cx - 0.3f, 0.02f, cz + 0.35f));
            }
            {   // 15. bölme: melek heykeli (yas tutan) nişte
                float a = 15 * 360f / segs; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                ArchTrim(root, "AngelNiche", d * R, -d, 2.0f, 3.6f, true);
                Kit("StatueAngel", root, d * (R - 0.6f), a + 180f, 1.0f);
                Kit("CandleCluster", root, d * (R - 1.4f), a);
            }
            Kit("Barrel2", root, new Vector3(4.6f, 0, -5.9f), 20f);
            Kit("CrateLong", root, new Vector3(-6.2f, 0, -1.0f), 80f);
            Kit("Jug", root, new Vector3(5.1f, 0, -5.2f), 40f);
            Kit("Pot", root, new Vector3(-5.9f, 0, 4.2f));
            Kit("BrassVase", root, new Vector3(3.4f, 0, 6.9f), 0f);
            Kit("Axe", root, new Vector3(-4.3f, 0, -6.2f), 30f);
            Kit("Lantern", root, new Vector3(1.2f, 0, -6.4f), 0f);
            Kit("Shell", root, new Vector3(-1.1f, 0.75f, 1.25f), 60f);
            // --- v2 dekor: ay rahiplerinin gözlem köşesi, sunak, sıralar, urneler, sancaklar ---
            {
                var dir = Quaternion.Euler(0, 72f, 0) * Vector3.forward; // doğu-kuzeydoğu duvarı
                var dp = dir * (R - 1.3f);
                Kit("StudyTable", root, dp, 72f + 180f, 0.95f);
                Kit("ChairWood", root, dp - dir * 0.8f, 72f, 0.95f);
                Kit("Armillary", root, dp + Quaternion.Euler(0, 72f, 0) * new Vector3(1.4f, 0, 0.2f), 30f, 0.9f);
                Telescope(root, dp + Quaternion.Euler(0, 72f, 0) * new Vector3(-1.5f, 0, 0.1f), 160f, 55f, 0.9f); // şafta dönük dürbün
                Kit("Compass", root, dp + Vector3.up * 0.78f + Quaternion.Euler(0, 72f, 0) * new Vector3(0.3f, 0, 0), 20f);
                Kit("L_Candle", root, dp + Vector3.up * 1.4f - dir * 0.3f);
                Letter(root, dp + Vector3.up * 0.785f + Quaternion.Euler(0, 72f, 0) * new Vector3(-0.95f, 0, 0), 72f, 13);
                foreach (int i in new[] { 2, 6, 10, 14, 18 })
                {
                    float a = i * 360f / segs; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                    Kit("Banner", root, d * (R - 0.05f) + Vector3.up * (H - 0.6f), a + 180f, 1.2f);
                }
                foreach (var (a, item) in new[] { (36f, "Amphora"), (108f, "VaseCeramic"), (180f, "CeramicVase1"), (252f, "Amphora"), (324f, "BrassVase2") })
                {
                    var d = Quaternion.Euler(0, a + 9f, 0) * Vector3.forward;
                    Kit(item, root, d * (R - 0.6f), a, item == "Amphora" ? 0.9f : 1f);
                }
                // Kuyu başında sunak: kadehler, kâse, mum (ay suyu sunusu)
                var ap = new Vector3(-2.6f, 0, -1.9f);
                Slab("OfferingTable", root, ap + Vector3.up * 0.4f, new Vector3(1.1f, 0.8f, 0.6f), stone, 1f);
                Kit("Goblet0", root, ap + new Vector3(-0.3f, 0.8f, 0), 0f);
                Kit("Goblet2", root, ap + new Vector3(0.05f, 0.8f, 0.1f), 0f);
                Kit("Bowl2", root, ap + new Vector3(0.35f, 0.8f, -0.05f), 0f);
                Kit("CandleCluster", root, ap + new Vector3(0.1f, 0.8f, -0.2f));
                // Dua sıraları (kuzeybatı yay)
                foreach (float a in new[] { 300f, 330f })
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                    var b = Slab("Bench", root, d * 6.3f + Vector3.up * 0.42f, new Vector3(1.8f, 0.08f, 0.42f), K.furniture, 1f);
                    b.transform.localRotation = Quaternion.Euler(0, a, 0);
                    foreach (int sg in new[] { -1, 1 }) Slab("BenchLeg", root, d * 6.3f + Quaternion.Euler(0, a, 0) * new Vector3(sg * 0.75f, 0.2f, 0), new Vector3(0.08f, 0.4f, 0.36f), K.furniture, 1f, false).transform.localRotation = Quaternion.Euler(0, a, 0);
                }
                Kit("Chest", root, Quaternion.Euler(0, 200f, 0) * Vector3.forward * (R - 0.8f), 20f, 0.85f);
                Kit("BookStack", root, Quaternion.Euler(0, 228f, 0) * Vector3.forward * (R - 0.9f), 40f);
                Kit("Bucket2", root, new Vector3(1.9f, 0, -1.1f), 0f);
                for (int i = 0; i < 3; i++) MeshObj("RopeCoil", root, new Vector3(-1.95f, 0.03f + i * 0.04f, 0.9f), RingMesh(0.24f - i * 0.015f, 0.04f, 0.04f), K.beam);
                Kit("Rat", root, new Vector3(5.8f, 0, -2.2f), 200f);
                // Zincir kancalarında asılı fenerler + şaftan inen ay ışığı (baked)
                for (int k = 0; k < 4; k++)
                {
                    var d = Quaternion.Euler(0, 45f + k * 90f, 0) * Vector3.forward * 5.2f;
                    Kit("Lantern", root, d + Vector3.up * (H - 2.85f - k * 0.4f), k * 40f);
                    Kit("L_Candle", root, d + Vector3.up * (H - 2.55f - k * 0.4f));
                }
                var moon = LightGo("ShaftMoon", LightType.Spot, Pal.Moon, 18f, 14f, true);
                moon.transform.SetParent(root, false); moon.transform.localPosition = new Vector3(0, H + 2f, 0);
                moon.transform.localRotation = Quaternion.LookRotation(Vector3.down);
                moon.GetComponent<Light>().spotAngle = 70f; moon.GetComponent<Light>().innerSpotAngle = 40f;
                QuadObj("ShaftBeam", root, new Vector3(0, H * 0.5f + 1f, 0), new Vector2(2.4f, H + 2f), K.shaft, Quaternion.LookRotation(Vector3.right, Vector3.up));
                QuadObj("ShaftBeamB", root, new Vector3(0, H * 0.5f + 1f, 0), new Vector2(2.0f, H + 2f), K.shaft, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                foreach (float a in new[] { 0f, 120f, 240f })
                {
                    var f = LightGo("WellFill", LightType.Point, Pal.Candle, 2.6f, 9f, true);
                    f.transform.SetParent(root, false); f.transform.localPosition = Quaternion.Euler(0, a + 60f, 0) * Vector3.forward * 5f + Vector3.up * 3.2f;
                }
            }
            for (int k = 0; k < 6; k++)
            {
                var d = Quaternion.Euler(0, k * 60f + 15f, 0) * Vector3.forward;
                Slab("MossPatch", root, d * (5.6f + Hash01(k, 8)) + Vector3.up * 0.006f, new Vector3(1.0f + Hash01(k, 9), 0.01f, 0.7f + Hash01(k, 10)), moss, 1f, false)
                    .transform.localRotation = Quaternion.Euler(0, k * 37f, 0);
            }
            foreach (int k in new[] { 0, 5, 10, 15 })
            {
                var d = Quaternion.Euler(0, k * 18f, 0) * Vector3.forward;
                KitR("Cobweb", root, d * (R - 0.1f) + Vector3.up * (H - 1.4f), Quaternion.Euler(0, k * 18f + 180f, 180f), 1.8f);
            }
            // giriş (güney): kapı + basamaklar
            {
                var d = Vector3.back;
                Kit("Door", root, d * (R - 0.02f), 0f, 1.1f);
                arrive = new GameObject("WellArrive").transform;
                arrive.SetParent(root, false);
                arrive.localPosition = d * (R - 1.4f) + Vector3.up * 0.05f;
            }

            var core = new Bounds(new Vector3(0, 0.6f, 0), new Vector3(2.8f, 1.4f, 2.8f));
            KitProbes(root, new Vector3(0, H * 0.5f, 0), new Vector3(R * 1.6f, H - 0.4f, R * 1.6f), 5, 3, 5, core);
            KitReflection(root, new Vector3(0, 2f, 0), new Vector3(R * 2f, H, R * 2f));
            return root;
        }
    }
}
