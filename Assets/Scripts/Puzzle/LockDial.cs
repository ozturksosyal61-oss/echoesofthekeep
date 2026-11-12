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
        [SerializeField] int index; // kilitteki sırası (çözülünce doğru kelimenin harfi gösterilir)
        int shownSeed = -1, shownValue = -1;
        bool shownSolved;
        float ringAngle;

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) return;
            ServerSetValue((Value + 1) % LetterPuzzle.Ring);
        }

        void Update()
        {
            int seed = letter != null ? letter.Seed : 0;
            bool solved = PuzzleState == PuzzleState.Solved;
            if ((seed != shownSeed || Value != shownValue || solved != shownSolved) && label != null)
            {
                shownSeed = seed; shownValue = Value; shownSolved = solved;
                if (seed == 0) label.text = "";
                else
                {
                    var s = LetterPuzzle.Make(seed);
                    label.text = (solved ? s.word[Mathf.Clamp(index, 0, LetterPuzzle.Len - 1)] : s.alphabet[Mathf.Clamp(Value, 0, LetterPuzzle.Ring - 1)]).ToString();
                }
            }
            if (ring != null)
            {
                ringAngle = Mathf.MoveTowardsAngle(ringAngle, Value * 60f, 400f * Time.deltaTime);
                ring.localRotation = Quaternion.Euler(0, 0, ringAngle);
            }
        }
    }
}
