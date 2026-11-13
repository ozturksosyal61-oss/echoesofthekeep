using UnityEngine;

namespace CoopGame
{
    /// <summary>Oyuncu ayarları (PlayerPrefs): müzik ve konuşma sesi, bakış hassasiyeti, mikrofon, kare hızı. Menüde Ayarlar panelinden değişir.</summary>
    public static class Settings
    {
        static float F(string k, float d) => PlayerPrefs.GetFloat(k, d);
        static void F(string k, float v, bool save) { PlayerPrefs.SetFloat(k, v); if (save) PlayerPrefs.Save(); }

        public static float Music { get => F("Set.Music", 0.8f); set => F("Set.Music", Mathf.Clamp01(value), true); }
        /// <summary>Konuşma sesi 0..2 (1 = normal; 1 üstü yumuşak kırpmalı yükseltme).</summary>
        public static float Voice { get => F("Set.Voice", 1f); set => F("Set.Voice", Mathf.Clamp(value, 0f, 2f), true); }
        /// <summary>Bakış hassasiyeti çarpanı 0.4..2.</summary>
        static float look = -1f;
        public static float Look { get { if (look < 0f) look = F("Set.Look", 1f); return look; } set { look = Mathf.Clamp(value, 0.4f, 2f); F("Set.Look", look, true); } }
        public static bool MicOn { get => PlayerPrefs.GetInt("Set.Mic", 1) == 1; set { PlayerPrefs.SetInt("Set.Mic", value ? 1 : 0); PlayerPrefs.Save(); } }
        // 30 / 60 (120 kaldırıldı: telefonu ısıtıyordu; eski kayıtlı 120 → 60)
        public static int Fps { get { int f = PlayerPrefs.GetInt("Set.Fps", 60); return f <= 30 ? 30 : 60; } set { PlayerPrefs.SetInt("Set.Fps", value); PlayerPrefs.Save(); ApplyFps(); } }

        /// <summary>Menü (Lobby sahnesi) 30 FPS ile sınırlı: video arka plan ve UI için yeterli, cihaz boşuna ısınmaz.</summary>
        public static void ApplyFps()
        {
            QualitySettings.vSyncCount = 0;
            bool menu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Session.LobbyScene;
            Application.targetFrameRate = menu ? Mathf.Min(30, Fps) : Fps;
        }
    }
}
