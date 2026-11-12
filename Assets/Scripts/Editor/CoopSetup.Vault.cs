using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// O'nun 2. odası: Hazine Kasası (ref: Docs/Reference/kasa.png). Sekizgen rotunda (iç açıklık 18 m), kaburgalı kubbe + ay ışıklı
    /// oculus (tepe ~13.4), 4.2 m'de ahşap galeri halkası, ortada serbest döner merdiven. Alt duvar kesme taş + kemerli hazine nişleri,
    /// güneyde parmaklıklı hazine bölmesi, üst duvar meşe panel. Doğu: zindanın yuvarlak kasa kapısı; batı: mahzen geçidi.
    /// Kök "Vault": zindan yerel (-17.5, 0, 0). Mahzen + sarnıç bu oda için OWestShift kadar batıya kaydırıldı.
    /// Hikâye: zorlanmış sandık + mahzene uzanan sikke izi, boş kadife kaide (kayıp emanet), açık hesap defteri, hırsızın kemikleri,
    /// galeride hazinedarın yarım mektubu.
    /// </summary>
    public static partial class CoopSetup
    {
        const float OWestShift = -14f;               // mahzen, sarnıç ve geçitleri (zindan yerel x) bu kadar batıya
        const float VtA = 9f, VtWallH = 8.6f, VtRise = 4.8f, VtGal = 4.2f, VtGalD = 2.2f, VtCx = -17.5f;
        static readonly float VtTan22 = Mathf.Tan(22.5f * Mathf.Deg2Rad);

        static Vector3 VtDir(int i) { float a = i * 45f * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); }
        static Vector3 VtTang(int i) { var d = VtDir(i); return new Vector3(-d.z, 0, d.x); }
        static float YawIn(Vector3 inward) => Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
        /// <summary>Yan i'nin iç yüzeyinde nokta: u yan boyunca, y yükseklik, off yüzeyden odaya (negatif: duvarın içine/niş).</summary>
        static Vector3 VtFace(int i, float u, float y, float off) => VtDir(i) * (VtA - off - (i == 0 ? 0.08f : 0f)) + VtTang(i) * u + Vector3.up * y; // doğu: zindan duvarı önünde 8 cm kaplama
        static Vector3 VtCorner(int c, float r, float y) { float a = (c * 45f + 22.5f) * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }

        static GameObject VtKit(string name, Transform p, int side, float u, float y, float off, float scale = 1f, float yawAdd = 0f)
            => Kit(name, p, VtFace(side, u, y, off), YawIn(-VtDir(side)) + yawAdd, scale);

        /// <summary>Aynı malzemeli çocukları tek ağa birleştirir (korkuluk çubukları, sikkeler: çizim çağrısı az).</summary>
        static GameObject CombineChildren(Transform holder, string meshName, Material mat)
        {
            var list = new List<CombineInstance>();
            var w2l = holder.worldToLocalMatrix;
            foreach (var mf in holder.GetComponentsInChildren<MeshFilter>())
                list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = w2l * mf.transform.localToWorldMatrix });
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.CombineMeshes(list.ToArray(), true, true);
            m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m);
            for (int i = holder.childCount - 1; i >= 0; i--) Object.DestroyImmediate(holder.GetChild(i).gameObject);
            holder.gameObject.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, meshName);
            holder.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return holder.gameObject;
        }

        /// <summary>Sekizgen duvar parçası (yan i). Açıklıklar u merkezli; niche=true açıklığın arkasına niş sırtı koyar.</summary>
        static void VtWall(Transform root, int i, float th, float inset, Material mat, params (float u, float w, float bottom, float top, bool niche)[] ops)
        {
            var holder = new GameObject("Wall" + i).transform;
            holder.SetParent(root, false);
            holder.localPosition = VtDir(i) * (VtA - inset + th * 0.5f);
            holder.localRotation = Quaternion.LookRotation(-VtDir(i));
            float L = 2f * VtA * VtTan22 + 2f * th * VtTan22 + 0.02f;
            // LookRotation(-d): yerel +x = cross(up, -d) = VtTang(i), yerel +z odaya
            void Piece(float a0, float a1, float y0, float y1)
            {
                if (a1 - a0 < 0.01f || y1 - y0 < 0.01f) return;
                Slab("P", holder, new Vector3((a0 + a1) * 0.5f, (y0 + y1) * 0.5f, 0), new Vector3(a1 - a0, y1 - y0, th), mat, 2f);
            }
            var sorted = new List<(float u, float w, float bottom, float top, bool niche)>(ops);
            sorted.Sort((x, y) => x.u.CompareTo(y.u));
            float cur = -L * 0.5f;
            foreach (var o in sorted)
            {
                Piece(cur, o.u - o.w * 0.5f, 0, VtWallH);
                Piece(o.u - o.w * 0.5f, o.u + o.w * 0.5f, 0, o.bottom);
                Piece(o.u - o.w * 0.5f, o.u + o.w * 0.5f, o.top, VtWallH);
                cur = o.u + o.w * 0.5f;
                if (o.niche)
                    Slab("NicheBack", holder, new Vector3(o.u, (o.bottom + o.top) * 0.5f, -th * 0.5f - 0.05f), new Vector3(o.w + 0.2f, o.top - o.bottom + 0.2f, 0.1f), K.velvet, 1f, false); // kadife astar: eşyalar okunur
            }
            Piece(cur, L * 0.5f, 0, VtWallH);
        }

        /// <summary>Kubbe dilimi (yan i): duvar başından oculus halkasına; metre cinsinden UV, içe bakan.</summary>
        static Mesh VtDomeMesh(int i, float rOc)
        {
            const int rows = 9;
            float rc = VtA / Mathf.Cos(22.5f * Mathf.Deg2Rad);
            float tEnd = Mathf.Acos(rOc / rc);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            float a0 = (i * 45f - 22.5f) * Mathf.Deg2Rad, a1 = (i * 45f + 22.5f) * Mathf.Deg2Rad;
            float slant = 0f; Vector3 prevMid = Vector3.zero;
            for (int k = 0; k <= rows; k++)
            {
                float t = tEnd * k / rows;
                float r = rc * Mathf.Cos(t), y = VtWallH + VtRise * Mathf.Sin(t);
                var p0 = new Vector3(Mathf.Cos(a0) * r, y, Mathf.Sin(a0) * r);
                var p1 = new Vector3(Mathf.Cos(a1) * r, y, Mathf.Sin(a1) * r);
                var mid = (p0 + p1) * 0.5f;
                if (k > 0) slant += (mid - prevMid).magnitude;
                prevMid = mid;
                float half = (p1 - p0).magnitude * 0.5f;
                v.Add(p0); v.Add(p1);
                uv.Add(new Vector2(-half / 2.5f, slant / 2.5f)); uv.Add(new Vector2(half / 2.5f, slant / 2.5f));
            }
            for (int k = 0; k < rows; k++)
            {
                int b = k * 2;
                var n = new Vector3(0, VtWallH - 2f, 0) - (v[b] + v[b + 3]) * 0.5f;
                Tri(tris, v, b, b + 1, b + 3, n); Tri(tris, v, b, b + 3, b + 2, n);
            }
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m);
            return SaveMesh(m, "vault_dome_" + i);
        }

        /// <summary>Galeri döşemesi dilimi (yan i): dış yamuk kenarı duvarda, iç kenarı korkulukta. Üst yüz y=0.</summary>
        static Mesh VtGalleryMesh(int i, float th)
        {
            float ro = VtA / Mathf.Cos(22.5f * Mathf.Deg2Rad), ri = (VtA - VtGalD) / Mathf.Cos(22.5f * Mathf.Deg2Rad);
            float a0 = (i * 45f - 22.5f) * Mathf.Deg2Rad, a1 = (i * 45f + 22.5f) * Mathf.Deg2Rad;
            Vector3 P(float r, float a, float y) => new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, bool vertical)
            {
                int s = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                foreach (var p in new[] { a, b, c, d })
                    uv.Add(vertical ? new Vector2((p.x + p.z) / 1.5f, p.y / 1.5f) : new Vector2(p.x / 1.5f, p.z / 1.5f));
                Tri(tris, v, s, s + 1, s + 2, n); Tri(tris, v, s, s + 2, s + 3, n);
            }
            Vector3 o0 = P(ro, a0, 0), o1 = P(ro, a1, 0), i0 = P(ri, a0, 0), i1 = P(ri, a1, 0), dn = Vector3.down * th;
            Quad(o0, o1, i1, i0, Vector3.up, false);
            Quad(o0 + dn, o1 + dn, i1 + dn, i0 + dn, Vector3.down, false);
            Quad(i0, i1, i1 + dn, i0 + dn, -VtDir(i), true);
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m);
            return SaveMesh(m, "vault_gallery_" + i);
        }

        static void CoinStack(Transform p, Vector3 pos, float h, Material gold)
            => Prim("Coins", p, PrimitiveType.Cylinder, pos + Vector3.up * (h * 0.5f), new Vector3(0.075f, h * 0.5f, 0.075f), gold);

        static void LooseCoin(Transform p, Vector3 pos, float yaw, Material gold)
        {
            var c = Prim("Coin", p, PrimitiveType.Cylinder, pos + Vector3.up * 0.006f, new Vector3(0.07f, 0.004f, 0.07f), gold);
            c.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), yaw, Random.Range(-6f, 6f));
        }

        /// <summary>Demir kuşaklı açık hazine kutusu (içi altın yığını) ya da kapalı sağlam kasa.</summary>
        static void StrongBox(Transform p, string name, Vector3 pos, float yaw, Vector3 size, bool open, Material gold, Material wood)
        {
            var b = new GameObject(name).transform;
            b.SetParent(p, false); b.localPosition = pos; b.localRotation = Quaternion.Euler(0, yaw, 0);
            float t = 0.05f;
            if (open)
            {
                Slab("Bottom", b, new Vector3(0, t * 0.5f, 0), new Vector3(size.x, t, size.z), wood, 1f, false);
                foreach (int s in new[] { -1, 1 })
                {
                    Slab("SideX", b, new Vector3(s * (size.x - t) * 0.5f, size.y * 0.5f, 0), new Vector3(t, size.y, size.z), wood, 1f, false);
                    Slab("SideZ", b, new Vector3(0, size.y * 0.5f, s * (size.z - t) * 0.5f), new Vector3(size.x, size.y, t), wood, 1f, false);
                }
                Prim("Gold", b, PrimitiveType.Sphere, new Vector3(0, size.y * 0.82f, 0), new Vector3(size.x * 0.92f, size.y * 0.5f, size.z * 0.88f), gold);
                var lid = Slab("Lid", b, new Vector3(0, size.y + size.z * 0.42f, -size.z * 0.5f - 0.02f), new Vector3(size.x, size.z * 0.9f, t), wood, 1f, false);
                lid.transform.localRotation = Quaternion.Euler(-12f, 0, 0);
            }
            else Slab("Body", b, new Vector3(0, size.y * 0.5f, 0), size, wood, 1f, true);
            foreach (float x in new[] { -0.32f, 0.32f })
                Slab("Band", b, new Vector3(x * size.x, size.y * 0.5f, 0), new Vector3(0.05f, size.y + 0.02f, size.z + 0.02f), K.iron, 1f, false);
            Slab("Rim", b, new Vector3(0, size.y - 0.03f, 0), new Vector3(size.x + 0.02f, 0.05f, size.z + 0.02f), K.iron, 1f, false);
            if (!open) Prim("Lock", b, PrimitiveType.Cube, new Vector3(0, size.y * 0.62f, size.z * 0.5f + 0.02f), new Vector3(0.1f, 0.13f, 0.04f), K.brass);
            ProbeLit(b.gameObject);
        }

        /// <summary>Hazinedar terazisi (masada).</summary>
        static void Scales(Transform p, Vector3 pos, Material gold)
        {
            var s = new GameObject("Scales").transform;
            s.SetParent(p, false); s.localPosition = pos;
            Prim("Foot", s, PrimitiveType.Cylinder, new Vector3(0, 0.015f, 0), new Vector3(0.18f, 0.015f, 0.18f), K.brass);
            Rod("Post", s, new Vector3(0, 0.02f, 0), new Vector3(0, 0.5f, 0), 0.025f, K.brass);
            var beam = Rod("Beam", s, new Vector3(-0.24f, 0.49f, 0), new Vector3(0.24f, 0.51f, 0), 0.018f, K.brass);
            foreach (var (x, y) in new[] { (-0.24f, 0.49f), (0.24f, 0.51f) })
            {
                float py = y - 0.3f + (x < 0 ? -0.02f : 0.02f);
                foreach (float a in new[] { 0f, 120f, 240f })
                {
                    var off = Quaternion.Euler(0, a, 0) * new Vector3(0.07f, 0, 0);
                    Rod("Chain", s, new Vector3(x, y, 0), new Vector3(x, py, 0) + off, 0.006f, K.brass);
                }
                Prim("Pan", s, PrimitiveType.Cylinder, new Vector3(x, py, 0), new Vector3(0.16f, 0.008f, 0.16f), K.brass);
                if (x < 0) CoinStack(s, new Vector3(x, py + 0.01f, 0), 0.02f, gold);
            }
            ProbeLit(s.gameObject, false);
        }

        static float n0(int i) => (i == 1 || i == 5) ? -1.75f : 1.75f; // mum konan niş

        static Transform BuildVault(Transform roomO)
        {
            Random.InitState(7117);
            var root = new GameObject("Vault").transform;
            root.SetParent(roomO, false);
            root.localPosition = new Vector3(VtCx, 0, 0);
            var wallM = KTex("K_VaultWall", "stone_block_wall", new Color(0.92f, 0.9f, 0.88f), 0.25f, 1.3f);
            var floorM = KTex("K_VaultFloor", "large_floor_tiles_02", new Color(0.62f, 0.6f, 0.6f), 0.45f, 1.1f);
            var stone = K.castleStone;
            var gold = GlowMat("K_Gold", Pal.Gold, Pal.Gold * 0.12f, 0.55f); gold.SetFloat("_Metallic", 0.35f); // metalik koyu kalıyordu (gerçek yansıma yok)
            var wood = K.furniture;
            var bone = MakeMat("K_Bone", new Color(0.6f, 0.55f, 0.46f));
            var velvet = K.velvet;
            var det = new GameObject("Details").transform; det.SetParent(root, false);
            float sideHalf = VtA * VtTan22;                  // 3.73
            float galIn = VtA - VtGalD, galHalf = galIn * VtTan22;

            // --- Kabuk: zemin, sekiz duvar, kubbe, oculus ---
            Deck(root, "Floor", new Vector3(0, -0.25f, 0), VtA * 2 + 0.6f, VtA * 2 + 0.6f, 0.5f, floorM, 3, 3);
            (float, float, float, float, bool) Niche(float u) => (u, 1.5f, 0.5f, 2.55f, true);
            VtWall(root, 0, 0.08f, 0.08f, wallM, (0f, 3f, 0f, 3f, false));            // doğu: zindan kapısı (zindan duvarının önünde ince kaplama)
            VtWall(root, 4, 0.4f, 0f, wallM, (0f, 3f, 0f, 3f, false));             // batı: mahzen geçidi
            VtWall(root, 2, 0.4f, 0f, wallM);                                       // kuzey: kitaplıklar + merdiven önü
            VtWall(root, 6, 0.4f, 0f, wallM, (0f, 3.2f, 0f, 3.1f, false));         // güney: parmaklıklı hazine bölmesi
            foreach (int i in new[] { 1, 3, 5, 7 }) VtWall(root, i, 0.4f, 0f, wallM, Niche(-1.75f), Niche(1.75f));
            for (int i = 0; i < 8; i++)
            {
                var d = new GameObject("Dome" + i);
                d.transform.SetParent(root, false);
                d.AddComponent<MeshFilter>().sharedMesh = VtDomeMesh(i, 1.5f);
                d.AddComponent<MeshRenderer>().sharedMaterial = wallM;
                d.isStatic = true;
            }
            float rc = VtA / Mathf.Cos(22.5f * Mathf.Deg2Rad), tEnd = Mathf.Acos(1.5f / rc), top = VtWallH + VtRise * Mathf.Sin(tEnd);
            // Kaburgalar (köşelerden oculusa) + köşe sütunları + korniş
            for (int c = 0; c < 8; c++)
            {
                Vector3 prev = VtCorner(c, rc - 0.12f, VtWallH);
                for (int k = 1; k <= 8; k++)
                {
                    float t = tEnd * k / 8f;
                    var p = VtCorner(c, (rc - 0.12f) * Mathf.Cos(t), VtWallH + VtRise * Mathf.Sin(t) - 0.08f);
                    Rod("Rib", root, prev, p, 0.3f, stone);
                    prev = p;
                }
                var cp = VtCorner(c, rc - 0.3f, 0);
                var col = Slab("Column", root, cp + Vector3.up * (VtWallH * 0.5f), new Vector3(0.55f, VtWallH, 0.55f), stone, 1.5f);
                col.transform.localRotation = Quaternion.Euler(0, -(c * 45f + 22.5f), 0);
                Slab("ColumnBase", root, cp + Vector3.up * 0.25f, new Vector3(0.8f, 0.5f, 0.8f), stone, 1.5f, false).transform.localRotation = col.transform.localRotation;
                Slab("ColumnCap", root, cp + Vector3.up * (VtWallH - 0.2f), new Vector3(0.8f, 0.4f, 0.8f), stone, 1.5f, false).transform.localRotation = col.transform.localRotation;
            }
            for (int i = 0; i < 8; i++)
            {
                var cor = Slab("Cornice", root, VtFace(i, 0, VtWallH - 0.05f, 0.12f), new Vector3(sideHalf * 2f, 0.22f, 0.3f), stone, 1.5f, false);
                cor.transform.localRotation = Quaternion.LookRotation(-VtDir(i));
            }
            {   // Oculus: halka, gökyüzü diski, ay huzmesi + baked spot
                var glow = GlowMat("K_VaultOculus", Pal.Moon * 0.4f, Pal.Moon * 1.2f);
                MeshObj("OculusRing", root, new Vector3(0, top - 0.05f, 0), RingMesh(1.55f, 0.22f, 0.3f), stone);
                Prim("Sky", root, PrimitiveType.Cylinder, new Vector3(0, top + 0.25f, 0), new Vector3(3.1f, 0.01f, 3.1f), glow);
                float L = top - 0.3f;
                QuadObj("Shaft", root, new Vector3(0.6f, top - L * 0.5f, 0.3f), new Vector2(2.4f, L), K.shaft, Quaternion.LookRotation(Vector3.right, Vector3.up) * Quaternion.Euler(0, 0, -4f));
                QuadObj("ShaftB", root, new Vector3(0.6f, top - L * 0.5f, 0.3f), new Vector2(2.0f, L), K.shaft, Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(0, 0, 4f));
                var ml = LightGo("OculusMoon", LightType.Spot, Pal.Moon, 16f, 18f, true);
                ml.transform.SetParent(root, false);
                ml.transform.localPosition = new Vector3(0, top - 0.2f, 0);
                ml.transform.localRotation = Quaternion.LookRotation(new Vector3(0.08f, -1f, 0.04f));
                ml.GetComponent<Light>().spotAngle = 30f; ml.GetComponent<Light>().innerSpotAngle = 16f;
            }

            // --- Galeri halkası (y 4.2): döşeme, alt konsollar, iç kiriş, demir korkuluk ---
            for (int i = 0; i < 8; i++)
            {
                var g = new GameObject("Gallery" + i);
                g.transform.SetParent(root, false);
                g.transform.localPosition = new Vector3(0, VtGal, 0);
                var gm = VtGalleryMesh(i, 0.3f);
                g.AddComponent<MeshFilter>().sharedMesh = gm;
                g.AddComponent<MeshRenderer>().sharedMaterial = K.parquet;
                g.AddComponent<MeshCollider>().sharedMesh = gm;
                g.isStatic = true;
                var inn = VtDir(i) * galIn;
                Rod("EdgeBeam", root, inn + VtTang(i) * galHalf + Vector3.up * (VtGal - 0.38f), inn - VtTang(i) * galHalf + Vector3.up * (VtGal - 0.38f), 0.2f, K.beam);
                foreach (float u in new[] { -1.6f, 0f, 1.6f })
                {
                    if (i == 0 && u == 0f || i == 4 && u == 0f || i == 6 && u == 0f) continue; // kapı/bölme üstü
                    Rod("Bracket", root, VtFace(i, u, VtGal - 1.3f, 0.02f), VtFace(i, u, VtGal - 0.32f, 1.5f), 0.14f, K.beam);
                    Rod("Joist", root, VtFace(i, u, VtGal - 0.36f, 0.02f), VtFace(i, u, VtGal - 0.36f, VtGalD - 0.05f), 0.14f, K.beam);
                }
                // Korkuluk: tek ağa birleşen çubuklar + görünmez engel
                var rail = new GameObject("Railing" + i).transform; rail.SetParent(root, false);
                Vector3 ra = inn + VtTang(i) * galHalf + Vector3.up * VtGal, rb = inn - VtTang(i) * galHalf + Vector3.up * VtGal;
                bool stairGap = i == 2; // merdiven köprüsü (x -2.35..-0.85)
                int nb = Mathf.RoundToInt((ra - rb).magnitude / 0.22f);
                for (int k = 0; k <= nb; k++)
                {
                    var p = Vector3.Lerp(ra, rb, k / (float)nb);
                    if (stairGap && Mathf.Abs(p.x + 1.6f) < 0.8f) continue;
                    Rod("Bar", rail, p, p + Vector3.up * 0.95f, 0.025f, K.iron);
                }
                var segs = new List<(Vector3, Vector3)>();
                if (stairGap)
                {   // köprü ağzında üst/alt bar da kesilir (çıkış açık görünsün)
                    Vector3 ga = new Vector3(-2.4f, VtGal, galIn), gb = new Vector3(-0.8f, VtGal, galIn);
                    bool aLeft = ra.x < rb.x;
                    segs.Add(aLeft ? (ra, ga) : (ra, gb)); segs.Add(aLeft ? (gb, rb) : (ga, rb));
                }
                else segs.Add((ra, rb));
                foreach (var (s0, s1) in segs)
                {
                    Rod("Top", rail, s0 + Vector3.up * 0.98f, s1 + Vector3.up * 0.98f, 0.06f, K.iron);
                    Rod("Mid", rail, s0 + Vector3.up * 0.1f, s1 + Vector3.up * 0.1f, 0.04f, K.iron);
                }
                CombineChildren(rail, "vault_rail_" + i, K.iron);
                ProbeLit(rail.gameObject, false);
                if (stairGap) continue;
                var blk = new GameObject("RailBlock").AddComponent<BoxCollider>();
                blk.transform.SetParent(root, false);
                blk.transform.localPosition = (ra + rb) * 0.5f + Vector3.up * 0.65f;
                blk.transform.localRotation = Quaternion.LookRotation(-VtDir(i));
                blk.size = new Vector3((ra - rb).magnitude + 0.3f, 1.3f, 0.25f);
            }
            // Kuzey korkuluğunda merdiven ağzı: engeli iki parçaya böl (yukarıdaki tek parça yerine)
            {
                foreach (var (x0, x1) in new[] { (-galHalf - 0.15f, -2.4f), (-0.8f, galHalf + 0.15f) })
                {
                    var b = new GameObject("RailBlock").AddComponent<BoxCollider>();
                    b.transform.SetParent(root, false);
                    b.transform.localPosition = new Vector3((x0 + x1) * 0.5f, VtGal + 0.65f, galIn);
                    b.size = new Vector3(x1 - x0, 1.3f, 0.25f);
                }
            }

            // --- Üst duvar: meşe panel + çıtalar (galeri seviyesi) ---
            for (int i = 0; i < 8; i++)
            {
                var rot = Quaternion.LookRotation(-VtDir(i));
                var pn = Slab("UpperPanel", root, VtFace(i, 0, (VtGal + VtWallH - 0.3f) * 0.5f, 0.02f), new Vector3(sideHalf * 2f - 0.7f, VtWallH - 0.3f - VtGal, 0.04f), K.panel, 1.2f, false);
                pn.transform.localRotation = rot;
                var bat = new GameObject("Battens" + i).transform; bat.SetParent(root, false);
                for (int k = -2; k <= 2; k++)
                    Rod("B", bat, VtFace(i, k * 1.25f, VtGal, 0.06f), VtFace(i, k * 1.25f, VtWallH - 0.3f, 0.06f), 0.09f, K.beam);
                foreach (float y in new[] { VtGal + 0.12f, 6.4f, VtWallH - 0.35f })
                    Rod("R", bat, VtFace(i, -sideHalf + 0.35f, y, 0.07f), VtFace(i, sideHalf - 0.35f, y, 0.07f), 0.1f, K.beam);
                CombineChildren(bat, "vault_battens_" + i, K.beam);
                ProbeLit(bat.gameObject);
            }

            // --- Döner merdiven (ortada serbest; kuzey galeriye çıkar). Geniş (r 1.8), dış kenar görünmez engelli, çıkış açık. ---
            {
                var st = new GameObject("SpiralStair").transform; st.SetParent(root, false);
                var axis = new Vector3(-1.6f, 0, 4.3f);
                st.localPosition = axis;
                const int N = 21; const float rise = 0.2f, rIn = 0.18f, rOut = 1.8f, dTh = 27.4f;
                const int railFrom = 3, railTo = 18; // alt 3 basamak giriş, son 2 basamak çıkış: korkuluksuz
                Prim("Newel", st, PrimitiveType.Cylinder, new Vector3(0, 2.4f, 0), new Vector3(0.36f, 2.4f, 0.36f), K.iron).AddComponent<CapsuleCollider>();
                var posts = new GameObject("Rail").transform; posts.SetParent(st, false);
                const float hh = 0.95f;
                void Guard(Vector3 a0, Vector3 b0) // a0/b0: basamak/köprü yüzeyinde iki nokta
                {
                    Rod("Post", posts, b0, b0 + Vector3.up * hh, 0.035f, K.iron);
                    Rod("Hand", posts, a0 + Vector3.up * hh, b0 + Vector3.up * hh, 0.05f, K.iron);
                    Rod("Mid", posts, a0 + Vector3.up * 0.45f, b0 + Vector3.up * 0.45f, 0.025f, K.iron);
                    var h = b0 - a0; h.y = 0;
                    var c = new GameObject("RailBlock").AddComponent<BoxCollider>();
                    c.transform.SetParent(st, false);
                    c.transform.localPosition = (a0 + b0) * 0.5f + Vector3.up * 0.65f;
                    c.transform.localRotation = Quaternion.FromToRotation(Vector3.right, h.normalized);
                    c.size = new Vector3(h.magnitude + 0.15f, 1.3f + Mathf.Abs(a0.y - b0.y), 0.22f);
                }
                Vector3 Edge(int k) { float t = (-90f + k * dTh) * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * (rOut - 0.05f) + Vector3.up * (rise * (k + 1)); }
                for (int k = 0; k < N; k++)
                {
                    float th = (-90f + k * dTh) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Cos(th), 0, Mathf.Sin(th));
                    float y = rise * (k + 1);
                    var tread = Slab("Tread", st, dir * ((rIn + rOut) * 0.5f) + Vector3.up * (y - 0.04f), new Vector3(0.92f, 0.08f, rOut - rIn + 0.1f), K.beam, 1f, false);
                    tread.transform.localRotation = Quaternion.LookRotation(dir);
                    var bc = tread.AddComponent<BoxCollider>();
                    bc.center = new Vector3(0, -0.06f, 0); bc.size = new Vector3(0.92f, 0.2f, rOut - rIn + 0.1f);
                    Slab("Riser", st, dir * ((rIn + rOut) * 0.5f) + Vector3.up * (y - 0.14f), new Vector3(0.04f, 0.12f, rOut - rIn), K.iron, 1f, false).transform.localRotation = Quaternion.LookRotation(dir);
                    if (k == railFrom) Rod("Post", posts, Edge(k), Edge(k) + Vector3.up * hh, 0.035f, K.iron);
                    if (k > railFrom && k <= railTo) Guard(Edge(k - 1), Edge(k));
                }
                // Köprü: son basamaktan (y 4.2, ~98°) galeri kenarına; iki yanı korkuluklu
                float bz0 = 1.5f, bz1 = galIn - axis.z + 0.15f, bw = 0.78f;
                Slab("Bridge", st, new Vector3(0, VtGal - 0.055f, (bz0 + bz1) * 0.5f), new Vector3(bw * 2f, 0.1f, bz1 - bz0), K.beam, 1f);
                Rod("Post", posts, new Vector3(bw, VtGal, bz0), new Vector3(bw, VtGal + hh, bz0), 0.035f, K.iron);
                Guard(new Vector3(bw, VtGal, bz0), new Vector3(bw, VtGal, bz1 - 0.1f));
                Guard(new Vector3(-bw, VtGal, bz0), new Vector3(-bw, VtGal, bz1 - 0.1f));
                // Sağ: son korkuluk dikmesinden köprüye; sol: köprüden direğe (üst basamağın açık kenarı)
                Guard(Edge(railTo), new Vector3(bw, VtGal, bz0));
                float tl = 114f * Mathf.Deg2Rad;
                Guard(new Vector3(Mathf.Cos(tl) * 0.22f, VtGal, Mathf.Sin(tl) * 0.22f), new Vector3(-bw, VtGal, bz0));
                CombineChildren(posts, "vault_stair_rail", K.iron);
                ProbeLit(posts.gameObject, false);
                ProbeLit(st.gameObject);
            }

            // --- Zemin kakması: pirinç pusula halkaları (avize altında) ---
            MeshObj("InlayRingA", det, new Vector3(0, 0.004f, 0), RingMesh(2.6f, 0.07f, 0.008f), K.brass);
            MeshObj("InlayRingB", det, new Vector3(0, 0.004f, 0), RingMesh(1.7f, 0.05f, 0.008f), K.brass);
            for (int k = 0; k < 8; k++)
            {
                var d = VtDir(k);
                Rod("InlayRay", det, d * 1.75f + Vector3.up * 0.004f, d * 2.55f + Vector3.up * 0.004f, 0.04f, K.brass).transform.localScale = new Vector3(1f, 1f, 0.2f);
            }

            // --- Doğu (giriş) ---
            ArchTrim(root, "EntryArch", VtFace(0, 0, 0, 0.0f), -VtDir(0), 3.4f, 4.6f, false);
            VtKit("WallSconce", root, 0, -2.4f, 2.4f, 0.03f);
            VtKit("WallSconce", root, 0, 2.4f, 2.4f, 0.03f); // VtFace doğu kaplamasını hesaba katar
            Kit("L_Candle", root, VtFace(0, -2.4f, 2.5f, 0.7f));
            Kit("L_Candle", root, VtFace(0, 2.4f, 2.5f, 0.7f));
            StrongBox(det, "Box", VtFace(0, -3.0f, 0, 0.6f), 90f, new Vector3(0.9f, 0.55f, 0.6f), false, gold, wood);
            StrongBox(det, "Box", VtFace(0, -3.0f, 0.55f, 0.6f), 96f, new Vector3(0.7f, 0.45f, 0.5f), false, gold, wood);
            VtKit("CrateStack", root, 0, 2.9f, 0, 0.7f, 0.75f, 20f);

            // --- Batı (mahzen geçidi): sikke izi geçide uzanır (Detay 1) ---
            ArchTrim(root, "CellarArch", VtFace(4, 0, 0, 0.0f), -VtDir(4), 3.4f, 4.6f, false);
            VtKit("WallSconce", root, 4, -2.4f, 2.4f, 0.03f);
            Kit("L_Candle", root, VtFace(4, -2.4f, 2.5f, 0.7f));
            VtKit("Lantern", root, 4, 2.2f, 0, 0.6f, 1f, 30f);
            for (int k = 0; k < 16; k++)
            {
                float f = k / 15f;
                var p = Vector3.Lerp(new Vector3(-3.4f, 0, -3.1f), new Vector3(-VtA + 0.2f, 0, 0.3f), f) + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
                LooseCoin(det, p, Random.Range(0f, 360f), gold);
            }

            // --- Kuzey: kitaplıklar (hesap defterleri) ---
            VtKit("BookshelfFull0", root, 2, -2.3f, 0, 0.3f);
            VtKit("BookshelfFull2", root, 2, 0f, 0, 0.3f);
            VtKit("BookshelfFull1", root, 2, 2.3f, 0, 0.3f);
            VtKit("PaperScatter", root, 2, 1.2f, 0.01f, 1.3f, 1f, 40f);
            VtKit("BookStack", root, 2, 3.2f, 0, 1.0f, 1f, 15f);
            VtKit("Ladder", root, 2, 1.15f, 0, 0.75f, 0.9f, 0f);

            // --- Güney: parmaklıklı hazine bölmesi (Detay 3: boş kadife kaide) ---
            {
                var al = new GameObject("TreasureBay").transform; al.SetParent(root, false);
                var d = VtDir(6); var tg = VtTang(6);
                float depth = 2.3f, w = 3.2f, h = 3.1f, back = VtA + 0.4f + depth;
                Slab("BayFloor", al, d * (VtA + 0.4f + depth * 0.5f) + Vector3.up * -0.25f, new Vector3(w + 0.8f, 0.5f, depth + 0.1f), floorM, 2f);
                Slab("BayCeil", al, d * (VtA + 0.4f + depth * 0.5f) + Vector3.up * (h + 0.25f), new Vector3(w + 0.8f, 0.5f, depth + 0.1f), wallM, 2f);
                Slab("BayBack", al, d * (back + 0.25f) + Vector3.up * (h * 0.5f), new Vector3(w + 0.8f, h + 0.2f, 0.5f), wallM, 2f);
                foreach (int s in new[] { -1, 1 })
                    Slab("BaySide", al, d * (VtA + 0.4f + depth * 0.5f) + tg * (s * (w * 0.5f + 0.25f)) + Vector3.up * (h * 0.5f), new Vector3(0.5f, h + 0.2f, depth + 0.1f), wallM, 2f);
                ArchTrim(root, "BayArch", VtFace(6, 0, 0, 0.0f), -d, 3.6f, 4.7f, false);
                Kit("IronGate", root, VtFace(6, 0, 0, -0.15f), YawIn(-d), 1.05f);
                var gb = new GameObject("GateBlock").AddComponent<BoxCollider>();
                gb.transform.SetParent(al, false); gb.transform.localPosition = VtFace(6, 0, 1.5f, -0.15f);
                gb.size = new Vector3(w, 3f, 0.15f);
                gb.transform.localRotation = Quaternion.LookRotation(-d);
                // İçerik (sırttan odaya)
                Vector3 B(float u, float y, float fromBack) => d * (back - fromBack) + tg * u + Vector3.up * y;
                Slab("BayShelf", al, B(0, 1.6f, 0.2f), new Vector3(0.4f, 0.05f, 2.8f), K.beam, 1f, false).transform.localRotation = Quaternion.LookRotation(d) * Quaternion.Euler(0, 90, 0);
                Slab("BayShelf2", al, B(0, 2.3f, 0.2f), new Vector3(0.4f, 0.05f, 2.8f), K.beam, 1f, false).transform.localRotation = Quaternion.LookRotation(d) * Quaternion.Euler(0, 90, 0);
                float yawB = YawIn(-d);
                Kit("BrassVase", al, B(-1.0f, 1.63f, 0.2f), yawB, 0.6f);
                Kit("Goblet0", al, B(-0.4f, 1.63f, 0.2f), yawB);
                Kit("Goblet2", al, B(0.1f, 1.63f, 0.2f), yawB);
                Kit("BrassCandleholder1", al, B(0.8f, 1.63f, 0.2f), yawB);
                Kit("BrassVase2", al, B(-0.8f, 2.33f, 0.2f), yawB, 0.7f);
                Kit("PocketWatch", al, B(0.0f, 2.33f, 0.2f), yawB);
                Kit("Goblet1", al, B(0.6f, 2.33f, 0.2f), yawB);
                KitR("Shield", al, d * (back - 0.02f) + tg * 1.25f + Vector3.up * 1.0f, Quaternion.Euler(0, YawIn(-d), 0), 0.8f);
                Kit("Sword", al, d * (back - 0.08f) + tg * -1.25f + Vector3.up * 1.2f, YawIn(-d));
                Kit("Chest", al, B(-1.0f, 0, 0.7f), yawB + 8f, 0.85f);
                StrongBox(al, "GoldBox", B(1.0f, 0, 0.8f), yawB - 10f, new Vector3(0.8f, 0.42f, 0.55f), true, gold, wood);
                // Boş kadife kaide (kayıp emanet) + toz halkası
                Slab("Plinth", al, B(0, 0.45f, 1.3f), new Vector3(0.55f, 0.9f, 0.55f), stone, 1f);
                Slab("Cushion", al, B(0, 0.94f, 1.3f), new Vector3(0.46f, 0.08f, 0.46f), velvet, 1f, false);
                MeshObj("DustRing", al, B(0, 0.985f, 1.3f), RingMesh(0.13f, 0.02f, 0.004f), MakeMat("K_Dust", new Color(0.42f, 0.38f, 0.33f)));
                for (int k = 0; k < 6; k++) CoinStack(al, B(-0.6f + k * 0.24f, 0, 1.9f - (k % 2) * 0.2f), 0.03f + (k % 3) * 0.035f, gold);
                Kit("L_Candle", al, B(-0.5f, 2.7f, 0.5f));
                var bf = LightGo("BayFill", LightType.Point, Pal.Candle, 3.2f, 4.5f, true);
                bf.transform.SetParent(al, false); bf.transform.localPosition = B(0.6f, 2.5f, 1.1f);
                Kit("Candlestick", al, B(-0.5f, 2.33f, 0.2f), yawB);
                var pg = new GameObject("LightProbes"); pg.transform.SetParent(al, false);
                var pts = new List<Vector3>();
                foreach (float u in new[] { -1.1f, 1.1f }) foreach (float y in new[] { 0.5f, 2.4f }) foreach (float f in new[] { 0.4f, 1.9f }) pts.Add(B(u, y, f));
                pg.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
                ProbeLit(al.gameObject);
            }
            VtKit("WallSconce", root, 6, -2.6f, 2.4f, 0.03f);
            Kit("L_Candle", root, VtFace(6, -2.6f, 2.5f, 0.7f));
            VtKit("StatueKing", root, 6, 2.75f, 0, 0.6f, 0.95f);

            // --- Çapraz yanlar: kemerli hazine nişleri (raf + eşyalar) + arada aplik ---
            string[][] nicheSets =
            {
                new[] { "Goblet0", "Plate", "Goblet2", "BrassCandleholder0", "Compass", "Goblet1" },
                new[] { "BrassVase", "Elephant", "Bust", "CeramicVase1", "MantelClock", "Shell" },
                new[] { "Amphora", "Goblet1", "HorseHead", "BrassVase2", "TeaSet", "Goblet0" },
                new[] { "CeramicVase3", "LionHead", "BrassCandleholder2", "WhaleStatue", "ShipModel", "Plate" },
            };
            int ns = 0;
            foreach (int i in new[] { 1, 3, 5, 7 })
            {
                var set = nicheSets[ns++];
                for (int n = 0; n < 2; n++)
                {
                    float u = n == 0 ? -1.75f : 1.75f;
                    ArchTrim(root, "NicheArch", VtFace(i, u, 0.5f, 0.0f), -VtDir(i), 1.7f, 2.9f, false);
                    Slab("NicheShelf", root, VtFace(i, u, 1.55f, -0.2f), new Vector3(1.5f, 0.05f, 0.4f), K.beam, 1f, false).transform.localRotation = Quaternion.LookRotation(-VtDir(i));
                    for (int k = 0; k < 3; k++)
                    {
                        string item = set[n * 3 + k];
                        float y = k == 1 ? 1.58f : 0.53f, du = k == 1 ? 0f : (k == 0 ? -0.4f : 0.4f);
                        float sc = item == "Bust" || item == "Amphora" ? 0.75f : item == "Elephant" || item == "ShipModel" || item == "WhaleStatue" || item == "HorseHead" || item == "LionHead" ? 0.6f : 1f;
                        VtKit(item, root, i, u + du, y, -0.2f, sc);
                    }
                    CoinStack(det, VtFace(i, u + 0.45f, 1.58f, -0.18f), 0.05f + n * 0.03f, gold);
                    CoinStack(det, VtFace(i, u - 0.42f, 1.58f, -0.22f), 0.03f, gold);
                }
                VtKit("WallSconce", root, i, 0, 2.6f, 0.03f);
                Kit("L_Candle", root, VtFace(i, 0, 2.7f, 0.7f));
                var fill = LightGo("SconceFill", LightType.Point, Pal.Candle, 2.6f, 8f, true); // aplik dolgusu (baked)
                fill.transform.SetParent(root, false); fill.transform.localPosition = VtFace(i, 0, 2.9f, 1.2f);
                Kit("CandleCluster", root, VtFace(i, n0(i), 0.53f, -0.15f)); // niş içi mum
                VtKit("PaintingPortrait", root, i, 0, 7.35f, 0.07f, 0.95f);
            }

            // --- Zemin: sandıklar, kasalar, sikke yığınları ---
            Kit("Chest", root, VtFace(1, 0, 0, 0.75f), YawIn(-VtDir(1)), 0.9f);
            Kit("Chest", root, VtFace(5, 0, 0, 0.75f), YawIn(-VtDir(5)), 0.9f);
            StrongBox(det, "GoldBox", VtFace(7, 0.1f, 0, 0.9f), YawIn(-VtDir(7)), new Vector3(0.9f, 0.45f, 0.6f), true, gold, wood);
            StrongBox(det, "Box", VtFace(3, -0.3f, 0, 0.9f), YawIn(-VtDir(3)) + 5f, new Vector3(1.0f, 0.6f, 0.65f), false, gold, wood);
            StrongBox(det, "Box", VtFace(3, 0.6f, 0, 1.3f), YawIn(-VtDir(3)) - 18f, new Vector3(0.7f, 0.45f, 0.5f), false, gold, wood);
            StrongBox(det, "Box", new Vector3(4.6f, 0, 3.4f), 30f, new Vector3(0.8f, 0.5f, 0.55f), false, gold, wood);
            StrongBox(det, "Box", new Vector3(4.7f, 0.5f, 3.35f), 38f, new Vector3(0.6f, 0.38f, 0.45f), false, gold, wood);
            foreach (var p in new[] { new Vector3(3.9f, 0, 4.1f), new Vector3(4.15f, 0, 4.25f), new Vector3(-5.2f, 0, -4.6f), new Vector3(-5.0f, 0, -4.35f), new Vector3(5.6f, 0, -3.7f), new Vector3(5.8f, 0, -3.95f) })
                CoinStack(det, p, Random.Range(0.04f, 0.16f), gold);

            // --- Orta zemin: hazine yığınları (referanstaki gibi dağınık sandıklar) ---
            StrongBox(det, "Box", new Vector3(-2.2f, 0, -5.6f), 12f, new Vector3(0.9f, 0.55f, 0.6f), false, gold, wood);
            StrongBox(det, "GoldBox", new Vector3(-1.1f, 0, -5.9f), -20f, new Vector3(0.75f, 0.4f, 0.5f), true, gold, wood);
            Kit("Chest", root, new Vector3(5.9f, 0, 2.6f), -70f, 0.85f);
            StrongBox(det, "Box", new Vector3(-5.8f, 0, 2.2f), 70f, new Vector3(0.8f, 0.5f, 0.55f), false, gold, wood);
            StrongBox(det, "Box", new Vector3(-5.75f, 0.5f, 2.25f), 64f, new Vector3(0.6f, 0.36f, 0.42f), false, gold, wood);
            foreach (var p in new[] { new Vector3(-1.9f, 0, -4.9f), new Vector3(-1.6f, 0, -4.75f), new Vector3(-0.4f, 0, -5.3f), new Vector3(5.4f, 0, 2.0f), new Vector3(-5.1f, 0, 1.6f) })
                CoinStack(det, p, Random.Range(0.05f, 0.18f), gold);
            Prim("GoldHeap", det, PrimitiveType.Sphere, new Vector3(-0.9f, 0.02f, -5.1f), new Vector3(0.7f, 0.14f, 0.55f), gold);
            Kit("Rug", root, new Vector3(-3.4f, 0, 0.2f), 90f, 0.8f);

            // --- Detay 2: zorlanmış sandık (kapak yerde, levye + çekiç, kırık asma kilit) ---
            {
                var fp = new Vector3(-4.6f, 0, -3.7f);
                StrongBox(det, "ForcedBox", fp, 35f, new Vector3(0.95f, 0.5f, 0.62f), true, gold, wood);
                var lid = Slab("FallenLid", det, fp + new Vector3(0.8f, 0.03f, 0.6f), new Vector3(0.95f, 0.05f, 0.6f), wood, 1f, false);
                lid.transform.localRotation = Quaternion.Euler(0, 62f, 4f);
                Kit("Hammer", root, fp + new Vector3(-0.7f, 0.02f, 0.3f), 110f);
                Kit("Wrench", root, fp + new Vector3(0.2f, 0.02f, 0.85f), 20f);
                MeshObj("BrokenLock", det, fp + new Vector3(0.35f, 0.02f, -0.6f), RingMesh(0.05f, 0.012f, 0.02f), K.brass).transform.localRotation = Quaternion.Euler(80, 30, 0);
                Prim("LockBody", det, PrimitiveType.Cube, fp + new Vector3(0.42f, 0.04f, -0.66f), new Vector3(0.09f, 0.07f, 0.04f), K.brass).transform.localRotation = Quaternion.Euler(0, 40, 90);
                for (int k = 0; k < 7; k++) LooseCoin(det, fp + new Vector3(Random.Range(-0.6f, 0.9f), 0, Random.Range(-0.8f, 0.7f)), Random.Range(0f, 360f), gold);
            }

            // --- Detay 4: hazinedarın sayım masası (açık defter, gözlük, terazi, sikke sıraları) ---
            {
                var tp = new Vector3(2.6f, 0, -1.6f);
                Kit("Table", root, tp, 0f, 1.15f);
                float ty = 0.82f * 1.15f;
                Kit("ChairWood", root, tp + new Vector3(0, 0, -0.85f), 8f);
                Kit("FoldStool", root, tp + new Vector3(1.5f, 0, 0.3f), 60f);
                Kit("Rug", root, tp + new Vector3(0, 0, -0.1f), 0f, 0.9f);
                Scales(det, tp + new Vector3(-0.55f, ty, 0.12f), gold);
                Kit("BookStack", root, tp + new Vector3(0.75f, ty, 0.18f), 80f);
                var ledger = Slab("Ledger", det, tp + new Vector3(0.05f, ty + 0.02f, -0.08f), new Vector3(0.48f, 0.035f, 0.34f), K.leatherRed, 1f, false);
                Slab("LedgerPages", det, tp + new Vector3(0.05f, ty + 0.042f, -0.08f), new Vector3(0.44f, 0.012f, 0.31f), K.paperA, 1f, false);
                Kit("Spectacles", root, tp + new Vector3(0.2f, ty + 0.05f, -0.15f), 200f);
                Kit("Magnifier", root, tp + new Vector3(0.45f, ty, -0.2f), 60f);
                Prim("Quill", det, PrimitiveType.Cylinder, tp + new Vector3(-0.15f, ty + 0.05f, -0.12f), new Vector3(0.008f, 0.12f, 0.008f), K.paperC).transform.localRotation = Quaternion.Euler(70, 40, 0);
                Prim("Inkwell", det, PrimitiveType.Cylinder, tp + new Vector3(-0.2f, ty + 0.03f, 0.12f), new Vector3(0.06f, 0.03f, 0.06f), K.bottleGlass);
                Kit("Candelabra", root, tp + new Vector3(0.95f, ty, -0.05f), 0f, 0.9f);
                Kit("L_Candelabra", root, tp + new Vector3(0.95f, ty + 0.45f, -0.05f));
                for (int k = 0; k < 8; k++) CoinStack(det, tp + new Vector3(-0.9f + (k % 4) * 0.1f, ty, -0.22f + (k / 4) * 0.12f), 0.02f + (k * 7 % 5) * 0.012f, gold);
                Kit("PaperA", root, tp + new Vector3(0.55f, ty + 0.005f, 0.05f), 15f);
                Kit("PaperScatter", root, tp + new Vector3(0.5f, 0.01f, -1.2f), 70f);
                StrongBox(det, "Box", tp + new Vector3(-1.4f, 0, 0.1f), 85f, new Vector3(0.7f, 0.42f, 0.5f), false, gold, wood);
                Kit("Basket", root, tp + new Vector3(1.3f, 0, -0.7f), 20f);
            }

            // --- Detay 5: hırsızın kemikleri (galeri altında, kasa yanında) + fare ---
            {
                var bp = VtFace(5, -1.2f, 0, 1.1f);
                Prim("Skull", det, PrimitiveType.Sphere, bp + new Vector3(0, 0.1f, 0), new Vector3(0.18f, 0.2f, 0.21f), bone);
                for (int k = 0; k < 6; k++)
                {
                    var a = bp + new Vector3(Random.Range(-0.5f, 0.5f), 0.025f, Random.Range(-0.4f, 0.4f));
                    Rod("Bone", det, a, a + Quaternion.Euler(0, Random.Range(0, 360f), 0) * new Vector3(0.32f, 0, 0), 0.035f, bone);
                }
                LooseCoin(det, bp + new Vector3(0.25f, 0, 0.1f), 30f, gold);
                Kit("Rat", root, bp + new Vector3(0.7f, 0, -0.3f), 140f);
                Kit("Dagger", root, bp + new Vector3(-0.35f, 0.02f, 0.25f), 75f);
            }

            // --- Galeri: hazinedarın yazı köşesi (güney), kitaplıklar, sandıklar, fener ---
            {
                float gy = VtGal;
                VtKit("BookshelfFull1", root, 1, 0, gy, 0.3f);
                VtKit("BookshelfFull0", root, 3, 0, gy, 0.3f);
                VtKit("BookshelfFull2", root, 7, 0, gy, 0.3f);
                VtKit("Cabinet", root, 5, 0, gy, 0.35f, 0.9f);
                var dp = VtFace(6, 0.8f, gy, 1.0f);
                Kit("StudyTable", root, dp, YawIn(-VtDir(6)) + 180f, 0.9f);
                Kit("ChairWood", root, dp + VtDir(6) * -0.7f, YawIn(VtDir(6)), 0.95f);
                Kit("L_Candle", root, dp + Vector3.up * 1.3f);
                VtKit("Chest", root, 4, 1.9f, gy, 0.7f, 0.8f);
                VtKit("CrateLong", root, 4, -1.8f, gy, 0.7f, 0.8f, 10f);
                VtKit("Lantern", root, 2, 2.6f, gy, 0.45f, 1f, 30f);
                Kit("L_Lantern", root, VtFace(2, 2.6f, gy + 0.3f, 0.45f));
                VtKit("HorseStatue", root, 0, 2.2f, gy, 0.9f, 0.8f);
                VtKit("Armillary", root, 0, -2.2f, gy, 0.9f, 0.9f);
                VtKit("PaperScatter", root, 6, -1.4f, gy + 0.01f, 1.2f, 1f, 130f);
                VtKit("BarrelLying", root, 1, 2.3f, gy, 0.8f, 0.8f, 90f);
                for (int i = 0; i < 8; i += 2)
                {
                    VtKit("Banner", root, i, 0, VtWallH - 0.45f, 0.08f, 1.3f);
                    foreach (float u in new[] { -1.5f, 1.5f })
                    {
                        VtKit("WallSconce", root, i, u, gy + 1.9f, 0.08f);
                        Kit("L_Candle", root, VtFace(i, u, gy + 2.0f, 0.7f));
                    }
                    var gf = LightGo("GalleryFill", LightType.Point, Pal.Candle, 2.4f, 9f, true);
                    gf.transform.SetParent(root, false); gf.transform.localPosition = VtFace(i, 0, gy + 2.2f, 1.4f);
                }
                var df = LightGo("DomeFill", LightType.Point, Color.Lerp(Pal.Moon, Pal.Candle, 0.35f), 4f, 14f, true); // kubbe boşluğu
                df.transform.SetParent(root, false); df.transform.localPosition = new Vector3(0, 9.6f, 0);
            }

            // --- Avize (oculus zincirinden) ---
            Kit("ChandelierOrnate", root, new Vector3(0, 6.6f, 0), 0f, 1.7f);
            Rod("ChandelierChain", root, new Vector3(0, 7.6f, 0), new Vector3(0, top - 0.2f, 0), 0.05f, K.iron);
            Kit("L_Chandelier", root, new Vector3(0, 6.4f, 0));
            Kit("CandelabraFloor", root, new Vector3(-3.6f, 0, -2.0f), 30f);
            Kit("CandelabraFloor", root, new Vector3(5.8f, 0, 1.2f), -60f);

            // --- Ağlar ---
            for (int c = 0; c < 8; c += 2)
            {
                var cp = VtCorner(c, rc - 0.5f, 0);
                float yaw = YawIn(-new Vector3(cp.x, 0, cp.z).normalized) - 135f;
                KitR("Cobweb", root, cp, Quaternion.Euler(0, yaw, 0), 1.0f);
                KitR("Cobweb", root, VtCorner(c + 1, rc - 0.5f, VtGal - 0.35f), Quaternion.Euler(0, YawIn(-VtCorner(c + 1, 1f, 0).normalized) - 135f, 180f), 1.3f);
            }

            // --- Işık sondaları (sekizgen içi) + yansıma ---
            {
                var pg = new GameObject("LightProbes"); pg.transform.SetParent(root, false);
                var pts = new List<Vector3>();
                float lim = VtA - 0.5f;
                for (float x = -lim; x <= lim + 0.01f; x += lim / 3f)
                    for (float z = -lim; z <= lim + 0.01f; z += lim / 3f)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(z) > lim * 1.38f) continue;
                        foreach (float y in new[] { 0.4f, 1.8f, 3.5f, 5.2f, 7.4f, 10.6f })
                        {
                            if (y > 9f && Mathf.Abs(x) + Mathf.Abs(z) > lim) continue;
                            pts.Add(new Vector3(x, y, z));
                        }
                    }
                pg.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
            }
            KitReflection(root, new Vector3(0, 3f, 0), new Vector3(VtA * 2f, 9f, VtA * 2f));
            ProbeLit(det.gameObject);
            KeepBaked(root);
            return root;
        }
    }
}
