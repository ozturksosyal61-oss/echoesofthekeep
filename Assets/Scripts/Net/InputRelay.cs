using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Fusion'a yerel girdiyi verir (OnInput). Game sahnesinde runner'a callback olarak eklenir.</summary>
    public class InputRelay : MonoBehaviour, INetworkRunnerCallbacks
    {
        NetworkRunner runner;

        void Start()
        {
            runner = Session.Runner;
            if (runner != null) runner.AddCallbacks(this);
        }

        void OnDestroy()
        {
            if (runner != null) runner.RemoveCallbacks(this);
        }

        public void OnInput(NetworkRunner r, NetworkInput input)
        {
            var m = InputSource.Move;
            var world = Quaternion.Euler(0f, FollowCamera.Yaw, 0f) * new Vector3(m.x, 0f, m.y);
            input.Set(new PlayerInput { Move = new Vector2(world.x, world.z), Yaw = FollowCamera.Yaw,
                                         Pitch = (short)Mathf.RoundToInt(Mathf.Clamp(FollowCamera.LookPitch, -80f, 80f) * 100f) });
        }

        public void OnInputMissing(NetworkRunner r, PlayerRef player, NetworkInput input) { }
        public void OnPlayerJoined(NetworkRunner r, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner r, PlayerRef player) { }
        public void OnShutdown(NetworkRunner r, ShutdownReason reason) => Session.OnRunnerShutdown(r);
        public void OnConnectedToServer(NetworkRunner r) { }
        public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner r, NetAddress addr, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
        public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner r) { }
        public void OnSceneLoadStart(NetworkRunner r) { }
        public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    }
}
