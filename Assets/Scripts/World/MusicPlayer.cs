using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopGame
{
    /// <summary>
    /// Arka plan müziği (CC0, Assets/ThirdParty/Music/Resources). Menü ve Büyük Salon: Cathedral; lobi + X: AbandonedCastle; O (yer altı): CastleCellar.
    /// Bölge değişince 2.5 sn çapraz geçiş. Ses düşük tutulur (sesli sohbet önde). Sahneler arası yaşar.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public const string PrefKey = "MusicVolume";
        const float Fade = 2.5f, BaseVolume = 0.32f;
        static MusicPlayer inst;

        AudioSource a, b;   // a: çalan, b: gelen
        string current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("MusicPlayer");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<MusicPlayer>();
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(PrefKey, 1f);
            set { PlayerPrefs.SetFloat(PrefKey, Mathf.Clamp01(value)); }
        }

        void Awake()
        {
            a = MakeSource(); b = MakeSource();
        }

        AudioSource MakeSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f; s.priority = 0;
            s.ignoreListenerPause = true;
            return s;
        }

        string Wanted()
        {
            if (SceneManager.GetActiveScene().name == Session.LobbyScene) return "Music_Cathedral";
            var me = PlayerAvatar.Local;
            if (me == null) return current ?? "Music_AbandonedCastle";
            return me.Room == Layout.AreaFinal ? "Music_Cathedral" : me.Room == Layout.AreaO ? "Music_CastleCellar" : "Music_AbandonedCastle";
        }

        void Update()
        {
            string w = Wanted();
            if (w != current)
            {
                var clip = Resources.Load<AudioClip>(w);
                current = w;
                if (clip != null)
                {
                    (a, b) = (b, a); // eski çalan b'ye geçer, söner
                    a.clip = clip; a.volume = 0f; a.Play();
                }
            }
            float target = BaseVolume * Volume, step = Time.unscaledDeltaTime / Fade * BaseVolume;
            a.volume = Mathf.MoveTowards(a.volume, a.clip != null ? target : 0f, step);
            b.volume = Mathf.MoveTowards(b.volume, 0f, step);
            if (b.isPlaying && b.volume <= 0f) { b.Stop(); b.clip = null; }
        }
    }
}
