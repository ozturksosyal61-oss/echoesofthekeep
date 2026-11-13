using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Mahzen bulmacasının dünya görselleri (iki tarafta, ağ kodu yok): lahit sembol/rakamları ve kule yazıtı tohumdan kurulur,
    /// kuledeki 3 mum B'nin kalan kibritini gösterir, giriş kapısı mahzen kapalıyken iner, çözülünce mangallar yanar ve ödül kapısı açılır.
    /// </summary>
    public class CryptVisuals : MonoBehaviour
    {
        [SerializeField] CryptPuzzle crypt;
        [SerializeField] Transform[] coffinSymbols = new Transform[0];
        [SerializeField] Transform[] coffinNumerals = new Transform[0];
        [SerializeField] Transform[] inscription = new Transform[0];
        [SerializeField] Material carveMat;        // lahitler: ışıklı (karanlıkta görünmez)
        [SerializeField] Material inscriptionMat;  // kule yazıtı
        [SerializeField] GameObject[] candleFlames = new GameObject[0];
        [SerializeField] LiftDoor gate;
        [SerializeField] LiftDoor rewardGate;
        [SerializeField] GameObject[] solvedOn = new GameObject[0];

        int shownSeed = -1;
        int carveShown = -1; // -1: yeniden uygula

        void Update()
        {
            if (crypt == null) return;
            if (crypt.Seed != shownSeed) Rebuild(crypt.Seed);

            bool solved = crypt.PuzzleState == PuzzleState.Solved;
            var phase = crypt.CurrentPhase;
            if (gate != null) gate.Open = solved || phase == CryptPuzzle.Phase.Open;
            if (rewardGate != null) rewardGate.Open = solved;
            foreach (var g in solvedOn) if (g != null && g.activeSelf != solved) g.SetActive(solved);

            // Lahit oymaları yalnız kibrit yanarken (ya da çözülünce) görünür; ortam ışığında parlamasın.
            int carve = solved || crypt.Lit ? 1 : 0;
            if (carve != carveShown) { carveShown = carve; SetCarveVisible(carve == 1); }

            int m = crypt.PuzzleState == PuzzleState.Active ? crypt.Matches : solved ? 0 : CryptPuzzle.MatchCount;
            for (int i = 0; i < candleFlames.Length; i++)
            {
                bool on = i < m || (i == m && crypt.Lit);
                if (candleFlames[i] != null && candleFlames[i].activeSelf != on) candleFlames[i].SetActive(on);
            }
        }

        void SetCarveVisible(bool on)
        {
            foreach (var t in coffinSymbols) SetRenderers(t, on);
            foreach (var t in coffinNumerals) SetRenderers(t, on);
        }

        static void SetRenderers(Transform t, bool on)
        {
            if (t == null) return;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        }

        void Rebuild(int seed)
        {
            shownSeed = seed;
            carveShown = -1;
            foreach (var t in coffinSymbols) Glyphs.Clear(t);
            foreach (var t in coffinNumerals) Glyphs.Clear(t);
            foreach (var t in inscription) Glyphs.Clear(t);
            if (seed == 0) return;
            var s = CryptPuzzle.Make(seed);
            for (int i = 0; i < coffinSymbols.Length && i < CryptPuzzle.Coffins; i++)
            {
                Glyphs.Symbol(coffinSymbols[i], s.coffinSymbol[i], carveMat, 0.5f);
                Glyphs.Numeral(coffinNumerals[i], s.coffinNumeral[i], carveMat, 0.32f);
            }
            for (int k = 0; k < inscription.Length && k < s.inscription.Length; k++)
                Glyphs.Numeral(inscription[k], s.inscription[k], inscriptionMat, 0.55f);
        }
    }
}
