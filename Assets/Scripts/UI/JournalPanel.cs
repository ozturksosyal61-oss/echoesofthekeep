using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Günlük: okunan mektupların listesi (hikâye sırasıyla). Okunmamışlar gizli; dokununca okuma ekranı açılır.</summary>
    public class JournalPanel : MonoBehaviour
    {
        [SerializeField] Text count;
        [SerializeField] Button[] rows = new Button[0];
        [SerializeField] Text[] labels = new Text[0];

        static readonly Color On = new Color(0.96f, 0.91f, 0.8f), Off = new Color(0.96f, 0.91f, 0.8f, 0.3f);

        void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int k = i;
                rows[i].onClick.AddListener(() => { if (k < LoreTexts.Order.Length && LoreTexts.IsRead(LoreTexts.Order[k])) LetterView.Show(LoreTexts.Order[k]); });
            }
        }

        void OnEnable() => Refresh();

        void Refresh()
        {
            int total = LoreTexts.Order.Length;
            if (count != null) count.text = LoreTexts.ReadCount + " / " + total + " mektup";
            for (int i = 0; i < rows.Length; i++)
            {
                bool has = i < total;
                rows[i].gameObject.SetActive(has);
                if (!has) continue;
                bool read = LoreTexts.IsRead(LoreTexts.Order[i]);
                labels[i].text = (i + 1) + ".  " + (read ? LoreTexts.Get(LoreTexts.Order[i]).title : "· · ·");
                labels[i].color = read ? On : Off;
                rows[i].interactable = read;
            }
        }
    }
}
