using UnityEngine;

namespace CoopGame
{
    /// <summary>Bulmaca çıktısı (yalnızca görsel): bulmaca çözülünce hedef dişliler dönmeye başlar. Ağ kodu yok; PuzzleManager olayını dinler.</summary>
    public class PuzzleSpin : MonoBehaviour
    {
        [SerializeField, Range(0, PuzzleManager.MaxPuzzles - 1)] int puzzleId;
        [SerializeField] Spin[] targets = new Spin[0];

        void OnEnable() => PuzzleManager.PuzzleChanged += OnPuzzle;
        void OnDisable() => PuzzleManager.PuzzleChanged -= OnPuzzle;

        void Start()
        {
            var m = PuzzleManager.Instance;
            Apply(m != null && m.GetState(puzzleId) == PuzzleState.Solved);
        }

        void OnPuzzle(int id, PuzzleState old, PuzzleState now)
        {
            if (id == puzzleId) Apply(now == PuzzleState.Solved);
        }

        void Apply(bool on)
        {
            foreach (var s in targets) if (s != null) s.enabled = on;
        }
    }
}
