using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Buton etkileşimi. 1. şahıs: ekran ortasındaki nişangâhtan raycast, bakılan butona "Etkileş" çıkar (nişangâh renklenir).
    /// 3. şahıs: nişangâh gizli, en yakın buton (yakınlık). Editor'de E tuşu da çalışır. İstek host'a RPC ile gider.
    /// </summary>
    public class InteractPrompt : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image crosshair;

        static readonly Color CrossIdle = new Color(1f, 1f, 1f, 0.7f);
        static readonly Color CrossHot = new Color(1f, 0.85f, 0.2f, 1f);

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
            var cam = FollowCamera.Instance;
            bool fp = cam != null && cam.FirstPerson;

            if (me != null)
                current = fp ? Raycast(cam.transform) : Nearest(me.transform.position);

            if (crosshair != null)
            {
                if (crosshair.gameObject.activeSelf != fp) crosshair.gameObject.SetActive(fp);
                crosshair.color = current != null ? CrossHot : CrossIdle;
            }

            bool show = current != null;
            if (button.gameObject.activeSelf != show)
            {
                button.gameObject.SetActive(show);
                // (1a) Hedef algılandı mı: butona girdi/çıktı
                Dbg.Interact("(1a) " + (fp ? "raycast" : "yakınlık") + ": " +
                             (show ? "GİRDİ " + current.Id + " d=" + Vector3.Distance(me.transform.position, current.transform.position).ToString("F1") : "ÇIKTI") +
                             (me != null ? " konum=" + me.transform.position.ToString("F1") : " (yerel avatar yok)"));
            }
            if (!show) return;
            label.text = current.Label;
            if (Input.GetKeyDown(KeyCode.E)) Fire();
        }

        /// <summary>Kamera merkezinden ileri ışın; ilk çarpan collider bir Interactable ise o. Duvar/kapı arkasındakini görmez.</summary>
        static Interactable Raycast(Transform cam)
        {
            if (!Physics.Raycast(cam.position, cam.forward, out var hit, Interactable.RayRange, ~0, QueryTriggerInteraction.Ignore))
                return null;
            return hit.collider.GetComponentInParent<Interactable>();
        }

        static Interactable Nearest(Vector3 pos)
        {
            Interactable best = null;
            float bestD = Interactable.ClientRange;
            foreach (var i in Interactable.All)
            {
                float d = Vector3.Distance(pos, i.transform.position);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
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
