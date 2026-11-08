using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Game sahnesi: host dünya durumunu ve her oyuncu için avatarı spawn eder; oturum kapanınca Lobby'ye döner.</summary>
    public class GameSession : SimulationBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] GameObject avatarPrefab;
        [SerializeField] GameObject roomStatePrefab;

        readonly HashSet<PlayerRef> spawned = new HashSet<PlayerRef>();
        NetworkRunner session;

        void Start()
        {
            session = Session.Runner;
            if (session == null) { UnityEngine.SceneManagement.SceneManager.LoadScene(Session.LobbyScene); return; }
            session.AddGlobal(this);
            if (session.IsServer)
            {
                // Paylaşılan dünya durumu (lamba/kapı): host spawn eder, state authority host kalır.
                // Prefab henüz yüklü olmayabilir (senkron Spawn "Failed to load prefab synchronously" atar): SpawnAsync.
                session.SpawnAsync(roomStatePrefab.GetComponent<NetworkObject>());
                Dbg.Interact("(0) RoomState SpawnAsync istendi");
                foreach (var p in session.ActivePlayers) Spawn(p);
            }
        }

        void Update()
        {
            // Host çıktı / bağlantı koptu: runner durur.
            if (session != null && Session.Runner == session && !session.IsRunning && !session.IsStarting)
            {
                session = null;
                Session.Leave();
            }
        }

        void Spawn(PlayerRef player)
        {
            if (!spawned.Add(player)) return;
            int role = Roles.Index(session, player);
            // Rol A → Oda A, rol B → Oda B (250 m ötede). SpawnAsync: prefab yüklenene kadar bekler.
            session.SpawnAsync(avatarPrefab.GetComponent<NetworkObject>(), Layout.Spawn(role), Quaternion.identity, player);
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (session != null && session.IsServer) Spawn(player);
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (session == null || !session.IsServer) return;
            spawned.Remove(player);
            foreach (var a in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                if (a.Object != null && a.Object.IsValid && a.Object.InputAuthority == player)
                    session.Despawn(a.Object);
        }
    }
}
