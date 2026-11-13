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
            Settings.ApplyFps(); // oyuncu ayarı (varsayılan 60; Ayarlar: 30/60; menüde 30)
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, __) => Settings.ApplyFps();
#if !UNITY_EDITOR
            // Çok yüksek çözünürlüklü ekranlar (yatayda 1200 px üstü kısa kenar): render 1080p'ye ölçeklenir.
            // Görüntü farkı küçük, GPU yükü/ısınma belirgin azalır. 1080p ve altı cihazlar etkilenmez.
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            int shortSide = Mathf.Min(Screen.width, Screen.height);
            if (urp != null && shortSide > 1200) urp.renderScale = Mathf.Clamp(1080f / shortSide, 0.7f, 1f);
#endif
            Debug.Log("[Perf] FrameRateConfig: vSync=0 targetFrameRate=" + Application.targetFrameRate + " ekranHz=" + Screen.currentResolution.refreshRateRatio.value.ToString("F1"));
        }
    }
}
