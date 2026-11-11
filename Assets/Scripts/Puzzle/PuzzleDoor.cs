using UnityEngine;

namespace CoopGame
{
    /// <summary>Bulmaca çıktısı: bulmaca çözülünce kapıyı açar (LiftDoor). Ağ kodu yok; PuzzleManager olayını dinler.</summary>
    public class PuzzleDoor : MonoBehaviour
    {
        [SerializeField, Range(0, PuzzleManager.MaxPuzzles - 1)] int puzzleId;
        [SerializeField] LiftDoor door;

        void OnEnable() => PuzzleManager.PuzzleChanged += OnPuzzle;
        void OnDisable() => PuzzleManager.PuzzleChanged -= OnPuzzle;

        void Start()
        {
            var m = PuzzleManager.Instance;
            door.Open = m != null && m.GetState(puzzleId) == PuzzleState.Solved;
        }

        void OnPuzzle(int id, PuzzleState old, PuzzleState now)
        {
            if (id != puzzleId) return;
            door.Open = now == PuzzleState.Solved;
            Dbg.Puzzle("PuzzleDoor(" + name + ") bulmaca " + id + " " + now + " → kapı " + (door.Open ? "açık" : "kapalı"));
        }
    }
}
