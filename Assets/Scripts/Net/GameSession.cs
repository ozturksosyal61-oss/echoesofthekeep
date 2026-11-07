using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Game sahnesi: host her oyuncu için avatar spawn eder; oturum kapanınca Lobby'ye döner.</summary>
    public class GameSession : SimulationBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] GameObject avatarPrefab;
        [SerializeField] GameObject roomStatePrefab;

        readonly Dictionary<PlayerRef, NetworkObject> avatars = new Dictionary<PlayerRef, NetworkObject>();
        NetworkRunner session;

        void Start()
        {
            session = Session.Runner;
            if (session == null) { UnityEngine.SceneManagement.SceneManager.LoadScene(Session.LobbyScene); return; }
            session.AddGlobal(this);
            if (session.IsServer)
            {
                // Paylaşılan dünya durumu (lamba/kapı): host spawn eder, state authority host kalır.
                session.Spawn(roomStatePrefab.GetComponent<NetworkObject>());
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
            if (avatars.ContainsKey(player)) return;
            int role = Roles.Index(session, player);
            // Rol A → Oda A, rol B → Oda B (250 m ötede).
            avatars[player] = session.Spawn(avatarPrefab.GetComponent<NetworkObject>(), Layout.Spawn(role), Quaternion.identity, player);
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (session != null && session.IsServer) Spawn(player);
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (session != null && session.IsServer && avatars.TryGetValue(player, out var obj))
            {
                session.Despawn(obj);
                avatars.Remove(player);
            }
        }
    }
}
