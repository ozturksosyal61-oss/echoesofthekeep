using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Oyun içi menü (MENÜ düğmesi / Esc): Devam, Günlük, İpucu, Ayarlar, Ana menüye dön (iki dokunuşla onay). Oyun durmaz (çok oyunculu).</summary>
    public class GameMenu : MonoBehaviour
    {
        [SerializeField] GameObject root, mainView, settingsView, journalView, hintView;
        [SerializeField] Button resume, journal, settings, leave, settingsBack, journalBack, mute, hint, hintBack, report;

        static GameMenu inst;
        public static bool Exists => inst != null;
        public static bool IsOpen => inst != null && inst.root.activeSelf;

        Text leaveLabel, muteLabel, reportLabel;
        float reportUntil;

        void MuteText()
        {
            if (muteLabel == null) return;
            muteLabel.text = Session.Review ? "ROL DEĞİŞTİR" : PlayerAvatar.RemoteMuted ? "SESİ AÇ" : "SESSİZE AL";
        }
        string leaveText;
        float confirmUntil;

        void Awake()
        {
            inst = this;
            leaveLabel = leave.GetComponentInChildren<Text>();
            leaveText = leaveLabel.text;
            resume.onClick.AddListener(Close);
            journal.onClick.AddListener(() => Show(journalView));
            settings.onClick.AddListener(() => Show(settingsView));
            settingsBack.onClick.AddListener(() => Show(mainView));
            journalBack.onClick.AddListener(() => Show(mainView));
            if (hint != null) hint.onClick.AddListener(() => Show(hintView));
            if (hintBack != null) hintBack.onClick.AddListener(() => Show(mainView));
            if (mute != null)
            {
                muteLabel = mute.GetComponentInChildren<Text>();
                mute.onClick.AddListener(() =>
                {
                    if (Session.Review) { if (PlayerAvatar.Local != null) PlayerAvatar.Local.RequestSwitchRole(); Close(); return; }
                    PlayerAvatar.RemoteMuted = !PlayerAvatar.RemoteMuted; MuteText();
                });
            }
            if (report != null)
            {   // Kötüye kullanım (App Store 1.2): iki dokunuşla onay → sesi kapat, bildirim sayfasını aç, odadan çık (engelle)
                reportLabel = report.GetComponentInChildren<Text>();
                if (Session.Review) report.gameObject.SetActive(false); // inceleme: tek kişi, bildirecek kimse yok
                report.onClick.AddListener(() =>
                {
                    if (Time.unscaledTime >= reportUntil) { reportUntil = Time.unscaledTime + 3f; reportLabel.text = "EMİN MİSİN?"; return; }
                    PlayerAvatar.RemoteMuted = true;
                    string t = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";
                    Application.OpenURL("mailto:" + LobbyUI.SupportMail + "?subject=" + System.Uri.EscapeDataString("Oyuncu bildirimi / Player report")
                        + "&body=" + System.Uri.EscapeDataString("Oda / Room: " + Session.Code + "
Zaman / Time: " + t + "
Ne oldu? / What happened?
"));
                    Session.Leave();
                });
            }
            leave.onClick.AddListener(() =>
            {
                if (Time.unscaledTime < confirmUntil) { Session.Leave(); return; }
                confirmUntil = Time.unscaledTime + 3f;
                leaveLabel.text = "EMİN MİSİN? TEKRAR DOKUN";
            });
            root.SetActive(false);
        }

        public static void Toggle()
        {
            if (inst == null) return;
            if (inst.root.activeSelf) inst.Close();
            else { inst.root.SetActive(true); inst.Show(inst.mainView); }
        }

        void Close() => root.SetActive(false);

        void Show(GameObject view)
        {
            mainView.SetActive(view == mainView);
            settingsView.SetActive(view == settingsView);
            journalView.SetActive(view == journalView);
            if (hintView != null) hintView.SetActive(view == hintView);
            confirmUntil = 0f;
            leaveLabel.text = leaveText;
            reportUntil = 0f;
            if (reportLabel != null) reportLabel.text = "BİLDİR / ENGELLE";
            MuteText();
        }

        void Update()
        {
            if (leaveLabel != null && confirmUntil > 0f && Time.unscaledTime >= confirmUntil) { confirmUntil = 0f; leaveLabel.text = leaveText; }
            if (reportLabel != null && reportUntil > 0f && Time.unscaledTime >= reportUntil) { reportUntil = 0f; reportLabel.text = "BİLDİR / ENGELLE"; }
            if (Input.GetKeyDown(KeyCode.Escape) && !LetterView.IsOpen) Toggle();
        }
    }
}
