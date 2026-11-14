using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Oyun içi menüde ipucu görünümü: şu anki bulmaca, açılmış kademeler, "İPUCU AÇ" (kademeler arası bekleme).</summary>
    public class HintPanel : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text[] lines = new Text[Hints.Levels];
        [SerializeField] Button reveal;

        Text revealLabel;

        void Awake()
        {
            revealLabel = reveal.GetComponentInChildren<Text>();
            reveal.onClick.AddListener(() => { Hints.Reveal(Hints.Current); Refresh(); });
        }

        void OnEnable() => Refresh();
        void Update() => Refresh();

        void Refresh()
        {
            int p = Hints.Current;
            bool has = Hints.Has(p);
            title.text = has ? Hints.Names[p] : "Şu an çözülecek bir bulmaca yok.";
            int lv = Hints.Level(p);
            for (int i = 0; i < lines.Length; i++) lines[i].text = has && i < lv ? (i + 1) + ".  " + Hints.Text(p, i) : "";
            bool left = has && lv < Hints.Levels;
            reveal.gameObject.SetActive(left);
            if (!left) return;
            float w = Hints.Wait;
            reveal.interactable = Hints.CanReveal(p);
            revealLabel.text = w > 0f ? "SONRAKİ İPUCU " + Mathf.CeilToInt(w) + " SN" : lv == 0 ? "İPUCU AÇ" : "SONRAKİ İPUCU";
        }
    }
}
