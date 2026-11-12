using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 4 "Şifreli mektup" (host mantığı). O'nun kütüphanesinde kürsüde mektup: 5 sembol + mühürde Roma rakamı (anahtar).
    /// X'in arşivinde şifre çarkı: dış halkada 6 sembol, dönen iç halkada 6 harf. X çarkı anahtara getirir (plaka = rakam),
    /// O'nun okuduğu sembollerin karşısındaki harflerden kelimeyi bulur. O kapıdaki 5 harf kadranını kelimeye ayarlar → çözülür.
    /// Yanlış anahtarda harfler anlamsız çıkar. Başarısızlık yok (ara nefes bulmacası). Bu düğümün değeri = tohum.
    /// </summary>
    public class LetterPuzzle : PuzzleNode
    {
        public const int Len = 5, Ring = 6;
        static readonly string[] Words = { "MEZAR", "TABUT", "KAPAN", "DUMAN", "KARGA", "ZAMAN", "BALTA", "ORMAN" };
        const string Decoys = "BCDEGHKLMNOPRSTUVYZ";

        [SerializeField] LockDial[] dials = new LockDial[0];
        [SerializeField] Transform[] letterGlyphs = new Transform[0]; // O: mektuptaki 5 sembol
        [SerializeField] Transform sealGlyph;                          // O: mühürdeki rakam
        [SerializeField] Material inkMat, sealMat;
        [SerializeField] float glyphSize = 0.16f;

        public static LetterPuzzle Instance { get; private set; }
        public int Seed => Value;
        int shownSeed = -1;

        public struct Setup
        {
            public string word;
            public char[] ring;     // iç halka harfleri (yuva sırası)
            public int[] outer;     // dış halka: yuva → sembol
            public int key;         // doğru çark konumu (0..5); mühürde key+1
            public int[] letter;    // mektuptaki semboller
            public char[] alphabet; // kilit kadranı harfleri (alfabetik; eşlemeyi ele vermez)
            public char RingAt(int outerSlot, int wheel) => ring[(outerSlot + wheel) % Ring];
        }

        public static Setup Make(int seed)
        {
            var r = new System.Random(seed);
            var s = new Setup { word = Words[r.Next(Words.Length)] };
            var letters = new System.Collections.Generic.List<char>();
            foreach (char c in s.word) if (!letters.Contains(c)) letters.Add(c);
            while (letters.Count < Ring)
            {
                char c = Decoys[r.Next(Decoys.Length)];
                if (!letters.Contains(c)) letters.Add(c);
            }
            s.ring = letters.ToArray();
            for (int i = Ring - 1; i > 0; i--) { int j = r.Next(i + 1); (s.ring[i], s.ring[j]) = (s.ring[j], s.ring[i]); }
            s.outer = new int[Ring];
            for (int i = 0; i < Ring; i++) s.outer[i] = i;
            for (int i = Ring - 1; i > 0; i--) { int j = r.Next(i + 1); (s.outer[i], s.outer[j]) = (s.outer[j], s.outer[i]); }
            s.key = r.Next(Ring);
            s.letter = new int[Len];
            for (int k = 0; k < Len; k++)
            {
                int idx = System.Array.IndexOf(s.ring, s.word[k]);
                int slot = ((idx - s.key) % Ring + Ring) % Ring;
                s.letter[k] = s.outer[slot];
            }
            s.alphabet = (char[])s.ring.Clone();
            System.Array.Sort(s.alphabet);
            return s;
        }

        protected override void OnEnable() { base.OnEnable(); Instance = this; }
        protected override void OnDisable() { base.OnDisable(); if (Instance == this) Instance = null; }

        public override void ServerFixedUpdate()
        {
            // Tohum çözülmüş durumda da üretilir (gezinti / "Bulmaca N" ile atlanınca mektup, çark ve kadranlar boş kalmasın)
            if (Seed == 0 && PuzzleState != PuzzleState.Locked) { ServerSetValue(Random.Range(1, int.MaxValue)); return; }
            if (PuzzleState != PuzzleState.Active) return;
            var s = Make(Seed);
            for (int k = 0; k < Len && k < dials.Length; k++)
                if (dials[k] == null || s.alphabet[Mathf.Clamp(dials[k].Value, 0, Ring - 1)] != s.word[k]) return;
            Dbg.Puzzle("Mektup çözüldü: " + s.word);
            ServerSolve();
        }

        void Update()
        {
            if (Seed == shownSeed) return;
            shownSeed = Seed;
            foreach (var g in letterGlyphs) if (g != null) Glyphs.Clear(g);
            if (sealGlyph != null) Glyphs.Clear(sealGlyph);
            if (Seed == 0) return;
            var s = Make(Seed);
            for (int k = 0; k < Len && k < letterGlyphs.Length; k++)
                if (letterGlyphs[k] != null) Glyphs.Symbol(letterGlyphs[k], s.letter[k], inkMat, glyphSize, 0.01f);
            if (sealGlyph != null) Glyphs.Numeral(sealGlyph, s.key + 1, sealMat, glyphSize * 0.9f, 0.01f);
        }
    }
}
