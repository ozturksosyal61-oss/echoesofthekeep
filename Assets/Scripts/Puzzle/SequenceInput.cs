using System;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Sıralı girdi doğrulama (host). inputs düğümleri tetiklendikçe order'daki sırayla karşılaştırır; değeri = ilerleme (0..n).
    /// Yanlış girdide değer -1 olur (hata), resetDelay sonra tüm girdiler ve ilerleme sıfırlanır. Tamamlanınca ServerSolve().
    /// Doğru sıra Inspector'dan (order: inputs dizisindeki indeksler) ayarlanır; iki cihaz da aynı sahneyi kullandığı için ayrıca senkron gerekmez.
    /// </summary>
    public class SequenceInput : PuzzleNode
    {
        public const int Error = -1;

        [SerializeField] PuzzleNode[] inputs = new PuzzleNode[0];
        [Tooltip("Doğru sıra: inputs dizisindeki indeksler (0 tabanlı)")]
        [SerializeField] int[] order = { 0, 1, 2, 3 };
        [SerializeField] float resetDelay = 1f;

        int resetAtTick = -1;

        public PuzzleNode[] Inputs => inputs;
        public int[] Order => order;
        public int Progress => Mathf.Max(0, Value);
        public bool Failed => Value == Error;

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (var i in inputs) if (i != null) i.ServerActivated += OnInput;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (var i in inputs) if (i != null) i.ServerActivated -= OnInput;
        }

        void OnInput(PuzzleNode node)
        {
            if (!IsServer || PuzzleState != PuzzleState.Active || resetAtTick >= 0 || order.Length == 0) return;
            if (node.Value == 0) return; // yalnızca "çekildi/basıldı" girdi sayılır
            int idx = Array.IndexOf(inputs, node);
            int step = Progress;
            if (idx == order[step])
            {
                ServerSetValue(step + 1);
                Dbg.Puzzle(name + " doğru: adım " + (step + 1) + "/" + order.Length + " (girdi " + idx + ")");
                if (step + 1 >= order.Length) ServerSolve();
            }
            else
            {
                ServerSetValue(Error);
                resetAtTick = M.Runner.Tick + Mathf.CeilToInt(resetDelay / M.Runner.DeltaTime);
                Dbg.Puzzle(name + " YANLIŞ: adım " + (step + 1) + " beklenen " + order[step] + " gelen " + idx + " → sıfırlanacak");
            }
        }

        public override void ServerFixedUpdate()
        {
            if (resetAtTick < 0 || M.Runner.Tick < resetAtTick) return;
            resetAtTick = -1;
            ServerReset();
        }

        public override void ServerReset()
        {
            base.ServerReset();
            foreach (var i in inputs) if (i != null) i.ServerReset();
        }

        void OnValidate()
        {
            for (int i = 0; i < order.Length; i++)
                if (order[i] < 0 || order[i] >= inputs.Length)
                    Debug.LogWarning("[Puzzle] " + name + ": order[" + i + "]=" + order[i] + " inputs aralığı dışında", this);
        }
    }
}
