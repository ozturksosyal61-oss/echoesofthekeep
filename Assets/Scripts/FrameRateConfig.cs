using UnityEngine;

namespace CoopGame
{
    /// <summary>Kare hızı ayarı: vSync kapalı, hedef kare hızı sabit. (Ayar menüsü sonra.)</summary>
    public static class FrameRateConfig
    {
        // Mobilde -1 = platform varsayılanı (30) demektir, sınırsız değil. Varsayılan 60; ölçüm için yüksek değer (ör. 240) kullan.
        public const int Target = 240;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            Settings.ApplyFps(); // oyuncu ayarı (varsayılan 60; Ayarlar: 30/60/120)
            Debug.Log("[Perf] FrameRateConfig: vSync=0 targetFrameRate=" + Application.targetFrameRate + " ekranHz=" + Screen.currentResolution.refreshRateRatio.value.ToString("F1"));
        }
    }
}
