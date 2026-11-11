using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca düğümü taban sınıfı (sahne nesnesi, ağ nesnesi değil). Değeri ve bulmaca durumu PuzzleManager'da senkronlanır;
    /// "Server*" metotları yalnızca host'ta etkilidir, OnValue/OnPuzzleState her iki tarafta (görseller) çağrılır.
    /// Aynı GameObject'te Interactable varsa etkileşim mevcut RPC zinciriyle (PlayerAvatar.RPC_Interact) ServerInteract'e gelir.
    /// </summary>
    public abstract class PuzzleNode : MonoBehaviour
    {
        /// <summary>Düğümlerin Interactable anahtarı: 100 + nodeId (1-2 lamba/kapı için ayrılmış).</summary>
        public const int InteractKeyBase = 100;

        public static readonly List<PuzzleNode> All = new List<PuzzleNode>();
        static readonly Dictionary<int, PuzzleNode> byId = new Dictionary<int, PuzzleNode>();

        [SerializeField, Range(0, PuzzleManager.MaxPuzzles - 1)] int puzzleId;
        [SerializeField, Range(0, PuzzleManager.MaxNodes - 1)] int nodeId;

        /// <summary>Host'ta: düğüm "tetiklendi" (kol çekildi, plaka basıldı/bırakıldı). Mantık bileşenleri (SequenceInput) dinler.</summary>
        public event Action<PuzzleNode> ServerActivated;

        public int PuzzleId => puzzleId;
        public int NodeId => nodeId;

        protected static PuzzleManager M => PuzzleManager.Instance;
        protected static bool IsServer => M != null && M.HasStateAuthority;
        public int Value => M != null ? M.GetValue(nodeId) : 0;
        public PuzzleState PuzzleState => M != null ? M.GetState(puzzleId) : PuzzleState.Locked;

        public static PuzzleNode Find(int nodeId) => byId.TryGetValue(nodeId, out var n) ? n : null;

        protected virtual void OnEnable()
        {
            if (byId.TryGetValue(nodeId, out var other) && other != this)
                Debug.LogError("[Puzzle] nodeId çakışması: " + nodeId + " (" + other.name + ", " + name + ")");
            byId[nodeId] = this;
            All.Add(this);
            PuzzleManager.NodeChanged += HandleNode;
            PuzzleManager.PuzzleChanged += HandlePuzzle;
            var it = GetComponent<Interactable>();
            if (it != null)
            {
                it.Key = InteractKeyBase + nodeId;
                it.ServerHandler = ServerInteract;
            }
        }

        protected virtual void OnDisable()
        {
            if (byId.TryGetValue(nodeId, out var n) && n == this) byId.Remove(nodeId);
            All.Remove(this);
            PuzzleManager.NodeChanged -= HandleNode;
            PuzzleManager.PuzzleChanged -= HandlePuzzle;
            var it = GetComponent<Interactable>();
            if (it != null) it.ServerHandler = null;
        }

        protected virtual void Start()
        {
            OnValue(Value);
            OnPuzzleState(PuzzleState);
        }

        void HandleNode(int id, int value) { if (id == nodeId) OnValue(value); }
        void HandlePuzzle(int id, PuzzleState old, PuzzleState now) { if (id == puzzleId) OnPuzzleState(now); }

        // ---- Host ----

        /// <summary>Host: bir oyuncu bu düğümle etkileşti (RPC doğrulandıktan sonra).</summary>
        public virtual void ServerInteract(PlayerAvatar who) { }

        /// <summary>Host: her simülasyon tick'i (PuzzleManager.FixedUpdateNetwork).</summary>
        public virtual void ServerFixedUpdate() { }

        /// <summary>Host: başlangıç değerine döndür.</summary>
        public virtual void ServerReset() => ServerSetValue(0);

        /// <summary>Host: bu düğümün bulmacasını çözülmüş yapar; durum ağ üzerinden iki tarafa yansır.</summary>
        public void ServerSolve()
        {
            if (!IsServer) return;
            Dbg.Puzzle(name + " → ServerSolve(bulmaca " + puzzleId + ")");
            M.ServerSetState(puzzleId, PuzzleState.Solved);
        }

        protected void ServerSetValue(int value, bool stamp = false)
        {
            if (IsServer) M.ServerSetValue(nodeId, value, stamp);
        }

        protected void RaiseServerActivated() => ServerActivated?.Invoke(this);

        // ---- Her iki taraf (görsel) ----

        protected virtual void OnValue(int value) { }
        protected virtual void OnPuzzleState(PuzzleState state) { }
    }
}
