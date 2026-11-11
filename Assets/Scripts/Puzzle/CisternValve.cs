using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Sarnıç vanası (B): her etkileşim bir çeviriş (değer = çeviriş sayısı, görsel dönüş için). Etkisini CisternPuzzle uygular.
    /// Sembol ve boru rengi tohumdan yerelde kurulur.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class CisternValve : PuzzleNode
    {
        [SerializeField] CisternPuzzle cistern;
        [SerializeField] int valveIndex;
        [SerializeField] Transform wheel;
        [SerializeField] Transform symbolAnchor;
        [SerializeField] Renderer[] pipe = new Renderer[0];
        [SerializeField] Material symbolMat;

        int lastTurnTick = -100000;
        int shownSeed = -1;
        float angle;

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            int now = M.Runner.Tick;
            if ((now - lastTurnTick) * M.Runner.DeltaTime < CisternPuzzle.ValveCooldown) return;
            lastTurnTick = now;
            ServerSetValue(Value + 1);
            RaiseServerActivated();
        }

        public override void ServerReset() { } // çeviriş sayacı yalnızca görsel; sıfırlanmaz

        void Update()
        {
            if (cistern != null && cistern.Seed != shownSeed) Rebuild(cistern.Seed);
            if (wheel != null)
            {
                angle = Mathf.MoveTowards(angle, Value * 90f, 260f * Time.deltaTime);
                wheel.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        void Rebuild(int seed)
        {
            shownSeed = seed;
            if (symbolAnchor != null) Glyphs.Clear(symbolAnchor);
            if (seed == 0) return;
            var s = CisternPuzzle.Make(seed);
            if (symbolAnchor != null) Glyphs.Symbol(symbolAnchor, s.valveSymbol[valveIndex], symbolMat, 0.5f);
            var c = CisternPuzzle.Colors[s.valveColor[valveIndex]];
            foreach (var r in pipe)
            {
                if (r == null) continue;
                var m = r.material;
                m.color = c;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 0.35f);
            }
        }
    }
}
