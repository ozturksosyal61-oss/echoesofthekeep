using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Yerel girdi, her tick host'a gider. Move: dünya uzayında (kamera yönüyle döndürülmüş) x/z. Yaw: bakış yönü (gövde buna döner).</summary>
    public struct PlayerInput : INetworkInput
    {
        public Vector2 Move;
        public float Yaw;
    }
}
