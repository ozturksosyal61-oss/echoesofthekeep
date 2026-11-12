using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Dolu kitaplıklar: BookshelfFull0..2 = Bookshelf modeli + her rafa oturan kitaplar (tek ağ, malzeme başına alt ağ).
    /// Raf yükseklikleri modelden ölçülmüş sabitler (kitaplar yüzmez/kaymaz). Odalarda Books(...) ile yerleştirilir.
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

        static void BuildFullShelves()
        {
            var shelf = KitPrefab("Bookshelf");
            if (shelf == null) return;
            // Raf yüzeyleri (wooden_bookshelf_worn glTF'inden ölçüldü, 1.9 m boya ölçekli): (üst yüzey y, üstündeki boşluk).
            // Ağ çalışma anında okunamıyor (isReadable kapalı), bu yüzden sabit.
            const float ks = 1.9f / 2.0634f;
            var shelves = new List<(float y, float clear)>();
            foreach (var (top, under) in new[] { (0.108f, 0.348f), (0.379f, 0.628f), (0.655f, 0.916f), (0.949f, 1.233f), (1.256f, 1.619f), (1.648f, 2.016f) })
                shelves.Add((top * ks + 0.002f, (under - top) * ks - 0.015f));
            var box = new Bounds(Vector3.zero, new Vector3(0.613f * 2f + 0.04f, 1.9f, 0.535f));

            var mats = new[] { K.leather, K.leatherRed, K.leatherGreen, K.paperC, K.books };
            float inW = 1.13f, x0 = box.center.x - inW * 0.5f, zc = box.center.z + box.size.z * 0.08f;
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
