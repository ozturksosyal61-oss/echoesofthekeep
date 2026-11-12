using UnityEngine;

namespace CoopGame
{
    /// <summary>O'nun kapısındaki harf kadranı (bulmaca 4). Değer = alfabe indeksi (0..5); her etkileşim bir sonraki harf.</summary>
    [RequireComponent(typeof(Interactable))]
    public class LockDial : PuzzleNode
    {
        [SerializeField] LetterPuzzle letter;
        [SerializeField] TextMesh label;
        [SerializeField] Transform ring;
        int shownSeed = -1, shownValue = -1;
        float ringAngle;

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            ServerSetValue((Value + 1) % LetterPuzzle.Ring);
        }

        void Update()
        {
            int seed = letter != null ? letter.Seed : 0;
            if ((seed != shownSeed || Value != shownValue) && label != null)
            {
                shownSeed = seed; shownValue = Value;
                label.text = seed == 0 ? "" : LetterPuzzle.Make(seed).alphabet[Mathf.Clamp(Value, 0, LetterPuzzle.Ring - 1)].ToString();
            }
            if (ring != null)
            {
                ringAngle = Mathf.MoveTowardsAngle(ringAngle, Value * 60f, 400f * Time.deltaTime);
                ring.localRotation = Quaternion.Euler(0, 0, ringAngle);
            }
        }
    }
}
