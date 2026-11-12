using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 6 döner ayna (X, çatı): her etkileşim bir adım döndürür (değer = toplam adım). steps=16 → 22.5°, steps=8 → 45°.
    /// pivot görsel olarak döner; normal yönü (NormalAz) çatı yerelinde baseAz + adım * açı.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class MoonMirror : PuzzleNode
    {
        [SerializeField] int steps = MoonPuzzle.MirrorSteps;
        [SerializeField] float baseAz;
        [SerializeField] Transform pivot;
        [SerializeField] float cooldown = 0.3f;

        int lastTick = -100000;
        float shownAz;
        bool init;

        public int Steps => steps;
        public int Step => ((Value % steps) + steps) % steps;
        public float NormalAz => baseAz + Step * 360f / steps;

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            int now = M.Runner.Tick;
            if ((now - lastTick) * M.Runner.DeltaTime < cooldown) return;
            lastTick = now;
            ServerSetValue(Step + 1);
        }

        /// <summary>Host: bulut sonrası rastgele konum.</summary>
        public void ServerScramble() => ServerSetValue(Random.Range(0, steps));

        public override void ServerReset() { }

        void Update()
        {
            if (pivot == null) return;
            float target = NormalAz;
            if (!init) { shownAz = target; init = true; }
            shownAz = Mathf.MoveTowardsAngle(shownAz, target, 220f * Time.deltaTime);
            pivot.localRotation = Quaternion.Euler(0, shownAz, 0);
        }
    }
}
