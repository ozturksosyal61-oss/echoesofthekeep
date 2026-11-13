using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Mobil su (CoopGame/Water): kenarlara hizalı düz ızgara ağ; köşe rengi R = kenar/engel yakınlığı (köpük).
    /// Dikdörtgenler su alanı, engeller (sütun, duvar ayağı) köpük kaynağı. Işık probe'u ile aydınlanır (lightmap yok).
    /// </summary>
    public static partial class CoopSetup
    {
        static Material WaterMat(string name, Color deep, Color shallow, float waveScale = 1.3f, float strength = 0.22f)
        {
            var m = MakeMat(name, deep, "CoopGame/Water");
            m.SetColor("_Shallow", shallow);
            m.SetFloat("_WaveScale", waveScale);
            m.SetFloat("_WaveStrength", strength);
            return m;
        }

        /// <summary>areas/obstacles: yerel XZ dikdörtgenleri (x, z = min; width, height = boyut). y: yüzey yüksekliği.</summary>
        static GameObject WaterSurface(string name, Transform parent, Vector3 pos, Rect[] areas, Rect[] obstacles, Material mat,
                                       float y = 0f, float cell = 0.35f, float foamDist = 0.6f)
        {
            obstacles ??= new Rect[0];
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var a in areas) { x0 = Mathf.Min(x0, a.xMin); x1 = Mathf.Max(x1, a.xMax); z0 = Mathf.Min(z0, a.yMin); z1 = Mathf.Max(z1, a.yMax); }
            List<float> Lines(float lo, float hi, IEnumerable<float> edges)
            {
                var l = new List<float>();
                for (float q = lo; q < hi; q += cell) l.Add(q);
                l.Add(hi);
                foreach (var e in edges) if (e > lo && e < hi) l.Add(e);
                l.Sort();
                var o = new List<float> { l[0] };
                foreach (var q in l) if (q - o[o.Count - 1] > 0.04f) o.Add(q);
                o[o.Count - 1] = hi;
                return o;
            }
            var ex = new List<float>(); var ez = new List<float>();
            foreach (var r in areas) { ex.Add(r.xMin); ex.Add(r.xMax); ez.Add(r.yMin); ez.Add(r.yMax); }
            foreach (var r in obstacles) { ex.Add(r.xMin); ex.Add(r.xMax); ez.Add(r.yMin); ez.Add(r.yMax); }
            var xs = Lines(x0, x1, ex); var zs = Lines(z0, z1, ez);

            bool Inside(float x, float z)
            {
                bool ins = false;
                foreach (var a in areas) if (a.Contains(new Vector2(x, z))) { ins = true; break; }
                if (!ins) return false;
                foreach (var o in obstacles) if (o.Contains(new Vector2(x, z))) return false;
                return true;
            }
            int nx = xs.Count - 1, nz = zs.Count - 1;
            var wet = new bool[nx, nz];
            var dry = new List<Rect>(); // köpük kaynağı: alan dışı hücreler + engeller (+ dış çerçeve)
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float cx = (xs[i] + xs[i + 1]) * 0.5f, cz = (zs[j] + zs[j + 1]) * 0.5f;
                    wet[i, j] = Inside(cx, cz);
                    if (!wet[i, j]) dry.Add(Rect.MinMaxRect(xs[i], zs[j], xs[i + 1], zs[j + 1]));
                }
            dry.AddRange(obstacles);
            dry.Add(Rect.MinMaxRect(x0 - 1, z0 - 1, x0, z1 + 1)); dry.Add(Rect.MinMaxRect(x1, z0 - 1, x1 + 1, z1 + 1));
            dry.Add(Rect.MinMaxRect(x0, z0 - 1, x1, z0)); dry.Add(Rect.MinMaxRect(x0, z1, x1, z1 + 1));

            float Foam(float x, float z)
            {
                float best = foamDist;
                foreach (var r in dry)
                {
                    float dx = Mathf.Max(r.xMin - x, 0, x - r.xMax), dz = Mathf.Max(r.yMin - z, 0, z - r.yMax);
                    if (dx < best && dz < best) best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
                }
                return 1f - best / foamDist;
            }

            var v = new List<Vector3>(); var c = new List<Color>(); var n = new List<Vector3>(); var t = new List<int>();
            var idx = new Dictionary<int, int>();
            int Vert(int i, int j)
            {
                int k = i * (nz + 1) + j;
                if (idx.TryGetValue(k, out var id)) return id;
                id = v.Count; idx[k] = id;
                v.Add(new Vector3(xs[i], y, zs[j])); n.Add(Vector3.up);
                float f = Foam(xs[i], zs[j]); c.Add(new Color(f, 0, 0, 1));
                return id;
            }
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    if (!wet[i, j]) continue;
                    int a = Vert(i, j), b = Vert(i, j + 1), d = Vert(i + 1, j + 1), e = Vert(i + 1, j);
                    t.Add(a); t.Add(b); t.Add(d); t.Add(a); t.Add(d); t.Add(e);
                }
            var mesh = new Mesh { indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            var go = MeshObj(name, parent, pos, SaveMesh(mesh, "water_" + name.ToLowerInvariant()), mat);
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveGI = ReceiveGI.LightProbes;
            mr.lightProbeUsage = LightProbeUsage.BlendProbes;
            mr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            go.isStatic = false;
            return go;
        }
    }
}
