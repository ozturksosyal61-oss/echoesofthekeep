using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Hazır cümleler (ses dışı yedek iletişim; serbest metin yok). SÖZ düğmesi / T tuşu paneli açar, cümleye dokununca
    /// iki ekranın üstünde birkaç saniye görünür. Gönderim PlayerAvatar.RPC_Say ile; yerel sıklık sınırı var.
    /// </summary>
    public class QuickChat : MonoBehaviour
    {
        public static readonly string[] Phrases =
        {
            "Dur!", "Tekrar söyle", "Hazırım", "Tamam, yaptım", "Bekle, arıyorum",
            "Yavaş anlat", "Bir şey buldum", "Yanlış oldu", "Evet", "Hayır",
        };

        [SerializeField] Button toggle;
        [SerializeField] GameObject panel;
        [SerializeField] Button[] buttons = new Button[0];
        [SerializeField] Text toast;

        static QuickChat inst;
        float toastUntil, sentAt = -10f;

        void Awake()
        {
            inst = this;
            toggle.onClick.AddListener(() => panel.SetActive(!panel.activeSelf));
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                buttons[i].onClick.AddListener(() => Send(k));
                var t = buttons[i].GetComponentInChildren<Text>();
                if (t != null && i < Phrases.Length) t.text = Phrases[i];
            }
            panel.SetActive(false);
            toast.text = "";
        }

        void Send(int k)
        {
            panel.SetActive(false);
            if (PlayerAvatar.Local == null || Time.unscaledTime - sentAt < 1f) return;
            sentAt = Time.unscaledTime;
            PlayerAvatar.Local.Say(k);
        }

        public static void Receive(PlayerAvatar from, int k)
        {
            if (inst == null || from == null || k < 0 || k >= Phrases.Length) return;
            string who = from.IsLocal ? "Sen" : from.RoleIndex == Roles.None ? "Arkadaşın" : Roles.Name(from.RoleIndex);
            inst.toast.text = who + ":  " + Phrases[k];
            inst.toastUntil = Time.unscaledTime + 4f;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T) && !GameMenu.IsOpen && !LetterView.IsOpen) panel.SetActive(!panel.activeSelf);
            float a = Mathf.Clamp01((toastUntil - Time.unscaledTime) / 0.6f);
            var c = toast.color; c.a = a; toast.color = c;
        }
    }
}
