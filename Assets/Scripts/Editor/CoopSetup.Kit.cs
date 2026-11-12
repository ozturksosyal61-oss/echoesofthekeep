using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Art kit (Assets/Art/Kit): ortak malzeme kütüphanesi (K), üretilen dokular, dekor prefabları (Kit("ad", ...)),
    /// tek ışık profili (L_* prefabları + KitLighting.lighting) ve post-process profili (KitPostFx.asset).
    /// Yeni odalar yalnızca bu kitten kurulur; palet ve değerler Docs/StyleGuide.md ile aynıdır.
    /// </summary>
    public static partial class CoopSetup
    {
        const string KitDir = "Assets/Art/Kit";
        const string KitMatDir = KitDir + "/Materials";
        const string KitPrefabDir = KitDir + "/Prefabs";
        const string KitTexDir = KitDir + "/Textures";
        const string KitLightDir = KitDir + "/Lighting";
        const string KitShowroomPath = KitDir + "/KitShowroom.unity";
        const string ManualRoot = "Assets/ThirdParty/Manual/";
        const string AcgRoot = "Assets/ThirdParty/ambientCG/";

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        /// <summary>Docs/StyleGuide.md paleti.</summary>
        static class Pal
        {
            public static readonly Color Candle = Hex("#FFB066"), Lantern = Hex("#FFA24A"), Moon = Hex("#8CA8E6");
            public static readonly Color AmbCastle = Hex("#141A26"), AmbUnder = Hex("#1A120C");
            public static readonly Color FogCastle = Hex("#1B2233"), FogUnder = Hex("#1A120C"), FogCistern = Hex("#0F1E22");
            public static readonly Color Bordo = Hex("#6E1F24"), Gold = Hex("#B8923F"), Purple = Hex("#4A3550"), Parchment = Hex("#CDBE9A");
            public static readonly Color[] Puzzle = { Hex("#A8322B"), Hex("#3F7D45"), Hex("#2F5F9E"), Hex("#C9A227") };
            public static readonly Color[] Vitray = { Hex("#7A3B3B"), Hex("#3E6A5A"), Hex("#3B4E7A"), Hex("#A8893A") };
        }

        class KitMats
        {
            public Material panel, plaster, beam, parquet, furniture, castleStone, hallFloor, cellarBrick, cellarFloor, cisternWall, cisternFloor,
                            velvet, velvetPurple, carpet, leather, leatherRed, leatherGreen, books, iron, brass, bronze, wax, flame, lanternGlass,
                            paperA, paperB, paperC, paintingA, paintingB, flameCard, clearGlass, bottleGlass, cobweb, shaft, vitray, rose, banner, rug;
        }

        static KitMats K;
        static readonly Dictionary<string, Material> kitPropMats = new Dictionary<string, Material>();
        static readonly Dictionary<string, GameObject> kitPrefabs = new Dictionary<string, GameObject>();
        static StringBuilder kitDump;

        [MenuItem("CoopGame/Build Art Kit")]
        public static void BuildKitMenu()
        {
            SetupUrp();
            PrepareArt();
            PrepareKit();
            AssetDatabase.SaveAssets();
        }

        static void PrepareKit()
        {
            foreach (var d in new[] { KitMatDir, KitPrefabDir, KitTexDir, KitLightDir }) Directory.CreateDirectory(d);
            kitPropMats.Clear();
            kitPrefabs.Clear();
            kitDump = new StringBuilder();
            matDir = KitMatDir;
            try
            {
                BuildKitMaterials();
                BuildKitPrefabs();
                KitLightingSettings();
            }
            finally { matDir = "Assets/Materials"; }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/kit_dump.txt", kitDump.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[CoopSetup] Art kit: " + kitPrefabs.Count + " prefab.");
        }

        // ---------- Üretilen dokular ----------

        class Img
        {
            public readonly int w, h;
            public readonly Color[] p;
            public Img(int w, int h, Color c) { this.w = w; this.h = h; p = new Color[w * h]; for (int i = 0; i < p.Length; i++) p[i] = c; }

            public void Px(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= w || y >= h || a <= 0f) return;
                int i = y * w + x; var d = p[i]; a = Mathf.Clamp01(a);
                p[i] = new Color(Mathf.Lerp(d.r, c.r, a), Mathf.Lerp(d.g, c.g, a), Mathf.Lerp(d.b, c.b, a), Mathf.Max(d.a, a * c.a));
            }

            public void Line(Vector2 a, Vector2 b, float width, Color c, float alpha = 1f)
            {
                float r = width * 0.5f + 1f;
                int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r), x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r);
                int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r), y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r);
                var ab = b - a; float len2 = Mathf.Max(ab.sqrMagnitude, 1e-6f);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var pt = new Vector2(x + 0.5f, y + 0.5f);
                        float t = Mathf.Clamp01(Vector2.Dot(pt - a, ab) / len2);
                        float d = (pt - (a + ab * t)).magnitude;
                        Px(x, y, c, alpha * Mathf.Clamp01(width * 0.5f - d + 0.5f));
                    }
            }

            public void Disc(Vector2 c, float r, Color col, float alpha = 1f) => Line(c, c, r * 2f, col, alpha);

            public void Ring(Vector2 c, float r, float width, Color col, float alpha = 1f)
            {
                float o = r + width * 0.5f + 1f;
                for (int y = Mathf.FloorToInt(c.y - o); y <= Mathf.CeilToInt(c.y + o); y++)
                    for (int x = Mathf.FloorToInt(c.x - o); x <= Mathf.CeilToInt(c.x + o); x++)
                    {
                        float d = Mathf.Abs((new Vector2(x + 0.5f, y + 0.5f) - c).magnitude - r);
                        Px(x, y, col, alpha * Mathf.Clamp01(width * 0.5f - d + 0.5f));
                    }
            }

            public void Rect(int x0, int y0, int x1, int y1, Color col, float alpha = 1f)
            {
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Px(x, y, col, alpha);
            }
        }

        static Texture2D SaveTex(string name, Img img, bool alpha, int maxSize = 512)
        {
            string path = KitTexDir + "/" + name + ".png";
            var t = new Texture2D(img.w, img.h, TextureFormat.RGBA32, false);
            t.SetPixels(img.p); t.Apply();
            var bytes = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes))
            {
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti.alphaIsTransparency != alpha || ti.wrapMode != TextureWrapMode.Clamp || ti.maxTextureSize != maxSize)
            {
                ti.alphaIsTransparency = alpha; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = maxSize;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Hash01(int a, int b, int salt = 0)
        {
            unchecked
            {
                uint u = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(salt * 83492791);
                u ^= u >> 13; u *= 0x5bd1e995; u ^= u >> 15;
                return (u % 10000) / 10000f;
            }
        }

        static Color GlassColor(int a, int b)
        {
            float r = Hash01(a, b), r2 = Hash01(a, b, 5);
            if (r < 0.8f) return new Color(0.36f, 0.46f, 0.7f) * (0.55f + 0.5f * r2);
            return Pal.Vitray[(int)(r2 * 3.999f)] * 1.25f;
        }

        static Texture2D TexCobweb()
        {
            var col = new Color(0.86f, 0.88f, 0.92f, 1f);
            var im = new Img(256, 256, new Color(col.r, col.g, col.b, 0f));
            var rnd = new System.Random(7);
            const int spokes = 7;
            var o = new Vector2(2, 2);
            var dir = new Vector2[spokes]; var len = new float[spokes];
            for (int i = 0; i < spokes; i++)
            {
                float ang = i / (spokes - 1f) * 90f;
                if (i > 0 && i < spokes - 1) ang += (float)(rnd.NextDouble() - 0.5) * 7f;
                ang *= Mathf.Deg2Rad;
                dir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                len[i] = 246f * (0.7f + 0.3f * (float)rnd.NextDouble());
                im.Line(o, o + dir[i] * len[i], 1.3f, col, 0.62f);
            }
            for (int k = 1; k <= 10; k++)
                for (int i = 0; i < spokes - 1; i++)
                {
                    float ra = k / 10.5f * (0.96f + 0.08f * (float)rnd.NextDouble()), rb = k / 10.5f * (0.96f + 0.08f * (float)rnd.NextDouble());
                    var pa = o + dir[i] * len[i] * ra; var pb = o + dir[i + 1] * len[i + 1] * rb;
                    var mid = o + ((pa + pb) * 0.5f - o) * 0.92f;
                    float fade = 0.42f * (1f - 0.5f * k / 10f);
                    im.Line(pa, mid, 1f, col, fade); im.Line(mid, pb, 1f, col, fade);
                }
            return SaveTex("Cobweb", im, true, 256);
        }

        static Texture2D TexShaft()
        {
            var im = new Img(64, 256, new Color(1, 1, 1, 0));
            for (int y = 0; y < im.h; y++)
                for (int x = 0; x < im.w; x++)
                {
                    float u = (x + 0.5f) / im.w, v = (y + 0.5f) / im.h;
                    float a = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.6f) * Mathf.SmoothStep(0f, 1f, v) * (0.8f + 0.2f * Mathf.PerlinNoise(u * 9f, 0.3f));
                    im.p[y * im.w + x] = new Color(1, 1, 1, a);
                }
            return SaveTex("LightShaft", im, true, 256);
        }

        static Texture2D TexLancet()
        {
            const int W = 256, H = 512;
            var im = new Img(W, H, new Color(0, 0, 0, 0));
            var stone = new Color(0.17f, 0.17f, 0.2f, 1f); var lead = new Color(0.03f, 0.03f, 0.04f, 1f);
            bool Inside(float u, float v, float m)
            {
                if (v < m) return false;
                if (v < 0.8f) return Mathf.Abs(u) < 0.9f - m;
                float r = 1.2f - m;
                return new Vector2(u + 0.3f, v - 0.8f).magnitude < r && new Vector2(u - 0.3f, v - 0.8f).magnitude < r;
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f, v = (y + 0.5f) / H * 2f;
                    if (!Inside(u, v, 0f)) continue;
                    Color c;
                    if (!Inside(u, v, 0.08f)) c = stone;
                    else
                    {
                        const float s = 4.5f;
                        float fa = (u + v * 0.9f) * s, fb = (u - v * 0.9f) * s;
                        bool isLead = fa - Mathf.Floor(fa) < 0.08f || fb - Mathf.Floor(fb) < 0.08f || (Mathf.Abs(u) < 0.03f && v < 1.25f) || !Inside(u, v, 0.105f);
                        c = isLead ? lead : GlassColor(Mathf.FloorToInt(fa) + (u < 0 ? 31 : 0), Mathf.FloorToInt(fb));
                        if (!isLead) c *= 0.65f + 0.35f * Mathf.PerlinNoise(u * 3f + 5f, v * 3f);
                        c.a = 1f;
                    }
                    im.p[y * W + x] = c;
                }
            return SaveTex("VitrayLancet", im, true, 512);
        }

        static Texture2D TexRose()
        {
            const int S = 512;
            var im = new Img(S, S, new Color(0, 0, 0, 0));
            var stone = new Color(0.17f, 0.17f, 0.2f, 1f); var lead = new Color(0.03f, 0.03f, 0.04f, 1f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    if (r > 0.98f) continue;
                    float th = (Mathf.Atan2(v, u) / (2f * Mathf.PI) + 1f) % 1f;
                    Color c;
                    if (r > 0.9f) c = stone;
                    else
                    {
                        int ring = r < 0.2f ? 0 : r < 0.52f ? 1 : 2;
                        int n = ring == 0 ? 1 : ring == 1 ? 12 : 24;
                        float f = th * n; float ft = f - Mathf.Floor(f);
                        float spokeW = 0.012f / Mathf.Max(r, 0.05f) * n / (2f * Mathf.PI);
                        bool isLead = Mathf.Abs(r - 0.2f) < 0.015f || Mathf.Abs(r - 0.52f) < 0.018f || r > 0.875f || (ring > 0 && (ft < spokeW || ft > 1f - spokeW));
                        // dış halkada yonca kemeri
                        if (ring == 2 && !isLead)
                        {
                            float px = (ft - 0.5f) * (2f * Mathf.PI * r / n), py = r - 0.78f;
                            if (Mathf.Abs(new Vector2(px, py).magnitude - 0.075f) < 0.01f) isLead = true;
                        }
                        c = isLead ? lead : ring == 0 ? Pal.Vitray[3] * 1.3f : GlassColor(Mathf.FloorToInt(f) * 7 + ring, ring * 13);
                        if (!isLead) c *= 0.7f + 0.3f * Mathf.PerlinNoise(u * 4f + 9f, v * 4f + 2f);
                        c.a = 1f;
                    }
                    im.p[y * S + x] = c;
                }
            return SaveTex("VitrayRose", im, true, 512);
        }

        static void FleurDeLis(Img im, Vector2 c, float s, Color gold)
        {
            // orta yaprak
            for (int y = -62; y <= 82; y++)
            {
                float hw = 21f * Mathf.Pow(Mathf.Sin(Mathf.PI * (y + 62) / 144f), 0.75f) * s;
                im.Line(c + new Vector2(-hw, y * s), c + new Vector2(hw, y * s), 1.6f, gold);
            }
            // yan yapraklar (kıvrık)
            foreach (int sg in new[] { -1, 1 })
            {
                Vector2 p0 = new Vector2(sg * 8, -8), p1 = new Vector2(sg * 62, 78), p2 = new Vector2(sg * 56, -2), p3 = new Vector2(sg * 36, -20);
                Vector2 prev = p0;
                for (int i = 1; i <= 24; i++)
                {
                    float t = i / 24f;
                    Vector2 q = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
                    im.Line(c + prev * s, c + q * s, Mathf.Lerp(17f, 9f, t) * s, gold);
                    prev = q;
                }
                im.Line(c + p2 * s, c + p3 * s, 7f * s, gold);
                im.Line(c + new Vector2(sg * 5, -24) * s, c + new Vector2(sg * 27, -56) * s, 9f * s, gold);
            }
            im.Line(c + new Vector2(-30, -15) * s, c + new Vector2(30, -15) * s, 11f * s, gold);
            for (int y = -66; y <= -22; y++)
            {
                float hw = 9f * Mathf.Sin(Mathf.PI * (y + 66) / 44f) * s;
                im.Line(c + new Vector2(-hw, y * s), c + new Vector2(hw, y * s), 1.6f, gold);
            }
        }

        static Texture2D TexBanner()
        {
            const int W = 256, H = 512;
            var gold = new Color(Pal.Gold.r, Pal.Gold.g, Pal.Gold.b, 1f);
            var im = new Img(W, H, new Color(Pal.Bordo.r, Pal.Bordo.g, Pal.Bordo.b, 1f));
            im.Rect(12, 60, 15, H - 14, gold); im.Rect(W - 16, 60, W - 13, H - 14, gold);
            im.Rect(12, H - 17, W - 13, H - 14, gold);
            im.Rect(22, 70, 23, H - 24, gold, 0.7f); im.Rect(W - 24, 70, W - 23, H - 24, gold, 0.7f);
            FleurDeLis(im, new Vector2(W * 0.5f, 300), 1.05f, gold);
            var rnd = new System.Random(11);
            var cut = new float[W];
            for (int x = 0; x < W; x++)
            {
                float tri = Mathf.Abs(((x / 42.6f) % 1f) - 0.5f) * 2f; // dilimli etek
                cut[x] = 18f + 46f * tri + 10f * Mathf.PerlinNoise(x * 0.11f, 3.3f) + (rnd.NextDouble() < 0.04 ? 22f : 0f);
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x; var c = im.p[i];
                    float fold = 0.78f + 0.22f * Mathf.Sin(x / (float)W * Mathf.PI * 5f + 0.6f);
                    float worn = 0.8f + 0.2f * Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                    c = new Color(c.r * fold * worn, c.g * fold * worn, c.b * fold * worn, y < cut[x] ? 0f : 1f);
                    if (Mathf.PerlinNoise(x * 0.09f + 40f, y * 0.09f) > 0.83f && y < 150) c.a = 0f; // yırtık
                    im.p[i] = c;
                }
            return SaveTex("Banner", im, true, 512);
        }

        static Texture2D TexRug()
        {
            const int W = 512, H = 256;
            var gold = new Color(Pal.Gold.r * 0.9f, Pal.Gold.g * 0.9f, Pal.Gold.b * 0.9f, 1f);
            var dark = new Color(0.2f, 0.06f, 0.07f, 1f);
            var im = new Img(W, H, new Color(Pal.Bordo.r, Pal.Bordo.g, Pal.Bordo.b, 1f));
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int e = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                    Color c = im.p[y * W + x];
                    if (e < 10) c = dark;
                    else if (e < 14) c = gold;
                    else if (e < 34)
                    {
                        float t = ((x + y) / 20f) % 1f, t2 = ((x - y + 1000) / 20f) % 1f;
                        c = (Mathf.Abs(t - 0.5f) < 0.08f || Mathf.Abs(t2 - 0.5f) < 0.08f) ? gold : dark;
                    }
                    else if (e < 38) c = gold;
                    else
                    {
                        float a = ((x + y * 1.2f) / 46f) % 1f, b = ((x - y * 1.2f + 1000) / 46f) % 1f;
                        if (a < 0.05f || b < 0.05f) c = Color.Lerp(c, dark, 0.7f);
                        float mx = (x - W * 0.5f) / 110f, my = (y - H * 0.5f) / 62f; float md = Mathf.Sqrt(mx * mx + my * my);
                        if (Mathf.Abs(md - 1f) < 0.05f || Mathf.Abs(md - 0.6f) < 0.04f) c = gold;
                        else if (md < 0.6f) c = Color.Lerp(c, dark, 0.55f);
                    }
                    float worn = 0.62f + 0.38f * Mathf.PerlinNoise(x * 0.025f, y * 0.025f);
                    float grain = 0.92f + 0.08f * Hash01(x, y);
                    im.p[y * W + x] = new Color(c.r * worn * grain, c.g * worn * grain, c.b * worn * grain, 1f);
                }
            return SaveTex("Rug", im, false, 512);
        }

        static Texture2D TexPaper(string name, int variant)
        {
            const int S = 256;
            var ink = new Color(0.17f, 0.11f, 0.07f, 1f);
            var im = new Img(S, S, new Color(Pal.Parchment.r, Pal.Parchment.g, Pal.Parchment.b, 1f));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float st = 0.8f + 0.2f * Mathf.PerlinNoise(x * 0.03f + variant * 7f, y * 0.03f);
                    int e = Mathf.Min(Mathf.Min(x, S - 1 - x), Mathf.Min(y, S - 1 - y));
                    st *= Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(e / 26f));
                    var c = im.p[y * S + x]; im.p[y * S + x] = new Color(c.r * st, c.g * st * 0.98f, c.b * st * 0.94f, 1f);
                }
            var rnd = new System.Random(20 + variant);
            void Scribble(int y, int x0, int x1)
            {
                float x = x0;
                while (x < x1)
                {
                    float wlen = 8 + (float)rnd.NextDouble() * 22;
                    Vector2 prev = new Vector2(x, y);
                    for (float xx = x + 2; xx < Mathf.Min(x + wlen, x1); xx += 2.2f)
                    {
                        var q = new Vector2(xx, y + ((float)rnd.NextDouble() - 0.5f) * 5.5f);
                        im.Line(prev, q, 1.1f, ink, 0.75f); prev = q;
                    }
                    x += wlen + 6;
                }
            }
            if (variant == 0) for (int r = 0; r < 12; r++) Scribble(226 - r * 17, 28, r == 11 ? 130 : 228);
            if (variant == 1)
            {
                var c = new Vector2(128, 150);
                im.Ring(c, 62, 1.5f, ink, 0.8f); im.Ring(c, 44, 1.2f, ink, 0.7f); im.Ring(c, 10, 1.5f, ink, 0.8f);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    im.Line(c + d * 44, c + d * 70, 1.2f, ink, 0.75f);
                }
                im.Line(c, c + new Vector2(20, 34), 1.6f, ink, 0.8f); im.Line(c, c + new Vector2(-30, 8), 1.6f, ink, 0.8f);
                for (int r = 0; r < 3; r++) Scribble(64 - r * 16, 36, 220);
            }
            return SaveTex(name, im, false, 256);
        }

        static Texture2D TexPainting(string name, int variant)
        {
            const int W = 256, H = 192;
            var im = new Img(W, H, Color.black);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / H, o = variant * 11.3f;
                    float horizon = 0.4f + 0.1f * Mathf.PerlinNoise(u * 2.2f + o, 0.5f);
                    float near = 0.2f + 0.16f * Mathf.PerlinNoise(u * 3.4f + o, 1.7f);
                    Color sky = variant == 0 ? new Color(0.5f, 0.38f, 0.2f) : new Color(0.2f, 0.25f, 0.34f);
                    Color c = Color.Lerp(sky, new Color(0.09f, 0.12f, 0.15f), Mathf.SmoothStep(horizon, 1f, v));
                    c += Color.white * 0.07f * (Mathf.PerlinNoise(u * 5f + o, v * 9f) - 0.5f);
                    if (variant == 1 && new Vector2((u - 0.74f) * W / H, v - 0.78f).magnitude < 0.06f) c = new Color(0.7f, 0.72f, 0.66f);
                    if (variant == 1)
                    {   // tepede kale silüeti
                        float hill = 0.34f + 0.22f * Mathf.Exp(-Mathf.Pow((u - 0.42f) * 3.2f, 2f));
                        bool keep = (Mathf.Abs(u - 0.42f) < 0.07f && v < 0.74f) || (Mathf.Abs(u - 0.33f) < 0.025f && v < 0.82f) || (Mathf.Abs(u - 0.5f) < 0.02f && v < 0.68f);
                        if (v < hill || (keep && v > 0.3f)) c = new Color(0.05f, 0.05f, 0.06f);
                    }
                    if (v < horizon && variant == 0) c = Color.Lerp(new Color(0.1f, 0.11f, 0.06f), new Color(0.24f, 0.19f, 0.1f), Mathf.PerlinNoise(u * 6f, v * 10f));
                    if (v < near) c = new Color(0.06f, 0.05f, 0.04f);
                    float d2 = (u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f);
                    float k = (1f - 1.5f * d2) * (0.88f + 0.12f * Hash01(x / 2, y / 2));
                    im.p[y * W + x] = new Color(c.r * k, c.g * k, c.b * k, 1f);
                }
            return SaveTex(name, im, false, 256);
        }

        // ---------- Malzeme kütüphanesi ----------

        static Material KTex(string name, string tex, Color tint, float smooth = 0.2f, float bump = 1f, float metallic = 0f)
        {
            return TexMat(name, tex, tint, smooth, bump, metallic);
        }

        static Material KAcg(string name, string id, Color tint, float smooth, float metallic)
        {
            var m = MakeMat(name, tint);
            string b = AcgRoot + id + "/" + id + "_1K-JPG_";
            SetMaps(m, Tex(b + "Color.jpg", false), Tex(b + "NormalGL.jpg", true), smooth, 1f, metallic);
            return m;
        }

        /// <summary>Saydam unlit (örümcek ağı, ışık huzmesi). additive: toplamalı.</summary>
        static Material KUnlit(string name, Texture2D tex, Color color, bool additive)
        {
            var m = MakeMat(name, color, "Universal Render Pipeline/Unlit");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha", 1f); m.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false); m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Ay huzmesi: Art/Kit/Shaders/LightShaft.shader (yandan bakınca sönen toplamalı kart).</summary>
        static Material KShaft(string name, Texture2D tex, Color color)
        {
            var m = MakeMat(name, color, "CoopGame/LightShaft");
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Fade", 2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Alfa kesmeli, çift taraflı Lit (flama, vitray). emission > 0: doku aynı zamanda ışıma haritası.</summary>
        static Material KCutout(string name, Texture2D tex, Color color, float emission = 0f)
        {
            var m = MakeMat(name, color);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            m.SetFloat("_Smoothness", 0.1f);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", Color.white * emission);
                // Bake'e girmez: renkli camın ışıması lightmap'te alacalı leke yapıyor; ışığı pencerenin ay spotu verir.
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Düz renkli kumaş: renk paletten, yalnızca kadife normal haritası (dokunun kendi kırmızısı palet dışı kalıyor).</summary>
        static Material KCloth(string name, Color color)
        {
            var m = MakeMat(name, new Color(color.r, color.g, color.b, 1f));
            SetMaps(m, null, Tex(TexRoot + "velour_velvet/velour_velvet_nor_gl.jpg", true), 0.1f, 1f, 0f);
            return m;
        }

        static Material KFlat(string name, Texture2D tex, Color tint, float smooth = 0.1f)
        {
            var m = MakeMat(name, tint);
            m.SetTexture("_BaseMap", tex); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void BuildKitMaterials()
        {
            K = new KitMats
            {
                panel = KTex("K_Panel", "wooden_panels", new Color(0.34f, 0.37f, 0.38f), 0.22f, 1.2f),
                beam = KTex("K_Beam", "dark_wooden_planks", new Color(0.5f, 0.44f, 0.4f), 0.15f, 1.2f),
                parquet = KTex("K_Parquet", "old_wood_floor", new Color(0.6f, 0.5f, 0.43f), 0.28f),
                furniture = KTex("K_Furniture", "wood_cabinet_worn_long", new Color(0.5f, 0.41f, 0.35f), 0.25f),
                castleStone = KTex("K_CastleStone", "medieval_blocks_05", new Color(0.5f, 0.53f, 0.62f), 0.15f, 1.2f),
                hallFloor = KTex("K_HallFloor", "monastery_stone_floor", new Color(0.5f, 0.5f, 0.56f), 0.55f, 1.1f),
                cellarBrick = KTex("K_CellarBrick", "castle_brick_01", new Color(0.78f, 0.62f, 0.52f), 0.2f, 1.3f),
                cellarFloor = KTex("K_CellarFloor", "slab_tiles", new Color(0.75f, 0.64f, 0.58f), 0.72f, 1.2f),
                cisternWall = KTex("K_CisternWall", "mossy_stone_wall", new Color(0.46f, 0.62f, 0.58f), 0.35f, 1.3f),
                cisternFloor = KTex("K_CisternFloor", "mossy_cobblestone", new Color(0.44f, 0.58f, 0.56f), 0.5f, 1.3f),
                velvet = KCloth("K_Velvet", Pal.Bordo * 1.15f),
                velvetPurple = KCloth("K_VelvetPurple", Pal.Purple * 0.8f),
                carpet = KTex("K_Carpet", "dirty_carpet", new Color(1f, 0.42f, 0.4f), 0.05f),
                leather = KTex("K_Leather", "brown_leather", new Color(0.62f, 0.52f, 0.46f), 0.3f),
                leatherRed = KTex("K_LeatherRed", "brown_leather", new Color(0.75f, 0.3f, 0.28f), 0.3f),
                leatherGreen = KTex("K_LeatherGreen", "brown_leather", new Color(0.4f, 0.55f, 0.4f), 0.3f),
                books = KTex("K_Books", "book_pattern", new Color(0.7f, 0.6f, 0.52f), 0.2f),
                iron = KTex("K_Iron", "rusty_metal_02", new Color(0.4f, 0.37f, 0.35f), 0.35f, 1f, 0.5f),
                brass = KAcg("K_Brass", "Metal008", new Color(0.92f, 0.74f, 0.42f), 0.55f, 0.65f),
                bronze = KAcg("K_Bronze", "Metal008", new Color(0.66f, 0.47f, 0.28f), 0.45f, 0.6f),
                wax = MakeMat("K_Wax", new Color(0.86f, 0.8f, 0.66f)),
                flame = GlowMat("K_Flame", new Color(1f, 0.62f, 0.25f), new Color(1f, 0.52f, 0.16f) * 2.4f),
                lanternGlass = GlowMat("K_LanternGlass", new Color(1f, 0.72f, 0.38f), new Color(1f, 0.58f, 0.22f) * 1.5f),
                paperA = KFlat("K_PaperA", TexPaper("PaperLetter", 0), new Color(0.62f, 0.62f, 0.62f)),
                paperB = KFlat("K_PaperB", TexPaper("PaperDiagram", 1), new Color(0.62f, 0.62f, 0.62f)),
                paperC = KFlat("K_PaperC", TexPaper("PaperBlank", 2), new Color(0.62f, 0.62f, 0.62f)),
                paintingA = KFlat("K_PaintingA", TexPainting("PaintingLandscape", 0), Color.white, 0.35f),
                paintingB = KFlat("K_PaintingB", TexPainting("PaintingKeep", 1), Color.white, 0.35f),
                flameCard = KUnlit("K_FlameCard", Tex(ManualRoot + "chandelier/textures/albedo.jpg", false), Color.white, true),
                clearGlass = KUnlit("K_ClearGlass", null, new Color(0.5f, 0.55f, 0.6f, 0.06f), false),
                bottleGlass = MakeMat("K_BottleGlass", new Color(0.1f, 0.17f, 0.11f)),
                cobweb = KUnlit("K_Cobweb", TexCobweb(), new Color(0.75f, 0.78f, 0.85f, 0.55f), false),
                shaft = KShaft("K_LightShaft", TexShaft(), new Color(Pal.Moon.r, Pal.Moon.g, Pal.Moon.b, 0.22f)),
                vitray = KCutout("K_Vitray", TexLancet(), Color.white, 0.5f),
                rose = KCutout("K_VitrayRose", TexRose(), Color.white, 0.5f),
                banner = KCutout("K_Banner", TexBanner(), new Color(0.6f, 0.56f, 0.56f)),
                rug = KFlat("K_Rug", TexRug(), new Color(0.56f, 0.5f, 0.5f), 0.05f),
            };
            // sıva: dokunun kontrastı yumuşatılmış türevi (Art/Kit/Textures/PlasterSoft.jpg) + özgün normal
            K.plaster = MakeMat("K_Plaster", new Color(0.62f, 0.62f, 0.66f));
            SetMaps(K.plaster, Tex(KitTexDir + "/PlasterSoft.jpg", false), Tex(TexRoot + "worn_plaster_wall/worn_plaster_wall_nor_gl.jpg", true), 0.08f, 0.8f, 0f);
            K.flame.SetFloat("_Smoothness", 0f);
            K.bottleGlass.SetFloat("_Smoothness", 0.85f);
        }

        // ---------- Prefab altyapısı ----------

        static GameObject QuadObj(string name, Transform parent, Vector3 pos, Vector2 size, Material mat, Quaternion? rot = null)
        {
            var go = Prim(name, parent, PrimitiveType.Quad, pos, new Vector3(size.x, size.y, 1f), mat);
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>a ile b arasında ince çubuk (zincir, kol).</summary>
        static GameObject Rod(string name, Transform parent, Vector3 a, Vector3 b, float thick, Material mat)
        {
            float len = Mathf.Round((b - a).magnitude * 100f) / 100f;
            var go = Slab(name, parent, (a + b) * 0.5f, new Vector3(thick, len, thick), mat, 1f, false);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        static Mesh FlameMesh() => LatheMesh("kit_flame", new[]
        {
            new Vector2(0f, 0f), new Vector2(0.5f, 0.22f), new Vector2(0.42f, 0.5f), new Vector2(0.16f, 0.85f), new Vector2(0f, 1f),
        }, 6);

        static Mesh CandleMesh() => LatheMesh("kit_candle", new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.96f), new Vector2(0.8f, 1f), new Vector2(0f, 0.97f),
        }, 8);

        static Mesh RingMesh(float r, float w, float h) => LatheMesh("kit_ring_" + F(r) + "_" + F(w) + "_" + F(h), new[]
        {
            new Vector2(r - w, 0f), new Vector2(r, 0f), new Vector2(r, h), new Vector2(r - w, h), new Vector2(r - w, 0f),
        }, 24);

        /// <summary>Mum: balmumu + alev. pos: mumun tabanı. lit=false: sönük.</summary>
        static void KCandle(Transform parent, Vector3 pos, float h = 0.14f, float r = 0.016f, bool lit = true)
        {
            var wax = MeshObj("Wax", parent, pos, CandleMesh(), K.wax);
            wax.transform.localScale = new Vector3(r, h, r);
            wax.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (!lit) return;
            var fl = MeshObj("Flame", parent, pos + new Vector3(0, h + 0.004f, 0), FlameMesh(), K.flame);
            fl.transform.localScale = new Vector3(0.024f, 0.055f, 0.024f);
            fl.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>
        /// Prefabı kaydeder. Dekor ışık haritası yemez (light probe kullanır), ama gi=true ise baked gölge/yansıma verir.
        /// cull: ekran yüksekliğinin bu oranından küçükken çizilmez (LODGroup).
        /// </summary>
        static GameObject SaveKit(GameObject go, bool gi = true, float cull = 0.02f, bool isStatic = true, bool wall = false)
        {
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (gi) flags |= StaticEditorFlags.ContributeGI;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var r = t.GetComponent<Renderer>();
                bool glow = r != null && r.sharedMaterial != null && (r.sharedMaterial == K.flame || r.sharedMaterial == K.shaft || r.sharedMaterial == K.cobweb);
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, !isStatic ? 0 : glow ? StaticEditorFlags.BatchingStatic : flags);
                if (r is MeshRenderer mr) mr.receiveGI = ReceiveGI.LightProbes;
            }
            // Probe çapası: zemine/duvara yapışık dekor probe gövdesinin dışında kalıp kararmasın diye örnekleme noktası odanın içine alınır.
            var own = go.GetComponentsInChildren<Renderer>().Where(r => PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject) == null).ToArray();
            if (own.Length > 0)
            {
                var b = RendBounds(own);
                var a = new GameObject("ProbeAnchor").transform;
                a.SetParent(go.transform, false);
                a.position = wall ? new Vector3(b.center.x, b.center.y, 0.6f)
                           : new Vector3(b.center.x, (b.max.y <= 0.05f && b.min.y < -0.2f) ? b.center.y : Mathf.Max(b.center.y, 0.45f), b.center.z);
                foreach (var r in own) r.probeAnchor = a;
            }
            var rends = go.GetComponentsInChildren<Renderer>();
            if (cull > 0f && rends.Length > 0 && go.GetComponent<LODGroup>() == null)
            {
                var lg = go.AddComponent<LODGroup>();
                lg.SetLODs(new[] { new LOD(cull, rends) });
                lg.RecalculateBounds();
            }
            var p = PrefabUtility.SaveAsPrefabAsset(go, KitPrefabDir + "/" + go.name + ".prefab");
            kitPrefabs[go.name] = p;
            Object.DestroyImmediate(go);
            return p;
        }

        static GameObject KitPrefab(string name)
        {
            if (kitPrefabs.TryGetValue(name, out var p) && p != null) return p;
            p = AssetDatabase.LoadAssetAtPath<GameObject>(KitPrefabDir + "/" + name + ".prefab");
            if (p == null) Debug.LogError("[CoopSetup] Kit prefabı yok: " + name);
            kitPrefabs[name] = p;
            return p;
        }

        /// <summary>Kit prefabını yerleştirir.</summary>
        static GameObject Kit(string name, Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            return KitR(name, parent, pos, Quaternion.Euler(0, yaw, 0), scale);
        }

        static GameObject KitR(string name, Transform parent, Vector3 pos, Quaternion rot, float scale = 1f)
        {
            var p = KitPrefab(name);
            var go = p != null ? (GameObject)PrefabUtility.InstantiatePrefab(p) : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static void PrepModel(string path)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) return;
            bool dirty = false;
            var lod = typeof(ModelImporter).GetProperty("generateMeshLods");
            if (lod != null && lod.CanWrite && !(bool)lod.GetValue(mi)) { lod.SetValue(mi, true); dirty = true; }
            if (mi.importCameras) { mi.importCameras = false; dirty = true; }
            if (mi.importLights) { mi.importLights = false; dirty = true; }
            if (mi.meshCompression != ModelImporterMeshCompression.Medium) { mi.meshCompression = ModelImporterMeshCompression.Medium; dirty = true; }
            if (!mi.generateSecondaryUV) { mi.generateSecondaryUV = true; dirty = true; }
            if (dirty) mi.SaveAndReimport();
        }

        static Material KitPropMat(string texDir, string model, string srcName, Color tint)
        {
            string low = srcName.ToLowerInvariant();
            if (low.Contains("canvas") || low.Contains("artwork")) return model.Contains("fancy") ? K.paintingA : K.paintingB;
            if (low.Contains("glass")) return model.Contains("lantern") ? K.lanternGlass : model.Contains("bottle") ? K.bottleGlass : K.clearGlass;
            string diff = null, nor = null, bestPart = ""; int bestScore = -1;
            if (Directory.Exists(texDir))
                foreach (var f in Directory.GetFiles(texDir).OrderBy(x => x))
                {
                    string fn = Path.GetFileName(f);
                    if (fn.EndsWith(".meta") || !fn.Contains("_diff_")) continue;
                    string part = fn.Substring(0, fn.IndexOf("_diff_")).ToLowerInvariant();
                    if (part.StartsWith(model.ToLowerInvariant())) part = part.Substring(model.Length).Trim('_');
                    string tail = part.Contains("_") ? part.Substring(part.LastIndexOf('_') + 1) : part;
                    int score = part.Length > 0 && (low.Contains(part) || (tail.Length >= 2 && low.EndsWith("_" + tail))) ? 2 : part.Length == 0 ? 1 : 0;
                    if (score > bestScore)
                    {
                        bestScore = score; bestPart = part;
                        diff = texDir + "/" + fn;
                        string n = texDir + "/" + fn.Replace("_diff_", "_nor_gl_");
                        nor = File.Exists(n) ? n : null;
                    }
                }
            if (low.Contains("flame")) return K.flame;
            if (diff == null) return low.EndsWith("candles") ? K.wax : K.brass; // dokusuz metal modeller
            bool metal = model.ToLowerInvariant().Contains("brass");
            string key = model + "|" + diff + "|" + ColorUtility.ToHtmlStringRGB(tint);
            if (kitPropMats.TryGetValue(key, out var m)) return m;
            string name = "P_" + model + (bestPart.Length > 0 ? "_" + bestPart : "") + (tint == Color.white ? "" : "_" + ColorUtility.ToHtmlStringRGB(tint));
            m = MakeMat(name, tint);
            SetMaps(m, diff != null ? Tex(diff, false) : null, nor != null ? Tex(nor, true) : null, metal ? 0.5f : 0.22f, 1f, metal ? 0.7f : 0f);
            kitPropMats[key] = m;
            return m;
        }

        static Bounds RendBounds(Renderer[] rends)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return b;
        }

        static int TriCount(Renderer r)
        {
            Mesh m = r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
            if (m == null) return 0;
            int n = 0;
            for (int i = 0; i < m.subMeshCount; i++) n += (int)(m.GetIndexCount(i) / 3);
            return n;
        }

        /// <summary>
        /// Model dosyasından kit prefabı. size: hedef yükseklik (byMax=true ise en uzun kenar). part>=0: set modelinde x'e göre sıralı tek parça.
        /// mat verilirse tüm yüzeylere o atanır; yoksa modelin kendi dokusu (tint ile). Taban merkezi prefab orijinidir.
        /// </summary>
        static GameObject ModelProp(string prefabName, string modelPath, string texDir, float size, bool solid = true, bool byMax = false,
                                    int part = -1, Material mat = null, Color? tint = null, Vector3? euler = null, float cull = 0.02f, bool save = true, bool wall = false)
        {
            PrepModel(modelPath);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var wrap = new GameObject(prefabName);
            if (asset == null) { Debug.LogError("[CoopSetup] Model yok: " + modelPath); return save ? SaveKit(wrap) : wrap; }
            string model = Path.GetFileNameWithoutExtension(modelPath);

            var inst = Object.Instantiate(asset);
            inst.name = "Model";
            inst.transform.SetParent(wrap.transform, false);
            foreach (var a in inst.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(a);
            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return save ? SaveKit(wrap) : wrap;

            if (part <= 0)
            {
                kitDump.AppendLine(model + ": " + rends.Length + " parça, " + rends.Sum(TriCount) + " üçgen");
                foreach (var r in rends.OrderBy(r => r.bounds.center.x))
                    kitDump.AppendLine("   " + r.name + " c=" + r.bounds.center.ToString("0.00") + " s=" + r.bounds.size.ToString("0.00") + " tri=" + TriCount(r) +
                                       " mats=" + string.Join(",", r.sharedMaterials.Select(x => x != null ? x.name : "null")));
            }
            if (part >= 0)
            {
                var sorted = rends.OrderBy(r => r.bounds.center.x).ToArray();
                var keep = sorted[Mathf.Min(part, sorted.Length - 1)];
                foreach (var r in sorted) if (r != keep) r.enabled = false;
                foreach (var r in sorted) if (r != keep && r.transform.childCount == 0) Object.DestroyImmediate(r.gameObject); else if (r != keep) Object.DestroyImmediate(r);
                rends = new[] { keep };
            }

            if (euler.HasValue) inst.transform.localRotation = Quaternion.Euler(euler.Value) * inst.transform.localRotation;
            var b0 = RendBounds(rends);
            float cur = byMax ? Mathf.Max(b0.size.x, Mathf.Max(b0.size.y, b0.size.z)) : b0.size.y;
            inst.transform.localScale *= size / Mathf.Max(0.0001f, cur);
            var b1 = RendBounds(rends);
            inst.transform.position -= new Vector3(b1.center.x, b1.min.y, b1.center.z);

            foreach (var r in rends)
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = mat != null ? mat : KitPropMat(texDir, model, src[i] != null ? src[i].name : "", tint ?? Color.white);
                r.sharedMaterials = dst;
            }
            if (solid)
            {
                var b2 = RendBounds(rends);
                var col = wrap.AddComponent<BoxCollider>();
                col.center = b2.center; col.size = b2.size * 0.94f;
            }
            return save ? SaveKit(wrap, true, cull, true, wall) : wrap;
        }

        static GameObject PhProp(string prefabName, string model, float size, bool solid = true, bool byMax = false, int part = -1,
                                 Color? tint = null, Vector3? euler = null, float cull = 0.02f, bool save = true, bool wall = false)
        {
            return ModelProp(prefabName, ModelRoot + model + "/" + model + ".fbx", ModelRoot + model + "/textures", size, solid, byMax, part, null, tint, euler, cull, save, wall);
        }

        static GameObject ManualProp(string prefabName, string folder, float size, float smooth, float metallic, bool solid = true, Vector3? euler = null, bool save = true)
        {
            string dir = ManualRoot + folder + "/";
            var m = MakeMat("P_" + folder, Color.white);
            SetMaps(m, Tex(dir + folder + "_diff.jpg", false), Tex(dir + folder + "_nor.jpg", true), smooth, 1f, metallic);
            return ModelProp(prefabName, dir + folder + ".obj", dir, size, solid, false, -1, m, null, euler, 0.02f, save);
        }

        // ---------- Dekor prefabları ----------

        static void BuildKitPrefabs()
        {
            // --- Poly Haven modelleri (yükseklik m) ---
            PhProp("Table", "WoodenTable_01", 0.82f);
            PhProp("Console", "ClassicConsole_01", 0.85f);
            PhProp("Commode", "GothicCommode_01", 0.95f);
            PhProp("Cabinet", "GothicCabinet_01", 2.05f);
            PhProp("Bookshelf", "wooden_bookshelf_worn", 1.9f);
            PhProp("Drawer", "vintage_wooden_drawer_01", 0.85f);
            PhProp("Armchair", "ArmChair_01", 1.05f, tint: new Color(0.5f, 0.22f, 0.22f));
            PhProp("Stool", "wooden_stool_01", 0.5f, tint: new Color(0.62f, 0.56f, 0.5f));
            PhProp("Ladder", "wooden_ladder_02", 2.3f);
            PhProp("GrandfatherClock", "vintage_grandfather_clock_01", 2.15f);
            PhProp("Chest", "treasure_chest", 0.7f);
            PhProp("Barrel", "wine_barrel_01", 1.0f);
            PhProp("BarrelLying", "wine_barrel_01", 0.78f, euler: new Vector3(90, 0, 0));
            PhProp("Crate", "wooden_crate_02", 0.6f);
            PhProp("CrateLong", "wooden_crate_01", 0.42f);
            PhProp("Bucket", "wooden_bucket_01", 0.36f);
            PhProp("Door", "large_castle_door", 2.6f, solid: false);
            PhProp("StatueKing", "gothic_statue", 1.7f);
            PhProp("Bust", "marble_bust_01", 0.6f, tint: new Color(0.55f, 0.55f, 0.58f));
            PhProp("PaintingLandscape", "fancy_picture_frame_01", 0.75f, solid: false, wall: true);
            PhProp("PaintingKeep", "hanging_picture_frame_02", 0.6f, solid: false, wall: true);
            PhProp("Vase", "brass_vase_03", 0.34f, solid: false);
            PhProp("Pot", "brass_pot_01", 0.28f, solid: false);
            PhProp("Bowl", "wooden_bowl_02", 0.09f, solid: false);
            for (int i = 0; i < 3; i++) PhProp("Goblet" + i, "brass_goblets", 0.16f + 0.01f * i, solid: false, part: i);
            for (int i = 0; i < 4; i++) PhProp("Bottle" + i, "wine_bottles_01", 0.32f, solid: false, part: i);
            for (int i = 0; i < 3; i++) PhProp("BrassCandleholder" + i, "brass_candleholders", new[] { 0.62f, 0.34f, 0.3f }[i], solid: false, part: i);
            PhProp("Wrench", "combination_wrench", 0.24f, solid: false, byMax: true);
            PhProp("Hammer", "cross_pein_hammer", 0.3f, solid: false, byMax: true);
            PhProp("Mallet", "wooden_hammer_01", 0.32f, solid: false, byMax: true);
            PhProp("Vice", "bench_vice_01", 0.3f, solid: false, byMax: true);
            PhProp("Plane", "hand_plane_no4", 0.26f, solid: false, byMax: true);
            PhProp("Dagger", "ornate_medieval_dagger", 0.38f, solid: false, byMax: true);
            PhProp("Lantern", "wooden_lantern_01", 0.42f, solid: false);
            // Faz 6 ekleri (Poly Haven CC0): heykel, silah, ayna, masa üstü, raf
            PhProp("HorseStatue", "horse_statue_01", 0.9f);
            PhProp("Shield", "kite_shield", 1.05f, solid: false, wall: true);
            PhProp("Sword", "antique_estoc", 1.25f, solid: false);
            PhProp("WarHammer", "ornate_war_hammer", 0.72f, solid: false);
            PhProp("Mace", "ornate_medieval_mace", 0.63f, solid: false);
            PhProp("Mirror", "ornate_mirror_01", 1.1f, solid: false, wall: true);
            PhProp("Magnifier", "magnifying_glass_01", 0.27f, solid: false, byMax: true, euler: new Vector3(90, 0, 0));
            PhProp("Compass", "seadogs_compass", 0.09f, solid: false);
            PhProp("OilLamp", "vintage_oil_lamp", 0.45f, solid: false);
            PhProp("CandlestickWood", "wooden_candlestick", 0.22f, solid: false);
            PhProp("Jug", "jug_01", 0.22f, solid: false);
            PhProp("VaseCeramic", "antique_ceramic_vase_01", 0.45f, solid: false);
            PhProp("Plate", "carved_wooden_plate", 0.04f, solid: false);
            PhProp("FrameStanding", "standing_picture_frame_01", 0.25f, solid: false);
            PhProp("Basket", "wicker_basket_02", 0.24f);
            PhProp("Axe", "wooden_axe", 0.65f, solid: false);
            PhProp("RoundTable", "round_wooden_table_01", 0.8f);
            PhProp("DisplayShelf", "wooden_display_shelves_01", 1.6f);
            PhProp("FirePit", "stone_fire_pit", 0.36f);
            PhProp("LanternChandelier", "lantern_chandelier_01", 0.9f, solid: false);
            PhProp("Barrel2", "Barrel_01", 0.9f);
            PhProp("HorseHead", "horse_head", 0.45f, solid: false);
            PhProp("PaintingPortrait", "hanging_picture_frame_03", 1.0f, solid: false, wall: true);

            // --- Manuel (CC-BY) modeller ---
            ManualProp("StatueAngel", "statue", 1.9f, 0.3f, 0f);
            ManualProp("Amphora", "amphora", 0.75f, 0.35f, 0f);
            ManualProp("Armillary", "armillary", 0.62f, 0.55f, 0.6f, solid: false);
            ManualProp("Bell", "bell", 1.1f, 0.45f, 0.6f);
            {
                string dir = ManualRoot + "chandelier/";
                var cm = MakeMat("P_chandelier", Color.white);
                SetMaps(cm, Tex(dir + "textures/Chandelier_SHD_albedo.jpg", false), Tex(dir + "textures/Chandelier_SHD_normal.png", true), 0.4f, 1f, 0.5f);
                var ch = ModelProp("ChandelierOrnate", dir + "source/Chandelier asset.fbx", dir + "textures", 1.5f, false, false, -1, cm, save: false);
                var srcMats = AssetDatabase.LoadAssetAtPath<GameObject>(dir + "source/Chandelier asset.fbx").GetComponentInChildren<Renderer>().sharedMaterials;
                foreach (var r in ch.GetComponentsInChildren<Renderer>())
                {
                    var ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length && i < srcMats.Length; i++) if (srcMats[i] != null && srcMats[i].name.ToLowerInvariant().Contains("fire")) ms[i] = K.flameCard;
                    r.sharedMaterials = ms;
                }
                SaveKit(ch, true, 0.01f);
            }

            // --- Mumlar / aydınlatma gövdeleri ---
            {
                var go = new GameObject("Candle");
                Prim("Dish", go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0), new Vector3(0.09f, 0.006f, 0.09f), K.brass);
                KCandle(go.transform, new Vector3(0, 0.012f, 0), 0.15f, 0.018f);
                SaveKit(go, false);

                go = new GameObject("CandleCluster");
                Prim("Dish", go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0), new Vector3(0.2f, 0.006f, 0.2f), K.brass);
                KCandle(go.transform, new Vector3(-0.04f, 0.012f, 0.02f), 0.19f, 0.02f);
                KCandle(go.transform, new Vector3(0.045f, 0.012f, 0.03f), 0.12f, 0.022f);
                KCandle(go.transform, new Vector3(0.005f, 0.012f, -0.05f), 0.08f, 0.024f, false);
                SaveKit(go, false);

                Candelabra("Candelabra", 0.44f);
                Candelabra("CandelabraFloor", 1.35f);
                {   // tek mumlu torna ahşap şamdan
                    var cs = new GameObject("Candlestick");
                    MeshObj("Stem", cs.transform, Vector3.zero, LatheMesh("kit_candlestick", new[]
                    {
                        new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.06f, 0.015f), new Vector2(0.025f, 0.04f), new Vector2(0.03f, 0.08f),
                        new Vector2(0.014f, 0.12f), new Vector2(0.022f, 0.19f), new Vector2(0.014f, 0.24f), new Vector2(0.04f, 0.26f), new Vector2(0f, 0.26f),
                    }, 10), K.furniture);
                    KCandle(cs.transform, new Vector3(0, 0.26f, 0), 0.13f, 0.017f);
                    SaveKit(cs, false);
                }

                go = new GameObject("WallSconce"); // +Z: odaya doğru; orijin duvar yüzeyi
                Slab("Plate", go.transform, new Vector3(0, 0, 0.012f), new Vector3(0.12f, 0.22f, 0.024f), K.iron, 1f, false);
                Slab("Arm", go.transform, new Vector3(0, -0.04f, 0.1f), new Vector3(0.02f, 0.02f, 0.18f), K.iron, 1f, false);
                Slab("Bar", go.transform, new Vector3(0, -0.04f, 0.18f), new Vector3(0.24f, 0.018f, 0.018f), K.iron, 1f, false);
                foreach (float x in new[] { -0.11f, 0.11f })
                {
                    Prim("Cup", go.transform, PrimitiveType.Cylinder, new Vector3(x, -0.024f, 0.18f), new Vector3(0.05f, 0.008f, 0.05f), K.iron);
                    KCandle(go.transform, new Vector3(x, -0.016f, 0.18f), x < 0 ? 0.13f : 0.1f);
                }
                SaveKit(go, false, 0.02f, true, true);

                go = new GameObject("ChandelierIron"); // orijin: tavan bağlantısı; çember 1.7 m aşağıda
                var t = go.transform;
                const float drop = 1.7f;
                Rod("Chain", t, Vector3.zero, new Vector3(0, -drop + 0.75f, 0), 0.025f, K.iron);
                MeshObj("Ring", t, new Vector3(0, -drop, 0), RingMesh(0.8f, 0.05f, 0.07f), K.iron);
                MeshObj("RingTop", t, new Vector3(0, -drop + 0.32f, 0), RingMesh(0.42f, 0.04f, 0.06f), K.iron);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * 0.5f + 0.4f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    Rod("Stay" + i, t, new Vector3(0, -drop + 0.75f, 0), d * 0.77f + new Vector3(0, -drop + 0.06f, 0), 0.016f, K.iron);
                    Rod("Spoke" + i, t, new Vector3(0, -drop + 0.03f, 0), d * 0.77f + new Vector3(0, -drop + 0.03f, 0), 0.02f, K.iron);
                }
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI * 2f / 10f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    KCandle(t, d * 0.775f + new Vector3(0, -drop + 0.07f, 0), 0.13f + 0.03f * Hash01(i, 3), 0.02f);
                }
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f + 0.3f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    KCandle(t, d * 0.4f + new Vector3(0, -drop + 0.38f, 0), 0.12f + 0.03f * Hash01(i, 8), 0.02f);
                }
                SaveKit(go, false, 0.01f);
            }

            // --- Kitap / kağıt ---
            {
                var leathers = new[] { K.leather, K.leatherRed, K.leatherGreen, K.leather };
                var go = new GameObject("BookStack");
                float y = 0f;
                for (int i = 0; i < 4; i++)
                {
                    var s = i % 2 == 0 ? new Vector3(0.17f, 0.04f, 0.24f) : new Vector3(0.15f, 0.03f, 0.21f);
                    var b = Slab("Book" + i, go.transform, new Vector3(0.01f * (i % 2), y + s.y * 0.5f, 0), s, leathers[i], 0.4f, false);
                    b.transform.localRotation = Quaternion.Euler(0, new[] { 4f, -11f, 16f, -3f }[i], 0);
                    y += s.y;
                }
                SaveKit(go, false);

                go = new GameObject("BookRow");
                float x = -0.14f;
                for (int i = 0; i < 7; i++)
                {
                    var s = i % 3 == 0 ? new Vector3(0.045f, 0.25f, 0.17f) : i % 3 == 1 ? new Vector3(0.03f, 0.21f, 0.15f) : new Vector3(0.04f, 0.23f, 0.16f);
                    var b = Slab("Book" + i, go.transform, new Vector3(x + s.x * 0.5f, s.y * 0.5f, 0), s, leathers[(i * 3 + 1) % 4], 0.4f, false);
                    if (i == 6) { b.transform.localRotation = Quaternion.Euler(0, 0, -14f); b.transform.localPosition += new Vector3(0.03f, -0.004f, 0); }
                    x += s.x + 0.002f;
                }
                SaveKit(go, false);

                var flat = Quaternion.Euler(90, 0, 0);
                var papers = new[] { K.paperA, K.paperB, K.paperC };
                for (int i = 0; i < 3; i++)
                {
                    go = new GameObject("Paper" + "ABC"[i]);
                    QuadObj("Sheet", go.transform, new Vector3(0, 0.003f, 0), new Vector2(0.21f, 0.28f), papers[i], flat);
                    SaveKit(go, false, 0.012f);
                }
                go = new GameObject("PaperPile");
                for (int i = 0; i < 4; i++)
                    QuadObj("Sheet" + i, go.transform, new Vector3(0.03f * (i - 1.5f), 0.003f + 0.002f * i, 0.02f * (i % 2)), new Vector2(0.21f, 0.28f),
                            papers[(i + 2) % 3], Quaternion.Euler(90, new[] { 8f, -17f, 31f, -6f }[i], 0));
                var scroll = Prim("Scroll", go.transform, PrimitiveType.Cylinder, new Vector3(0.2f, 0.022f, 0.03f), new Vector3(0.044f, 0.13f, 0.044f), K.paperC);
                scroll.transform.localRotation = Quaternion.Euler(90, 24, 0);
                SaveKit(go, false, 0.012f);

                go = new GameObject("PaperScatter"); // zemine saçılmış
                for (int i = 0; i < 6; i++)
                    QuadObj("Sheet" + i, go.transform, new Vector3(Hash01(i, 1) * 1.4f - 0.7f, 0.004f + 0.001f * i, Hash01(i, 2) * 1.0f - 0.5f), new Vector2(0.21f, 0.28f),
                            papers[i % 3], Quaternion.Euler(90, Hash01(i, 3) * 360f, 0));
                SaveKit(go, false, 0.012f);
            }

            // --- Kumaş / duvar / zemin dekoru ---
            {
                var go = new GameObject("Cobweb"); // köşe ağı: orijin köşe, +X ve +Y yönünde açılır
                QuadObj("Web", go.transform, new Vector3(0.6f, 0.6f, 0), new Vector2(1.2f, 1.2f), K.cobweb);
                SaveKit(go, false, 0.03f);

                go = new GameObject("Banner"); // orijin: üst orta, duvar yüzeyi; +Z odaya
                var rod = Prim("Rod", go.transform, PrimitiveType.Cylinder, new Vector3(0, 0, 0.05f), new Vector3(0.03f, 0.48f, 0.03f), K.iron);
                rod.transform.localRotation = Quaternion.Euler(0, 0, 90);
                QuadObj("Cloth", go.transform, new Vector3(0, -0.85f, 0.045f), new Vector2(0.85f, 1.7f), K.banner, Quaternion.Euler(0, 180, 0));
                SaveKit(go, false, 0.01f, true, true);

                go = new GameObject("Rug"); // 3.4 x 1.7 m
                QuadObj("Rug", go.transform, new Vector3(0, 0.012f, 0), new Vector2(3.4f, 1.7f), K.rug, Quaternion.Euler(90, 0, 0));
                SaveKit(go, false, 0.01f);

                go = new GameObject("TableCloth"); // masa kenarından sarkan bordo örtü (0.9 m genişlik)
                Slab("Top", go.transform, new Vector3(0, 0.006f, 0), new Vector3(0.9f, 0.012f, 0.62f), K.velvet, 0.6f, false);
                Slab("Drop", go.transform, new Vector3(0, -0.17f, 0.316f), new Vector3(0.9f, 0.36f, 0.012f), K.velvet, 0.6f, false);
                SaveKit(go, false);

                go = new GameObject("Curtain"); // orijin: üst orta; 1.1 m genişlik, 3 m boy
                for (int i = 0; i < 5; i++)
                {
                    var f = Slab("Fold" + i, go.transform, new Vector3(-0.44f + i * 0.22f, -1.5f, 0.06f + 0.03f * (i % 2)), new Vector3(0.24f, 3f, 0.05f), K.velvetPurple, 0.8f, false);
                    f.transform.localRotation = Quaternion.Euler(0, i % 2 == 0 ? 18f : -18f, 0);
                }
                Slab("Pelmet", go.transform, new Vector3(0, -0.08f, 0.09f), new Vector3(1.24f, 0.2f, 0.14f), K.velvetPurple, 0.8f, false);
                SaveKit(go, true, 0.01f, true, true);
            }

            // --- Pencereler (duvar açıklığı gerektirmez: duvar yüzeyine oturan ışıyan vitray + ay huzmesi + baked ay spotu) ---
            {
                var go = new GameObject("WindowLancet"); // orijin: pencere tabanı orta, duvar yüzeyi; +Z odaya. 1.3 x 2.6 m
                QuadObj("Glass", go.transform, new Vector3(0, 1.3f, 0.02f), new Vector2(1.3f, 2.6f), K.vitray, Quaternion.Euler(0, 180, 0));
                Slab("Sill", go.transform, new Vector3(0, -0.04f, 0.09f), new Vector3(1.5f, 0.09f, 0.22f), K.castleStone, 1f, false);
                AddMoon(go.transform, new Vector3(0, 1.5f, 0.05f), 1.0f, 4.6f);
                SaveKit(go, true, 0f);

                go = new GameObject("WindowRose"); // orijin: merkez, duvar yüzeyi; çap 2 m
                QuadObj("Glass", go.transform, new Vector3(0, 0, 0.02f), new Vector2(2f, 2f), K.rose, Quaternion.Euler(0, 180, 0));
                AddMoon(go.transform, new Vector3(0, 0, 0.05f), 1.5f, 5.5f);
                SaveKit(go, true, 0f);
            }

            // --- Hazır kompozisyonlar ---
            {
                var go = new GameObject("StudyTable"); // masa + mektuplar + mum + kitap + kadeh
                Kit("Table", go.transform, Vector3.zero);
                float h = 0.82f;
                Kit("PaperPile", go.transform, new Vector3(-0.15f, h, 0.02f), 12f);
                Kit("CandleCluster", go.transform, new Vector3(0.42f, h, 0.12f));
                Kit("BookStack", go.transform, new Vector3(-0.52f, h, -0.08f), 30f);
                Kit("Goblet1", go.transform, new Vector3(0.3f, h, -0.16f));
                SaveKit(go, true, 0f);

                go = new GameObject("Workbench"); // tezgah + aletler + mengene + notlar (fener ayrı: L_Lantern)
                Kit("Table", go.transform, Vector3.zero);
                Kit("Vice", go.transform, new Vector3(-0.55f, h, 0.1f), 90f);
                Kit("Hammer", go.transform, new Vector3(-0.15f, h, -0.1f), 35f);
                Kit("Wrench", go.transform, new Vector3(0.1f, h, 0.12f), -20f);
                Kit("Mallet", go.transform, new Vector3(0.38f, h, -0.14f), 70f);
                Kit("Plane", go.transform, new Vector3(0.55f, h, 0.14f), 10f);
                Kit("PaperB", go.transform, new Vector3(0.1f, h, -0.18f), -8f);
                SaveKit(go, true, 0f);

                go = new GameObject("CrateStack");
                Kit("Crate", go.transform, Vector3.zero, 8f);
                Kit("Crate", go.transform, new Vector3(0.7f, 0, 0.1f), -14f);
                Kit("CrateLong", go.transform, new Vector3(0.32f, 0.6f, 0.02f), 22f);
                Kit("Bucket", go.transform, new Vector3(-0.62f, 0, 0.3f));
                SaveKit(go, true, 0f);
            }

            // --- Işık profili (StyleGuide "Işık") ---
            SaveKit(LightGo("L_Candle", LightType.Point, Pal.Candle, 1.3f, 4.5f, true), false, 0f, false);
            SaveKit(LightGo("L_Candelabra", LightType.Point, Pal.Candle, 2.2f, 7f, true), false, 0f, false);
            SaveKit(LightGo("L_Chandelier", LightType.Point, Pal.Candle, 3.5f, 10f, true), false, 0f, false);
            {
                var go = new GameObject("L_Lantern"); // gerçek zamanlı, titreyen fener (oda başına en fazla 2 realtime ışık)
                Kit("Lantern", go.transform, Vector3.zero);
                var l = LightGo("Light", LightType.Point, Pal.Lantern, 2.2f, 5f, false);
                l.transform.SetParent(go.transform, false);
                l.transform.localPosition = new Vector3(0, 0.3f, 0);
                var fl = go.AddComponent<LightFlicker>();
                var so = new SerializedObject(fl);
                SetRef(so, "target", l.GetComponent<Light>());
                SetFlt(so, "amount", 0.15f);
                so.ApplyModifiedPropertiesWithoutUndo();
                SaveKit(go, false, 0f);
            }
        }

        static void Candelabra(string name, float stemH)
        {
            var go = new GameObject(name);
            float k = stemH - 0.44f; // gövde uzaması
            MeshObj("Stem", go.transform, Vector3.zero, LatheMesh("kit_" + name.ToLowerInvariant(), new[]
            {
                new Vector2(0f, 0f), new Vector2(0.1f + k * 0.06f, 0f), new Vector2(0.1f + k * 0.06f, 0.012f), new Vector2(0.035f, 0.045f + k * 0.05f), new Vector2(0.02f, 0.09f + k * 0.1f),
                new Vector2(0.034f, 0.13f + k * 0.5f), new Vector2(0.016f, 0.17f + k * 0.55f), new Vector2(0.016f, 0.42f + k), new Vector2(0.03f, 0.44f + k), new Vector2(0f, 0.44f + k),
            }, 10), K.brass);
            Slab("Arm", go.transform, new Vector3(0, 0.37f + k, 0), new Vector3(0.38f, 0.016f, 0.016f), K.brass, 1f, false);
            foreach (float x in new[] { -0.18f, 0f, 0.18f })
            {
                float y = (x == 0f ? 0.44f : 0.378f) + k;
                if (x != 0f) Slab("Up", go.transform, new Vector3(x, y + 0.02f, 0), new Vector3(0.014f, 0.05f, 0.014f), K.brass, 1f, false);
                Prim("Cup", go.transform, PrimitiveType.Cylinder, new Vector3(x, y + 0.05f, 0), new Vector3(0.05f, 0.008f, 0.05f), K.brass);
                KCandle(go.transform, new Vector3(x, y + 0.058f, 0), x == 0f ? 0.16f : 0.12f);
            }
            SaveKit(go, false);
        }

        static GameObject LightGo(string name, LightType type, Color color, float intensity, float range, bool baked)
        {
            var go = new GameObject(name);
            var l = go.AddComponent<Light>();
            l.type = type; l.color = color; l.intensity = intensity; l.range = range;
            l.lightmapBakeType = baked ? LightmapBakeType.Baked : LightmapBakeType.Realtime;
            l.shadows = baked ? LightShadows.Soft : LightShadows.None;
            return go;
        }

        /// <summary>Pencere prefabına ay: iki çapraz huzme kartı + aşağı eğimli baked spot.</summary>
        static void AddMoon(Transform win, Vector3 from, float width, float length)
        {
            var d = new Vector3(0, -Mathf.Sin(38f * Mathf.Deg2Rad), Mathf.Cos(38f * Mathf.Deg2Rad));
            var mid = from + d * (length * 0.5f);
            QuadObj("Shaft", win, mid, new Vector2(width, length), K.shaft, Quaternion.LookRotation(Vector3.Cross(Vector3.right, d), -d));
            QuadObj("ShaftB", win, mid, new Vector2(width * 0.7f, length), K.shaft, Quaternion.LookRotation(Vector3.right, -d));
            var l = LightGo("Moon", LightType.Spot, Pal.Moon, 26f, 12f, true);
            l.GetComponent<Light>().spotAngle = 48f; l.GetComponent<Light>().innerSpotAngle = 30f;
            l.transform.SetParent(win, false);
            l.transform.localPosition = from + new Vector3(0, 0.3f, 0.2f);
            l.transform.localRotation = Quaternion.LookRotation(d);
        }

        // ---------- Mimari yardımcıları ----------

        /// <summary>
        /// Ahşap lambri: duvar yüzeyinin önüne panel + süpürgelik + üst pervaz. face: yüzey üzerindeki taban merkezi, inward: odaya bakan yön.
        /// Açıklıklar (kapı) atlanır.
        /// </summary>
        static void Wainscot(Transform root, string name, Vector3 face, bool alongX, float len, Vector3 inward, float h = 1.5f, params Opening[] ops)
        {
            Vector3 Along(float a) => alongX ? new Vector3(a, 0, 0) : new Vector3(0, 0, a);
            Vector3 Size(float l, float y, float t) => alongX ? new Vector3(l, y, t) : new Vector3(t, y, l);
            void Seg(int i, float a0, float a1)
            {
                float l = a1 - a0; if (l < 0.05f) return;
                var c = face + Along((a0 + a1) * 0.5f);
                Slab(name + "_Panel" + i, root, c + inward * 0.02f + Vector3.up * (h * 0.5f), Size(l, h, 0.04f), K.panel, 2.2f, false);
                Slab(name + "_Base" + i, root, c + inward * 0.05f + Vector3.up * 0.09f, Size(l, 0.18f, 0.03f), K.beam, 1.2f, false);
                Slab(name + "_Rail" + i, root, c + inward * 0.055f + Vector3.up * (h + 0.03f), Size(l, 0.07f, 0.11f), K.beam, 1.2f, false);
            }
            var sorted = new List<Opening>(ops);
            sorted.Sort((x, y) => x.c.CompareTo(y.c));
            float cur = -len * 0.5f; int n = 0;
            foreach (var o in sorted)
            {
                if (o.bottom > h) continue;
                Seg(n++, cur, o.c - o.w * 0.5f);
                cur = o.c + o.w * 0.5f;
            }
            Seg(n, cur, len * 0.5f);
        }

        /// <summary>Kirişli ahşap tavan: tahta döşeme + z boyunca kirişler + iki ana kiriş. y: tavanın alt yüzü.</summary>
        static void BeamCeiling(Transform root, string name, Vector3 center, float sx, float sz, float spacing = 1.5f)
        {
            Deck(root, name + "_Planks", center + Vector3.up * 0.08f, sx, sz, 0.16f, K.beam, Mathf.Max(1, Mathf.RoundToInt(sx / 6f)), Mathf.Max(1, Mathf.RoundToInt(sz / 6f)));
            int n = Mathf.Max(2, Mathf.RoundToInt(sx / spacing));
            for (int i = 0; i < n; i++)
            {
                float x = -sx * 0.5f + sx * (i + 0.5f) / n;
                Slab(name + "_Joist" + i, root, center + new Vector3(x, -0.1f, 0), new Vector3(0.16f, 0.2f, sz), K.beam, 1.2f, false);
            }
            foreach (float z in new[] { -sz * 0.25f, sz * 0.25f })
                Slab(name + "_Main" + z, root, center + new Vector3(0, -0.34f, z), new Vector3(sx, 0.3f, 0.26f), K.beam, 1.2f, false);
        }

        /// <summary>Oda içine light probe ızgarası (dekor ve oyuncular baked ışığı buradan alır). size: odanın iç ölçüsü; ızgara zemine/duvara 20-25 cm kalır (gövdenin dışında kalan nesne kararır).</summary>
        static void KitProbes(Transform root, Vector3 center, Vector3 size, int nx, int ny, int nz, params Bounds[] skip)
        {
            var go = new GameObject("LightProbes");
            go.transform.SetParent(root, false);
            go.transform.localPosition = center;
            var pts = new List<Vector3>();
            for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++) for (int k = 0; k < nz; k++)
            {
                var pt = new Vector3((size.x - 0.5f) * (i / (nx - 1f) - 0.5f), (size.y - 0.4f) * (j / (ny - 1f) - 0.5f), (size.z - 0.5f) * (k / (nz - 1f) - 0.5f));
                bool blocked = false; // dolu hacmin (merdiven gövdesi vb.) içindeki probe simsiyah pişer
                foreach (var b in skip) if (b.Contains(center + pt)) blocked = true;
                if (!blocked) pts.Add(pt);
            }
            go.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
        }

        /// <summary>Oda başına tek baked yansıma probu (ıslak zemin/metal parlaması; gerçek zamanlı yansıma yok).</summary>
        static void KitReflection(Transform root, Vector3 center, Vector3 size)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.SetParent(root, false);
            go.transform.localPosition = center;
            var rp = go.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Baked;
            rp.size = size; rp.resolution = 64; rp.hdr = true; rp.boxProjection = false;
            rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black;
        }

        // ---------- Bake ----------

        static LightingSettings KitLightingSettings()
        {
            string path = KitLightDir + "/KitLighting.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, path); }
            ls.bakedGI = true; ls.realtimeGI = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            ls.mixedBakeMode = MixedLightingMode.Subtractive;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.lightmapResolution = 8f; ls.lightmapMaxSize = 1024; ls.lightmapPadding = 2;
            ls.directSampleCount = 24; ls.indirectSampleCount = 128; ls.environmentSampleCount = 32;
            ls.maxBounces = 2;
            ls.ao = true; ls.aoMaxDistance = 0.8f;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            EditorUtility.SetDirty(ls);
            return ls;
        }

        /// <summary>Açık (kaydedilmiş) sahnenin ışığını pişirir: lightmap + light probe + reflection probe.</summary>
        static void KitBake()
        {
            Lightmapping.lightingSettings = KitLightingSettings();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = Lightmapping.Bake();
            var lp = LightmapSettings.lightProbes;
            LightProbes.GetInterpolatedProbe(new Vector3(0, 1.5f, 0), null, out var sh);
            Debug.Log("[CoopSetup] Probes: " + (lp != null ? lp.count : -1) + " sh0=" + sh[0, 0].ToString("0.000") + "," + sh[1, 0].ToString("0.000") + "," + sh[2, 0].ToString("0.000"));
            Debug.Log("[CoopSetup] Bake " + (ok ? "tamam" : "BAŞARISIZ") + " " + sw.Elapsed.TotalSeconds.ToString("0") + " sn, lightmap: " + LightmapSettings.lightmaps.Length);
        }

        // ---------- Sergi odası (kit doğrulama) ----------

        /// <summary>Batch: -executeMethod CoopGame.EditorTools.CoopSetup.KitShots -shotDir klasör [-noBake]</summary>
        public static void KitShots()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-shotsOnly") < 0)
            {
                SetupUrp();
                PrepareArt();
                PrepareKit();
                BuildShowroom();
                if (System.Array.IndexOf(args, "-noBake") < 0) { KitBake(); EditorSceneManager.SaveOpenScenes(); }
            }
            EditorSceneManager.OpenScene(KitShowroomPath);
            LightProbes.Tetrahedralize();
            string dir = Arg("-shotDir") ?? "Temp/Shots";
            Directory.CreateDirectory(dir);
            Shot(dir + "/kit_wide.png", new Vector3(0, 1.75f, -4.1f), new Vector3(0, 1.5f, 4f), false);
            Shot(dir + "/kit_left.png", new Vector3(-1.2f, 1.7f, -2.6f), new Vector3(-5.5f, 1.0f, 3.5f), false);
            Shot(dir + "/kit_right.png", new Vector3(1.2f, 1.7f, -2.6f), new Vector3(5.5f, 1.0f, 3.5f), false);
            Shot(dir + "/kit_floor.png", new Vector3(0f, 1.5f, -3.6f), new Vector3(0f, 0.3f, -0.8f), false);
            Debug.Log("[CoopSetup] KitShots -> " + Path.GetFullPath(dir));
        }

        static void BuildShowroom()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.fieldOfView = 68f;
            ApplyAtmosphere(cam, Pal.AmbCastle * 0.6f, Pal.FogCastle, 0.018f);

            var root = new GameObject("Showroom").transform;
            const float SX = 14f, SZ = 9f, H = 4.4f;
            Deck(root, "Floor", new Vector3(0, -0.1f, 0), SX, SZ, 0.2f, K.parquet, 2, 2);
            Wall(root, "WallN", new Vector3(0, 0, SZ * 0.5f + 0.25f), true, SX + 1f, H, K.plaster, 3.5f);
            Wall(root, "WallS", new Vector3(0, 0, -SZ * 0.5f - 0.25f), true, SX + 1f, H, K.plaster, 3.5f);
            Wall(root, "WallE", new Vector3(SX * 0.5f + 0.25f, 0, 0), false, SZ, H, K.plaster, 3.5f);
            Wall(root, "WallW", new Vector3(-SX * 0.5f - 0.25f, 0, 0), false, SZ, H, K.plaster, 3.5f);
            Wainscot(root, "WainN", new Vector3(0, 0, SZ * 0.5f), true, SX, Vector3.back);
            Wainscot(root, "WainE", new Vector3(SX * 0.5f, 0, 0), false, SZ, Vector3.left);
            Wainscot(root, "WainW", new Vector3(-SX * 0.5f, 0, 0), false, SZ, Vector3.right);
            BeamCeiling(root, "Ceil", new Vector3(0, H, 0), SX, SZ);

            float zb = SZ * 0.5f - 0.5f;
            // duvar: pencereler, flamalar, tablolar, aplikler
            Kit("WindowLancet", root, new Vector3(-3.4f, 1.55f, SZ * 0.5f), 180f);
            Kit("WindowLancet", root, new Vector3(3.4f, 1.55f, SZ * 0.5f), 180f);
            KitR("WindowRose", root, new Vector3(SX * 0.5f, 2.9f, 0.5f), Quaternion.Euler(0, -90, 0));
            Kit("Banner", root, new Vector3(-1.2f, 3.9f, SZ * 0.5f), 180f);
            Kit("Banner", root, new Vector3(1.2f, 3.9f, SZ * 0.5f), 180f);
            KitR("PaintingLandscape", root, new Vector3(-SX * 0.5f + 0.06f, 2.0f, 1.5f), Quaternion.Euler(0, 90, 0));
            KitR("PaintingKeep", root, new Vector3(-SX * 0.5f + 0.06f, 2.1f, -0.6f), Quaternion.Euler(0, 90, 0));
            KitR("WallSconce", root, new Vector3(-SX * 0.5f + 0.04f, 2.2f, 3.0f), Quaternion.Euler(0, 90, 0));
            Kit("L_Candle", root, new Vector3(-SX * 0.5f + 0.6f, 2.4f, 3.0f));
            Kit("WallSconce", root, new Vector3(0, 2.3f, SZ * 0.5f - 0.04f), 180f);
            Kit("L_Candle", root, new Vector3(0, 2.5f, SZ * 0.5f - 0.6f));
            KitR("Curtain", root, new Vector3(SX * 0.5f - 0.05f, 3.6f, 3.4f), Quaternion.Euler(0, -90, 0));
            KitR("Cobweb", root, new Vector3(-SX * 0.5f + 0.02f, H - 0.35f, SZ * 0.5f - 0.03f), Quaternion.Euler(0, 0, -90), 1.3f);
            KitR("Cobweb", root, new Vector3(SX * 0.5f - 0.03f, H - 0.35f, SZ * 0.5f - 0.03f), Quaternion.Euler(0, 0, 180), 1.1f);
            // arka sıra
            Kit("Cabinet", root, new Vector3(-6.2f, 0, zb - 0.1f), 180f);
            Kit("Bookshelf", root, new Vector3(-5.0f, 0, zb), 180f);
            Kit("GrandfatherClock", root, new Vector3(-1.9f, 0, zb), 180f);
            Kit("Commode", root, new Vector3(-0.1f, 0, zb), 180f);
            Kit("Console", root, new Vector3(1.8f, 0, zb), 180f);
            Kit("Drawer", root, new Vector3(5.2f, 0, zb), 180f);
            Kit("StatueAngel", root, new Vector3(6.3f, 0, zb - 0.4f), 200f);
            Kit("Candelabra", root, new Vector3(-0.1f, 0.95f, zb));
            Kit("L_Candelabra", root, new Vector3(-0.1f, 1.7f, zb - 0.5f));
            Kit("Armillary", root, new Vector3(1.8f, 0.85f, zb));
            Kit("BookRow", root, new Vector3(5.2f, 0.85f, zb), 180f);
            Kit("Vase", root, new Vector3(4.7f, 0.85f, zb));
            // orta sıra
            Kit("StudyTable", root, new Vector3(-4.6f, 0, 1.3f), 180f);
            Kit("Armchair", root, new Vector3(-3.0f, 0, 1.4f), 150f);
            Kit("Chest", root, new Vector3(-1.4f, 0, 1.6f), 170f);
            Kit("Barrel", root, new Vector3(0.1f, 0, 1.8f));
            Kit("BarrelLying", root, new Vector3(1.4f, 0, 1.8f), 90f);
            Kit("CrateStack", root, new Vector3(2.9f, 0, 1.9f), 180f);
            Kit("Workbench", root, new Vector3(4.9f, 0, 1.3f), 180f);
            Kit("L_Lantern", root, new Vector3(4.3f, 0.82f, 1.45f));
            Kit("Ladder", root, new Vector3(6.5f, 0, 1.6f), 90f);
            // ön sıra
            Kit("Rug", root, new Vector3(0, 0, -0.9f));
            Kit("Amphora", root, new Vector3(-5.4f, 0, -0.6f));
            Kit("Bell", root, new Vector3(-4.0f, 0, -0.5f));
            Kit("Bust", root, new Vector3(-2.8f, 0, -0.6f), 170f);
            Kit("StatueKing", root, new Vector3(-6.4f, 0, 0.3f), 140f);
            Kit("Stool", root, new Vector3(-1.6f, 0, -0.5f));
            Kit("Bucket", root, new Vector3(-0.8f, 0, -0.4f));
            Kit("CandelabraFloor", root, new Vector3(0, 0, -0.5f), 20f);
            Kit("L_Candelabra", root, new Vector3(0, 1.75f, -0.9f));
            Kit("BookStack", root, new Vector3(0.9f, 0, -0.5f), 15f);
            Kit("PaperScatter", root, new Vector3(1.9f, 0, -0.9f));
            for (int i = 0; i < 3; i++) Kit("Goblet" + i, root, new Vector3(2.7f + 0.2f * i, 0, -0.4f));
            for (int i = 0; i < 4; i++) Kit("Bottle" + i, root, new Vector3(3.5f + 0.18f * i, 0, -0.4f));
            for (int i = 0; i < 3; i++) Kit("BrassCandleholder" + i, root, new Vector3(4.4f + 0.3f * i, 0, -0.4f));
            Kit("Candlestick", root, new Vector3(5.4f, 0, -0.4f));
            Kit("Pot", root, new Vector3(5.9f, 0, -0.4f));
            Kit("Bowl", root, new Vector3(6.3f, 0, -0.4f));
            Kit("Dagger", root, new Vector3(6.3f, 0.02f, -1.0f));
            Kit("Candle", root, new Vector3(-2.2f, 0, -1.2f));
            // avizeler
            Kit("ChandelierIron", root, new Vector3(-2.6f, H - 0.5f, 0.4f));
            Kit("L_Chandelier", root, new Vector3(-2.6f, H - 2.6f, 0.4f));
            Kit("ChandelierOrnate", root, new Vector3(2.6f, H - 2.2f, 0.4f));
            Kit("L_Chandelier", root, new Vector3(2.6f, H - 2.6f, 0.4f));

            KitProbes(root, new Vector3(0, H * 0.5f, 0), new Vector3(SX, H, SZ), 7, 3, 5);
            KitReflection(root, new Vector3(0, 1.6f, 0), new Vector3(SX, H, SZ));
            EditorSceneManager.SaveScene(scene, KitShowroomPath);
        }
    }
}
