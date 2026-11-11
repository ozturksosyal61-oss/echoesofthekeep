using Fusion;

namespace CoopGame
{
    /// <summary>
    /// Rol: X (yukarı, mekanizma/çan kulesi) ya da O (aşağı, mahzen/sarnıç). Lobi Holü'ndeki plakalarla seçilir (HallGate);
    /// seçilene kadar None. Slot: giriş sırası (holdeki doğma noktası ve HUD satırı için).
    /// </summary>
    public static class Roles
    {
        public const int None = -1, X = 0, O = 1;

        public static int Slot(NetworkRunner runner, PlayerRef player)
        {
            int i = 0;
            foreach (var other in runner.ActivePlayers)
                if (other.PlayerId < player.PlayerId) i++;
            return i;
        }

        public static string Name(int role) => role == X ? "X" : role == O ? "O" : "?";
    }
}
