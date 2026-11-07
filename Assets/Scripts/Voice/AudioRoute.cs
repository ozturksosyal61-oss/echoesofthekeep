using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace CoopGame
{
    /// <summary>
    /// Sesli konuşma sırasında çıkışı telefonun ana hoparlörüne alır (ahize/kulaklık hoparlörü yerine). iOS: playAndRecord +
    /// defaultToSpeaker + allowBluetooth, ardından overrideOutputAudioPort(speaker); Android: MODE_IN_COMMUNICATION + API 31+
    /// setCommunicationDevice(yerleşik hoparlör), eski sürümlerde setSpeakerphoneOn(true). Kulaklık/Bluetooth/USB çıkış varsa
    /// hoparlör zorlanmaz.
    ///
    /// Neden Reapply: mikrofon (Recorder) başlarken Unity/Photon ses oturumunu yeniden kurar ve ayarımızı ezer; kulaklık
    /// takılıp çıkarılınca da rota değişir. Bu yüzden Enter() bir kez yetmez: mikrofon başladıktan sonra ve her rota
    /// değişiminde (RouteSignature değişince) Reapply() çağrılır. Hata olursa oyun etkilenmez. Exit() eski moda döner.
    /// </summary>
    public static class AudioRoute
    {
        static bool active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = false;

        public static bool Active => active;

#if UNITY_ANDROID && !UNITY_EDITOR
        // AudioDeviceInfo türleri: kablolu kulaklık, kablolu kulaklık (mikrofonsuz), BT SCO, BT A2DP, USB kulaklık, BLE kulaklık
        static readonly int[] HeadsetTypes = { 3, 4, 7, 8, 22, 26 };
        const int ModeInCommunication = 3;
        const int TypeBuiltinSpeaker = 2;

        static AndroidJavaObject audio;
        static int previousMode;
        static bool previousSpeaker;
        static int sdkInt = -1;

        static AndroidJavaObject Audio()
        {
            if (audio != null) return audio;
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            audio = activity.Call<AndroidJavaObject>("getSystemService", "audio");
            return audio;
        }

        static int Sdk()
        {
            if (sdkInt < 0)
            {
                using var v = new AndroidJavaClass("android.os.Build$VERSION");
                sdkInt = v.GetStatic<int>("SDK_INT");
            }
            return sdkInt;
        }

        static bool HeadsetConnected(AndroidJavaObject am)
        {
            var devices = am.Call<AndroidJavaObject[]>("getDevices", 2); // GET_DEVICES_OUTPUTS
            bool found = false;
            foreach (var d in devices)
            {
                int type = d.Call<int>("getType");
                d.Dispose();
                if (Array.IndexOf(HeadsetTypes, type) >= 0) found = true;
            }
            return found;
        }

        static string Signature(AndroidJavaObject am)
        {
            var devices = am.Call<AndroidJavaObject[]>("getDevices", 2);
            var sb = new System.Text.StringBuilder();
            foreach (var d in devices)
            {
                sb.Append(d.Call<int>("getId")).Append(':').Append(d.Call<int>("getType")).Append(',');
                d.Dispose();
            }
            return sb.ToString();
        }

        static void RouteToSpeaker(AndroidJavaObject am)
        {
            bool headset = HeadsetConnected(am);
            am.Call("setMode", ModeInCommunication);
            if (Sdk() >= 31)
            {
                if (headset)
                {
                    am.Call("clearCommunicationDevice"); // kulaklık/BT: sistem seçsin
                    return;
                }
                using var list = am.Call<AndroidJavaObject>("getAvailableCommunicationDevices");
                int n = list.Call<int>("size");
                for (int i = 0; i < n; i++)
                {
                    using var d = list.Call<AndroidJavaObject>("get", i);
                    if (d.Call<int>("getType") != TypeBuiltinSpeaker) continue;
                    am.Call<bool>("setCommunicationDevice", d);
                    return;
                }
            }
            else if (!headset)
            {
                am.Call("setSpeakerphoneOn", true);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _BlofAudioEnterVoiceChat();
        [DllImport("__Internal")] static extern void _BlofAudioReapply();
        [DllImport("__Internal")] static extern int _BlofAudioRouteChangeCount();
        [DllImport("__Internal")] static extern void _BlofAudioLeaveVoiceChat();
        [DllImport("__Internal")] static extern int _BlofAudioInterrupted();
#endif

        /// <summary>
        /// Telefon görüşmesi / zil / Siri gibi bir kesinti sürüyor mu? Sürerken mikrofona ve ses moduna dokunulmaz (arama sırasında
        /// MODE_IN_COMMUNICATION'a geçmek veya Photon mikrofonunu başlatmak çökme/arama bozulması nedeniydi). Android: AudioManager
        /// modu IN_CALL(2)/RINGTONE(1)/CALL_SCREENING(4); iOS: AVAudioSession interruption began..ended arası. Hata olursa false.
        /// </summary>
        public static bool CallActive()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                int mode = Audio().Call<int>("getMode");
                return mode == 1 || mode == 2 || mode == 4;
#elif UNITY_IOS && !UNITY_EDITOR
                return _BlofAudioInterrupted() != 0;
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("AudioRoute.CallActive: " + e.Message);
            }
            return false;
        }

        public static void Enter()
        {
            try
            {
                if (CallActive()) return; // arama sürerken ses moduna dokunma; arama bitince Reapply/Enter yeniden dener
#if UNITY_ANDROID && !UNITY_EDITOR
                var am = Audio();
                if (!active)
                {
                    previousMode = am.Call<int>("getMode");
                    previousSpeaker = am.Call<bool>("isSpeakerphoneOn");
                }
                RouteToSpeaker(am);
                active = true;
#elif UNITY_IOS && !UNITY_EDITOR
                _BlofAudioEnterVoiceChat();
                active = true;
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("AudioRoute.Enter: " + e.Message);
            }
        }

        /// <summary>
        /// Rotayı yeniden uygular (mikrofon başladıktan sonra, kulaklık takılıp çıkınca, uygulama öne gelince).
        /// Enter() henüz çağrılmadıysa bir şey yapmaz.
        /// </summary>
        public static void Reapply()
        {
            if (!active) return;
            try
            {
                if (CallActive()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
                RouteToSpeaker(Audio());
#elif UNITY_IOS && !UNITY_EDITOR
                _BlofAudioReapply();
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("AudioRoute.Reapply: " + e.Message);
            }
        }

        /// <summary>
        /// Çıkış cihazlarının özeti; değişirse rota değişmiş demektir (kulaklık takıldı/çıkarıldı, BT bağlandı).
        /// Ucuzdur: saniyede bir yoklanır.
        /// </summary>
        public static string RouteSignature()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Signature(Audio());
#elif UNITY_IOS && !UNITY_EDITOR
                return _BlofAudioRouteChangeCount().ToString();
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("AudioRoute.RouteSignature: " + e.Message);
            }
            return "";
        }

        public static void Exit()
        {
            if (!active) return;
            active = false;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                var am = Audio();
                if (Sdk() >= 31) am.Call("clearCommunicationDevice");
                am.Call("setSpeakerphoneOn", previousSpeaker);
                am.Call("setMode", previousMode);
#elif UNITY_IOS && !UNITY_EDITOR
                _BlofAudioLeaveVoiceChat();
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("AudioRoute.Exit: " + e.Message);
            }
        }
    }
}
