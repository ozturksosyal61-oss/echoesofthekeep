using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 7 etkileşim düğümü: çan ipi (X), büyük çan tokmağı (X) ya da org tuşu (O). Değer = vuruş sayacı (ses/animasyon tetikler).
    /// Host etkileşimi BellPuzzle'a iletir. Görsel: çan sallanır / tuş çöker; ses BellView'de sentezlenir.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class BellNode : PuzzleNode
    {
        public enum Kind { Bell = 0, GreatBell = 1, Key = 2 }

        [SerializeField] Kind kind;
        [SerializeField] int index;
        [SerializeField] Transform swing;   // çan gövdesi (sallanır) / tuş (çöker)
        [SerializeField] float cooldown = 0.35f;

        int lastTick = -100000, shown = -1;
        float hitTime = -10f;
        Vector3 rest;
        Quaternion restRot;

        public Kind NodeKind => kind;
        public int Index => index;

        void Awake()
        {
            if (swing != null) { rest = swing.localPosition; restRot = swing.localRotation; }
        }

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            int now = M.Runner.Tick;
            if ((now - lastTick) * M.Runner.DeltaTime < cooldown) return;
            lastTick = now;
            ServerSetValue(Value + 1);
            var p = BellPuzzle.Instance;
            if (p == null) return;
            if (kind == Kind.Bell) p.ServerBell(index);
            else if (kind == Kind.GreatBell) p.ServerGreatBell();
            else p.ServerKey(index);
        }

        public override void ServerReset() { }

        protected override void OnValue(int value)
        {
            if (shown < 0 || value == shown) { shown = value; return; } // ilk değer: ses yok
            shown = value;
            if (value == 0) return;
            hitTime = Time.time;
            if (BellView.Instance != null) BellView.Instance.Play(kind, index, transform.position);
        }

        void Update()
        {
            if (swing == null) return;
            float t = Time.time - hitTime;
            if (kind == Kind.Key)
            {
                float d = t < 0.5f ? Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI) * 0.03f : 0f;
                swing.localPosition = rest + Vector3.down * d;
            }
            else
            {
                float amp = kind == Kind.GreatBell ? 7f : 14f;
                float a = t < 4f ? Mathf.Sin(t * 5.5f) * amp * Mathf.Exp(-t * 0.9f) : 0f;
                swing.localRotation = restRot * Quaternion.Euler(a, 0, 0);
            }
        }
    }
}
