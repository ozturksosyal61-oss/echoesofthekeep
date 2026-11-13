using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace CoopGame
{
    /// <summary>Ana menü: Oda Kur (4 haneli kod üretir, holde gösterilir) ya da Odaya Katıl (kod girme paneli açılır).</summary>
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] InputField codeInput;
        [SerializeField] Button createButton;
        [SerializeField] Button joinButton;      // kod panelindeki "Katıl"
        [SerializeField] Button openJoinButton;  // ana paneldeki "Odaya Katıl"
        [SerializeField] Button backButton;
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject joinPanel;
        [SerializeField] Text status;
        [SerializeField] Button startButton;     // başlangıç seçimi (yalnızca Oda Kur için)
        [SerializeField] Button continueButton;  // Devam Et: kayıttan oda kurar (kayıt yoksa Yeni Oyun)
        [SerializeField] Text continueHead, continueSub;
        [SerializeField] Text progressValue;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Button settingsButton, settingsBack;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Button journalButton, journalBack;
        [SerializeField] GameObject journalPanel;
        [SerializeField] Button creditsButton, creditsBack;
        [SerializeField] GameObject creditsPanel;

        // Seçenekler: 0 = kaldığın yer (kayıt varsa), sonra 1..LastPuzzle (1 = baştan)
        int startChoice;

        bool busy;

        void Awake()
        {
            codeInput.contentType = InputField.ContentType.IntegerNumber;
            if (startButton != null)
            {
                startChoice = Progress.Saved > 0 ? 0 : 1;
                ApplyStart();
                startButton.onClick.AddListener(() =>
                {
                    if (busy) return;
                    startChoice = (startChoice + 1) % (Progress.LastPuzzle + 2); // son seçenek: tümü açık
                    if (startChoice == 0 && Progress.Saved == 0) startChoice = 1;
                    ApplyStart();
                });
            }
            codeInput.characterLimit = 4;
            createButton.onClick.AddListener(() => { if (!busy) { if (startButton != null) ApplyStart(); else Progress.StartPuzzle = 1; StartCoroutine(Flow(true)); } });
            if (continueButton != null)
            {
                int saved = Progress.Saved;
                bool has = saved > 0 && saved < Progress.LastPuzzle;
                int next = Mathf.Min(saved + 1, Progress.LastPuzzle);
                if (continueHead != null) continueHead.text = has ? "DEVAM ET" : saved >= Progress.LastPuzzle ? "YENİDEN OYNA" : "YENİ OYUN";
                if (continueSub != null) continueSub.text = has ? "Bulmaca " + next + " · kaldığın yerden sürdür" : "Hikâyeye baştan başla";
                continueButton.onClick.AddListener(() => { if (!busy) { Progress.StartPuzzle = has ? next : 1; StartCoroutine(Flow(true)); } });
            }
            if (progressValue != null) progressValue.text = Progress.Saved + " / " + Progress.LastPuzzle;
            if (progressFill != null) progressFill.sizeDelta = new Vector2(520f * Progress.Saved / Progress.LastPuzzle, progressFill.sizeDelta.y);
            joinButton.onClick.AddListener(() => { if (!busy) StartCoroutine(Flow(false)); });
            openJoinButton.onClick.AddListener(() => { if (!busy) ShowJoin(true); });
            backButton.onClick.AddListener(() => { if (!busy) ShowJoin(false); });
            if (settingsButton != null) settingsButton.onClick.AddListener(() => { if (!busy) ShowSettings(true); });
            if (settingsBack != null) settingsBack.onClick.AddListener(() => ShowSettings(false));
            if (journalButton != null) journalButton.onClick.AddListener(() => { if (!busy) ShowPanel(journalPanel); });
            if (journalBack != null) journalBack.onClick.AddListener(() => ShowJoin(false));
            if (creditsButton != null) creditsButton.onClick.AddListener(() => { if (!busy) ShowPanel(creditsPanel); });
            if (creditsBack != null) creditsBack.onClick.AddListener(() => ShowJoin(false));
            ShowJoin(false);
        }

        void ShowSettings(bool on)
        {
            if (on) ShowPanel(settingsPanel); else ShowJoin(false);
        }

        void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(false);
            joinPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
            if (journalPanel != null) journalPanel.SetActive(panel == journalPanel);
            if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
            status.text = "";
        }

        void ApplyStart()
        {
            int saved = Progress.Saved;
            Progress.StartPuzzle = startChoice == 0 ? Mathf.Min(saved + 1, Progress.LastPuzzle) : startChoice;
            var label = startButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = startChoice == 0 ? "BAŞLANGIÇ: KALDIĞIN YER (BULMACA " + Progress.StartPuzzle + ")"
                           : startChoice == 1 ? "BAŞLANGIÇ: BAŞTAN"
                           : startChoice > Progress.LastPuzzle ? "BAŞLANGIÇ: TÜM ODALAR AÇIK (GEZİNTİ)"
                           : "BAŞLANGIÇ: BULMACA " + startChoice;
        }

        void ShowJoin(bool join)
        {
            mainPanel.SetActive(!join);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (journalPanel != null) journalPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            joinPanel.SetActive(join);
            status.text = join ? "Arkadaşının 4 haneli oda kodunu gir." : "";
            if (join) codeInput.text = "";
        }

        void SetBusy(bool b)
        {
            busy = b;
            createButton.interactable = !b;
            if (continueButton != null) continueButton.interactable = !b;
            joinButton.interactable = !b;
            openJoinButton.interactable = !b;
            backButton.interactable = !b;
            codeInput.interactable = !b;
        }

        IEnumerator Flow(bool create)
        {
            string code = codeInput.text;
            if (!create && !Session.IsValidCode(code))
            {
                status.text = "Kod 4 haneli olmalı.";
                yield break;
            }

            SetBusy(true);
            status.text = "Mikrofon izni isteniyor...";
            bool mic = false;
            yield return MicPermission.Ask(g => mic = g);

            status.text = create ? "Oda kuruluyor..." : "Odaya giriliyor...";
            StartGameResult result = null;

            if (create)
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    code = Session.NewCode();
                    var t = Session.Create(code);
                    while (!t.IsCompleted) yield return null;
                    result = t.Result;
                    if (result.Ok || result.ShutdownReason != ShutdownReason.GameIdAlreadyExists) break;
                }
            }
            else
            {
                var t = Session.Join(code);
                while (!t.IsCompleted) yield return null;
                result = t.Result;
            }

            if (result.Ok)
            {
                // Sahne geçişini Fusion yapar (host Game'i yükler, client takip eder).
                status.text = (create ? "Oda kodu: " + code : "Bağlandı: " + code) + (mic ? "" : "  (mikrofon izni yok)");
                yield break;
            }

            status.text = Explain(result);
            SetBusy(false);
        }

        static string Explain(StartGameResult r)
        {
            switch (r.ShutdownReason)
            {
                case ShutdownReason.GameNotFound: return "Bu kodla oda bulunamadı.";
                case ShutdownReason.GameIsFull: return "Oda dolu (en fazla 2 oyuncu).";
                case ShutdownReason.GameIdAlreadyExists: return "Kod kullanımda, tekrar dene.";
                case ShutdownReason.InvalidAuthentication: return "Photon AppId girilmemiş ya da geçersiz.";
                default: return "Bağlanılamadı: " + r.ShutdownReason + (string.IsNullOrEmpty(r.ErrorMessage) ? "" : " - " + r.ErrorMessage);
            }
        }
    }
}
