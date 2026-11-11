using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Görsel katman: Poly Haven dokulu malzemeler, prosedürel ağlar (duvar/zemin plakaları, çan, dişli), meşaleler, modeller,
    /// gökyüzü/sis/post-fx ve iki odanın (A çan kulesi, B zindan/sarnıç) + menü arka planının kurulumu.
    /// Oyun mantığına dokunmaz; bulmaca/etkileşim nesnelerinin konumları ve bileşenleri önceki gri kutuyla aynıdır.
    /// </summary>
    public static partial class CoopSetup
    {
        const string TexRoot = "Assets/ThirdParty/PolyHaven/Textures/";
        const string ModelRoot = "Assets/ThirdParty/PolyHaven/Models/";
        const string SkyPath = "Assets/ThirdParty/PolyHaven/Sky/rogland_clear_night.jpg";
        const string GenMeshDir = "Assets/Generated/Meshes";
        const string PostFxPath = "Assets/Art/Kit/Lighting/KitPostFx.asset";

        /// <summary>HDR + bloom/vinyet/renk. Düşük cihazda FPS sorun olursa false yap ve Setup All çalıştır.</summary>
        const bool PostFx = true;

        static readonly Color TorchColor = new Color(1f, 0.6f, 0.28f);
        static readonly Color MoonColor = new Color(0.55f, 0.66f, 1f);

        class ArtMats
        {
            public Material towerWall, towerFloor, wood, darkWood, dungeonWall, dungeonFloor, dungeonCeil, flagstone,
                            iron, bronze, brass, gold, water, flame, flameCore, plaque, plate, lanternGlass, sky;
        }

        static ArtMats A;
        static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> propMats = new Dictionary<string, Material>();

        // ---------- Malzemeler ----------

        static Texture2D Tex(string path, bool normal, int maxSize = 1024)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogError("[CoopSetup] Doku yok: " + path); return null; }
            bool dirty = false;
            var want = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != want) { ti.textureType = want; dirty = true; }
            if (ti.maxTextureSize != maxSize) { ti.maxTextureSize = maxSize; dirty = true; }
            if (ti.anisoLevel != 4) { ti.anisoLevel = 4; dirty = true; }
            var want6 = normal ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6;
            var ps = ti.GetPlatformTextureSettings("Android");
            if (!ps.overridden || ps.format != want6 || ps.maxTextureSize != maxSize)
            {
                ps.overridden = true; ps.format = want6; ps.maxTextureSize = maxSize;
                ti.SetPlatformTextureSettings(ps); dirty = true;
            }
            if (dirty) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void SetMaps(Material m, Texture2D diff, Texture2D nor, float smooth, float bump, float metallic)
        {
            if (diff != null) m.SetTexture("_BaseMap", diff);
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(m);
        }

        static Material TexMat(string name, string tex, Color tint, float smooth = 0.2f, float bump = 1f, float metallic = 0f)
        {
            var m = MakeMat(name, tint);
            string b = TexRoot + tex + "/" + tex;
            SetMaps(m, Tex(b + "_diff.jpg", false), Tex(b + "_nor_gl.jpg", true), smooth, bump, metallic);
            return m;
        }

        static Material GlowMat(string name, Color baseColor, Color emission, float smooth = 0.3f)
        {
            var m = MakeMat(name, baseColor);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void PrepareArt()
        {
            meshCache.Clear();
            propMats.Clear();
            A = new ArtMats
            {
                towerWall = TexMat("TowerWall", "medieval_blocks_05", new Color(0.78f, 0.74f, 0.7f), 0.15f, 1.2f),
                towerFloor = TexMat("TowerFloor", "old_wood_floor", new Color(0.62f, 0.52f, 0.44f), 0.25f),
                wood = TexMat("Wood", "medieval_wood", new Color(0.7f, 0.6f, 0.5f), 0.2f),
                darkWood = TexMat("DarkWood", "medieval_wood", new Color(0.32f, 0.26f, 0.22f), 0.15f),
                dungeonWall = TexMat("DungeonWall", "castle_wall_slates", new Color(0.5f, 0.55f, 0.6f), 0.3f, 1.3f),
                dungeonFloor = TexMat("DungeonFloor", "cobblestone_floor_08", new Color(0.5f, 0.54f, 0.58f), 0.45f, 1.3f),
                dungeonCeil = TexMat("DungeonCeil", "monastery_stone_floor", new Color(0.3f, 0.32f, 0.36f), 0.2f, 0.7f),
                flagstone = TexMat("Flagstone", "monastery_stone_floor", new Color(0.6f, 0.6f, 0.62f), 0.3f, 1.2f),
                iron = TexMat("Iron", "rusty_metal_02", new Color(0.42f, 0.38f, 0.36f), 0.35f, 1f, 0.5f),
                bronze = MakeMat("Bronze", new Color(0.62f, 0.4f, 0.17f)),
                brass = MakeMat("Brass", new Color(0.78f, 0.6f, 0.25f)),
                gold = GlowMat("Gold", new Color(1f, 0.78f, 0.25f), new Color(1f, 0.62f, 0.15f) * 1.4f, 0.7f),
                water = MakeMat("Water", new Color(0.02f, 0.06f, 0.07f)),
                flame = GlowMat("Flame", new Color(1f, 0.45f, 0.1f), new Color(1f, 0.38f, 0.06f) * 2.6f),
                flameCore = GlowMat("FlameCore", new Color(1f, 0.9f, 0.5f), new Color(1f, 0.82f, 0.4f) * 3.2f),
                plaque = TexMat("Plaque", "monastery_stone_floor", new Color(0.3f, 0.3f, 0.33f), 0.25f),
                plate = MakeMat("HintPlateMat", new Color(0.15f, 0.15f, 0.18f)),
                lanternGlass = GlowMat("LanternGlass", new Color(1f, 0.75f, 0.4f), new Color(1f, 0.6f, 0.25f) * 3f),
            };
            A.bronze.SetFloat("_Metallic", 0.55f); A.bronze.SetFloat("_Smoothness", 0.6f);
            A.brass.SetFloat("_Metallic", 0.5f); A.brass.SetFloat("_Smoothness", 0.65f);
            A.water.SetFloat("_Smoothness", 0.96f); A.water.SetFloat("_Metallic", 0.1f);
            A.gold.SetFloat("_Metallic", 0.6f);

            // Gece gökyüzü (panoramik)
            var ti = AssetImporter.GetAtPath(SkyPath) as TextureImporter;
            if (ti != null && (ti.maxTextureSize != 4096 || ti.mipmapEnabled))
            {
                ti.maxTextureSize = 4096; ti.mipmapEnabled = false; ti.wrapModeV = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            Directory.CreateDirectory("Assets/Materials");
            const string skyMatPath = "Assets/Materials/NightSky.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(skyMatPath) != null) AssetDatabase.DeleteAsset(skyMatPath);
            A.sky = new Material(Shader.Find("Skybox/Panoramic"));
            A.sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SkyPath));
            A.sky.SetFloat("_Exposure", 1.0f);
            A.sky.SetColor("_Tint", new Color(0.32f, 0.4f, 0.6f)); // gündüz çekilmiş zemini soğut/karart: ay ışığı havası
            A.sky.SetFloat("_Rotation", 200f);
            AssetDatabase.CreateAsset(A.sky, skyMatPath);

            SetupPostFx();
        }

        // ---------- Post-fx / atmosfer ----------

        static void SetupPostFx()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PostFxPath));
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath) != null) AssetDatabase.DeleteAsset(PostFxPath);
            AssetDatabase.DeleteAsset("Assets/Settings/PostFx.asset"); // eski konum
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostFxPath);

            // Değerler: Docs/StyleGuide.md "Post-process (tek profil)"
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f); bloom.intensity.Override(0.5f); bloom.scatter.Override(0.62f);
            bloom.highQualityFiltering.Override(false);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f); vig.smoothness.Override(0.45f);
            var col = profile.Add<ColorAdjustments>(true);
            col.postExposure.Override(0f); col.contrast.Override(10f); col.saturation.Override(-8f);
            col.colorFilter.Override(new Color(1f, 0.97f, 0.93f));
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            foreach (var c in profile.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Sahne atmosferi: gece gökyüzü, düz ortam ışığı, üstel sis, global post-fx hacmi, kamera ayarı.</summary>
        static void ApplyAtmosphere(Camera cam, Color ambient, Color fog, float fogDensity)
        {
            RenderSettings.skybox = A.sky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.reflectionIntensity = 0.4f;

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 140f; // 250 m ötedeki diğer oda hiç çizilmez
            cam.nearClipPlane = 0.1f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = PostFx;
            if (!PostFx) return;
            var vol = new GameObject("PostFx").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath);
        }

        static Light Moon(Vector3 euler, float intensity)
        {
            var go = new GameObject("Moon Light");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = MoonColor;
            l.intensity = intensity;
            l.shadows = LightShadows.Hard;
            l.shadowStrength = 0.92f;
            go.transform.rotation = Quaternion.Euler(euler);
            return l;
        }

        static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = pos;
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        // ---------- Prosedürel ağlar ----------

        static Mesh SaveMesh(Mesh m, string name)
        {
            Directory.CreateDirectory(GenMeshDir);
            string path = GenMeshDir + "/" + name + ".asset";
            m.name = name;
            var ex = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (ex != null)
            {
                EditorUtility.CopySerialized(m, ex);
                Object.DestroyImmediate(m);
                EditorUtility.SetDirty(ex);
                m = ex;
            }
            else AssetDatabase.CreateAsset(m, path);
            meshCache[name] = m;
            return m;
        }

        /// <summary>Üçgeni, yüzü istenen normale bakacak sarımla ekler.</summary>
        static void Tri(List<int> tris, List<Vector3> v, int a, int b, int c, Vector3 n)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), n) < 0f) { int t = b; b = c; c = t; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        static string F(float f) => f.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p').Replace('-', 'm');

        /// <summary>Kutu ağı: boyut ağa işlenir, UV'ler metre cinsinden döşenir (tile = bir doku tekrarının metresi).</summary>
        static Mesh SlabMesh(Vector3 s, float tile)
        {
            string key = "slab_" + F(s.x) + "_" + F(s.y) + "_" + F(s.z) + "_t" + F(tile);
            if (meshCache.TryGetValue(key, out var cached)) return cached;

            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            var h = s * 0.5f;
            void Face(Vector3 normal, Vector3 up)
            {
                var right = Vector3.Cross(normal, up);
                float en = Mathf.Abs(Vector3.Dot(normal, h)), er = Mathf.Abs(Vector3.Dot(right, h)), eu = Mathf.Abs(Vector3.Dot(up, h));
                var c = normal * en; int i = v.Count;
                v.Add(c - right * er - up * eu); v.Add(c + right * er - up * eu); v.Add(c + right * er + up * eu); v.Add(c - right * er + up * eu);
                uv.Add(new Vector2(-er, -eu) / tile); uv.Add(new Vector2(er, -eu) / tile); uv.Add(new Vector2(er, eu) / tile); uv.Add(new Vector2(-er, eu) / tile);
                for (int k = 0; k < 4; k++) n.Add(normal);
                Tri(tris, v, i, i + 3, i + 2, normal); Tri(tris, v, i, i + 2, i + 1, normal);
            }
            Face(Vector3.forward, Vector3.up); Face(Vector3.back, Vector3.up);
            Face(Vector3.right, Vector3.up); Face(Vector3.left, Vector3.up);
            Face(Vector3.up, Vector3.forward); Face(Vector3.down, Vector3.forward);

            var m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m); // lightmap UV'si (baked ışık)
            return SaveMesh(m, key);
        }

        /// <summary>Döner yüzey (çan). Profil: (yarıçap, y) alttan üste. Çift taraflı.</summary>
        static Mesh LatheMesh(string name, Vector2[] prof, int seg)
        {
            if (meshCache.TryGetValue(name, out var cached)) return cached;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            int rows = prof.Length;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                for (int k = 0; k < rows; k++)
                {
                    v.Add(new Vector3(Mathf.Cos(a) * prof[k].x, prof[k].y, Mathf.Sin(a) * prof[k].x));
                    uv.Add(new Vector2((float)i / seg * 4f, (float)k / (rows - 1)));
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
            m.RecalculateNormals();

            // iç yüz: aynı köşeler, ters normal + ters sarım
            var nrm = new List<Vector3>(m.normals);
            int count = v.Count;
            for (int i = 0; i < count; i++) { v.Add(v[i]); uv.Add(uv[i]); nrm.Add(-nrm[i]); }
            int tc = tris.Count;
            for (int i = 0; i < tc; i += 3) { tris.Add(tris[i] + count); tris.Add(tris[i + 2] + count); tris.Add(tris[i + 1] + count); }
            m.Clear();
            m.SetVertices(v); m.SetNormals(nrm); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Dişli: XY düzleminde, ekseni Z. Ortası delik.</summary>
        static Mesh GearMesh(int teeth, float rOuter, float depth, float rHole, float thick)
        {
            string name = "gear_" + teeth + "_" + F(rOuter) + "_" + F(thick);
            if (meshCache.TryGetValue(name, out var cached)) return cached;

            int cnt = teeth * 4;
            var outline = new Vector2[cnt];
            var inner = new Vector2[cnt];
            float step = Mathf.PI * 2f / teeth, rRoot = rOuter - depth;
            float[] frac = { 0f, 0.16f, 0.5f, 0.66f };
            float[] rad = { rRoot, rOuter, rOuter, rRoot };
            for (int t = 0; t < teeth; t++)
                for (int k = 0; k < 4; k++)
                {
                    float a = (t + frac[k]) * step;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    outline[t * 4 + k] = dir * rad[k];
                    inner[t * 4 + k] = dir * rHole;
                }

            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            float hz = thick * 0.5f;
            void Cap(float z, Vector3 normal)
            {
                int b = v.Count;
                for (int i = 0; i < cnt; i++) { v.Add(new Vector3(outline[i].x, outline[i].y, z)); n.Add(normal); uv.Add(outline[i] / (rOuter * 2f) + Vector2.one * 0.5f); }
                for (int i = 0; i < cnt; i++) { v.Add(new Vector3(inner[i].x, inner[i].y, z)); n.Add(normal); uv.Add(inner[i] / (rOuter * 2f) + Vector2.one * 0.5f); }
                for (int i = 0; i < cnt; i++)
                {
                    int j = (i + 1) % cnt;
                    Tri(tris, v, b + i, b + j, b + cnt + j, normal); Tri(tris, v, b + i, b + cnt + j, b + cnt + i, normal);
                }
            }
            void Side(Vector2[] ring, bool outward)
            {
                for (int i = 0; i < cnt; i++)
                {
                    int j = (i + 1) % cnt;
                    var e = ring[j] - ring[i];
                    var nn = new Vector3(e.y, -e.x, 0f).normalized;
                    var mid = (Vector3)(ring[i] + ring[j]);
                    if (Vector3.Dot(nn, mid) < 0f) nn = -nn;
                    if (!outward) nn = -nn;
                    int b = v.Count;
                    v.Add(new Vector3(ring[i].x, ring[i].y, -hz)); v.Add(new Vector3(ring[j].x, ring[j].y, -hz));
                    v.Add(new Vector3(ring[j].x, ring[j].y, hz)); v.Add(new Vector3(ring[i].x, ring[i].y, hz));
                    for (int k = 0; k < 4; k++) n.Add(nn);
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0.2f, 0)); uv.Add(new Vector2(0.2f, 0.2f)); uv.Add(new Vector2(0, 0.2f));
                    Tri(tris, v, b, b + 1, b + 2, nn); Tri(tris, v, b, b + 2, b + 3, nn);
                }
            }
            Cap(-hz, Vector3.back); Cap(hz, Vector3.forward);
            Side(outline, true); Side(inner, false);

            var m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        // ---------- Yapı taşları ----------

        /// <summary>Dokulu kutu (duvar/zemin/kiriş). Varsayılan statik + BoxCollider; hareketli parçada isStatic=false yap.</summary>
        static GameObject Slab(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, float tile = 2.5f, bool collide = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = SlabMesh(size, tile);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collide) go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
            return go;
        }

        static GameObject Prim(string name, Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject MeshObj(string name, Transform parent, Vector3 pos, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        struct Opening
        {
            public float c, w, bottom, top;
            public Opening(float c, float w, float bottom, float top) { this.c = c; this.w = w; this.bottom = bottom; this.top = top; }
        }

        /// <summary>Duvar: taban merkezi baseCenter, ekseni boyunca len, açıklıklar (kapı/pencere) boş bırakılır.</summary>
        static void Wall(Transform root, string name, Vector3 baseCenter, bool alongX, float len, float h, Material mat, float tile, params Opening[] ops)
        {
            const float t = 0.5f;
            Vector3 Along(float a) => alongX ? new Vector3(a, 0, 0) : new Vector3(0, 0, a);
            void Piece(string n, float a0, float a1, float y0, float y1)
            {
                if (a1 - a0 < 0.01f || y1 - y0 < 0.01f) return;
                var size = alongX ? new Vector3(a1 - a0, y1 - y0, t) : new Vector3(t, y1 - y0, a1 - a0);
                Slab(n, root, baseCenter + Along((a0 + a1) * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f), size, mat, tile);
            }
            var sorted = new List<Opening>(ops);
            sorted.Sort((x, y) => x.c.CompareTo(y.c));
            float cur = -len * 0.5f; int i = 0;
            foreach (var o in sorted)
            {
                Piece(name + "_P" + i, cur, o.c - o.w * 0.5f, 0, h);
                Piece(name + "_Sill" + i, o.c - o.w * 0.5f, o.c + o.w * 0.5f, 0, o.bottom);
                Piece(name + "_Lintel" + i, o.c - o.w * 0.5f, o.c + o.w * 0.5f, o.top, h);
                cur = o.c + o.w * 0.5f; i++;
            }
            Piece(name + "_P" + i, cur, len * 0.5f, 0, h);
        }

        /// <summary>
        /// Zemin/tavan: görsel olarak nx*nz parçaya bölünür (nesne başına 4 ek ışık sınırı büyük yüzeyde ışık kaybettirmesin),
        /// çarpışma tek kutudur. Doku tekrarı parça boyunun yarısı: parça sınırlarında desen kesintisiz.
        /// </summary>
        static void Deck(Transform root, string name, Vector3 center, float sx, float sz, float thick, Material mat, int nx, int nz)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = center;
            holder.AddComponent<BoxCollider>().size = new Vector3(sx, thick, sz);
            holder.isStatic = true;
            float cx = sx / nx, cz = sz / nz;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var p = new Vector3(-sx * 0.5f + cx * (i + 0.5f), 0, -sz * 0.5f + cz * (j + 0.5f));
                    var go = new GameObject(name + "_" + i + "_" + j);
                    go.transform.SetParent(holder.transform, false);
                    go.transform.localPosition = p;
                    go.AddComponent<MeshFilter>().sharedMesh = SlabMesh(new Vector3(cx, thick, cz), cx * 0.5f);
                    go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                    go.isStatic = true;
                }
        }

        /// <summary>Duvar meşalesi: demir kol + alev + titreyen noktasal ışık. facing: duvardan odaya doğru.</summary>
        static Light Torch(Transform parent, string name, Vector3 pos, Vector3 facing, float intensity = 9f, float range = 10f)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            Slab("Bracket", root, new Vector3(0, 0, 0.17f), new Vector3(0.07f, 0.07f, 0.36f), A.iron, 1f, false);
            Slab("Plate", root, new Vector3(0, 0, 0.02f), new Vector3(0.22f, 0.34f, 0.04f), A.iron, 1f, false);
            Prim("Cup", root, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0.36f), new Vector3(0.17f, 0.12f, 0.17f), A.iron);
            var flame = Prim("Flame", root, PrimitiveType.Sphere, new Vector3(0, 0.42f, 0.36f), new Vector3(0.2f, 0.42f, 0.2f), A.flame);
            Prim("Core", flame.transform, PrimitiveType.Sphere, new Vector3(0, -0.12f, 0), new Vector3(0.55f, 0.55f, 0.55f), A.flameCore);
            flame.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var light = PointLight(root, "Light", new Vector3(0, 0.75f, 1.1f), TorchColor, intensity, range); // duvardan uzak: yanık leke olmasın
            var fl = root.gameObject.AddComponent<LightFlicker>();
            var so = new SerializedObject(fl);
            SetRef(so, "target", light);
            SetRef(so, "flame", flame.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        /// <summary>
        /// Poly Haven modeli: hedef yüksekliğe ölçekler, tabanını pos'a oturtur, URP malzemesi atar (FBX'in kendi malzemesi kullanılmaz).
        /// solid: sınır kutusu kadar BoxCollider.
        /// </summary>
        static GameObject Prop(string model, Transform parent, Vector3 pos, float yaw, float height, bool solid = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + model + "/" + model + ".fbx");
            var wrap = new GameObject(model);
            wrap.transform.SetParent(parent, false);
            wrap.transform.localPosition = pos;
            if (asset == null) { Debug.LogError("[CoopSetup] Model yok: " + model); return wrap; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            inst.transform.SetParent(wrap.transform, false);
            var rends = inst.GetComponentsInChildren<Renderer>();
            Bounds B() { var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds); return b; }
            if (rends.Length == 0) return wrap;
            var b0 = B();
            inst.transform.localScale *= height / Mathf.Max(0.0001f, b0.size.y);
            var b1 = B();
            inst.transform.position += new Vector3(wrap.transform.position.x - b1.center.x, wrap.transform.position.y - b1.min.y, wrap.transform.position.z - b1.center.z);

            foreach (var r in rends)
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = PropMat(model, src[i] != null ? src[i].name : "");
                r.sharedMaterials = dst;
            }
            if (solid)
            {
                var b2 = B();
                var col = wrap.AddComponent<BoxCollider>();
                col.center = wrap.transform.InverseTransformPoint(b2.center);
                col.size = b2.size * 0.92f;
            }
            wrap.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            foreach (var t in wrap.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return wrap;
        }

        static Material PropMat(string model, string srcName)
        {
            string low = srcName.ToLowerInvariant();
            if (low.Contains("glass")) return A.lanternGlass;
            string dir = ModelRoot + model + "/textures";
            string diff = null, nor = null; int bestScore = -1;
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir))
                {
                    string fn = Path.GetFileName(f);
                    if (fn.EndsWith(".meta") || !fn.Contains("_diff_")) continue;
                    string part = fn.Substring(0, fn.IndexOf("_diff_")).ToLowerInvariant();
                    if (part.StartsWith(model.ToLowerInvariant())) part = part.Substring(model.Length).Trim('_');
                    int score = part.Length > 0 && low.Contains(part) ? 2 : part.Length == 0 ? 1 : 0;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        diff = dir + "/" + fn;
                        string n = dir + "/" + fn.Replace("_diff_", "_nor_gl_");
                        nor = File.Exists(n) ? n : null;
                    }
                }
            string key = model + "|" + diff;
            if (propMats.TryGetValue(key, out var m)) return m;
            m = MakeMat("Prop_" + model + "_" + propMats.Count, Color.white);
            SetMaps(m, diff != null ? Tex(diff, false) : null, nor != null ? Tex(nor, true) : null, 0.2f, 1f, 0f);
            propMats[key] = m;
            return m;
        }

        // ---------- Etkileşim/bulmaca nesneleri (bileşenler ve konumlar gri kutudakiyle aynı) ----------

        static void SetRefArray(SerializedObject so, string field, Object[] values)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[CoopSetup] Alan yok: " + field); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>Taş kaide + parlayan düğme. Taban konumu basePos (y=zemin).</summary>
        static GameObject MakeButtonProp(string name, Transform parent, Vector3 basePos, Color color, InteractId id, string label)
        {
            var b = Slab(name, parent, basePos + new Vector3(0, 0.55f, 0), new Vector3(0.7f, 1.1f, 0.5f), A.flagstone, 1.2f);
            b.isStatic = false;
            Slab("Top", b.transform, new Vector3(0, 0.58f, 0), new Vector3(0.8f, 0.08f, 0.6f), A.iron, 1f, false).isStatic = false;
            Prim("Cap", b.transform, PrimitiveType.Cylinder, new Vector3(0, 0.66f, 0), new Vector3(0.34f, 0.05f, 0.34f),
                 GlowMat(name + "Mat", color, color * 2.2f));
            PointLight(b.transform, "Glow", new Vector3(0, 0.95f, 0), color, 1.2f, 2.5f);
            var i = b.AddComponent<Interactable>();
            i.Key = (int)id; i.Label = label;
            return b;
        }

        /// <summary>Kalkan kapı paneli (LiftDoor): ahşap/demir gövde + demir kuşaklar. alongX: panel X ekseni boyunca uzanır.</summary>
        static LiftDoor MakeDoor(string name, Transform parent, Vector3 pos, bool alongX, Material body)
        {
            var size = alongX ? new Vector3(3f, 3f, 0.3f) : new Vector3(0.3f, 3f, 3f);
            var d = Slab(name, parent, pos, size, body, 1.5f);
            d.isStatic = false;
            for (int k = 0; k < 3; k++)
            {
                float y = -1.1f + k * 1.1f;
                var bs = alongX ? new Vector3(3.02f, 0.16f, 0.36f) : new Vector3(0.36f, 0.16f, 3.02f);
                Slab("Band" + k, d.transform, new Vector3(0, y, 0), bs, A.iron, 1f, false).isStatic = false;
            }
            return d.AddComponent<LiftDoor>();
        }

        // ---------- Bulmaca 1: A'da 4 kol, B'de doğru sırayı gösteren fresk + kasa kapısı ----------

        /// <summary>Varsayılan doğru sıra (kol indeksleri). Setup sonrası Inspector'dan SequenceInput.order ile değiştirilebilir.</summary>
        static readonly int[] Puzzle1Order = { 2, 0, 3, 1 };

        static SyncedLever MakeLever(Transform parent, int index, Vector3 pos, PrimitiveType shape, Color symbolColor)
        {
            var root = new GameObject("Lever" + index).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            Slab("Base", root, new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.6f, 0.55f), A.flagstone, 1.2f).isStatic = false;
            Slab("Slot", root, new Vector3(0, 0.61f, 0), new Vector3(0.5f, 0.04f, 0.4f), A.iron, 1f, false);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0, 0.6f, 0);
            Slab("Arm", pivot, new Vector3(0, 0.45f, 0), new Vector3(0.1f, 0.9f, 0.1f), A.iron, 1f).isStatic = false;
            var knob = Prim("Knob", pivot, PrimitiveType.Sphere, new Vector3(0, 0.95f, 0), Vector3.one * 0.24f, A.brass);
            knob.AddComponent<SphereCollider>();

            // Duvardaki taş levha + parlayan sembol (SequenceHint bu sembolü B'ye kopyalar)
            Slab("Plaque", root, new Vector3(0, 2.1f, 0.93f), new Vector3(1.1f, 1.1f, 0.12f), A.plaque, 1.2f, false);
            Slab("PlaqueFrame", root, new Vector3(0, 2.1f, 0.97f), new Vector3(1.3f, 1.3f, 0.06f), A.iron, 1f, false);
            var sym = Prim("Symbol_" + shape, root, shape, new Vector3(0, 2.1f, 0.62f), Vector3.one * 0.45f,
                           GlowMat("Symbol" + index + "Mat", symbolColor, symbolColor * 1.8f));
            sym.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var it = root.gameObject.AddComponent<Interactable>();
            it.Label = "Kolu çek";
            var lever = root.gameObject.AddComponent<SyncedLever>();
            var so = new SerializedObject(lever);
            so.FindProperty("puzzleId").intValue = 1;
            so.FindProperty("nodeId").intValue = index;
            SetRef(so, "handle", pivot);
            SetRef(so, "symbol", sym);
            so.ApplyModifiedPropertiesWithoutUndo();
            return lever;
        }

        static void BuildPuzzle1(Transform roomA, Transform roomB)
        {
            var def = new GameObject("Puzzle1").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 1;
            dso.FindProperty("title").stringValue = "Bulmaca 1 (kollar)";
            dso.FindProperty("startsActive").boolValue = true;
            var up = dso.FindProperty("unlocks"); up.arraySize = 1; up.GetArrayElementAtIndex(0).intValue = 2;
            dso.ApplyModifiedPropertiesWithoutUndo();

            // Oda A: kuzey duvarı önünde 4 kol, her birinin üstünde farklı şekil+renk sembol
            var shapes = new[] { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder, PrimitiveType.Capsule };
            var colors = new[] { new Color(0.95f, 0.18f, 0.15f), new Color(0.2f, 0.85f, 0.3f), new Color(0.2f, 0.45f, 1f), new Color(1f, 0.82f, 0.15f) };
            var levers = new SyncedLever[4];
            for (int i = 0; i < 4; i++)
                levers[i] = MakeLever(roomA, i, new Vector3(-4.5f + i * 3f, 0, 7f), shapes[i], colors[i]);

            var seq = new GameObject("Puzzle1_Sequence").AddComponent<SequenceInput>();
            seq.transform.SetParent(roomA, false);
            var sso = new SerializedObject(seq);
            sso.FindProperty("puzzleId").intValue = 1;
            sso.FindProperty("nodeId").intValue = 4;
            SetRefArray(sso, "inputs", levers);
            var op = sso.FindProperty("order"); op.arraySize = Puzzle1Order.Length;
            for (int i = 0; i < Puzzle1Order.Length; i++) op.GetArrayElementAtIndex(i).intValue = Puzzle1Order[i];
            sso.ApplyModifiedPropertiesWithoutUndo();

            // Oda B: doğu duvarında fresk (soldan sağa = 1→4; doğuya bakınca sol = kuzey)
            var board = Slab("HintBoard", roomB, new Vector3(7.9f, 2f, 0), new Vector3(0.18f, 2.2f, 9.6f), A.plaque, 2f, false);
            Slab("HintBoardFrameTop", roomB, new Vector3(7.86f, 3.16f, 0), new Vector3(0.26f, 0.14f, 9.9f), A.iron, 1f, false);
            Slab("HintBoardFrameBottom", roomB, new Vector3(7.86f, 0.84f, 0), new Vector3(0.26f, 0.14f, 9.9f), A.iron, 1f, false);
            var slots = new Transform[4];
            var plates = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                float z = 3.3f - i * 2.2f;
                var plate = Slab("HintPlate" + (i + 1), roomB, new Vector3(7.78f, 2f, z), new Vector3(0.06f, 1.4f, 1.7f), A.plate, 1f, false);
                plate.isStatic = false;
                plates[i] = plate.GetComponent<Renderer>();
                var slot = new GameObject("HintSlot" + (i + 1)).transform;
                slot.SetParent(roomB, false);
                slot.localPosition = new Vector3(7.5f, 2f, z);
                slots[i] = slot;
            }
            var hint = board.AddComponent<SequenceHint>();
            var hso = new SerializedObject(hint);
            SetRef(hso, "sequence", seq);
            SetRefArray(hso, "slots", slots);
            SetRefArray(hso, "slotPlates", plates);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Oda B: batı kasa kapısı + içinde ödül (altın dişli: bulmaca 3'e gönderme)
            var vaultDoor = MakeDoor("VaultDoor", roomB, new Vector3(-8.25f, 1.5f, 0), false, A.iron);
            var pd = vaultDoor.gameObject.AddComponent<PuzzleDoor>();
            var pso = new SerializedObject(pd);
            pso.FindProperty("puzzleId").intValue = 1;
            SetRef(pso, "door", vaultDoor);
            pso.ApplyModifiedPropertiesWithoutUndo();

        }

        // ---------- Bulmaca 2: kibrit mahzeni (B karanlıkta okur, A kulede kadranları çevirir) ----------

        static readonly Vector2[] BellProfile =
        {
            new Vector2(1.02f, 0f), new Vector2(1.1f, 0.06f), new Vector2(0.98f, 0.2f), new Vector2(0.8f, 0.45f), new Vector2(0.66f, 0.8f),
            new Vector2(0.58f, 1.15f), new Vector2(0.52f, 1.45f), new Vector2(0.42f, 1.65f), new Vector2(0.25f, 1.76f), new Vector2(0f, 1.8f),
        };

        static void Candle(Transform parent, Vector3 pos, Material wax, out GameObject flame)
        {
            Prim("Wax", parent, PrimitiveType.Cylinder, pos + new Vector3(0, 0.13f, 0), new Vector3(0.09f, 0.13f, 0.09f), wax);
            flame = Prim("Flame", parent, PrimitiveType.Sphere, pos + new Vector3(0, 0.31f, 0), new Vector3(0.05f, 0.1f, 0.05f), A.flame);
            flame.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static SymbolDial MakeDial(Transform parent, int index, Vector3 pos, CryptPuzzle crypt)
        {
            var root = new GameObject("Dial" + index).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, -90, 0); // yüz (-Z) odaya (doğu)
            Slab("Plate", root, Vector3.zero, new Vector3(1.5f, 1.5f, 0.12f), A.plaque, 1.2f).isStatic = false;
            Slab("Frame", root, new Vector3(0, 0, 0.04f), new Vector3(1.66f, 1.66f, 0.06f), A.iron, 1f, false);
            var ring = MeshObj("Ring", root, new Vector3(0, 0, -0.12f), GearMesh(12, 0.68f, 0.1f, 0.5f, 0.08f), A.bronze).transform;
            Prim("Pointer", root, PrimitiveType.Cube, new Vector3(0, 0.78f, -0.14f), new Vector3(0.08f, 0.16f, 0.06f), A.gold);
            var anchor = new GameObject("Glyph").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0, 0, -0.18f);

            var it = root.gameObject.AddComponent<Interactable>();
            it.Label = "Kadranı çevir";
            var d = root.gameObject.AddComponent<SymbolDial>();
            var so = new SerializedObject(d);
            so.FindProperty("puzzleId").intValue = 2;
            so.FindProperty("nodeId").intValue = 15 + index;
            so.FindProperty("dialIndex").intValue = index;
            SetRef(so, "crypt", crypt);
            SetRef(so, "glyphAnchor", anchor);
            SetRef(so, "ring", ring);
            SetRef(so, "glyphMat", A.gold);
            so.ApplyModifiedPropertiesWithoutUndo();
            return d;
        }

        static SyncedLever MakeConfirmLever(Transform parent, Vector3 pos, float yaw, int puzzleId, int nodeId, string label = "Mührü onayla")
        {
            var root = new GameObject("ConfirmLever").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            Slab("Base", root, new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.6f, 0.55f), A.flagstone, 1.2f).isStatic = false;
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0, 0.6f, 0);
            Slab("Arm", pivot, new Vector3(0, 0.45f, 0), new Vector3(0.1f, 0.9f, 0.1f), A.iron, 1f).isStatic = false;
            Prim("Knob", pivot, PrimitiveType.Sphere, new Vector3(0, 0.95f, 0), Vector3.one * 0.26f, A.gold).AddComponent<SphereCollider>();
            var it = root.gameObject.AddComponent<Interactable>();
            it.Label = label;
            var lever = root.gameObject.AddComponent<SyncedLever>();
            var so = new SerializedObject(lever);
            so.FindProperty("puzzleId").intValue = puzzleId;
            so.FindProperty("nodeId").intValue = nodeId;
            SetRef(so, "handle", pivot);
            so.ApplyModifiedPropertiesWithoutUndo();
            return lever;
        }

        /// <summary>
        /// Kule batı kapısı (bulmaca 1 açar) → aşağı inen merdiven → Saat Odası (zemin y=-4). Dönüş: oda çerçevesi
        /// (yerel x -8..1.5, z -5..5, y 0..4.5; batı duvarı x=-8.25), içindekiler eski kule batı duvarı koordinatlarıyla yerleşir.
        /// </summary>
        static Transform BuildClockRoom(Transform roomA)
        {
            var door = MakeDoor("StairDoor", roomA, new Vector3(-8.25f, 1.5f, 0), false, A.wood);
            var pd = door.gameObject.AddComponent<PuzzleDoor>();
            var pso = new SerializedObject(pd);
            pso.FindProperty("puzzleId").intValue = 1;
            SetRef(pso, "door", door);
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Merdiven: x -8.5 → -16, y 0 → -4. Çarpışma tek rampa, basamaklar görsel.
            const float x0 = -8.5f, x1 = -16f, drop = 4f;
            float run = x0 - x1, ang = Mathf.Atan2(drop, run);
            var ramp = Slab("StairRamp", roomA, Vector3.zero, new Vector3(Mathf.Sqrt(run * run + drop * drop) + 0.3f, 0.3f, 3f), A.flagstone, 1f);
            ramp.GetComponent<MeshRenderer>().enabled = false;
            ramp.transform.localPosition = new Vector3((x0 + x1) * 0.5f + 0.15f * Mathf.Sin(ang), -drop * 0.5f - 0.15f * Mathf.Cos(ang), 0);
            ramp.transform.localRotation = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg);
            const int steps = 16;
            float sr = run / steps, sh = drop / steps;
            for (int i = 0; i < steps; i++)
                Slab("Step" + i, roomA, new Vector3(x0 - sr * (i + 0.5f), -sh * i - 0.25f, 0), new Vector3(sr, 0.5f, 3f), A.flagstone, 1f, false);
            Wall(roomA, "StairWallN", new Vector3((x0 + x1) * 0.5f, -drop, 1.75f), true, run, 7f, A.towerWall, 3f);
            Wall(roomA, "StairWallS", new Vector3((x0 + x1) * 0.5f, -drop, -1.75f), true, run, 7f, A.towerWall, 3f);
            Deck(roomA, "StairCeil", new Vector3((x0 + x1) * 0.5f, 3.25f, 0), run, 4f, 0.5f, A.darkWood, 2, 1);
            Torch(roomA, "StairTorch", new Vector3(-12.25f, 0f, 1.48f), Vector3.back, 6f, 8f);

            // Oda çerçevesi: kule koordinatına göre (-18, -4, 0)
            var root = new GameObject("RoomA2_SaatOdasi").transform;
            root.SetParent(roomA, false);
            root.localPosition = new Vector3(-18f, -drop, 0);
            const float h = 4.5f;
            Deck(root, "Floor", new Vector3(-3.25f, -0.25f, 0), 10.5f, 10.5f, 0.5f, A.towerFloor, 2, 2);
            Deck(root, "Ceiling", new Vector3(-3.25f, h + 0.25f, 0), 10.5f, 10.5f, 0.5f, A.darkWood, 2, 2);
            Wall(root, "WallW", new Vector3(-8.25f, 0, 0), false, 10.5f, h, A.towerWall, 3f);
            Wall(root, "WallE", new Vector3(1.75f, 0, 0), false, 10.5f, h, A.towerWall, 3f, new Opening(0f, 3f, 0f, 3f));
            Slab("StairHeader", root, new Vector3(1.75f, 5.75f, 0), new Vector3(0.5f, 2.5f, 4f), A.towerWall, 2f, false);
            Wall(root, "WallN", new Vector3(-3.25f, 0, 5.25f), true, 10.5f, h, A.towerWall, 3f);
            Wall(root, "WallS", new Vector3(-3.25f, 0, -5.25f), true, 10.5f, h, A.towerWall, 3f, new Opening(2.75f, 3f, 0f, 3f)); // Hazne merdiveni
            foreach (float x in new[] { -6.5f, 0f })
                Slab("Beam", root, new Vector3(x, h - 0.25f, 0), new Vector3(0.4f, 0.5f, 10f), A.darkWood, 2f, false);

            // Saat mekanizması: kuzey duvarında dönen dişliler
            var gears = new GameObject("ClockGears").transform;
            gears.SetParent(root, false);
            gears.localPosition = new Vector3(-3.5f, 0, 4.88f);
            void Gear(string n, int teeth, float r, Vector2 p, float speed, Material mat)
            {
                var g = MeshObj(n, gears, new Vector3(p.x, p.y, 0), GearMesh(teeth, r, 0.16f, r * 0.22f, 0.16f), mat);
                Prim("Axle", g.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(r * 0.3f, 0.14f, r * 0.3f), A.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                AddSpin(g, speed);
            }
            Gear("GearA", 18, 1.4f, new Vector2(-1.2f, 2.3f), 6f, A.iron);
            Gear("GearB", 10, 0.85f, new Vector2(1.07f, 2.75f), -10.8f, A.bronze);
            Gear("GearC", 8, 0.62f, new Vector2(2.35f, 2.05f), 14f, A.iron);

            Torch(root, "TorchN", new Vector3(-6.5f, 2.8f, 4.98f), Vector3.back, 7f, 10f);
            Torch(root, "TorchS", new Vector3(-4.5f, 2.8f, -4.98f), Vector3.forward, 7f, 10f);
            Prop("WoodenTable_01", root, new Vector3(-5.6f, 0, -3.8f), 80f, 0.85f);
            Prop("wooden_crate_02", root, new Vector3(0.4f, 0, 4.0f), 25f, 0.8f);
            return root;
        }

        static void BuildPuzzle2(Transform roomA, Transform roomB)
        {
            var def = new GameObject("Puzzle2").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 2;
            dso.FindProperty("title").stringValue = "Bulmaca 2 (mahzen)";
            dso.FindProperty("startsActive").boolValue = false;
            var up = dso.FindProperty("unlocks"); up.arraySize = 1; up.GetArrayElementAtIndex(0).intValue = 3;
            dso.ApplyModifiedPropertiesWithoutUndo();

            // ---- Oda B: kasa odasının batısında geçit + karanlık mahzen (iç: x -15.25..-24.75, z -5..5) ----
            const float h = 3.2f;
            var wall = A.dungeonWall;
            Deck(roomB, "CryptCorridorFloor", new Vector3(-13.9f, -0.25f, 0), 2.3f, 3.5f, 0.5f, A.dungeonFloor, 1, 1);
            Deck(roomB, "CryptCorridorCeil", new Vector3(-13.9f, 3.25f, 0), 2.3f, 3.5f, 0.5f, A.dungeonCeil, 1, 1);
            Wall(roomB, "CryptCorridorN", new Vector3(-13.9f, 0, 1.75f), true, 2.3f, 3f, wall, 3f);
            Wall(roomB, "CryptCorridorS", new Vector3(-13.9f, 0, -1.75f), true, 2.3f, 3f, wall, 3f);
            Deck(roomB, "CryptFloor", new Vector3(-20f, -0.25f, 0), 10.5f, 10.5f, 0.5f, A.dungeonFloor, 2, 2);
            Deck(roomB, "CryptCeil", new Vector3(-20f, h + 0.25f, 0), 10.5f, 10.5f, 0.5f, A.dungeonCeil, 2, 2);
            var arch = new Opening(0f, 3f, 0f, 3f);
            Wall(roomB, "CryptWallE", new Vector3(-15f, 0, 0), false, 10.5f, h, wall, 3f, arch);
            Wall(roomB, "CryptWallW", new Vector3(-25f, 0, 0), false, 10.5f, h, wall, 3f, arch);
            Wall(roomB, "CryptWallN", new Vector3(-20f, 0, 5.25f), true, 10.5f, h, wall, 3f);
            Wall(roomB, "CryptWallS", new Vector3(-20f, 0, -5.25f), true, 10.5f, h, wall, 3f);
            // Batı nişi (bulmaca 3 geçidi olacak)
            Deck(roomB, "NicheFloor", new Vector3(-26.4f, -0.25f, 0), 2.3f, 3.5f, 0.5f, A.flagstone, 1, 1);
            Deck(roomB, "NicheCeil", new Vector3(-26.4f, 3.25f, 0), 2.3f, 3.5f, 0.5f, A.dungeonCeil, 1, 1);
            Wall(roomB, "NicheW", new Vector3(-27.5f, 0, 0), false, 3.5f, 3f, wall, 3f, new Opening(0f, 3f, 0f, 3f)); // sarnıç geçidi (bulmaca 3)
            Wall(roomB, "NicheN", new Vector3(-26.4f, 0, 1.75f), true, 2.3f, 3f, wall, 3f);
            Wall(roomB, "NicheS", new Vector3(-26.4f, 0, -1.75f), true, 2.3f, 3f, wall, 3f);

            var gate = MakeDoor("CryptGate", roomB, new Vector3(-15f, 1.5f, 0), false, A.iron);
            var rewardGate = MakeDoor("CryptRewardGate", roomB, new Vector3(-25f, 1.5f, 0), false, A.iron);

            // Lahitler: kuzeyde 3, güneyde 3. Kapakta sembol (orta) ve rakam (ayak ucu, koridor tarafı); ayak ucundan düz okunur.
            var syms = new Transform[6];
            var nums = new Transform[6];
            float[] xs = { -17.3f, -20f, -22.7f };
            for (int i = 0; i < 6; i++)
            {
                bool north = i < 3;
                float zc = north ? 3.9f : -3.9f, toward = north ? -1f : 1f;
                var c = new GameObject("Coffin" + i).transform;
                c.SetParent(roomB, false);
                c.localPosition = new Vector3(xs[i % 3], 0, zc);
                Slab("Body", c, new Vector3(0, 0.35f, 0), new Vector3(1.0f, 0.7f, 2.0f), A.flagstone, 1.2f);
                Slab("Lid", c, new Vector3(0, 0.76f, 0), new Vector3(1.15f, 0.12f, 2.15f), A.plaque, 1.2f, false);
                var rot = Quaternion.Euler(90, north ? 0 : 180, 0);
                syms[i] = new GameObject("Symbol").transform;
                syms[i].SetParent(c, false);
                syms[i].localPosition = new Vector3(0, 0.825f, -toward * 0.15f);
                syms[i].localRotation = rot;
                nums[i] = new GameObject("Numeral").transform;
                nums[i].SetParent(c, false);
                nums[i].localPosition = new Vector3(0, 0.825f, toward * 0.68f);
                nums[i].localRotation = rot;
            }

            // Mangallar: kaseler hep var; alev + ışık yalnızca çözülünce
            var lit = new GameObject("CryptLit").transform;
            lit.SetParent(roomB, false);
            foreach (var p in new[] { new Vector3(-24f, 0, 4.2f), new Vector3(-24f, 0, -4.2f) })
            {
                Slab("BrazierStand", roomB, p + new Vector3(0, 0.45f, 0), new Vector3(0.18f, 0.9f, 0.18f), A.iron, 1f, false);
                Prim("BrazierBowl", roomB, PrimitiveType.Cylinder, p + new Vector3(0, 0.95f, 0), new Vector3(0.6f, 0.1f, 0.6f), A.iron);
                var f = Prim("BrazierFlame", lit, PrimitiveType.Sphere, p + new Vector3(0, 1.2f, 0), new Vector3(0.38f, 0.5f, 0.38f), A.flame);
                f.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                PointLight(lit, "BrazierLight", p + new Vector3(0.6f, 1.8f, p.z > 0 ? -0.6f : 0.6f), TorchColor, 6f, 10f);
            }
            PointLight(lit, "NicheLight", new Vector3(-26f, 2.2f, 0), new Color(0.75f, 0.85f, 1f), 3f, 4f);
            lit.gameObject.SetActive(false);

            var respawn = new GameObject("CryptRespawn").transform;
            respawn.SetParent(roomB, false);
            respawn.localPosition = new Vector3(-11.6f, 0.05f, -2f);

            var crypt = new GameObject("Puzzle2_Crypt").AddComponent<CryptPuzzle>();
            crypt.transform.SetParent(roomB, false);
            crypt.transform.localPosition = new Vector3(-20f, 0, 0);

            // ---- Oda A2: Saat Odası (kulenin altında; bulmaca 1 çözülünce merdiven kapısı açılır) ----
            var clock = BuildClockRoom(roomA);
            Slab("InscriptionBoard", clock, new Vector3(-7.92f, 3.55f, 0), new Vector3(0.1f, 0.9f, 5.6f), A.plaque, 1.5f, false);
            Slab("InscriptionFrame", clock, new Vector3(-7.96f, 3.55f, 0), new Vector3(0.06f, 1.04f, 5.76f), A.iron, 1f, false);
            var ins = new Transform[3];
            var dials = new SymbolDial[3];
            for (int k = 0; k < 3; k++)
            {
                float z = -1.9f + k * 1.9f;
                ins[k] = new GameObject("Inscription" + k).transform;
                ins[k].SetParent(clock, false);
                ins[k].localPosition = new Vector3(-7.86f, 3.55f, z);
                ins[k].localRotation = Quaternion.Euler(0, -90, 0);
                dials[k] = MakeDial(clock, k, new Vector3(-7.88f, 2.0f, z), crypt);
            }
            Slab("CandleShelf", clock, new Vector3(-7.75f, 0.95f, 0), new Vector3(0.5f, 0.08f, 1.6f), A.darkWood, 1f, false);
            var wax = MakeMat("CandleWax", new Color(0.86f, 0.8f, 0.66f));
            var flames = new GameObject[3];
            for (int i = 0; i < 3; i++) Candle(clock, new Vector3(-7.72f, 0.99f, -0.5f + i * 0.5f), wax, out flames[i]);
            var confirm = MakeConfirmLever(clock, new Vector3(-6.2f, 0, 0), -90f, 2, 18);

            var cso = new SerializedObject(crypt);
            cso.FindProperty("puzzleId").intValue = 2;
            cso.FindProperty("nodeId").intValue = 10;
            SetRefArray(cso, "dials", dials);
            SetRef(cso, "confirm", confirm);
            cso.FindProperty("zoneCenter").vector3Value = roomB.position + new Vector3(-20.2f, 1.5f, 0);
            cso.FindProperty("zoneSize").vector3Value = new Vector3(8.8f, 3f, 10f);
            SetRef(cso, "respawn", respawn);
            cso.ApplyModifiedPropertiesWithoutUndo();

            var vis = crypt.gameObject.AddComponent<CryptVisuals>();
            var vso = new SerializedObject(vis);
            SetRef(vso, "crypt", crypt);
            SetRefArray(vso, "coffinSymbols", syms);
            SetRefArray(vso, "coffinNumerals", nums);
            SetRefArray(vso, "inscription", ins);
            SetRef(vso, "carveMat", A.brass);
            SetRef(vso, "inscriptionMat", A.gold);
            SetRefArray(vso, "candleFlames", flames);
            SetRef(vso, "gate", gate);
            SetRef(vso, "rewardGate", rewardGate);
            SetRefArray(vso, "solvedOn", new Object[] { lit.gameObject });
            vso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Bulmaca 3: sarnıç (B vanaları çevirir, A seviyeyi ve tabloyu görür) ----------

        /// <summary>Düz merdiven: holder üst kenarda, yerel +Z boyunca iner (run, drop), genişlik 3. Çarpışma rampa, basamaklar görsel.</summary>
        static Transform Stair(Transform parent, string name, Vector3 top, float yaw, float run, float drop, Material wallMat)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = top;
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            float ang = Mathf.Atan2(drop, run);
            var ramp = Slab("Ramp", root, Vector3.zero, new Vector3(3f, 0.3f, Mathf.Sqrt(run * run + drop * drop) + 0.3f), A.flagstone, 1f);
            ramp.GetComponent<MeshRenderer>().enabled = false;
            ramp.transform.localPosition = new Vector3(0, -drop * 0.5f - 0.15f * Mathf.Cos(ang), run * 0.5f - 0.15f * Mathf.Sin(ang));
            ramp.transform.localRotation = Quaternion.Euler(ang * Mathf.Rad2Deg, 0, 0);
            int steps = Mathf.Max(4, Mathf.RoundToInt(drop / 0.25f));
            float sr = run / steps, sh = drop / steps;
            for (int i = 0; i < steps; i++)
                Slab("Step" + i, root, new Vector3(0, -sh * i - 0.25f, sr * (i + 0.5f)), new Vector3(3f, 0.5f, sr), A.flagstone, 1f, false);
            Wall(root, "WallL", new Vector3(-1.75f, -drop, run * 0.5f), false, run, drop + 3f, wallMat, 3f);
            Wall(root, "WallR", new Vector3(1.75f, -drop, run * 0.5f), false, run, drop + 3f, wallMat, 3f);
            Deck(root, "Ceil", new Vector3(0, 3.25f, run * 0.5f), 4f, run, 0.5f, A.darkWood, 1, 2);
            Torch(root, "Torch", new Vector3(1.48f, 1.2f - drop * 0.5f, run * 0.5f), Vector3.left, 6f, 8f);
            return root;
        }

        static TextMesh WorldText(Transform parent, string text, Vector3 pos, Quaternion rot, float charSize, Color color)
        {
            var go = new GameObject("Text_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var tm = go.AddComponent<TextMesh>();
            tm.font = U.head;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = U.head.material;
            return tm;
        }

        static CisternValve MakeValve(Transform parent, int index, Vector3 pos, CisternPuzzle cistern)
        {
            var root = new GameObject("Valve" + index).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, -90, 0); // yüz (-Z) doğuya
            Slab("Plate", root, new Vector3(0, 1.3f, 0.05f), new Vector3(0.95f, 0.95f, 0.1f), A.iron, 1f).isStatic = false;
            var pipeMat = MakeMat("ValvePipe" + index, Color.gray);
            var pipes = new[]
            {
                Prim("PipeUp", root, PrimitiveType.Cylinder, new Vector3(0, 2.75f, -0.08f), new Vector3(0.17f, 1.25f, 0.17f), pipeMat).GetComponent<Renderer>(),
                Prim("PipeDown", root, PrimitiveType.Cylinder, new Vector3(0, 0.55f, -0.08f), new Vector3(0.17f, 0.55f, 0.17f), pipeMat).GetComponent<Renderer>(),
            };
            var wheel = MeshObj("Wheel", root, new Vector3(0, 1.3f, -0.2f), GearMesh(10, 0.4f, 0.05f, 0.3f, 0.06f), A.bronze).transform;
            Slab("SpokeA", wheel, Vector3.zero, new Vector3(0.66f, 0.06f, 0.05f), A.bronze, 1f, false).isStatic = false;
            Slab("SpokeB", wheel, Vector3.zero, new Vector3(0.06f, 0.66f, 0.05f), A.bronze, 1f, false).isStatic = false;
            Prim("Hub", wheel, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.14f, A.brass);
            Slab("SymbolPlate", root, new Vector3(0.78f, 2.2f, 0.02f), new Vector3(0.62f, 0.62f, 0.06f), A.plaque, 1f, false);
            var anchor = new GameObject("Symbol").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0.78f, 2.2f, -0.02f);

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
            SetRefArray(so, "pipe", pipes);
            SetRef(so, "symbolMat", A.gold);
            so.ApplyModifiedPropertiesWithoutUndo();
            return v;
        }

        static void BuildPuzzle3(Transform roomA, Transform roomB)
        {
            var def = new GameObject("Puzzle3").AddComponent<PuzzleDef>();
            var dso = new SerializedObject(def);
            dso.FindProperty("id").intValue = 3;
            dso.FindProperty("title").stringValue = "Bulmaca 3 (sarnıç)";
            dso.FindProperty("startsActive").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();

            var wall = A.dungeonWall;
            // ---- Oda B: mahzen nişinin batısında geçit + sarnıç (iç: x -30.25..-41.75, z -6..6) ----
            Deck(roomB, "CisternCorridorFloor", new Vector3(-28.9f, -0.25f, 0), 2.3f, 3.5f, 0.5f, A.dungeonFloor, 1, 1);
            Deck(roomB, "CisternCorridorCeil", new Vector3(-28.9f, 3.25f, 0), 2.3f, 3.5f, 0.5f, A.dungeonCeil, 1, 1);
            Wall(roomB, "CisternCorridorN", new Vector3(-28.9f, 0, 1.75f), true, 2.3f, 3f, wall, 3f);
            Wall(roomB, "CisternCorridorS", new Vector3(-28.9f, 0, -1.75f), true, 2.3f, 3f, wall, 3f);
            var gate = MakeDoor("CisternGate", roomB, new Vector3(-28.9f, 1.5f, 0), false, A.iron);
            const float h = 4f;
            Deck(roomB, "CisternFloor", new Vector3(-36f, -0.25f, 0), 12.5f, 12.5f, 0.5f, A.dungeonFloor, 3, 3);
            Deck(roomB, "CisternCeil", new Vector3(-36f, h + 0.25f, 0), 12.5f, 12.5f, 0.5f, A.dungeonCeil, 3, 3);
            var arch = new Opening(0f, 3f, 0f, 3f);
            Wall(roomB, "CisternWallE", new Vector3(-30f, 0, 0), false, 12.5f, h, wall, 3f, arch);
            Wall(roomB, "CisternWallW", new Vector3(-42f, 0, 0), false, 12.5f, h, wall, 3f);
            Wall(roomB, "CisternWallN", new Vector3(-36f, 0, 6.25f), true, 12.5f, h, wall, 3f);
            Wall(roomB, "CisternWallS", new Vector3(-36f, 0, -6.25f), true, 12.5f, h, wall, 3f, arch);
            // Güney nişi: bulmaca 4 geçidi (şimdilik kapalı oda)
            Deck(roomB, "CisternNicheFloor", new Vector3(-36f, -0.25f, -7.4f), 3.5f, 2.3f, 0.5f, A.flagstone, 1, 1);
            Deck(roomB, "CisternNicheCeil", new Vector3(-36f, 3.25f, -7.4f), 3.5f, 2.3f, 0.5f, A.dungeonCeil, 1, 1);
            Wall(roomB, "CisternNicheS", new Vector3(-36f, 0, -8.5f), true, 3.5f, 3f, wall, 3f);
            Wall(roomB, "CisternNicheE", new Vector3(-34.25f, 0, -7.4f), false, 2.3f, 3f, wall, 3f);
            Wall(roomB, "CisternNicheW", new Vector3(-37.75f, 0, -7.4f), false, 2.3f, 3f, wall, 3f);
            var nextGate = MakeDoor("CisternNextGate", roomB, new Vector3(-36f, 1.5f, -6.25f), true, A.iron);
            foreach (float x in new[] { -33.5f, -38.5f })
                foreach (float z in new[] { -3f, 3f })
                {
                    Slab("CisternPillar", roomB, new Vector3(x, h * 0.5f, z), new Vector3(0.7f, h, 0.7f), A.flagstone, 1.6f);
                    Slab("CisternPillarCap", roomB, new Vector3(x, h - 0.2f, z), new Vector3(1f, 0.4f, 1f), A.flagstone, 1.6f, false);
                }
            // Tavandan inen büyük besleme borusu (suyun geldiği yer)
            Prim("InflowPipe", roomB, PrimitiveType.Cylinder, new Vector3(-36f, 3.2f, 5.6f), new Vector3(0.5f, 0.8f, 0.5f), A.iron);
            var water = Slab("CisternWater", roomB, new Vector3(-36f, 0.02f, 0), new Vector3(11.5f, 0.04f, 12f), A.water, 1f, false);
            water.isStatic = false;
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Torch(roomB, "CisternTorchW1", new Vector3(-41.73f, 2.9f, -3f), Vector3.right, 8f, 10f);
            Torch(roomB, "CisternTorchW2", new Vector3(-41.73f, 2.9f, 3f), Vector3.right, 8f, 10f);
            Torch(roomB, "CisternTorchN", new Vector3(-33f, 2.9f, 5.98f), Vector3.back, 7f, 10f);
            Torch(roomB, "CisternTorchS", new Vector3(-39f, 2.9f, -5.98f), Vector3.forward, 7f, 10f);

            var respawn = new GameObject("CisternRespawn").transform;
            respawn.SetParent(roomB, false);
            respawn.localPosition = new Vector3(-22f, 0.05f, 0);

            var cistern = new GameObject("Puzzle3_Cistern").AddComponent<CisternPuzzle>();
            cistern.transform.SetParent(roomB, false);
            cistern.transform.localPosition = new Vector3(-36f, 0, 0);
            var valves = new CisternValve[4];
            for (int i = 0; i < 4; i++) valves[i] = MakeValve(roomB, i, new Vector3(-41.65f, 0, -4.5f + i * 3f), cistern);

            // ---- Oda A3: Hazne Odası (Saat Odası'nın güneyinden merdivenle; bulmaca 2 çözülünce kapı açılır) ----
            var clock = roomA.Find("RoomA2_SaatOdasi");
            var door = MakeDoor("HazneDoor", clock, new Vector3(-0.5f, 1.5f, -5.25f), true, A.wood);
            var pd = door.gameObject.AddComponent<PuzzleDoor>();
            var pso = new SerializedObject(pd);
            pso.FindProperty("puzzleId").intValue = 2;
            SetRef(pso, "door", door);
            pso.ApplyModifiedPropertiesWithoutUndo();
            Stair(clock, "HazneStair", new Vector3(-0.5f, 0, -5.5f), 180f, 6.5f, 3f, A.towerWall);

            var r = new GameObject("RoomA3_Hazne").transform;
            r.SetParent(clock, false);
            r.localPosition = new Vector3(-0.5f, -3f, -17f);
            const float ha = 5f;
            Deck(r, "Floor", new Vector3(0, -0.25f, 0), 10.5f, 10.5f, 0.5f, A.flagstone, 2, 2);
            Deck(r, "Ceiling", new Vector3(0, ha + 0.25f, 0), 10.5f, 10.5f, 0.5f, A.darkWood, 2, 2);
            Wall(r, "WallN", new Vector3(0, 0, 5.25f), true, 10.5f, ha, A.towerWall, 3f, arch);
            Slab("StairHeader", r, new Vector3(0, 5.75f, 5.25f), new Vector3(4f, 1.5f, 0.5f), A.towerWall, 2f, false);
            Wall(r, "WallS", new Vector3(0, 0, -5.25f), true, 10.5f, ha, A.towerWall, 3f);
            Wall(r, "WallE", new Vector3(5.25f, 0, 0), false, 10.5f, ha, A.towerWall, 3f);
            Wall(r, "WallW", new Vector3(-5.25f, 0, 0), false, 10.5f, ha, A.towerWall, 3f);
            Torch(r, "TorchE1", new Vector3(4.98f, 3f, -2f), Vector3.left, 8f, 11f);
            Torch(r, "TorchE2", new Vector3(4.98f, 3f, 2.5f), Vector3.left, 8f, 11f);
            Torch(r, "TorchW", new Vector3(-4.98f, 4.3f, -3.2f), Vector3.right, 6f, 9f);

            // Gösterge (güney duvarı): seviye sütunu, çentikler, boğulma bandı, hedef işareti
            const float unit = 0.38f, baseY = 0.4f;
            Slab("GaugeBase", r, new Vector3(0, 0.2f, -4.3f), new Vector3(1.3f, 0.4f, 1.3f), A.flagstone, 1.2f);
            Slab("GaugeBoard", r, new Vector3(0, 2.55f, -4.9f), new Vector3(1.6f, 4.4f, 0.1f), A.plaque, 1.5f, false);
            foreach (float x in new[] { -0.42f, 0.42f })
                foreach (float z in new[] { -4.72f, -3.88f })
                    Slab("GaugePost", r, new Vector3(x, baseY + 2.0f, z), new Vector3(0.06f, 4.0f, 0.06f), A.brass, 1f, false);
            Slab("GaugeCap", r, new Vector3(0, baseY + 4.05f, -4.3f), new Vector3(1.0f, 0.1f, 1.0f), A.brass, 1f, false);
            for (int i = 0; i <= 10; i++)
                Slab("Notch" + i, r, new Vector3(0, baseY + i * unit, -4.84f), new Vector3(i % 2 == 0 ? 1.3f : 0.9f, 0.035f, 0.03f), A.iron, 1f, false);
            Slab("DrownBand", r, new Vector3(0, baseY + 9f * unit, -4.83f), new Vector3(1.45f, 2f * unit, 0.02f),
                 GlowMat("DrownBand", new Color(0.6f, 0.08f, 0.06f), new Color(0.7f, 0.06f, 0.04f) * 0.9f), 1f, false);
            var column = new GameObject("GaugeColumn").transform;
            column.SetParent(r, false);
            column.localPosition = new Vector3(0, baseY, -4.3f);
            Prim("Water", column, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0.5f, 0.6f),
                 GlowMat("GaugeWater", new Color(0.1f, 0.35f, 0.5f), new Color(0.05f, 0.3f, 0.45f) * 0.9f));
            column.localScale = new Vector3(1f, 0.001f, 1f);
            var target = new GameObject("GaugeTarget").transform;
            target.SetParent(r, false);
            target.localPosition = new Vector3(0, baseY, -4.3f);
            foreach (float x in new[] { -0.62f, 0.62f })
                Slab("Mark", target, new Vector3(x, 0, 0), new Vector3(0.36f, 0.07f, 0.36f), A.gold, 1f, false).isStatic = false;

            // Tablo (batı duvarı): 4 satır sembol → miktar, altta ters akan boru rengi
            Slab("ChartBoard", r, new Vector3(-4.93f, 2.3f, 0), new Vector3(0.1f, 3.6f, 3.2f), A.plaque, 1.5f, false);
            Slab("ChartFrame", r, new Vector3(-4.97f, 2.3f, 0), new Vector3(0.06f, 3.76f, 3.36f), A.iron, 1f, false);
            var face = Quaternion.Euler(0, -90, 0);
            var syms = new Transform[4];
            var amts = new Transform[4];
            for (int row = 0; row < 4; row++)
            {
                float y = 3.65f - row * 0.72f;
                syms[row] = new GameObject("ChartSymbol" + row).transform;
                syms[row].SetParent(r, false);
                syms[row].localPosition = new Vector3(-4.87f, y, -0.8f);
                syms[row].localRotation = face;
                amts[row] = new GameObject("ChartAmount" + row).transform;
                amts[row].SetParent(r, false);
                amts[row].localPosition = new Vector3(-4.87f, y, 0.55f);
                amts[row].localRotation = face;
            }
            Slab("ChartRule", r, new Vector3(-4.87f, 1.06f, 0), new Vector3(0.02f, 0.03f, 2.6f), A.iron, 1f, false);
            var swatch = Slab("ReversedSwatch", r, new Vector3(-4.86f, 0.75f, -0.9f), new Vector3(0.04f, 0.32f, 0.7f),
                              MakeMat("ReversedSwatch", Color.gray), 1f, false);
            swatch.isStatic = false;
            WorldText(r, "TERS AKAR", new Vector3(-4.86f, 0.75f, 0.45f), face, 0.022f, GoldBright);
            Prop("wine_barrel_01", r, new Vector3(3.9f, 0, 3.9f), 30f, 1.15f);
            Prop("wine_barrel_01", r, new Vector3(3.8f, 0, -3.9f), 120f, 1.15f);

            var seal = MakeConfirmLever(r, new Vector3(2.4f, 0, -3.4f), 180f, 3, 31, "Sarnıcı mühürle");

            var cso = new SerializedObject(cistern);
            cso.FindProperty("puzzleId").intValue = 3;
            cso.FindProperty("nodeId").intValue = 20;
            SetRefArray(cso, "valves", valves);
            SetRef(cso, "seal", seal);
            cso.FindProperty("zoneCenter").vector3Value = roomB.position + new Vector3(-36f, 1.5f, 0);
            cso.FindProperty("zoneSize").vector3Value = new Vector3(11f, 3f, 12f);
            SetRef(cso, "respawn", respawn);
            cso.ApplyModifiedPropertiesWithoutUndo();

            var vis = cistern.gameObject.AddComponent<CisternVisuals>();
            var vso = new SerializedObject(vis);
            SetRef(vso, "cistern", cistern);
            SetRef(vso, "water", water.transform);
            SetRef(vso, "gaugeColumn", column);
            SetRef(vso, "gaugeTarget", target);
            vso.FindProperty("gaugeUnit").floatValue = unit;
            SetRefArray(vso, "chartSymbols", syms);
            SetRefArray(vso, "chartAmounts", amts);
            SetRef(vso, "reversedSwatch", swatch.GetComponent<Renderer>());
            SetRef(vso, "chartMat", A.gold);
            SetRef(vso, "gate", gate);
            SetRef(vso, "nextGate", nextGate);
            vso.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSpin(GameObject go, float degPerSec)
        {
            var s = go.AddComponent<Spin>();
            var so = new SerializedObject(s);
            so.FindProperty("degreesPerSecond").floatValue = degPerSec;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Oda A: çan kulesi tepesi ----------

        static Transform BuildTower(Vector3 center)
        {
            var root = new GameObject("RoomA_Mekanizma").transform;
            root.position = center;
            const float h = 7f;
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), 17, 17, 0.5f, A.towerFloor, 3, 3);
            Deck(root, "Ceiling", new Vector3(0, h + 0.25f, 0), 17, 17, 0.5f, A.darkWood, 3, 3);

            // Kuzey duvarı dolu (kollar), diğer üç duvarda ikişer pencere: gece göğü ve ay ışığı
            var win = new[] { new Opening(-4f, 2.2f, 1.1f, 4.3f), new Opening(4f, 2.2f, 1.1f, 4.3f) };
            Wall(root, "WallN", new Vector3(0, 0, 8.25f), true, 17, h, A.towerWall, 3f);
            Wall(root, "WallS", new Vector3(0, 0, -8.25f), true, 17, h, A.towerWall, 3f, win);
            Wall(root, "WallE", new Vector3(8.25f, 0, 0), false, 17, h, A.towerWall, 3f, win);
            Wall(root, "WallW", new Vector3(-8.25f, 0, 0), false, 17, h, A.towerWall, 3f, win[0], win[1], new Opening(0f, 3f, 0f, 3f)); // Saat Odası merdiveni
            foreach (float c in new[] { -4f, 4f })
            {
                Slab("SillS", root, new Vector3(c, 1.16f, -8.25f), new Vector3(2.5f, 0.14f, 0.8f), A.flagstone, 1.5f, false);
                Slab("SillE", root, new Vector3(8.25f, 1.16f, c), new Vector3(0.8f, 0.14f, 2.5f), A.flagstone, 1.5f, false);
                Slab("SillW", root, new Vector3(-8.25f, 1.16f, c), new Vector3(0.8f, 0.14f, 2.5f), A.flagstone, 1.5f, false);
            }

            // Çatı kirişleri
            foreach (float z in new[] { -5.5f, 5.5f }) Slab("BeamX", root, new Vector3(0, h - 0.22f, z), new Vector3(16f, 0.44f, 0.44f), A.darkWood, 2f, false);
            foreach (float x in new[] { -5.5f, 5.5f }) Slab("BeamZ", root, new Vector3(x, h - 0.62f, 0), new Vector3(0.4f, 0.4f, 16f), A.darkWood, 2f, false);
            Slab("BellBeam", root, new Vector3(0, h - 0.7f, 0), new Vector3(16f, 0.6f, 0.6f), A.darkWood, 2f, false);
            foreach (float x in new[] { -7.6f, 7.6f })
                foreach (float z in new[] { -7.6f, 7.6f })
                    Slab("Post", root, new Vector3(x, h * 0.5f, z), new Vector3(0.45f, h, 0.45f), A.darkWood, 2f, false);

            // Çan: kirişten asılı, hafifçe salınır
            var bell = new GameObject("Bell").transform;
            bell.SetParent(root, false);
            bell.localPosition = new Vector3(0, h - 1.0f, 0);
            bell.gameObject.AddComponent<Sway>();
            Slab("Yoke", bell, new Vector3(0, -0.2f, 0), new Vector3(1.9f, 0.45f, 0.5f), A.wood, 1.5f, false).isStatic = false;
            foreach (float x in new[] { -0.7f, 0.7f }) Slab("Strap", bell, new Vector3(x, 0.05f, 0), new Vector3(0.1f, 0.9f, 0.56f), A.iron, 1f, false).isStatic = false;
            var prof = new[]
            {
                new Vector2(1.02f, 0f), new Vector2(1.1f, 0.06f), new Vector2(0.98f, 0.2f), new Vector2(0.8f, 0.45f), new Vector2(0.66f, 0.8f),
                new Vector2(0.58f, 1.15f), new Vector2(0.52f, 1.45f), new Vector2(0.42f, 1.65f), new Vector2(0.25f, 1.76f), new Vector2(0f, 1.8f),
            };
            MeshObj("BellBody", bell, new Vector3(0, -2.25f, 0), LatheMesh("bell", prof, 28), A.bronze);
            Prim("Crown", bell, PrimitiveType.Cylinder, new Vector3(0, -0.42f, 0), new Vector3(0.3f, 0.1f, 0.3f), A.bronze);
            Slab("ClapperRod", bell, new Vector3(0, -1.5f, 0), new Vector3(0.05f, 1.4f, 0.05f), A.iron, 1f, false).isStatic = false;
            Prim("Clapper", bell, PrimitiveType.Sphere, new Vector3(0, -2.2f, 0), Vector3.one * 0.3f, A.iron);

            // Kuzey duvarında dönen dişliler (kolların üstünde)
            var gears = new GameObject("Gears").transform;
            gears.SetParent(root, false);
            gears.localPosition = new Vector3(0, 0, 7.88f);
            void Gear(string n, int teeth, float r, Vector2 p, float speed, Material mat = null)
            {
                var g = MeshObj(n, gears, new Vector3(p.x, p.y, 0), GearMesh(teeth, r, 0.16f, r * 0.22f, 0.16f), mat != null ? mat : A.iron);
                Prim("Axle", g.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(r * 0.3f, 0.14f, r * 0.3f), A.brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
                AddSpin(g, speed);
            }
            Gear("GearBig", 16, 1.3f, new Vector2(-1.4f, 5.0f), 8f);
            Gear("GearMid", 10, 0.85f, new Vector2(0.56f, 5.35f), -12.8f, A.bronze);
            Gear("GearSmall", 8, 0.69f, new Vector2(1.81f, 4.77f), 16f);

            // Işık: kolların arasında iki meşale, güney duvarında bir meşale, masada fener
            Torch(root, "TorchN1", new Vector3(-3f, 2.7f, 7.98f), Vector3.back, 9f, 12f);
            Torch(root, "TorchN2", new Vector3(3f, 2.7f, 7.98f), Vector3.back, 9f, 12f);
            Torch(root, "TorchS", new Vector3(0f, 2.7f, -7.98f), Vector3.forward, 8f, 12f);


            // Eşyalar
            Prop("WoodenTable_01", root, new Vector3(5.6f, 0, -5.8f), 20f, 0.85f);
            var lantern = Prop("wooden_lantern_01", root, new Vector3(5.5f, 0.85f, -5.8f), 35f, 0.5f, false);
            PointLight(lantern.transform, "LanternLight", new Vector3(0, 0.3f, 0), TorchColor, 3.5f, 6f);
            Prop("wine_barrel_01", root, new Vector3(6.7f, 0, 5.4f), 10f, 1.15f);
            Prop("wine_barrel_01", root, new Vector3(6.6f, 0, 6.7f), 130f, 1.15f);
            Prop("wooden_crate_02", root, new Vector3(-6.7f, 0, 6.4f), 15f, 0.8f);
            Prop("wooden_crate_02", root, new Vector3(-6.6f, 0.8f, 6.5f), 50f, 0.6f, false);
            return root;
        }

        // ---------- Oda B: zindan / sarnıç ----------

        static Transform BuildDungeon(Vector3 center)
        {
            var root = new GameObject("RoomB_Sarnic").transform;
            root.position = center;
            const float h = 4f;
            var wall = A.dungeonWall;
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), 17, 17, 0.5f, A.dungeonFloor, 3, 3);
            Deck(root, "Ceiling", new Vector3(0, h + 0.25f, 0), 17, 17, 0.5f, A.dungeonCeil, 3, 3);
            var doorway = new Opening(0f, 3f, 0f, 3f);
            Wall(root, "WallS", new Vector3(0, 0, -8.25f), true, 17, h, wall, 3f);
            Wall(root, "WallE", new Vector3(8.25f, 0, 0), false, 17, h, wall, 3f);
            Wall(root, "WallN", new Vector3(0, 0, 8.25f), true, 17, h, wall, 3f);
            Wall(root, "WallW", new Vector3(-8.25f, 0, 0), false, 17, h, wall, 3f, doorway); // kasa kapısı (PuzzleDoor)


            // Batıda kasa odası
            Deck(root, "VaultFloor", new Vector3(-10.625f, -0.25f, 0), 4.25f, 7f, 0.5f, A.flagstone, 1, 1);
            Deck(root, "VaultCeiling", new Vector3(-10.625f, h + 0.25f, 0), 4.25f, 7f, 0.5f, A.dungeonCeil, 1, 1);
            Wall(root, "VaultWallW", new Vector3(-12.75f, 0, 0), false, 7f, h, wall, 3f, new Opening(0f, 3f, 0f, 3f)); // mahzen geçidi (bulmaca 2)
            Wall(root, "VaultWallS", new Vector3(-10.625f, 0, -3.25f), true, 4.25f, h, wall, 3f);
            Wall(root, "VaultWallN", new Vector3(-10.625f, 0, 3.25f), true, 4.25f, h, wall, 3f);

            // Sütunlar
            foreach (float x in new[] { -4.2f, 4.2f })
                foreach (float z in new[] { -4.2f, 4.2f })
                {
                    Slab("Pillar", root, new Vector3(x, h * 0.5f, z), new Vector3(0.8f, h, 0.8f), A.flagstone, 1.6f);
                    Slab("PillarBase", root, new Vector3(x, 0.2f, z), new Vector3(1.15f, 0.4f, 1.15f), A.flagstone, 1.6f, false);
                    Slab("PillarCap", root, new Vector3(x, h - 0.2f, z), new Vector3(1.15f, 0.4f, 1.15f), A.flagstone, 1.6f, false);
                }

            // Meşaleler: freskin iki yanı (doğu), kasa kapısının yanı (batı), güney duvarı. Ortada soğuk dolgu ışığı (sarnıç havası).
            Torch(root, "TorchE1", new Vector3(7.98f, 2.5f, 5.6f), Vector3.left, 9f, 10f);
            Torch(root, "TorchE2", new Vector3(7.98f, 2.5f, -5.6f), Vector3.left, 9f, 10f);
            Torch(root, "TorchW", new Vector3(-7.98f, 2.5f, 3.2f), Vector3.right, 8f, 8f); // menzil: mahzene ulaşmasın
            Torch(root, "TorchS", new Vector3(-2.5f, 2.5f, -7.98f), Vector3.forward, 8f, 10f);
            PointLight(root, "RoomLight", new Vector3(0, 3.4f, 0), new Color(0.35f, 0.6f, 0.75f), 3.5f, 14f);


            // Eşyalar
            Prop("wine_barrel_01", root, new Vector3(-6.9f, 0, -6.8f), 0f, 1.15f);
            Prop("wine_barrel_01", root, new Vector3(-5.6f, 0, -7.0f), 80f, 1.15f);
            Prop("wine_barrel_01", root, new Vector3(-7.0f, 0, -5.5f), 200f, 1.15f);
            Prop("wooden_crate_02", root, new Vector3(6.8f, 0, 7.0f), 10f, 0.85f);
            Prop("wooden_crate_02", root, new Vector3(5.7f, 0, 7.1f), 70f, 0.7f);
            return root;
        }

        static void BuildWorld()
        {
            var roomA = BuildTower(Layout.RoomA);
            var roomB = BuildDungeon(Layout.RoomB);
            new GameObject("PuzzleChime").AddComponent<PuzzleChime>();
            BuildPuzzle1(roomA, roomB);
            BuildPuzzle2(roomA, roomB);
            BuildPuzzle3(roomA, roomB);
        }

        // ---------- Menü arka planı: şato kapısı, gece ----------

        static void BuildLobbyBackdrop()
        {
            var root = new GameObject("Backdrop").transform;
            Deck(root, "Ground", new Vector3(0, -0.25f, 0), 36, 24, 0.5f, A.dungeonFloor, 4, 3);
            // Sur: ortada kapı açıklığı, üstünde mazgallar
            Wall(root, "Curtain", new Vector3(0, 0, 6.25f), true, 36, 7f, A.towerWall, 3f, new Opening(0, 3.6f, 0, 4.4f));
            for (int i = -11; i <= 11; i++)
                if (i % 2 != 0) Slab("Merlon", root, new Vector3(i * 0.8f * 2f, 7.5f, 6.25f), new Vector3(1.6f, 1.0f, 0.5f), A.towerWall, 3f, false);
            foreach (float x in new[] { -9.5f, 9.5f })
            {
                Slab("Tower", root, new Vector3(x, 6f, 7.5f), new Vector3(5f, 12f, 5f), A.towerWall, 3f, false);
                for (int k = -1; k <= 1; k++) Slab("TowerMerlon", root, new Vector3(x + k * 1.9f, 12.5f, 5.25f), new Vector3(1.1f, 1.0f, 0.5f), A.towerWall, 3f, false);
                Slab("TowerWindow", root, new Vector3(x, 8.5f, 4.98f), new Vector3(0.7f, 1.5f, 0.06f), A.lanternGlass, 1f, false);
            }
            Slab("GateBack", root, new Vector3(0, 2.2f, 6.6f), new Vector3(3.8f, 4.6f, 0.2f), A.darkWood, 2f, false);
            Prop("large_castle_door", root, new Vector3(0, 0, 6.15f), 0f, 4.4f, false);
            Torch(root, "TorchL", new Vector3(-2.7f, 2.9f, 5.98f), Vector3.back, 11f, 12f);
            Torch(root, "TorchR", new Vector3(2.7f, 2.9f, 5.98f), Vector3.back, 11f, 12f);
            Prop("wine_barrel_01", root, new Vector3(-4.6f, 0, 5.2f), 20f, 1.15f, false);
            Prop("wine_barrel_01", root, new Vector3(-5.7f, 0, 5.4f), 100f, 1.15f, false);
            Prop("wooden_crate_02", root, new Vector3(4.9f, 0, 5.2f), 15f, 0.9f, false);
        }
    }
}
