using Fusion;

namespace CoopGame
{
    /// <summary>Giriş sırasına göre rol: aktif oyuncular arasında en düşük PlayerId = A, diğeri = B.</summary>
    public static class Roles
    {
        public static int Index(NetworkRunner runner, PlayerRef player)
        {
            int i = 0;
            foreach (var other in runner.ActivePlayers)
                if (other.PlayerId < player.PlayerId) i++;
            return i;
        }

        public static string Name(int index) => index == 0 ? "A" : "B";
    }
}
