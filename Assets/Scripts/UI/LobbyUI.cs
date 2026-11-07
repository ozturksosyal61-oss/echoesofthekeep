using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace CoopGame
{
    /// <summary>Lobby: 4 haneli oda kodu gir (Katıl) ya da oluştur (Oda Kur).</summary>
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] InputField codeInput;
        [SerializeField] Button createButton;
        [SerializeField] Button joinButton;
        [SerializeField] Text status;

        bool busy;

        void Awake()
        {
            codeInput.contentType = InputField.ContentType.IntegerNumber;
            codeInput.characterLimit = 4;
            createButton.onClick.AddListener(() => { if (!busy) StartCoroutine(Flow(true)); });
            joinButton.onClick.AddListener(() => { if (!busy) StartCoroutine(Flow(false)); });
            status.text = "Oda kodu gir ya da yeni oda kur.";
        }

        void SetBusy(bool b)
        {
            busy = b;
            createButton.interactable = !b;
            joinButton.interactable = !b;
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
