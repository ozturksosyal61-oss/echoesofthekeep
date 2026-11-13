using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Mektup okuma ekranı (yerel): karartılmış arka plan, parşömen panel, başlık/metin/imza; dokununca ya da E/Esc ile kapanır.</summary>
    public class LetterView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text title, body, sign;
        [SerializeField] Button close;

        static LetterView inst;
        public static bool IsOpen => inst != null && (inst.root.activeSelf || Time.unscaledTime - inst.closedAt < 0.3f);

        void Awake()
        {
            inst = this;
            close.onClick.AddListener(Hide);
            root.SetActive(false);
        }

        public static void Show(int id)
        {
            if (inst == null) return;
            LoreTexts.MarkRead(id);
            var e = LoreTexts.Get(id);
            inst.title.text = e.title; inst.body.text = e.body; inst.sign.text = e.sign;
            inst.root.SetActive(true);
            inst.openedAt = Time.unscaledTime;
        }

        float openedAt, closedAt = -10f;

        void Hide() { root.SetActive(false); closedAt = Time.unscaledTime; }

        void Update()
        {
            if (!root.activeSelf || Time.unscaledTime - openedAt < 0.3f) return;
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) Hide();
        }
    }
}
