using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Kapalı alanların konumları (250 m arayla; biri diğerinden görünmez). Alan, x koordinatından türetilir.
    /// 0 = X tarafı (çan kulesi), 1 = O tarafı (zindan/mahzen), 2 = Lobi Holü (oyun burada başlar), 3 = Büyük Salon (final).
    /// </summary>
    public static class Layout
    {
        public const int AreaX = 0, AreaO = 1, AreaHall = 2, AreaFinal = 3;

        public static readonly Vector3 RoomX = Vector3.zero;
        public static readonly Vector3 RoomO = new Vector3(250f, 0f, 0f);
        public static readonly Vector3 Hall = new Vector3(-250f, 0f, 0f);
        public static readonly Vector3 Final = new Vector3(-500f, 0f, 0f); // büyük salon (iki rol buluşur)

        public static int RoomOf(Vector3 p) => p.x < -400f ? AreaFinal : p.x < -125f ? AreaHall : p.x < 125f ? AreaX : AreaO;
        public static string RoomName(int room) => room == AreaHall ? "Lobi Holü" : room == AreaFinal ? "Büyük Salon" : room == AreaX ? "X · Çan Kulesi" : "O · Mahzen";

        /// <summary>Rol X → kule, rol O → zindan.</summary>
        public static Vector3 Spawn(int role) => (role == Roles.X ? RoomX : RoomO) + new Vector3(0f, 0.05f, 0f);

        /// <summary>Holde giriş kapısının önünde yan yana iki doğma noktası.</summary>
        public static Vector3 HallSpawn(int slot) => Hall + new Vector3(slot == 0 ? -1.2f : 1.2f, 0.05f, -8.5f);
    }
}
