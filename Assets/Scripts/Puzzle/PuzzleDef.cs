using System.Collections.Generic;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Sahnedeki bulmaca tanımı: id, başlangıçta aktif mi, çözülünce hangi bulmacaları açar. Ağ durumu PuzzleManager'da.</summary>
    public class PuzzleDef : MonoBehaviour
    {
        public static readonly List<PuzzleDef> All = new List<PuzzleDef>();

        [SerializeField, Range(0, PuzzleManager.MaxPuzzles - 1)] int id;
        [SerializeField] string title = "Bulmaca";
        [SerializeField] bool startsActive = true;
        [SerializeField] int[] unlocks = new int[0];

        public int Id => id;
        public string Title => title;
        public bool StartsActive => startsActive;
        public int[] Unlocks => unlocks;

        public PuzzleState State => PuzzleManager.Instance != null ? PuzzleManager.Instance.GetState(id) : PuzzleState.Locked;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static PuzzleDef Find(int id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return null;
        }

        public static string StateName(PuzzleState s) =>
            s == PuzzleState.Solved ? "çözüldü" : s == PuzzleState.Active ? "aktif" : "kilitli";
    }
}
