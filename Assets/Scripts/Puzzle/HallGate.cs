using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Lobi Holü kapısı: bir oyuncu X, diğeri O plakasında `hold` sn birlikte durunca host rolleri atar ve
    /// X'i kuleye (yukarı), O'yu zindana (aşağı) ışınlar. Değer: 0 bekliyor, 1 ikisi de plakada (sayım), 2 yola çıkıldı.
    /// </summary>
    public class HallGate : PuzzleNode
    {
        public const int Waiting = 0, Counting = 1, Departed = 2;

        public static HallGate Instance { get; private set; }

        [SerializeField] PressurePlate plateX;
        [SerializeField] PressurePlate plateO;
        [SerializeField] float hold = 1.5f;
        [SerializeField] Renderer[] glow; // sayım sırasında parlayan plaka kakmaları
        [SerializeField] Color glowColor = new Color(1f, 0.69f, 0.4f);

        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;

        protected override void OnEnable() { base.OnEnable(); Instance = this; }
        protected override void OnDisable() { base.OnDisable(); if (Instance == this) Instance = null; }

        public override void ServerFixedUpdate()
        {
            if (Value == Departed || plateX == null || plateO == null) return;
            var x = plateX.Occupant(null);
            var o = plateO.Occupant(x);
            if (x == null || o == null)
            {
                if (Value != Waiting) ServerSetValue(Waiting);
                return;
            }
            if (Value == Waiting) { ServerSetValue(Counting, stamp: true); return; }
            if (M.TickSeconds(M.GetTick(NodeId), M.Runner.Tick) < hold) return;

            Dbg.Puzzle("Hol: X=" + x.name + " O=" + o.name + " yola çıkıyor");
            ServerSetValue(Departed);
            x.ServerAssignRole(Roles.X);
            o.ServerAssignRole(Roles.O);
        }

        public override void ServerReset() { }

        protected override void OnValue(int value)
        {
            if (glow == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in glow)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(Emission, value == Counting ? glowColor * 2.2f : glowColor * 0.25f);
                r.SetPropertyBlock(block);
            }
        }
    }
}
