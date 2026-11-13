using Photon.Voice.Unity;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Sesli sohbet bekçisi (oyun sahnesi):
    /// - Telefon araması/zil gelince mikrofonu durdurur (arama sırasında kayıt çökme nedeniydi), arama bitince hoparlör rotasını ve kaydı geri açar.
    /// - Mikrofon başladıktan sonra ve çıkış cihazı değişince (kulaklık/BT) AudioRoute.Reapply: ses ahizeye değil ana hoparlöre gelir.
    /// - Ayarlar: mikrofon açık/kapalı, konuşma sesi (hoparlör kaynakları). TalkActive: biri konuşuyor mu (müzik kısılır).
    /// </summary>
    public class VoiceGuard : MonoBehaviour
    {
        public static bool TalkActive { get; private set; }
        public static bool MicGranted = true;

        Recorder rec;
        string sig;
        bool callPaused, wasRecording;
        float nextPoll, talkUntil;
        float[] reapplyAt = { 0.5f, 1.5f, 3f, 6f };
        float reapplyBase;

        void Start() { reapplyBase = Time.unscaledTime; }

        void ScheduleReapply() { reapplyBase = Time.unscaledTime; for (int i = 0; i < reapplyAt.Length; i++) reapplyAt[i] = Mathf.Abs(reapplyAt[i]); }

        void OnApplicationPause(bool paused) { if (!paused) { nextPoll = 0f; ScheduleReapply(); } }

        void Update()
        {
            float t = Time.unscaledTime;
            // planlı yeniden uygulamalar (pozitif = bekliyor)
            for (int i = 0; i < reapplyAt.Length; i++)
                if (reapplyAt[i] > 0f && t - reapplyBase >= reapplyAt[i]) { reapplyAt[i] = -reapplyAt[i]; AudioRoute.Reapply(); }

            if (t >= nextPoll)
            {
                nextPoll = t + 0.4f;
                var runner = Session.Runner;
                if (rec == null && runner != null) rec = runner.GetComponentInChildren<Recorder>();

                bool call = AudioRoute.CallActive();
                if (call && !callPaused)
                {
                    callPaused = true;
                    if (rec != null) { rec.TransmitEnabled = false; rec.RecordingEnabled = false; }
                }
                else if (!call && callPaused)
                {
                    callPaused = false;
                    AudioRoute.Enter();
                    if (rec != null) rec.RecordingEnabled = true;
                    ScheduleReapply();
                }

#if UNITY_ANDROID && !UNITY_EDITOR
                // İzin oyun sırasında (sistem ayarlarından) verildiyse: kaydı yeniden başlat, yoksa mikrofon sessiz kalır
                if (!MicGranted && UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
                {
                    MicGranted = true;
                    if (rec != null) { rec.RecordingEnabled = true; rec.RestartRecording(); }
                }
#endif
                if (rec != null && !callPaused)
                {
                    bool want = Settings.MicOn && MicGranted;
                    if (rec.TransmitEnabled != want) rec.TransmitEnabled = want;
                    bool recNow = rec.RecordingEnabled;
                    if (recNow && !wasRecording) ScheduleReapply(); // mikrofon yeni başladı: oturum rotayı ezmiş olabilir
                    wasRecording = recNow;
                }

                string s = AudioRoute.RouteSignature();
                if (s != sig) { sig = s; AudioRoute.Reapply(); }

                // konuşma sesi ayarı: hoparlör kaynakları
                float v = Settings.Voice;
                foreach (var g in FindObjectsByType<VoiceGain>(FindObjectsSortMode.None))
                {
                    g.Gain = Mathf.Max(1f, v);
                    var src = g.GetComponent<AudioSource>();
                    if (src != null) src.volume = Mathf.Min(1f, v);
                }
            }

            // biri konuşuyor mu (uzak: alınan ses; yerel: mikrofon seviyesi)
            bool talk = false;
            foreach (var a in PlayerAvatar.All)
                if (a != null && !a.IsLocal && a.Speaking) { talk = true; break; }
            if (!talk && rec != null && rec.TransmitEnabled && rec.LevelMeter != null && rec.LevelMeter.CurrentAvgAmp > 0.02f) talk = true;
            if (talk) talkUntil = t + 1.5f;
            TalkActive = t < talkUntil;
        }

        void OnDestroy() => TalkActive = false;
    }
}
