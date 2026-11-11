using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Kol: etkileşimle çekilir, değeri (0 yukarı / 1 aşağı) senkronlanır. latch: çekilince bulmaca sıfırlanana kadar aşağıda kalır
    /// (SequenceInput için); kapalıysa her etkileşimde değişir. Üstündeki sembol (symbol) SequenceHint tarafından kopyalanır.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class SyncedLever : PuzzleNode
    {
        [SerializeField] Transform handle;
        [SerializeField] GameObject symbol;
        [SerializeField] bool latch = true;
        [SerializeField] float upAngle = -40f, downAngle = 40f;

        float targetAngle;

        public GameObject Symbol => symbol;
        public bool Pulled => Value != 0;

        public override void ServerInteract(PlayerAvatar who)
        {
            if (PuzzleState != PuzzleState.Active) { Dbg.Puzzle(name + " etkileşim yok sayıldı: bulmaca " + PuzzleState); return; }
            if (latch && Pulled) return;
            ServerSetValue(Pulled ? 0 : 1);
            Dbg.Puzzle(name + " çekildi → " + Value + " (" + (who != null ? who.name : "?") + ")");
            RaiseServerActivated();
        }

        protected override void OnValue(int value) => targetAngle = value != 0 ? downAngle : upAngle;

        void Update()
        {
            if (handle == null) return;
            var target = Quaternion.Euler(targetAngle, 0f, 0f);
            handle.localRotation = Quaternion.RotateTowards(handle.localRotation, target, 360f * Time.deltaTime);
        }
    }
}
