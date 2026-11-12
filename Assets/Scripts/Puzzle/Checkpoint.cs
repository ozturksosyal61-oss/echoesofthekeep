using System.Collections.Generic;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Kontrol noktası işareti (sahne): bu bulmacadan başlayan rolün doğduğu yer.</summary>
    public class Checkpoint : MonoBehaviour
    {
        static readonly List<Checkpoint> all = new List<Checkpoint>();
        [SerializeField] int puzzle;
        [SerializeField] int role;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static bool TryFind(int role, int puzzle, out Vector3 pos)
        {
            foreach (var c in all)
                if (c.role == role && c.puzzle == puzzle) { pos = c.transform.position; return true; }
            pos = default;
            return false;
        }
    }
}
