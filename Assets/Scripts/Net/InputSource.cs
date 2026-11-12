using UnityEngine;

namespace CoopGame
{
    /// <summary>Girdi toplayıcı: sanal joystick + WASD (hareket), sürükleme (bakış). Editor'de klavye/fare, telefonda dokunma.</summary>
    public static class InputSource
    {
        public static Vector2 VirtualMove;
        static Vector2 look;

        public static void AddLook(Vector2 delta) => look += delta * Settings.Look;

        public static Vector2 TakeLook()
        {
            var l = look;
            look = default;
            return l;
        }

        public static Vector2 Move
        {
            get
            {
                var v = VirtualMove;
                v.x += Input.GetAxisRaw("Horizontal");
                v.y += Input.GetAxisRaw("Vertical");
                return Vector2.ClampMagnitude(v, 1f);
            }
        }
    }
}
