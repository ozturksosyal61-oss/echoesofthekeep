using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Kulede (A) sembol kadranı: her etkileşimde bir sonraki sembole döner. Değer = konum (0..5); konum → sembol eşlemesi
    /// mahzen tohumundan gelir (her denemede kadranların sırası da karışır). Gösterilen sembol yerel olarak çubuklardan kurulur.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class SymbolDial : PuzzleNode
    {
        [SerializeField] CryptPuzzle crypt;
        [SerializeField] int dialIndex;
        [SerializeField] Transform glyphAnchor;
        [SerializeField] Transform ring;
        [SerializeField] Material glyphMat;
        [SerializeField] float glyphSize = 0.7f;

        int shownSeed = -1, shownValue = -1;
        float ringAngle;

        public int Symbol(int seed) => CryptPuzzle.Make(seed).dialOrder[dialIndex][Mathf.Clamp(Value, 0, Glyphs.Count - 1)];

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            ServerSetValue((Value + 1) % Glyphs.Count);
        }

        public void ServerRandomize() => ServerSetValue(Random.Range(0, Glyphs.Count));

        protected override void OnValue(int value) => Refresh();

        void Update()
        {
            if (crypt != null && crypt.Seed != shownSeed) Refresh();
            if (ring != null)
            {
                float target = Mathf.Max(0, Value) * (360f / Glyphs.Count);
                ringAngle = Mathf.MoveTowardsAngle(ringAngle, target, 400f * Time.deltaTime);
                ring.localRotation = Quaternion.Euler(0, 0, ringAngle);
            }
        }

        void Refresh()
        {
            if (crypt == null || glyphAnchor == null) return;
            int seed = crypt.Seed;
            if (seed == shownSeed && Value == shownValue) return;
            shownSeed = seed; shownValue = Value;
            Glyphs.Clear(glyphAnchor);
            if (seed != 0) Glyphs.Symbol(glyphAnchor, Symbol(seed), glyphMat, glyphSize);
        }
    }
}
