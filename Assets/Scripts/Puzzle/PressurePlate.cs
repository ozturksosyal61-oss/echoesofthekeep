using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Basınç plakası: host her tick üstünde avatar var mı bakar; basılı durumu (değer 0/1) ve basıldığı anın tick'ini senkronlar.
    /// Ortak pencere: PressedTogether(a, b, sn) iki plakanın basılı ve basılma anlarının en fazla `window` sn arayla olduğunu kontrol eder.
    /// </summary>
    public class PressurePlate : PuzzleNode
    {
        [SerializeField] Vector2 size = new Vector2(1.6f, 1.6f); // x/z (yerel)
        [SerializeField] float height = 1f;
        [SerializeField] Transform top;
        [SerializeField] float sink = 0.06f;

        Vector3 topUp;

        public bool Pressed => Value != 0;
        public int PressedTick => M != null ? M.GetTick(NodeId) : 0;

        void Awake() { if (top != null) topUp = top.localPosition; }

        /// <summary>Plakanın üstündeki avatar (except hariç); yoksa null.</summary>
        public PlayerAvatar Occupant(PlayerAvatar except)
        {
            foreach (var a in PlayerAvatar.All)
            {
                if (a == except) continue;
                var p = transform.InverseTransformPoint(a.transform.position);
                if (Mathf.Abs(p.x) <= size.x * 0.5f && Mathf.Abs(p.z) <= size.y * 0.5f && p.y >= -0.2f && p.y <= height) return a;
            }
            return null;
        }

        public override void ServerFixedUpdate()
        {
            bool any = Occupant(null) != null;
            if (any == Pressed) return;
            ServerSetValue(any ? 1 : 0, stamp: any);
            Dbg.Puzzle(name + (any ? " basıldı tick=" + PressedTick : " bırakıldı"));
            RaiseServerActivated();
        }

        public static bool PressedTogether(PressurePlate a, PressurePlate b, float window)
        {
            if (M == null || a == null || b == null || !a.Pressed || !b.Pressed) return false;
            return Mathf.Abs(M.TickSeconds(a.PressedTick, b.PressedTick)) <= window;
        }

        protected override void OnValue(int value)
        {
            if (top != null) top.localPosition = topUp - Vector3.up * (value != 0 ? sink : 0f);
        }
    }
}
