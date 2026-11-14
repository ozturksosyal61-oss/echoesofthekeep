using System.Globalization;
using System.Text;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Oyun istatistiklerinin host cihazında kaydı (PlayerPrefs). Ağ değerleri PuzzleManager'da (Deaths, HintsUsed, PlaySeconds, PuzzleSeconds).
    /// "Kaldığın yer" ile devam edilirse (başlangıç = kayıtlı çözülen + 1) eski sayaçlar yüklenir; başka başlangıçta sıfırdan.
    /// Biçim: çözülen;ölüm;ipucu;süre;b1,b2,...
    /// </summary>
    public static class PlayStats
    {
        const string Key = "coop_stats";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Save(PuzzleManager m, int solved)
        {
            var sb = new StringBuilder();
            sb.Append(solved).Append(';').Append(m.Deaths).Append(';').Append(m.HintsUsed).Append(';')
              .Append(m.PlaySeconds.ToString("F0", Inv)).Append(';');
            for (int i = 0; i < PuzzleManager.MaxPuzzles; i++) { if (i > 0) sb.Append(','); sb.Append(m.PuzzleSeconds[i].ToString("F0", Inv)); }
            PlayerPrefs.SetString(Key, sb.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>Host, Spawned'da: kayıt bu başlangıçla sürüyorsa sayaçları geri yükler.</summary>
        public static void Load(PuzzleManager m, int start)
        {
            var p = PlayerPrefs.GetString(Key, "").Split(';');
            if (start <= 1 || p.Length < 5 || !int.TryParse(p[0], out int solved) || solved != start - 1) return;
            int.TryParse(p[1], out int d); int.TryParse(p[2], out int h);
            float.TryParse(p[3], NumberStyles.Float, Inv, out float s);
            m.Deaths = d; m.HintsUsed = h; m.PlaySeconds = s;
            var ps = p[4].Split(',');
            for (int i = 0; i < ps.Length && i < PuzzleManager.MaxPuzzles; i++)
                if (float.TryParse(ps[i], NumberStyles.Float, Inv, out float v)) m.PuzzleSeconds.Set(i, v);
        }

        public static string Clock(float sec)
        {
            int t = Mathf.RoundToInt(sec), h = t / 3600, mi = t / 60 % 60, s = t % 60;
            return h > 0 ? h + " sa " + mi + " dk" : mi + ":" + s.ToString("00");
        }

        /// <summary>Kapanış ekranı metni (iki satır: toplam, bulmaca süreleri).</summary>
        public static string Summary(PuzzleManager m)
        {
            if (m == null) return "";
            var sb = new StringBuilder();
            sb.Append("Süre ").Append(Clock(m.PlaySeconds)).Append("   ·   Ölüm ").Append(m.Deaths).Append("   ·   İpucu ").Append(m.HintsUsed).Append('\n');
            bool first = true;
            for (int i = 1; i <= Progress.LastPuzzle && i < Hints.Names.Length; i++)
            {
                float v = m.PuzzleSeconds[i];
                if (v < 1f) continue;
                if (!first) sb.Append("  ·  ");
                sb.Append(Hints.Names[i]).Append(' ').Append(Clock(v));
                first = false;
            }
            return sb.ToString();
        }
    }
}
