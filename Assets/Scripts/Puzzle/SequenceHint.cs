using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Doğru sırayı gösteren semboller (diğer odada): SequenceInput.Order'a göre her yuvaya ilgili kolun sembolünün kopyası konur.
    /// Yuva arka plakası ilerlemeyi gösterir (tamamlanan adım yeşil, hata kırmızı).
    /// </summary>
    public class SequenceHint : MonoBehaviour
    {
        [SerializeField] SequenceInput sequence;
        [SerializeField] Transform[] slots = new Transform[0];
        [SerializeField] Renderer[] slotPlates = new Renderer[0];

        static readonly Color Idle = new Color(0.15f, 0.15f, 0.18f);
        static readonly Color Done = new Color(0.2f, 0.75f, 0.3f);
        static readonly Color Fail = new Color(0.85f, 0.2f, 0.2f);

        void Start()
        {
            var order = sequence.Order;
            for (int i = 0; i < slots.Length && i < order.Length; i++)
            {
                int idx = order[i];
                var lever = idx >= 0 && idx < sequence.Inputs.Length ? sequence.Inputs[idx] as SyncedLever : null;
                if (lever == null || lever.Symbol == null) { Debug.LogWarning("[Puzzle] SequenceHint: yuva " + i + " için sembol yok"); continue; }
                var s = Instantiate(lever.Symbol, slots[i], false);
                s.transform.localPosition = Vector3.zero;
                s.transform.localRotation = Quaternion.identity;
                s.name = "Hint" + i + "_" + lever.Symbol.name;
            }
            Paint(sequence.Value);
        }

        void OnEnable() => PuzzleManager.NodeChanged += OnNode;
        void OnDisable() => PuzzleManager.NodeChanged -= OnNode;

        void OnNode(int nodeId, int value) { if (nodeId == sequence.NodeId) Paint(value); }

        void Paint(int value)
        {
            for (int i = 0; i < slotPlates.Length; i++)
                if (slotPlates[i] != null)
                    slotPlates[i].material.color = value == SequenceInput.Error ? Fail : i < value ? Done : Idle;
        }
    }
}
