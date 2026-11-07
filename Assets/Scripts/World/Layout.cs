using UnityEngine;

namespace CoopGame
{
    /// <summary>İki kapalı odanın konumları (250 m arayla). Oda, x koordinatından türetilir (odalar duvarla kapalı).</summary>
    public static class Layout
    {
        public static readonly Vector3 RoomA = Vector3.zero;
        public static readonly Vector3 RoomB = new Vector3(250f, 0f, 0f);

        public static int RoomOf(Vector3 p) => p.x < 125f ? 0 : 1;
        public static string RoomName(int room) => room == 0 ? "Oda A (Mekanizma)" : "Oda B (Sarnıç)";

        /// <summary>Rol A → Oda A, rol B → Oda B.</summary>
        public static Vector3 Spawn(int roleIndex) => (roleIndex == 0 ? RoomA : RoomB) + new Vector3(0f, 0.05f, 0f);
    }
}
