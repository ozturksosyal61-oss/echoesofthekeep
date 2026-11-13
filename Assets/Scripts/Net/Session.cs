using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopGame
{
    /// <summary>Oda yaşam döngüsü. Oda kodu = Photon oturum adı (4 hane). Create = Host, Join = Client (oda yoksa hata verir).</summary>
    public static class Session
    {
        public const int MaxPlayers = 2;
        public const string LobbyScene = "Lobby";
        public const string GameScene = "Game";

        public static NetworkRunner Runner { get; private set; }
        public static string Code { get; private set; }

        public static string NewCode() => Random.Range(0, 10000).ToString("D4");

        public static bool IsValidCode(string code)
        {
            if (code == null || code.Length != 4) return false;
            foreach (char c in code) if (c < '0' || c > '9') return false;
            return true;
        }

        public static Task<StartGameResult> Create(string code) => Start(code, true);
        public static Task<StartGameResult> Join(string code) => Start(code, false);

        static async Task<StartGameResult> Start(string code, bool host)
        {
            if (Runner != null) Leave();

            var prefab = Resources.Load<NetworkRunner>("NetworkRunner");
            var runner = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(runner.gameObject);
            runner.ProvideInput = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android: Photon (yerel) mikrofon = VOICE_COMMUNICATION kaynağı + yankı giderme. Unity mikrofonu, AudioRoute ses modunu
            // MODE_IN_COMMUNICATION'a geçirince bazı cihazlarda sessiz veri veriyordu (karşı taraf duymuyordu). Desteklenmezse Unity'ye düşer.
            var rec = runner.GetComponentInChildren<Photon.Voice.Unity.Recorder>();
            if (rec != null) { rec.UseMicrophoneTypeFallback = true; rec.MicrophoneType = Photon.Voice.Unity.Recorder.MicType.Photon; }
#endif

            var args = new StartGameArgs
            {
                GameMode = host ? GameMode.Host : GameMode.Client,
                SessionName = code,
                PlayerCount = MaxPlayers,
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
            };

            if (host)
            {
                int index = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Game.unity");
                var info = new NetworkSceneInfo();
                info.AddSceneRef(SceneRef.FromIndex(index), LoadSceneMode.Single);
                args.Scene = info;
            }
            else
            {
                args.EnableClientSessionCreation = false; // olmayan kodla yeni oda açma
            }

            var result = await runner.StartGame(args);
            if (!result.Ok)
            {
                Object.Destroy(runner.gameObject);
                return result;
            }

            Runner = runner;
            Code = code;
            return result;
        }

        static bool leaving;

        /// <summary>Odadan çık: runner'ı kapat (Shutdown), yok et, Lobby'ye dön. Tekrar oda kurulabilir.</summary>
        public static async void Leave()
        {
            if (leaving) return; // çift tıklama / OnShutdown aynı anda
            leaving = true;
            try
            {
                var r = Runner;
                Runner = null;
                Code = null;
                if (r != null)
                {
                    try { if (r.IsRunning) await r.Shutdown(); } catch (System.Exception e) { Debug.LogWarning("Session.Leave: " + e.Message); }
                    if (r != null) Object.Destroy(r.gameObject);
                }
                AudioRoute.Exit();
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (SceneManager.GetActiveScene().name != LobbyScene) SceneManager.LoadScene(LobbyScene);
            }
            finally { leaving = false; }
        }

        /// <summary>Runner kendiliğinden kapandı (host ayrıldı, bağlantı koptu): menüye dön.</summary>
        public static void OnRunnerShutdown(NetworkRunner r)
        {
            if (leaving || r == null || Runner != r) return;
            Leave();
        }
    }
}
