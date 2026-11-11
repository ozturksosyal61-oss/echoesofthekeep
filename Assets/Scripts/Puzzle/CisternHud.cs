using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Sarnıcın yerel tarafı: kamera su altındayken bulanık mavi sis + karartma, nefes durumu, boğulma geçişi ve deneme mesajları.
    /// </summary>
    public class CisternHud : MonoBehaviour
    {
        [SerializeField] Text status;
        [SerializeField] Text message;
        [SerializeField] Image under;
        [SerializeField] Image fade;
        [SerializeField] Transform water;

        static readonly Color UnderFog = new Color(0.02f, 0.09f, 0.1f);

        Color fog0; float density0;
        bool underApplied;
        int seenAttempt = -1;
        PuzzleState seenState;
        float messageUntil, fadeOut;

        void Start()
        {
            fog0 = RenderSettings.fogColor; density0 = RenderSettings.fogDensity;
            under.color = Color.clear; fade.color = Color.clear; message.text = ""; status.text = "";
        }

        void OnDestroy() => SetUnder(false);

        void SetUnder(bool on)
        {
            if (on == underApplied) return;
            underApplied = on;
            RenderSettings.fogColor = on ? UnderFog : fog0;
            RenderSettings.fogDensity = on ? 0.35f : density0;
        }

        void Say(string text, float seconds) { message.text = text; messageUntil = Time.time + seconds; }

        void Update()
        {
            var c = CisternPuzzle.Instance;
            var me = PlayerAvatar.Local;
            if (c == null || PuzzleManager.Instance == null) return;

            var st = c.PuzzleState;
            var phase = c.CurrentPhase;
            bool inZone = me != null && c.InZone(me.transform.position);
            var cam = Camera.main;
            bool underwater = inZone && cam != null && water != null && cam.transform.position.y < water.position.y;
            SetUnder(underwater);

            int attempt = c.Attempt;
            if (seenAttempt >= 0 && attempt > seenAttempt)
            {
                Say("Sarnıç seni yuttu. Vanalar yeniden düzenlendi.\nDeneme " + (attempt + 1), 4.5f);
                fadeOut = 1f;
            }
            seenAttempt = attempt;
            if (st != seenState)
            {
                if (st == PuzzleState.Active && seenState == PuzzleState.Locked) Say("Uzaklardan su sesi geliyor...", 4f);
                if (st == PuzzleState.Solved) Say("Sarnıç mühürlendi.", 4f);
                seenState = st;
            }
            if (Time.time > messageUntil) message.text = "";

            status.text = !inZone || st != PuzzleState.Active || phase != CisternPuzzle.Phase.Sealed ? "" :
                          underwater ? "Nefes: " + Mathf.CeilToInt(c.Breath01 * CisternPuzzle.BreathSeconds) + " sn" : "Su yükseliyor";
            if (inZone && underwater && Time.time > messageUntil) message.text = "Nefesin tükeniyor!";
            if (!inZone && phase == CisternPuzzle.Phase.Dying && Time.time > messageUntil) message.text = "Boruların içinden boğuk bir çırpınış...";

            float b = underwater ? 1f - c.Breath01 : 0f;
            under.color = underwater ? new Color(0.05f, 0.22f, 0.26f, 0.45f + 0.4f * b) : Color.clear;

            float f = inZone && phase == CisternPuzzle.Phase.Dying ? c.Dying01 : 0f;
            fadeOut = Mathf.MoveTowards(fadeOut, 0f, Time.deltaTime / 1.5f);
            float fo = me != null && Layout.RoomOf(me.transform.position) == Layout.RoomOf(c.transform.position) ? fadeOut : 0f;
            fade.color = new Color(0f, 0f, 0f, Mathf.Max(f, fo));
        }
    }
}
