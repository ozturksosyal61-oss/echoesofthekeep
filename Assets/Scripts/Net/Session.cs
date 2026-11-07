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
            runner.ProvideInput = false;

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

        /// <summary>Odadan çık, runner'ı yok et, Lobby'ye dön.</summary>
        public static async void Leave()
        {
            var r = Runner;
            Runner = null;
            Code = null;
            if (r != null)
            {
                await r.Shutdown();
                if (r != null) Object.Destroy(r.gameObject);
            }
            AudioRoute.Exit();
            if (SceneManager.GetActiveScene().name != LobbyScene) SceneManager.LoadScene(LobbyScene);
        }
    }
}
