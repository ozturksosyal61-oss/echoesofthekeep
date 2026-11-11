using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Çubuklardan çalışma zamanında kurulan semboller ve Roma rakamları (yerel XY düzlemi, yüz -Z). Işıklı malzemeyle çizilir:
    /// karanlıkta görünmez, yalnızca ışık (kibrit) varken okunur. Sembol çiftleri kasıtlı olarak benzer (kare/elmas, artı/çarpı, üçgen/ters üçgen).
    /// </summary>
    public static class Glyphs
    {
        public const int Count = 6;
        public static readonly string[] Names = { "Kare", "Elmas", "Artı", "Çarpı", "Üçgen", "Ters Üçgen" };

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) Object.Destroy(parent.GetChild(i).gameObject);
        }

        static void Bar(Transform parent, Vector2 p, Vector2 q, float w, float depth, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Bar";
            var t = go.transform;
            t.SetParent(parent, false);
            var d = q - p;
            t.localPosition = new Vector3((p.x + q.x) * 0.5f, (p.y + q.y) * 0.5f, -depth * 0.5f);
            t.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(d.magnitude + w, w, depth);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Vector2 Rot(Vector2 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        static void Poly(Transform parent, Vector2[] pts, float rot, float size, float w, float depth, Material mat, bool closed)
        {
            int n = closed ? pts.Length : pts.Length - 1;
            for (int i = 0; i < n; i++)
                Bar(parent, Rot(pts[i], rot) * size, Rot(pts[(i + 1) % pts.Length], rot) * size, w, depth, mat);
        }

        /// <summary>Sembol: boyut ~size (kenardan kenara).</summary>
        public static void Symbol(Transform parent, int id, Material mat, float size, float depth = 0.03f)
        {
            float w = size * 0.09f;
            var sq = new[] { new Vector2(-0.4f, -0.4f), new Vector2(0.4f, -0.4f), new Vector2(0.4f, 0.4f), new Vector2(-0.4f, 0.4f) };
            var tri = new[] { new Vector2(0f, 0.48f), new Vector2(-0.46f, -0.32f), new Vector2(0.46f, -0.32f) };
            switch (id)
            {
                case 0: Poly(parent, sq, 0f, size, w, depth, mat, true); break;
                case 1: Poly(parent, sq, 45f, size * 0.85f, w, depth, mat, true); break;
                case 2:
                case 3:
                    float r = id == 2 ? 0f : 45f;
                    Bar(parent, Rot(new Vector2(-0.5f, 0), r) * size, Rot(new Vector2(0.5f, 0), r) * size, w, depth, mat);
                    Bar(parent, Rot(new Vector2(0, -0.5f), r) * size, Rot(new Vector2(0, 0.5f), r) * size, w, depth, mat);
                    break;
                case 4: Poly(parent, tri, 0f, size, w, depth, mat, true); break;
                case 5: Poly(parent, tri, 180f, size, w, depth, mat, true); break;
            }
        }

        /// <summary>Roma rakamı (1..6), yükseklik ~size.</summary>
        public static void Numeral(Transform parent, int n, Material mat, float size, float depth = 0.03f)
        {
            float w = size * 0.1f, h = size * 0.4f, vw = size * 0.24f, gap = size * 0.3f;
            string s = n switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", _ => "" };
            float total = 0f;
            foreach (char c in s) total += c == 'I' ? gap * 0.6f : vw * 2f + gap * 0.3f;
            float x = -total * 0.5f;
            foreach (char c in s)
            {
                if (c == 'I')
                {
                    x += gap * 0.3f;
                    Bar(parent, new Vector2(x, -h), new Vector2(x, h), w, depth, mat);
                    x += gap * 0.3f;
                }
                else
                {
                    x += vw + gap * 0.15f;
                    Bar(parent, new Vector2(x - vw, h), new Vector2(x, -h), w, depth, mat);
                    Bar(parent, new Vector2(x + vw, h), new Vector2(x, -h), w, depth, mat);
                    x += vw + gap * 0.15f;
                }
            }
        }
    }
}
