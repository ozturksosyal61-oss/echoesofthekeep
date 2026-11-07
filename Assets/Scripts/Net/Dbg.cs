namespace CoopGame
{
    /// <summary>Etkileşim zinciri logları için ortak etiket: kim (HOST/CLIENT) ve hangi oyuncu.</summary>
    public static class Dbg
    {
        public static string Who
        {
            get
            {
                var r = Session.Runner;
                if (r == null) return "[?]";
                return (r.IsServer ? "[HOST" : "[CLIENT") + " p" + r.LocalPlayer.PlayerId + "]";
            }
        }

        public static void Interact(string msg) => UnityEngine.Debug.Log("[Interact] " + Who + " " + msg);
    }
}
