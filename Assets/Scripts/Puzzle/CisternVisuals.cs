using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Sarnıç görselleri (iki tarafta): B odasındaki su yüzeyi, A'daki gösterge (su sütunu + hedef işareti),
    /// A'daki tablo (sembol → miktar, ters akan borunun rengi), giriş kapısı ve sonraki geçidin kapısı.
    /// </summary>
    public class CisternVisuals : MonoBehaviour
    {
        [SerializeField] CisternPuzzle cistern;
        [SerializeField] Transform water;          // B: yüzey (yerel y = seviye * LevelHeight)
        [SerializeField] Transform gaugeColumn;    // A: ölçek y = seviye/Max (pivot altta)
        [SerializeField] Transform gaugeTarget;    // A: hedef işareti
        [SerializeField] float gaugeUnit = 0.38f;  // A göstergesinde seviye başına yükseklik
        [SerializeField] Transform[] chartSymbols = new Transform[0];
        [SerializeField] Transform[] chartAmounts = new Transform[0];
        [SerializeField] Renderer reversedSwatch;
        [SerializeField] Material chartMat;
        [SerializeField] LiftDoor gate;
        [SerializeField] LiftDoor nextGate;

        int shownSeed = -1;
        float shownLevel;
        Vector3 waterBase, targetBase;

        void Awake()
        {
            if (water != null) waterBase = water.localPosition;
            if (gaugeTarget != null) targetBase = gaugeTarget.localPosition;
        }

        void Update()
        {
            if (cistern == null) return;
            if (cistern.Seed != shownSeed) Rebuild(cistern.Seed);

            shownLevel = Mathf.MoveTowards(shownLevel, cistern.Level, 1.6f * Time.deltaTime);
            if (water != null) water.localPosition = waterBase + Vector3.up * (shownLevel * CisternPuzzle.LevelHeight);
            if (gaugeColumn != null)
            {
                var s = gaugeColumn.localScale;
                s.y = Mathf.Max(0.001f, shownLevel * gaugeUnit);
                gaugeColumn.localScale = s;
            }

            bool solved = cistern.PuzzleState == PuzzleState.Solved;
            if (gate != null) gate.Open = solved || cistern.CurrentPhase == CisternPuzzle.Phase.Open;
            if (nextGate != null) nextGate.Open = solved;
        }

        void Rebuild(int seed)
        {
            shownSeed = seed;
            foreach (var t in chartSymbols) Glyphs.Clear(t);
            foreach (var t in chartAmounts) Glyphs.Clear(t);
            if (seed == 0) return;
            var s = CisternPuzzle.Make(seed);
            // Tablo sırası vana sırasından bağımsız: semboller kendi sırasıyla (sıralı) listelenir
            var order = new int[CisternPuzzle.Valves];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            System.Array.Sort(order, (a, b) => s.valveSymbol[a].CompareTo(s.valveSymbol[b]));
            for (int row = 0; row < chartSymbols.Length && row < order.Length; row++)
            {
                int v = order[row];
                Glyphs.Symbol(chartSymbols[row], s.valveSymbol[v], chartMat, 0.45f);
                Glyphs.Amount(chartAmounts[row], s.valveAmount[v], chartMat, 0.4f);
            }
            if (reversedSwatch != null)
            {
                var m = reversedSwatch.material;
                var c = CisternPuzzle.Colors[s.reversedColor];
                m.color = c;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 0.5f);
            }
            if (gaugeTarget != null) gaugeTarget.localPosition = targetBase + Vector3.up * (s.target * gaugeUnit);
        }
    }
}
