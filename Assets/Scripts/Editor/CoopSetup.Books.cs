using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Dolu kitaplıklar: BookshelfFull0..2 = Bookshelf modeli + her rafa oturan kitaplar (tek ağ, malzeme başına alt ağ).
    /// Raf yükseklikleri modelin yukarı bakan yüzeylerinden okunur (kitaplar yüzmez/kaymaz). Odalarda Books(...) ile yerleştirilir.
    /// </summary>
    public static partial class CoopSetup
    {
        const int ShelfVariants = 3;

        /// <summary>Dolu kitaplık (konumdan seçilen varyant).</summary>
        static GameObject Books(Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            int v = Mathf.Abs(Mathf.RoundToInt(pos.x * 7.3f + pos.z * 3.1f + pos.y)) % ShelfVariants;
            return Kit("BookshelfFull" + v, parent, pos, yaw, scale);
        }

        static List<float> ShelfSurfaces(GameObject shelf, out Bounds box)
        {
            var ys = new List<float>();
            box = new Bounds();
            bool first = true;
            var mfs = shelf.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfs)
            {
                var mesh = mf.sharedMesh; if (mesh == null) continue;
                var m = shelf.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var vs = mesh.vertices; var ns = mesh.normals;
                for (int i = 0; i < vs.Length; i++)
                {
                    var p = m.MultiplyPoint3x4(vs[i]);
                    if (first) { box = new Bounds(p, Vector3.zero); first = false; } else box.Encapsulate(p);
                }
                for (int i = 0; i < vs.Length && i < ns.Length; i++)
                {
                    var p = m.MultiplyPoint3x4(vs[i]);
                    if (m.MultiplyVector(ns[i]).normalized.y > 0.85f) ys.Add(p.y);
                }
            }
            // iç bölgedeki yüzeyler: x ortası (yan panel üstleri hariç)
            var inner = new List<float>();
            foreach (var mf in mfs)
            {
                var mesh = mf.sharedMesh; if (mesh == null) continue;
                var m = shelf.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var vs = mesh.vertices; var ns = mesh.normals;
                for (int i = 0; i < vs.Length && i < ns.Length; i++)
                {
                    var p = m.MultiplyPoint3x4(vs[i]);
                    if (Mathf.Abs(p.x - box.center.x) < box.size.x * 0.3f && m.MultiplyVector(ns[i]).normalized.y > 0.85f) inner.Add(p.y);
                }
            }
            inner.Sort();
            var res = new List<float>();
            foreach (float y in inner)
                if (res.Count == 0 || y - res[res.Count - 1] > 0.05f) res.Add(y); else res[res.Count - 1] = Mathf.Max(res[res.Count - 1], y);
            return res;
        }

        static void BuildFullShelves()
        {
            var shelf = KitPrefab("Bookshelf");
            if (shelf == null) return;
            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(shelf);
            tmp.transform.position = Vector3.zero; tmp.transform.rotation = Quaternion.identity;
            var surf = ShelfSurfaces(tmp, out var box);
            Object.DestroyImmediate(tmp);
            // Raf: üstünde en az 18 cm boşluk olan yüzey; en üst (dolap tepesi) hariç
            var shelves = new List<(float y, float clear)>();
            for (int i = 0; i + 1 < surf.Count; i++)
            {
                float clear = surf[i + 1] - surf[i] - 0.04f;
                if (clear > 0.18f) shelves.Add((surf[i], Mathf.Min(clear, 0.4f)));
            }
            if (shelves.Count < 3) // okunamadıysa eşit aralık
            {
                Debug.LogWarning("[CoopSetup] Kitaplık rafları okunamadı (" + surf.Count + " yüzey), eşit aralık kullanılıyor");
                shelves.Clear();
                for (int i = 0; i < 6; i++) shelves.Add((0.06f + i * 0.31f, 0.27f));
            }
            kitDump.AppendLine("BookshelfFull rafları: " + string.Join(", ", shelves.ConvertAll(s => s.y.ToString("0.00") + "/" + s.clear.ToString("0.00"))) +
                               " kutu=" + box.size.ToString("0.00"));

            var mats = new[] { K.leather, K.leatherRed, K.leatherGreen, K.paperC, K.books };
            float inW = box.size.x - 0.13f, x0 = box.center.x - inW * 0.5f, zc = box.center.z + box.size.z * 0.08f;
            for (int v = 0; v < ShelfVariants; v++)
            {
                var r = new System.Random(4100 + v * 31);
                float Rf(float a, float b) => a + (b - a) * (float)r.NextDouble();
                var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
                var tris = new List<int>[mats.Length];
                for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();
                void Box(int mat, Vector3 c, Vector3 s, float rollZ = 0f, float yaw = 0f)
                {
                    var q = Quaternion.Euler(0, yaw, rollZ);
                    var h = s * 0.5f;
                    var faces = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                    foreach (var f in faces)
                    {
                        var u = Mathf.Abs(f.y) > 0.5f ? Vector3.right : Vector3.up;
                        var w = Vector3.Cross(f, u);
                        int b0 = verts.Count;
                        for (int k = 0; k < 4; k++)
                        {
                            float a = k == 0 || k == 3 ? -1 : 1, bb = k < 2 ? -1 : 1;
                            var p = Vector3.Scale(f + u * a + w * bb, h);
                            verts.Add(c + q * p); norms.Add(q * f);
                            uvs.Add(new Vector2((a + 1) * 0.5f, (bb + 1) * 0.5f) * 0.5f + new Vector2(0.25f * (mat % 2), 0.25f * (verts.Count % 3)));
                        }
                        tris[mat].AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 });
                    }
                }
                foreach (var (sy, clear) in shelves)
                {
                    float x = x0 + Rf(0f, 0.04f), end = x0 + inW;
                    while (x < end - 0.03f)
                    {
                        int roll = r.Next(100);
                        float hMax = Mathf.Min(clear - 0.02f, 0.34f);
                        if (roll < 6) { x += Rf(0.04f, 0.12f); continue; } // boşluk
                        if (roll < 14 && end - x > 0.24f) // yatık yığın
                        {
                            float y = sy, w = Rf(0.17f, 0.22f);
                            int n = 2 + r.Next(3);
                            for (int k = 0; k < n && y + 0.05f < sy + clear; k++)
                            {
                                float t = Rf(0.03f, 0.05f);
                                Box(r.Next(3), new Vector3(x + w * 0.5f, y + t * 0.5f, zc), new Vector3(w, t, Rf(0.15f, 0.2f)), 0f, Rf(-6f, 6f));
                                y += t;
                            }
                            x += w + 0.02f; continue;
                        }
                        if (roll < 17 && v == 2 && end - x > 0.2f) // tomar destesi
                        {
                            for (int k = 0; k < 3; k++)
                                Box(3, new Vector3(x + 0.09f, sy + 0.03f + k * 0.055f, zc), new Vector3(0.05f, 0.05f, 0.24f), 0f, Rf(-15f, 15f));
                            x += 0.2f; continue;
                        }
                        float bw = Rf(0.022f, 0.055f), bh = Mathf.Max(0.15f, Rf(hMax * 0.62f, hMax)), bd = Rf(0.14f, 0.21f);
                        if (roll < 22 && x > x0 + 0.1f) // yaslanmış kitap (sol komşuya)
                        {
                            float tilt = Rf(10f, 22f), dx = Mathf.Sin(tilt * Mathf.Deg2Rad) * bh;
                            float cy = sy + Mathf.Cos(tilt * Mathf.Deg2Rad) * bh * 0.5f + Mathf.Sin(tilt * Mathf.Deg2Rad) * bw * 0.5f;
                            Box(r.Next(3), new Vector3(x + dx * 0.5f + bw * 0.5f, cy, zc), new Vector3(bw, bh, bd), tilt);
                            x += dx + bw + 0.01f; continue;
                        }
                        Box(r.Next(100) < 12 ? 4 : r.Next(3), new Vector3(x + bw * 0.5f, sy + bh * 0.5f, zc + Rf(-0.015f, 0.015f)), new Vector3(bw, bh, bd));
                        x += bw + Rf(0.001f, 0.004f);
                    }
                }
                var mesh = new Mesh { indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
                mesh.subMeshCount = mats.Length;
                for (int i = 0; i < mats.Length; i++) mesh.SetTriangles(tris[i], i);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                mesh = SaveMesh(mesh, "kit_shelfbooks_" + v);

                var go = new GameObject("BookshelfFull" + v);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(shelf);
                inst.transform.SetParent(go.transform, false);
                var bk = new GameObject("Books");
                bk.transform.SetParent(go.transform, false);
                bk.AddComponent<MeshFilter>().sharedMesh = mesh;
                bk.AddComponent<MeshRenderer>().sharedMaterials = mats;
                SaveKit(go, true, 0.03f);
            }
        }
    }
}
