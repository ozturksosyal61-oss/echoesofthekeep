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
            // Tek kişilik test (editör / geliştirme sürümü): oturumda tek oyuncu varsa kendi plakasında rolünü alır
            var solo = (x == null) != (o == null) && PlayerAvatar.All.Count == 1 && (Application.isEditor || Debug.isDebugBuild || Session.Review) ? (x != null ? x : o) : null;
            if ((x == null || o == null) && solo == null)
            {
                if (Value != Waiting) ServerSetValue(Waiting);
                return;
            }
            if (Value == Waiting) { ServerSetValue(Counting, stamp: true); return; }
            if (M.TickSeconds(M.GetTick(NodeId), M.Runner.Tick) < hold) return;

            if (solo != null)
            {
                Dbg.Puzzle("Hol (tek kişi test): " + solo.name + " → " + (x != null ? "X" : "O"));
                ServerSetValue(Departed);
                solo.ServerAssignRole(x != null ? Roles.X : Roles.O);
                return;
            }
            Dbg.Puzzle("Hol: X=" + x.name + " O=" + o.name + " yola çıkıyor");
            ServerSetValue(Departed);
            x.ServerAssignRole(Roles.X);
            o.ServerAssignRole(Roles.O);
        }

        public override void ServerReset() { }

        protected override void OnValue(int value) => ApplyGlow(value);

        // Bekleme: plakalar ve levhalar nabız gibi parlar (lobide yeri belli olsun); sayım: tam parlak; yola çıkınca sönük.
        void Update()
        {
            if (Value == Waiting) ApplyGlow(Waiting);
        }

        void ApplyGlow(int value)
        {
            if (glow == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            float k = value == Counting ? 3f : value == Departed ? 0.35f : 1.1f + 0.45f * Mathf.Sin(Time.time * 2.2f);
            foreach (var r in glow)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(Emission, glowColor * k);
                r.SetPropertyBlock(block);
            }
        }
    }
}
