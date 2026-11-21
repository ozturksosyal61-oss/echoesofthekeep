using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace CoopGame
{
    /// <summary>Menü videosu: VideoClip içe aktarılamadıysa (Linux CI build'i H.264 mp4'ü içe aktaramaz)
    /// StreamingAssets/menu_loop.mp4'ü URL'den oynatır.</summary>
    static class MenuVideoFallback
    {
        const string FileName = "menu_loop.mp4";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Fix();
            SceneManager.sceneLoaded += (_, __) => Fix();
        }

        static void Fix()
        {
            foreach (var vp in Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None))
            {
                if (vp.source == VideoSource.VideoClip && vp.clip != null) continue;
                if (vp.source == VideoSource.Url && !string.IsNullOrEmpty(vp.url)) continue;
                vp.source = VideoSource.Url;
                vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, FileName);
                vp.isLooping = true;
                vp.Play();
            }
        }
    }
}
