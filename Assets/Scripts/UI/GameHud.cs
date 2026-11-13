using System.Collections;
using Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Game HUD (sade): bölgeye girince birkaç saniyelik başlık kartı, lobide altta küçük oda kodu + yönerge,
    /// sol üstte konuşma göstergeleri (rol X/O). Bulmaca durum listesi yok; bazı çözümler kısa bir kartla duyurulur.</summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] Text roleText;
        [SerializeField] Text infoText;
        [SerializeField] Text roomText;
        [SerializeField] Image[] speakDots;   // giriş sırası (slot)
        [SerializeField] Text[] speakLabels;
        [SerializeField] Button leaveButton;
        [SerializeField] Text cardSub;        // başlık kartının alt satırı (roleText = kart başlığı)

        static readonly Color Idle = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color Speaking = new Color(0.2f, 1f, 0.3f, 1f);

        bool micGranted;
        int shownRoom = -1;
        float cardUntil;
        readonly System.Collections.Generic.Dictionary<int, PuzzleState> seen = new System.Collections.Generic.Dictionary<int, PuzzleState>();
        const float CardTime = 5f, CardFade = 1.2f;

        static string AreaTitle(int a) => a == Layout.AreaHall ? "LOBİ HOLÜ" : a == Layout.AreaFinal ? "BÜYÜK SALON" : a == Layout.AreaX ? "ÇAN KULESİ" : "MAHZENLER";
        static string AreaLine(int a) => a == Layout.AreaHall ? "Yollar burada ayrılıyor: biri yukarı, biri aşağı."
                                       : a == Layout.AreaFinal ? "Ayrılan yollar burada birleşiyor."
                                       : a == Layout.AreaX ? "Dişlilerin ve çanların arasında, rüzgârın içinde."
                                       : "Taşın ve suyun altında, ışığın ulaşmadığı yerde.";
        // Kendi HUD'u olmayan bulmacalar için çözüm kartı
        static string SolvedLine(int id) => id == 1 ? "Kollar yerine oturdu. Aşağıda ağır bir kapı açıldı."
                                          : id == 4 ? "Mektubun sırrı çözüldü. Mühürlü geçit aralandı." : null;

        void Card(string title, string line)
        {
            roleText.text = title; if (cardSub != null) cardSub.text = line;
            cardUntil = Time.unscaledTime + CardTime;
        }

        void Awake()
        {
            leaveButton.onClick.AddListener(() => { if (GameMenu.Exists) GameMenu.Toggle(); else Session.Leave(); });
        }

        IEnumerator Start()
        {
            // Lobby izni zaten istedi; burada durumu doğrula (reddedilmişse dinleme-only).
            yield return MicPermission.Ask(g => micGranted = g);
            var runner = Session.Runner;
            if (runner != null)
            {
                var rec = runner.GetComponentInChildren<Recorder>();
                if (rec != null && !micGranted) rec.TransmitEnabled = false;
            }
            VoiceGuard.MicGranted = micGranted;
            AudioRoute.Enter();
            if (GetComponent<VoiceGuard>() == null) gameObject.AddComponent<VoiceGuard>();
        }

        void Update()
        {
            var runner = Session.Runner;
            if (runner == null || !runner.IsRunning) return;

            int count = 0;
            foreach (var _ in runner.ActivePlayers) count++;

            var me = PlayerAvatar.Local;
            bool inHall = me != null && me.Room == Layout.AreaHall;
            if (me != null && me.Room != shownRoom) { shownRoom = me.Room; Card(AreaTitle(me.Room), AreaLine(me.Room)); }
            foreach (var d in PuzzleDef.All)
            {
                var st = d.State;
                if (seen.TryGetValue(d.Id, out var prev) && prev == PuzzleState.Active && st == PuzzleState.Solved && SolvedLine(d.Id) != null) Card("", SolvedLine(d.Id));
                seen[d.Id] = st;
            }
            float a = Mathf.Clamp01((cardUntil - Time.unscaledTime) / CardFade);
            roleText.color = WithA(roleText.color, a);
            if (cardSub != null) cardSub.color = WithA(cardSub.color, a);

            // Metinler 5 Hz (her karede string üretip UI'yı yeniden kurmak cihazı boşuna yoruyordu)
            if (Time.unscaledTime < nextText) return;
            nextText = Time.unscaledTime + 0.2f;

            // Lobi: altta küçük oda kodu + yönerge; diğer alanlarda yalnız mikrofon uyarısı
            SetText(infoText, (inHall ? "Oda kodu " + Session.Code + "  ·  " + count + "/" + Session.MaxPlayers + " oyuncu" : "") +
                              (micGranted && Settings.MicOn ? "" : (inHall ? "  ·  " : "") + "Mikrofon kapalı"));
            SetText(roomText, !inHall ? ""
                          : count < Session.MaxPlayers ? "Arkadaşını bekle; oda kodunu ona söyle."
                          : HallGate.Instance != null && HallGate.Instance.Value == HallGate.Counting ? "Yerinizde durun..."
                          : "Biriniz X plakasına (merdiven sahanlığı), diğeriniz O plakasına (doğu nişi) geçsin.");

            for (int i = 0; i < speakDots.Length; i++)
            {
                string label = "-"; bool talking = false;
                foreach (var avatar in PlayerAvatar.All)
                {
                    if (avatar == null || avatar.Object == null || !avatar.Object.IsValid || avatar.Slot != i) continue;
                    label = (avatar.RoleIndex == Roles.None ? "Oyuncu " + (i + 1) : Roles.Name(avatar.RoleIndex)) + (avatar.IsLocal ? " (sen)" : "");
                    talking = avatar.Speaking;
                    break;
                }
                SetText(speakLabels[i], label);
                var c = talking ? Speaking : Idle;
                if (speakDots[i].color != c) speakDots[i].color = c;
            }
        }

        float nextText;

        static void SetText(Text t, string s) { if (t != null && t.text != s) t.text = s; }

        static Color WithA(Color c, float a) { c.a = a; return c; }
    }
}
