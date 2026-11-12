using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// İlerleme kaydı (host cihazında, PlayerPrefs): son çözülen bulmaca. Menüde seçilen başlangıç bulmacası (StartPuzzle)
    /// host oyunu kurunca uygulanır: öncekiler çözülmüş sayılır. Holde rol alınca oyuncu o bulmacanın kontrol noktasına ışınlanır.
    /// </summary>
    public static class Progress
    {
        public const int LastPuzzle = 4; // yeni bulmaca eklenince artır
        const string Key = "coop_progress";

        /// <summary>Menüden: 1 = baştan.</summary>
        public static int StartPuzzle = 1;

        public static int Saved => Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, LastPuzzle);

        public static void Save(int solved)
        {
            PlayerPrefs.SetInt(Key, Mathf.Clamp(solved, 0, LastPuzzle));
            PlayerPrefs.Save();
        }

        /// <summary>Şu an oynanan bulmaca: en küçük aktif olan (yoksa 0).</summary>
        public static int Current(PuzzleManager m)
        {
            if (m == null) return 0;
            for (int id = 1; id < PuzzleManager.MaxPuzzles; id++)
                if (m.GetState(id) == PuzzleState.Active) return id;
            return 0;
        }
    }
}
