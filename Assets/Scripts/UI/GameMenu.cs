using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Oyun içi menü (MENÜ düğmesi / Esc): Devam, Günlük, Ayarlar, Ana menüye dön (iki dokunuşla onay). Oyun durmaz (çok oyunculu).</summary>
    public class GameMenu : MonoBehaviour
    {
        [SerializeField] GameObject root, mainView, settingsView, journalView;
        [SerializeField] Button resume, journal, settings, leave, settingsBack, journalBack;

        static GameMenu inst;
        public static bool Exists => inst != null;
        public static bool IsOpen => inst != null && inst.root.activeSelf;

        Text leaveLabel;
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
            confirmUntil = 0f;
            leaveLabel.text = leaveText;
        }

        void Update()
        {
            if (leaveLabel != null && confirmUntil > 0f && Time.unscaledTime >= confirmUntil) { confirmUntil = 0f; leaveLabel.text = leaveText; }
            if (Input.GetKeyDown(KeyCode.Escape) && !LetterView.IsOpen) Toggle();
        }
    }
}
