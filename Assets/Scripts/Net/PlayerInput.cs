using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Yerel girdi, her tick host'a gider. Move: dünya uzayında (kamera yönüyle döndürülmüş) x/z.</summary>
    public struct PlayerInput : INetworkInput
    {
        public Vector2 Move;
    }
}
