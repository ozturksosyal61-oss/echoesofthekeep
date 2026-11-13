using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Game Center (yalnız iOS): sessiz giriş, arkadaş daveti ve oda kodu aktarımı (Plugins/iOS/CoopGameCenter.mm).
    /// Davet eden eşleşince Host olur (LobbyUI odayı kurar, SendCode ile kodu yollar); davetli kodu alınca PendingCode dolar,
    /// LobbyUI onu görünce odaya katılır. Diğer platformlarda Available = false.
    /// </summary>
    public class GameCenter : MonoBehaviour
    {
        public static GameCenter Instance { get; private set; }
        public static bool Available => Application.platform == RuntimePlatform.IPhonePlayer;

        public static bool Authenticated { get; private set; }
        public static string PlayerName { get; private set; } = "";
        /// <summary>Davetli: Game Center'dan gelen oda kodu (LobbyUI tüketir).</summary>
        public static string PendingCode;
        /// <summary>Davet eden: eşleşme kuruldu, oda kurulup kod yollanmalı (LobbyUI tüketir).</summary>
        public static bool PendingHost;

        public static event Action Changed;
        public static event Action<string> Failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null || !Available) return;
            var go = new GameObject("GameCenter"); // adı yerel köprü için sabit
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameCenter>();
            Native.Authenticate();
        }

        public static void Invite() { if (Available) Native.Invite(); }
        public static void SendCode(string code) { if (Available) Native.SendCode(code); }
        public static void CancelMatch() { PendingHost = false; if (Available) Native.Cancel(); }
        public static void ShowDashboard() { if (Available) Native.Dashboard(); }

        // ---- Yerel köprüden gelen mesajlar (UnitySendMessage) ----
        void OnAuth(string msg)
        {
            Authenticated = msg.StartsWith("1|");
            PlayerName = msg.Length > 2 ? msg.Substring(2) : "";
            if (!Authenticated) Debug.Log("[GameCenter] giriş yok: " + PlayerName);
            Changed?.Invoke();
        }

        void OnMatch(string role)
        {
            if (role == "host") PendingHost = true;
            Changed?.Invoke();
        }

        void OnCode(string code)
        {
            if (Session.IsValidCode(code)) PendingCode = code;
            Changed?.Invoke();
        }

        void OnMatchFail(string reason)
        {
            PendingHost = false;
            Failed?.Invoke(reason);
        }

        static class Native
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")] static extern void _CoopGCAuthenticate();
            [DllImport("__Internal")] static extern void _CoopGCInvite();
            [DllImport("__Internal")] static extern void _CoopGCSendCode(string code);
            [DllImport("__Internal")] static extern void _CoopGCCancelMatch();
            [DllImport("__Internal")] static extern void _CoopGCShowDashboard();
            public static void Authenticate() => _CoopGCAuthenticate();
            public static void Invite() => _CoopGCInvite();
            public static void SendCode(string c) => _CoopGCSendCode(c);
            public static void Cancel() => _CoopGCCancelMatch();
            public static void Dashboard() => _CoopGCShowDashboard();
#else
            public static void Authenticate() { }
            public static void Invite() { }
            public static void SendCode(string c) { }
            public static void Cancel() { }
            public static void Dashboard() { }
#endif
        }
    }
}
