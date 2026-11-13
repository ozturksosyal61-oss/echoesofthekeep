using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Ayarlar paneli (ana menü): müzik ve konuşma sesi, bakış hassasiyeti, mikrofon açık/kapalı, kare hızı. Değerler Settings'e yazılır.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Slider music, voice, look;
        [SerializeField] Text musicVal, voiceVal, lookVal;
        [SerializeField] Button micButton, fpsButton;

        static readonly int[] FpsOptions = { 30, 60 };

        void Awake()
        {
            music.minValue = 0f; music.maxValue = 1f; music.value = Settings.Music;
            voice.minValue = 0f; voice.maxValue = 2f; voice.value = Settings.Voice;
            look.minValue = 0.4f; look.maxValue = 2f; look.value = Settings.Look;
            music.onValueChanged.AddListener(v => { Settings.Music = v; Refresh(); });
            voice.onValueChanged.AddListener(v => { Settings.Voice = v; Refresh(); });
            look.onValueChanged.AddListener(v => { Settings.Look = v; Refresh(); });
            micButton.onClick.AddListener(() => { Settings.MicOn = !Settings.MicOn; Refresh(); });
            fpsButton.onClick.AddListener(() =>
            {
                int i = System.Array.IndexOf(FpsOptions, Settings.Fps);
                Settings.Fps = FpsOptions[(i + 1) % FpsOptions.Length];
                Refresh();
            });
            Refresh();
        }

        void Refresh()
        {
            musicVal.text = Mathf.RoundToInt(Settings.Music * 100f) + "%";
            voiceVal.text = Mathf.RoundToInt(Settings.Voice * 100f) + "%";
            lookVal.text = Settings.Look.ToString("0.0") + "x";
            micButton.GetComponentInChildren<Text>().text = Settings.MicOn ? "AÇIK" : "KAPALI";
            fpsButton.GetComponentInChildren<Text>().text = Settings.Fps + " FPS";
        }
    }
}
