using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// X'in arşivindeki şifre çarkı (bulmaca 4). Değer = iç halka konumu (0..5); her etkileşim bir adım döndürür.
    /// Dış halka: yuvalarda semboller (sabit), iç halka: harfler (döner). Üstteki plaka konumu Roma rakamıyla gösterir (değer+1).
    /// Yuvalar saat yönünde, tepeden başlayarak 60°; iç harf j, (j - değer) yuvasının karşısına gelir.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class CipherWheel : PuzzleNode
    {
        [SerializeField] LetterPuzzle letter;
        [SerializeField] Transform outerRing, innerRing, numeralAnchor;
        [SerializeField] Material glyphMat;
        [SerializeField] Font font;
        [SerializeField] float outerRadius = 0.85f, innerRadius = 0.52f, glyphSize = 0.26f, letterSize = 0.05f;
        int shownSeed = -1, shownValue = -1;
        float angle;

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            ServerSetValue((Value + 1) % LetterPuzzle.Ring);
        }

        static Vector3 Slot(int i, float r)
        {
            float a = i * Mathf.PI * 2f / LetterPuzzle.Ring;
            return new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r, 0f);
        }

        void Update()
        {
            int seed = letter != null ? letter.Seed : 0;
            if (seed != shownSeed) Rebuild(seed);
            if (Value != shownValue && numeralAnchor != null)
            {
                shownValue = Value;
                Glyphs.Clear(numeralAnchor);
                Glyphs.Numeral(numeralAnchor, Mathf.Clamp(Value, 0, 5) + 1, glyphMat, glyphSize * 0.8f, 0.02f);
            }
            if (innerRing != null)
            {
                float target = -Mathf.Max(0, Value) * (360f / LetterPuzzle.Ring); // iç halka saat yönünün tersine: harf j → yuva j-değer
                angle = Mathf.MoveTowardsAngle(angle, target, 240f * Time.deltaTime);
                innerRing.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        void Rebuild(int seed)
        {
            shownSeed = seed;
            if (outerRing != null) Glyphs.Clear(outerRing);
            if (innerRing != null) Glyphs.Clear(innerRing);
            if (seed == 0) return;
            var s = LetterPuzzle.Make(seed);
            for (int i = 0; i < LetterPuzzle.Ring; i++)
            {
                if (outerRing != null)
                {
                    var a = new GameObject("Slot" + i).transform;
                    a.SetParent(outerRing, false);
                    a.localPosition = Slot(i, outerRadius);
                    Glyphs.Symbol(a, s.outer[i], glyphMat, glyphSize, 0.02f);
                }
                if (innerRing != null)
                {
                    var t = new GameObject("Letter" + i);
                    t.transform.SetParent(innerRing, false);
                    t.transform.localPosition = Slot(i, innerRadius) + new Vector3(0, 0, -0.01f);
                    var tm = t.AddComponent<TextMesh>();
                    tm.font = font; tm.text = s.ring[i].ToString(); tm.fontSize = 64; tm.characterSize = letterSize;
                    tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(0.9f, 0.78f, 0.5f);
                    if (font != null) t.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
            }
        }
    }
}
