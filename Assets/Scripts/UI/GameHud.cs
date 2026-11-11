using System.Collections;
using Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Game HUD: rol, oda kodu, oyuncu sayısı, mikrofon durumu ve konuşan oyuncu göstergesi (A/B satırları).</summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] Text roleText;
        [SerializeField] Text infoText;
        [SerializeField] Text roomText;
        [SerializeField] Image[] speakDots;   // [0]=A, [1]=B
        [SerializeField] Text[] speakLabels;
        [SerializeField] Button leaveButton;

        static readonly Color Idle = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color Speaking = new Color(0.2f, 1f, 0.3f, 1f);

        bool micGranted;

        void Awake()
        {
            leaveButton.onClick.AddListener(Session.Leave);
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
            AudioRoute.Enter();
        }

        void Update()
        {
            var runner = Session.Runner;
            if (runner == null || !runner.IsRunning) return;

            int count = 0;
            foreach (var _ in runner.ActivePlayers) count++;

            if (runner.LocalPlayer != PlayerRef.None)
                roleText.text = "Rol: " + Roles.Name(Roles.Index(runner, runner.LocalPlayer));

            infoText.text = "Kod: " + Session.Code + "   Oyuncu: " + count + "/" + Session.MaxPlayers +
                            (micGranted ? "" : "\nMikrofon kapalı (izin yok)");

            // Bulunduğun alan + paylaşılan durum (diğer odadaki etkileşimin sonucunu da görebil).
            var me = PlayerAvatar.Local;
            roomText.text = me != null ? Layout.RoomName(me.Room) : "-";
            foreach (var d in PuzzleDef.All) roomText.text += "\n" + d.Title + ": " + PuzzleDef.StateName(d.State);

            for (int i = 0; i < speakDots.Length; i++) { speakDots[i].color = Idle; speakLabels[i].text = Roles.Name(i); }
            foreach (var avatar in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object == null || !avatar.Object.IsValid) continue;
                int r = avatar.RoleIndex;
                if (r < 0 || r >= speakDots.Length) continue;
                speakLabels[r].text = Roles.Name(r) + (avatar.IsLocal ? " (sen)" : "");
                speakDots[r].color = avatar.Speaking ? Speaking : Idle;
            }
        }
    }
}
