using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Odalar arası geçit (host): bulmacası (puzzleId) çözülmüşse bölgeye giren oyuncuyu hedefe ışınlar
    /// (uzun merdiven/kat geçişinin yerine; geçit karanlık bir kemer olarak görünür).
    /// </summary>
    public class Passage : PuzzleNode
    {
        [SerializeField] Vector3 zoneCenter, zoneSize; // dünya
        [SerializeField] Transform target;

        public override void ServerFixedUpdate()
        {
            if (PuzzleState != PuzzleState.Solved || target == null) return;
            var zone = new Bounds(zoneCenter, zoneSize);
            foreach (var a in PlayerAvatar.All)
                if (a != null && zone.Contains(a.transform.position + Vector3.up * 0.5f))
                {
                    a.GetComponent<NetworkCharacterController>().Teleport(target.position, target.rotation);
                    Dbg.Puzzle(a.name + " geçit → " + target.name);
                }
        }
    }
}
