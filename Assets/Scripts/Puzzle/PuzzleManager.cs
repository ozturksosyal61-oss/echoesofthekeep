using System;
using Fusion;
using UnityEngine;

namespace CoopGame
{
    public enum PuzzleState : byte { Locked = 0, Active = 1, Solved = 2 }

    /// <summary>
    /// Bulmaca ağ durumu (state authority host; RoomState ile aynı NetworkObject'te spawn olur).
    /// Bulmaca id'si başına durum (Kilitli/Aktif/Çözüldü), düğüm id'si başına int değer + zaman damgası (tick).
    /// Değişiklikler Render'da yerel kopyayla karşılaştırılıp olay olarak yayılır: host ve client'ta aynı şekilde, geç katılanda da.
    /// Sahne tarafı (PuzzleNode/PuzzleDef/PuzzleDoor) ağ nesnesi değildir; id ile buraya bağlanır.
    /// </summary>
    public class PuzzleManager : NetworkBehaviour
    {
        public const int MaxPuzzles = 16;
        public const int MaxNodes = 96;

        public static PuzzleManager Instance { get; private set; }

        /// <summary>(puzzleId, eski, yeni). Her iki tarafta Render'dan.</summary>
        public static event Action<int, PuzzleState, PuzzleState> PuzzleChanged;
        /// <summary>(nodeId, yeni değer). Her iki tarafta Render'dan.</summary>
        public static event Action<int, int> NodeChanged;

        [Networked, Capacity(MaxPuzzles)] public NetworkArray<byte> States => default;
        [Networked, Capacity(MaxNodes)] public NetworkArray<int> Values => default;
        [Networked, Capacity(MaxNodes)] public NetworkArray<int> Ticks => default;

        // İstatistik (host sayar, kapanış ekranında gösterilir; PlayStats kaydeder)
        [Networked] public int Deaths { get; set; }
        [Networked] public int HintsUsed { get; set; }
        [Networked] public float PlaySeconds { get; set; }
        [Networked, Capacity(MaxPuzzles)] public NetworkArray<float> PuzzleSeconds => default;
        float saveAt;

        readonly byte[] seenStates = new byte[MaxPuzzles];
        readonly int[] seenValues = new int[MaxNodes];

        public override void Spawned()
        {
            Instance = this;
            if (HasStateAuthority)
                foreach (var d in PuzzleDef.All)
                    if (d.StartsActive && GetState(d.Id) == PuzzleState.Locked) ServerSetState(d.Id, PuzzleState.Active);
            if (HasStateAuthority)
            {   // Menüde seçilen başlangıç: öncekiler sırayla çözülmüş sayılır (kilit açma zinciri sonrakini aktif eder)
                int start = Mathf.Clamp(Progress.StartPuzzle, 1, Progress.LastPuzzle + 1); // Last+1: hepsi çözülmüş (gezinti)
                PlayStats.Load(this, start);
                for (int id = 1; id < start; id++) ServerSetState(id, PuzzleState.Solved);
                Progress.Save(start - 1);
            }
            Dbg.Puzzle("PuzzleManager Spawned HasStateAuthority=" + HasStateAuthority + " defs=" + PuzzleDef.All.Count + " nodes=" + PuzzleNode.All.Count);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        public static bool ValidPuzzle(int id) => id >= 0 && id < MaxPuzzles;
        public static bool ValidNode(int id) => id >= 0 && id < MaxNodes;

        public PuzzleState GetState(int puzzleId) => ValidPuzzle(puzzleId) ? (PuzzleState)States[puzzleId] : PuzzleState.Locked;
        public int GetValue(int nodeId) => ValidNode(nodeId) ? Values[nodeId] : 0;
        public int GetTick(int nodeId) => ValidNode(nodeId) ? Ticks[nodeId] : 0;

        /// <summary>Yalnızca host. Çözülünce PuzzleDef.Unlocks listesindeki kilitli bulmacaları aktif eder.</summary>
        public void ServerSetState(int puzzleId, PuzzleState s)
        {
            if (!HasStateAuthority || !ValidPuzzle(puzzleId)) return;
            var old = GetState(puzzleId);
            if (old == s) return;
            States.Set(puzzleId, (byte)s);
            Dbg.Puzzle("Bulmaca " + puzzleId + ": " + old + " → " + s);
            if (s != PuzzleState.Solved) return;
            Progress.Save(puzzleId);
            PlayStats.Save(this, puzzleId);
            var def = PuzzleDef.Find(puzzleId);
            if (def == null || def.Unlocks == null) return;
            foreach (var u in def.Unlocks)
                if (GetState(u) == PuzzleState.Locked) ServerSetState(u, PuzzleState.Active);
        }

        /// <summary>Yalnızca host. stamp: zaman damgasını şimdiki tick yap (ör. basınç plakasına basıldığı an).</summary>
        public void ServerSetValue(int nodeId, int value, bool stamp = false)
        {
            if (!HasStateAuthority || !ValidNode(nodeId)) return;
            Values.Set(nodeId, value);
            if (stamp) Ticks.Set(nodeId, Runner.Tick);
        }

        /// <summary>İki tick arasındaki süre (sn).</summary>
        public float TickSeconds(int fromTick, int toTick) => (toTick - fromTick) * Runner.DeltaTime;

        /// <summary>Yalnızca host: ölüm (mahzen, sarnıç, labirent).</summary>
        public void ServerAddDeath() { if (HasStateAuthority) { Deaths++; PlayStats.Save(this, Progress.Saved); } }

        /// <summary>Yalnızca host: bir oyuncu ipucu açtı.</summary>
        public void ServerAddHint() { if (HasStateAuthority) { HintsUsed++; PlayStats.Save(this, Progress.Saved); } }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (GetState(Progress.LastPuzzle) != PuzzleState.Solved)
            {   // süre: oyun bitene kadar; bulmaca başına: aktif olduğu sürece
                float dt = Runner.DeltaTime;
                PlaySeconds += dt;
                for (int id = 1; id <= Progress.LastPuzzle; id++)
                    if (GetState(id) == PuzzleState.Active) PuzzleSeconds.Set(id, PuzzleSeconds[id] + dt);
                if (Time.unscaledTime >= saveAt) { saveAt = Time.unscaledTime + 30f; PlayStats.Save(this, Progress.Saved); }
            }
            var nodes = PuzzleNode.All;
            for (int i = 0; i < nodes.Count; i++) nodes[i].ServerFixedUpdate();
        }

        public override void Render()
        {
            for (int i = 0; i < MaxPuzzles; i++)
            {
                byte s = States[i];
                if (s == seenStates[i]) continue;
                var old = (PuzzleState)seenStates[i];
                seenStates[i] = s;
                PuzzleChanged?.Invoke(i, old, (PuzzleState)s);
            }
            for (int i = 0; i < MaxNodes; i++)
            {
                int v = Values[i];
                if (v == seenValues[i]) continue;
                seenValues[i] = v;
                NodeChanged?.Invoke(i, v);
            }
        }
    }
}
