using UnityEngine;

namespace CoopGame
{
    /// <summary>Test araçları (FPS/ping, gizli debug paneli, menüde başlangıç seçici) yalnız editörde ve development build'de.
    /// App Store sürümü (BuildOptions.None) bunları göstermez; TestFlight'a test sürümü için DEV=1 ile development build yükle.</summary>
    public static class TestTools
    {
        public static bool On => Application.isEditor || Debug.isDebugBuild;
    }
}
