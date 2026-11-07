using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Yakındaki butonla etkileşim: yaklaşınca "Etkileş" düğmesi görünür (Editor'de E tuşu da). İstek host'a RPC ile gider.</summary>
    public class InteractPrompt : MonoBehaviour
    {
        [SerializeField] Button button;

        Text label;
        Interactable current;

        void Awake()
        {
            label = button.GetComponentInChildren<Text>();
            button.onClick.AddListener(Fire);
            button.gameObject.SetActive(false);
        }

        void Update()
        {
            current = null;
            var me = PlayerAvatar.Local;
            if (me != null)
            {
                float best = Interactable.ClientRange;
                foreach (var i in Interactable.All)
                {
                    float d = Vector3.Distance(me.transform.position, i.transform.position);
                    if (d < best) { best = d; current = i; }
                }
            }

            bool show = current != null;
            if (button.gameObject.activeSelf != show)
            {
                button.gameObject.SetActive(show);
                // (1a) Yakınlık algılandı mı: butona girdi/çıktı
                Dbg.Interact("(1a) yakınlık: " + (show ? "GİRDİ " + current.Id + " d=" + Vector3.Distance(me.transform.position, current.transform.position).ToString("F1") : "ÇIKTI") +
                             (me != null ? " konum=" + me.transform.position.ToString("F1") : " (yerel avatar yok)"));
            }
            if (!show) return;
            label.text = current.Label;
            if (Input.GetKeyDown(KeyCode.E)) Fire();
        }

        void Fire()
        {
            // (1b) Dokunma/E algılandı mı
            Dbg.Interact("(1b) Etkileş basıldı: current=" + (current != null ? current.Id.ToString() : "YOK") +
                         " yerelAvatar=" + (PlayerAvatar.Local != null ? "var" : "YOK"));
            if (current != null && PlayerAvatar.Local != null) PlayerAvatar.Local.RequestInteract(current.Id);
        }
    }
}
