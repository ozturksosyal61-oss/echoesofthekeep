using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 5'e özgü simge seti (Glyphs'ten farklı): çubuklardan rün işaretleri + zar noktaları (1..6). Yerel XY düzlemi, yüz -Z.
    /// Rünler kasıtlı olarak birbirine benzer çiftler içerir (Algiz/Tiwaz, Fehu/Ansuz) — tarif ederken dikkat ister.
    /// </summary>
    public static class Runes
    {
        public const int Count = 6;
        public static readonly string[] Names = { "Fehu", "Ansuz", "Tiwaz", "Algiz", "Dagaz", "Sowilo" };

        static void Bar(Transform parent, Vector2 p, Vector2 q, float w, float depth, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Kill(go.GetComponent<Collider>());
            go.name = "Bar";
            var t = go.transform;
            t.SetParent(parent, false);
            var d = q - p;
            t.localPosition = new Vector3((p.x + q.x) * 0.5f, (p.y + q.y) * 0.5f, -depth * 0.5f);
            t.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(d.magnitude + w, w, depth);
            Setup(go, mat);
        }

        static void Kill(Object o) { if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        static void Setup(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void Lines(Transform parent, float size, float w, float depth, Material mat, params float[] xy)
        {
            for (int i = 0; i + 3 < xy.Length; i += 4)
                Bar(parent, new Vector2(xy[i], xy[i + 1]) * size, new Vector2(xy[i + 2], xy[i + 3]) * size, w, depth, mat);
        }

        /// <summary>Rün: yükseklik ~size.</summary>
        public static void Rune(Transform parent, int id, Material mat, float size, float depth = 0.03f)
        {
            float w = size * 0.09f;
            switch (id)
            {
                case 0: Lines(parent, size, w, depth, mat, -0.2f, -0.5f, -0.2f, 0.5f, -0.2f, 0.1f, 0.25f, 0.45f, -0.2f, -0.15f, 0.25f, 0.2f); break;   // Fehu ᚠ
                case 1: Lines(parent, size, w, depth, mat, -0.2f, -0.5f, -0.2f, 0.5f, -0.2f, 0.5f, 0.25f, 0.15f, -0.2f, 0.2f, 0.25f, -0.15f); break;   // Ansuz ᚨ
                case 2: Lines(parent, size, w, depth, mat, 0f, -0.5f, 0f, 0.5f, 0f, 0.5f, -0.3f, 0.15f, 0f, 0.5f, 0.3f, 0.15f); break;                // Tiwaz ᛏ
                case 3: Lines(parent, size, w, depth, mat, 0f, -0.5f, 0f, 0.5f, 0f, 0f, -0.3f, 0.45f, 0f, 0f, 0.3f, 0.45f); break;                    // Algiz ᛉ
                case 4: Lines(parent, size, w, depth, mat, -0.3f, -0.5f, -0.3f, 0.5f, 0.3f, -0.5f, 0.3f, 0.5f, -0.3f, 0.5f, 0.3f, -0.5f, -0.3f, -0.5f, 0.3f, 0.5f); break; // Dagaz ᛞ
                case 5: Lines(parent, size, w, depth, mat, 0.15f, 0.5f, -0.2f, 0.15f, -0.2f, 0.15f, 0.2f, -0.15f, 0.2f, -0.15f, -0.15f, -0.5f); break; // Sowilo ᛋ
            }
        }

        /// <summary>Zar yüzü noktaları (1..6), kenardan kenara ~size.</summary>
        public static void Pips(Transform parent, int n, Material mat, float size, float depth = 0.03f)
        {
            float a = size * 0.32f, r = size * 0.2f;
            var pts = n switch
            {
                1 => new[] { Vector2.zero },
                2 => new[] { new Vector2(-a, a), new Vector2(a, -a) },
                3 => new[] { new Vector2(-a, a), Vector2.zero, new Vector2(a, -a) },
                4 => new[] { new Vector2(-a, a), new Vector2(a, a), new Vector2(-a, -a), new Vector2(a, -a) },
                5 => new[] { new Vector2(-a, a), new Vector2(a, a), Vector2.zero, new Vector2(-a, -a), new Vector2(a, -a) },
                _ => new[] { new Vector2(-a, a), new Vector2(a, a), new Vector2(-a, 0), new Vector2(a, 0), new Vector2(-a, -a), new Vector2(a, -a) },
            };
            foreach (var p in pts)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Kill(go.GetComponent<Collider>());
                go.name = "Pip";
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(p.x, p.y, -depth * 0.5f);
                go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                go.transform.localScale = new Vector3(r, depth * 0.5f, r);
                Setup(go, mat);
            }
        }
    }
}
