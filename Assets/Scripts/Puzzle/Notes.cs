using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 7'ye özgü simge seti: nota işaretleri (Glyphs/Runes/ay evrelerinden farklı). Yerel XY düzlemi, yüz -Z, yükseklik ~size.
    /// 0 Tam, 1 Yarım, 2 Dörtlük, 3 Sekizlik, 4 Çift sekizlik, 5 Diyez. Tam/yarım içi boş baş; dörtlük/sekizlik dolu baş.
    /// </summary>
    public static class Notes
    {
        public const int Count = 6;
        public static readonly string[] Names = { "Tam nota", "Yarım nota", "Dörtlük", "Sekizlik", "Çift sekizlik", "Diyez" };

        static void Kill(Object o) { if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        static GameObject Piece(Transform parent, PrimitiveType type, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Kill(go.GetComponent<Collider>());
            go.name = "Note";
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static void Bar(Transform parent, Vector2 p, Vector2 q, float w, float depth, Material mat)
        {
            var t = Piece(parent, PrimitiveType.Cube, mat).transform;
            var d = q - p;
            t.localPosition = new Vector3((p.x + q.x) * 0.5f, (p.y + q.y) * 0.5f, -depth * 0.5f);
            t.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(d.magnitude + w, w, depth);
        }

        /// <summary>Nota başı (eğik elips). hollow: 12 çubuktan halka.</summary>
        static void Head(Transform parent, Vector2 c, float s, bool hollow, float depth, Material mat)
        {
            float rx = 0.2f * s, ry = 0.13f * s;
            var rot = Quaternion.Euler(0, 0, 22f);
            if (!hollow)
            {
                var t = Piece(parent, PrimitiveType.Sphere, mat).transform;
                t.localPosition = new Vector3(c.x, c.y, -depth * 0.5f);
                t.localRotation = rot;
                t.localScale = new Vector3(rx * 2f, ry * 2f, depth);
                return;
            }
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Vector2 P(float a) { var v = rot * new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0); return c + new Vector2(v.x, v.y); }
                Bar(parent, P(a0), P(a1), s * 0.065f, depth, mat);
            }
        }

        /// <summary>Nota simgesi çiz (parent altına).</summary>
        public static void Draw(Transform parent, int id, Material mat, float size, float depth = 0.02f)
        {
            float s = size, w = s * 0.06f;
            var head = new Vector2(-0.05f * s, -0.32f * s);
            var stemX = head.x + 0.17f * s;
            switch (id)
            {
                case 0: Head(parent, new Vector2(0, -0.05f * s), s * 1.15f, true, depth, mat); break;
                case 1:
                    Head(parent, head, s, true, depth, mat);
                    Bar(parent, new Vector2(stemX, head.y + 0.04f * s), new Vector2(stemX, 0.45f * s), w, depth, mat);
                    break;
                case 2:
                    Head(parent, head, s, false, depth, mat);
                    Bar(parent, new Vector2(stemX, head.y + 0.04f * s), new Vector2(stemX, 0.45f * s), w, depth, mat);
                    break;
                case 3:
                    Head(parent, head, s, false, depth, mat);
                    Bar(parent, new Vector2(stemX, head.y + 0.04f * s), new Vector2(stemX, 0.45f * s), w, depth, mat);
                    Bar(parent, new Vector2(stemX, 0.45f * s), new Vector2(stemX + 0.2f * s, 0.12f * s), w * 1.1f, depth, mat);
                    break;
                case 4:
                    {
                        var h1 = new Vector2(-0.28f * s, -0.32f * s); var h2 = new Vector2(0.2f * s, -0.32f * s);
                        Head(parent, h1, s * 0.9f, false, depth, mat);
                        Head(parent, h2, s * 0.9f, false, depth, mat);
                        float x1 = h1.x + 0.15f * s, x2 = h2.x + 0.15f * s;
                        Bar(parent, new Vector2(x1, h1.y), new Vector2(x1, 0.4f * s), w, depth, mat);
                        Bar(parent, new Vector2(x2, h2.y), new Vector2(x2, 0.4f * s), w, depth, mat);
                        Bar(parent, new Vector2(x1, 0.4f * s), new Vector2(x2, 0.4f * s), w * 1.8f, depth, mat);
                    }
                    break;
                default:
                    Bar(parent, new Vector2(-0.12f * s, -0.45f * s), new Vector2(-0.12f * s, 0.4f * s), w, depth, mat);
                    Bar(parent, new Vector2(0.12f * s, -0.4f * s), new Vector2(0.12f * s, 0.45f * s), w, depth, mat);
                    Bar(parent, new Vector2(-0.3f * s, -0.08f * s), new Vector2(0.3f * s, 0.06f * s), w * 1.8f, depth, mat);
                    Bar(parent, new Vector2(-0.3f * s, 0.18f * s), new Vector2(0.3f * s, 0.32f * s), w * 1.8f, depth, mat);
                    break;
            }
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) Kill(parent.GetChild(i).gameObject);
        }
    }
}
