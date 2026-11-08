using UnityEngine;

namespace CoopGame
{
    /// <summary>Kare hızı ayarı: vSync kapalı, hedef kare hızı sabit. (Ayar menüsü sonra.)</summary>
    public static class FrameRateConfig
    {
        // Mobilde -1 = platform varsayılanı (30) demektir, sınırsız değil. Varsayılan 60; ölçüm için yüksek değer (ör. 240) kullan.
        public const int Target = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Target;
            Debug.Log("[Perf] FrameRateConfig: vSync=0 targetFrameRate=" + Target + " ekranHz=" + Screen.currentResolution.refreshRateRatio.value.ToString("F1"));
        }
    }
}
