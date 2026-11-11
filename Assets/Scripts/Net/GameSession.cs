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
            // Herkes Lobi Holü'nde doğar; roller plakalarla seçilir (HallGate). Yola çıkıldıktan sonra katılan (yeniden bağlanan)
            // oyuncu boşta kalan rolü alır ve o rolün odasında doğar. SpawnAsync: prefab yüklenene kadar bekler.
            int taken = Roles.None;
            foreach (var a in PlayerAvatar.All)
                if (a != null && a.Object != null && a.Object.IsValid && a.RoleIndex != Roles.None) taken = a.RoleIndex;
            if (taken == Roles.None)
            {
                session.SpawnAsync(avatarPrefab.GetComponent<NetworkObject>(), Layout.HallSpawn(Roles.Slot(session, player)), Quaternion.identity, player);
                return;
            }
            int role = taken == Roles.X ? Roles.O : Roles.X;
            session.SpawnAsync(avatarPrefab.GetComponent<NetworkObject>(), Layout.Spawn(role), Quaternion.identity, player,
                               (runner, obj) => obj.GetComponent<PlayerAvatar>().InitRole(role));
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
